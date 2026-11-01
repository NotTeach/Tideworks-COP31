using System;
using System.Collections;
using Tideworks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gunluk tamir tezgahlarinin ortak kismi (Pamuk, pusula...): adim listesi ve ilerleme cubugu,
/// asagidaki 8 yuvadan (4 alet + 4 raf malzemesi) secim, yardim ("?" karti: Goster / Benim yerime yap)
/// ve takilan oyuncu icin parlayan ipuclari (birkac yanlis ya da uzun beklemeden sonra, Ayarlar > Hints acikken).
/// Alt siniflar adimlari tanimlar: hangi yuva gerekli, nereye tiklanir, ipucu metinleri ve "benim yerime yap" hareketi.
/// </summary>
public abstract class RepairBench : MonoBehaviour
{
    [SerializeField] protected WorkshopDayController host;

    [Header("Ust bar ve adimlar")]
    [SerializeField] Text progressText;
    [SerializeField] RectTransform progressFill;
    [SerializeField] float progressWidth = 278f;
    [SerializeField] Image[] stepRows;
    [SerializeField] Image[] stepCircles;
    [SerializeField] Text[] stepNumbers;
    [SerializeField] Text[] stepTitles;
    [SerializeField] Sprite rowDone, rowCurrent, circleDone, circleCurrent, circlePending;
    [SerializeField] Color titleDone = new Color32(52, 108, 60, 255);
    [SerializeField] Color titleNormal = new Color32(45, 40, 35, 255);
    [SerializeField] Text message;

    [Header("Aletler ve raf (8 yuva)")]
    [SerializeField] protected Button[] slots;
    [SerializeField] RectTransform slotSelected;

    [Header("Yardim")]
    [SerializeField] RectTransform helpButton;
    [SerializeField] GameObject helpOverlay;
    [SerializeField] Text helpStep;
    [SerializeField] Text helpPick;
    [SerializeField] Text helpClick;
    [SerializeField] Image helpIcon;
    [SerializeField] RectTransform slotHint;
    [SerializeField] RectTransform targetHint;
    [SerializeField] float idleHintDelay = 20f;
    [SerializeField] int wrongBeforeHint = 2;
    [SerializeField] int wrongBeforeCard = 4;

    protected const int None = -1;
    protected int selected = None;
    protected int step, phase, count;
    protected bool busy;
    bool started, auto, hinting, cardShown;
    int wrongCount;
    float idle;

    protected int StepCount => stepRows.Length;
    protected bool Finished => step >= StepCount;

    /// <summary>Oyuncu tiklamalari: animasyon, otomatik yardim ya da yardim karti acikken yok sayilir.</summary>
    protected bool Locked => busy || auto || helpOverlay.activeSelf;

    // ---------------- Alt siniflarin tanimladiklari ----------------

    /// <summary>Su anki adim / asamada gereken yuva (yoksa None).</summary>
    protected abstract int Required();

    /// <summary>Su anki adimda tiklanacak yer (ipucu halkasi icin).</summary>
    protected abstract RectTransform TargetRect();

    /// <summary>Yardim kartinin 2. satiri, Ingilizce anahtar ("Then click ...").</summary>
    protected abstract string StepTools();

    /// <summary>Bu adimin kisa ipucu, Ingilizce anahtar.</summary>
    protected abstract string Hint();

    /// <summary>Adim basligi, Ingilizce anahtar (yardim karti).</summary>
    protected abstract string StepTitle(int i);

    /// <summary>Yuvanin adi ("the {0}" kaliplarina girer). Turkcede belirtme hali ayri verilir.</summary>
    protected abstract string ItemName(int item, bool accusative);

    /// <summary>Tamir edilen esyayi bastaki bozuk haline getirir ve ilk mesaji yazar.</summary>
    protected abstract void ResetSubject();

    /// <summary>"Do it for me": bu adimin bir hamlesini (gereken yuva zaten secili) yapar.</summary>
    protected abstract IEnumerator AutoMove();

    /// <summary>"Do it for me" baslarken yardimcinin sozu.</summary>
    protected virtual string HelperLine => "Let me show you, dear...";

    // ---------------- Akis ----------------

    /// <summary>Tezgah ilk acildiginda; sonraki acilislarda kalinan yerden devam eder.</summary>
    public void Begin()
    {
        if (started) { Say(L(Hint())); return; }
        started = true;
        ResetBench();
    }

    public void Restart()
    {
        if (Locked || Finished) return;
        ResetBench();
    }

    void ResetBench()
    {
        StopAllCoroutines();
        busy = auto = false;
        step = phase = count = 0;
        wrongCount = 0;
        idle = 0f;
        cardShown = false;
        helpOverlay.SetActive(false);
        ShowHints(false);
        Deselect();
        ResetSubject();
        RefreshSteps();
    }

    public void Back() { if (!Locked) host.ReturnToInterior(); }

    /// <summary>Son adim yapildi: butun adimlar tiklenir (kutlama animasyonundan once).</summary>
    protected void MarkAllDone()
    {
        step = StepCount;
        RefreshSteps();
    }

    /// <summary>Kutlamadan sonra: atolyeye donulur, musteri tesekkur eder.</summary>
    protected void Complete()
    {
        busy = false;
        Progress();
        host.RepairFinished();
    }

    // ---------------- Secim ----------------

    public void Select(int item)
    {
        if (Locked) return;
        SelectItem(item);
    }

    protected void SelectItem(int item)
    {
        if (item == selected) { Deselect(); return; }
        selected = item;
        var slot = (RectTransform)slots[item].transform;
        slotSelected.gameObject.SetActive(true);
        slotSelected.anchoredPosition = slot.anchoredPosition;
        slotSelected.sizeDelta = slot.sizeDelta + new Vector2(12, 12);
        if (hinting) PlaceHints();
    }

    protected void Deselect()
    {
        selected = None;
        slotSelected.gameObject.SetActive(false);
        if (hinting) PlaceHints();
    }

    // ---------------- Yardim ----------------

    void Update()
    {
        if (!started || Finished) return;
        bool waiting = !busy && !auto && !helpOverlay.activeSelf;
        if (waiting) idle += Time.unscaledDeltaTime;
        if (waiting && !hinting && GameSettings.Hints && (idle > idleHintDelay || wrongCount >= wrongBeforeHint))
            ShowHints(true);
        float sc = hinting && !GameSettings.ReduceMotion ? 1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) : 1f;
        helpButton.localScale = new Vector3(sc, sc, 1f);
    }

    /// <summary>Basarili bir hamleden sonra: bekleme sayaci sifirlanir, ipucu yeni hedefe gecer.</summary>
    protected void Progress()
    {
        idle = 0f;
        if (hinting) PlaceHints();
    }

    void CountWrong()
    {
        wrongCount++;
        if (wrongCount >= wrongBeforeCard && !cardShown) OpenHelp();
    }

    void ShowHints(bool on)
    {
        hinting = on && !Finished;
        if (hinting) PlaceHints();
        else
        {
            slotHint.gameObject.SetActive(false);
            targetHint.gameObject.SetActive(false);
        }
    }

    void PlaceHints()
    {
        int need = Required();
        bool slotOn = need != None && selected != need;
        slotHint.gameObject.SetActive(slotOn);
        if (slotOn)
        {
            var slot = (RectTransform)slots[need].transform;
            slotHint.anchoredPosition = slot.anchoredPosition;
            slotHint.sizeDelta = slot.sizeDelta * 1.15f;
        }
        var target = TargetRect();
        bool targetOn = target && (need == None || selected == need);
        targetHint.gameObject.SetActive(targetOn);
        if (targetOn)
        {
            // Hedef esyanin icinde ya da tezgahta olabilir: halkanin ebeveyninin koordinatina cevir
            var parent = (RectTransform)targetHint.parent;
            Vector2 p = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
            targetHint.anchoredPosition = p - new Vector2(parent.rect.xMin, parent.rect.yMax);
            targetHint.sizeDelta = Vector2.one * Mathf.Max(120f, Mathf.Max(target.rect.width, target.rect.height) * 1.3f);
        }
    }

    /// <summary>"?" butonu ya da cok yanlis denemeden sonra: bu adimin yardim karti.</summary>
    public void OpenHelp()
    {
        if (busy || auto || Finished) return;
        cardShown = true;
        int need = Required();
        helpStep.text = Loc.F("Step {0} of {1}: {2}", step + 1, StepCount, Loc.T(StepTitle(step)));
        helpIcon.gameObject.SetActive(need != None);
        if (need != None)
        {
            helpIcon.sprite = ((Image)slots[need].targetGraphic).sprite;
            helpPick.text = Loc.F("1. Pick the {0}.", ItemName(need, true));
        }
        helpClick.text = "2. " + L(StepTools());
        helpOverlay.SetActive(true);
    }

    public void CloseHelp() => helpOverlay.SetActive(false);

    public void ShowMe()
    {
        helpOverlay.SetActive(false);
        ShowHints(true);
        Say(L("Look for the glowing spots!") + " " + L(Hint()));
    }

    public void DoItForMe()
    {
        helpOverlay.SetActive(false);
        if (busy || auto || Finished) return;
        StartCoroutine(AutoStep());
    }

    /// <summary>Yardimci bu adimi oyuncunun gozu onunde yapar.</summary>
    IEnumerator AutoStep()
    {
        auto = true;
        ShowHints(false);
        int s0 = step;
        Say(L(HelperLine));
        yield return new WaitForSecondsRealtime(0.6f);
        while (step == s0 && !Finished)
        {
            int need = Required();
            if (need != None && selected != need)
            {
                SelectItem(need);
                yield return new WaitForSecondsRealtime(0.45f);
            }
            yield return AutoMove();
            yield return new WaitForSecondsRealtime(0.25f);
        }
        auto = false;
    }

    // ---------------- Yardimcilar ----------------

    /// <summary>Dogru yuva secili mi? Degilse uyarir ve yanlis sayilir.</summary>
    protected bool Need(int item)
    {
        if (selected == item) return true;
        CountWrong();
        TwSes.Efekt(Ses.Hata, 0.6f);
        if (selected == None) Say(Loc.F("Pick the {0} first. ", ItemName(item, true)) + L(Hint()));
        else Say(Capitalize(Loc.F("The {0} won't help here. ", ItemName(selected, false))) + L(Hint()));
        return false;
    }

    /// <summary>Bu adimda olmayan bir yere tiklandi.</summary>
    protected void Wrong()
    {
        CountWrong();
        TwSes.Efekt(Ses.Hata, 0.6f);
        Say(L(Hint()));
    }

    protected void NextStep(string text)
    {
        wrongCount = 0;
        idle = 0f;
        cardShown = false;
        ShowHints(false);
        TwSes.Efekt(Ses.Onay);
        step++;
        phase = count = 0;
        Deselect();
        RefreshSteps();
        Say(L(text));
    }

    void RefreshSteps()
    {
        int done = Mathf.Min(step, StepCount);
        for (int i = 0; i < StepCount; i++)
        {
            bool isDone = i < step, isCurrent = i == step;
            stepRows[i].gameObject.SetActive(isDone || isCurrent);
            stepRows[i].sprite = isDone ? rowDone : rowCurrent;
            stepCircles[i].sprite = isDone ? circleDone : isCurrent ? circleCurrent : circlePending;
            stepNumbers[i].gameObject.SetActive(!isDone);
            stepTitles[i].color = isDone ? titleDone : titleNormal;
        }
        progressText.text = $"{done} / {StepCount}";
        progressFill.sizeDelta = new Vector2(progressWidth * done / StepCount, progressFill.sizeDelta.y);
    }

    protected void Say(string text) => message.text = text;

    protected static string L(string en) => Loc.T(en);

    static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    protected static void Show(Graphic g, bool on)
    {
        g.gameObject.SetActive(on);
        SetAlpha(g, on ? 1f : 0f);
        g.rectTransform.localScale = Vector3.one;
    }

    protected static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    protected static float EaseOutBack(float t)
    {
        const float c = 1.7f;
        float u = t - 1f;
        return 1f + (c + 1f) * u * u * u + c * u * u;
    }

    protected static IEnumerator Tween(float duration, Action<float> step)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            step(t / duration);
            yield return null;
        }
        step(1f);
    }
}
