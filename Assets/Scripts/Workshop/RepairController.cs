using System;
using System.Collections;
using Tideworks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tamir tezgahi: Hatice Teyze'nin radyosu 6 adimda tamir edilir.
/// Asagidan bir alet ya da raftan bir malzeme secilir, sonra radyoda dogru yere tiklanir.
/// Yanlis secimde mesaj cubugu ipucu verir. Takilan oyuncuya yardim: "?" karti (Goster / Benim yerime yap),
/// birkac yanlis ya da uzun bekleme sonrasi dogru alet ve dogru yer parlar (Ayarlar > Hints acikken).
/// </summary>
public class RepairController : MonoBehaviour
{
    public enum Item { None = -1, Screwdriver, Cloth, Pliers, Solder, Glass, Copper, Wood, Rope, Filament }

    [SerializeField] WorkshopOpeningController host;

    [Header("Ust bar")]
    [SerializeField] Text progressText;
    [SerializeField] RectTransform progressFill;
    [SerializeField] float progressWidth = 278f;

    [Header("Tamir adimlari (6 satir)")]
    [SerializeField] Image[] stepRows;
    [SerializeField] Image[] stepCircles;
    [SerializeField] Text[] stepNumbers;
    [SerializeField] Text[] stepTitles;
    [SerializeField] Sprite rowDone, rowCurrent, circleDone, circleCurrent, circlePending;
    [SerializeField] Color titleDone = new Color32(52, 108, 60, 255);
    [SerializeField] Color titleNormal = new Color32(45, 40, 35, 255);

    [Header("Mesaj")]
    [SerializeField] Text message;

    [Header("Aletler ve raf (Item sirasiyla 9 yuva)")]
    [SerializeField] Button[] slots;
    [SerializeField] RectTransform slotSelected;
    [Tooltip("Raftaki 5 malzemenin sayaci (Glass, Copper, Wood, Rope, Filament)")]
    [SerializeField] Text[] shelfCounts;

    [Header("Radyo")]
    [SerializeField] Image lid;
    [SerializeField] Sprite lidNew;
    [SerializeField] Image[] screws;
    [SerializeField] Image[] dust;
    [SerializeField] Image glassCracked;
    [SerializeField] Image glassClear;
    [SerializeField] Image looseWire;
    [SerializeField] Image copperWire;
    [SerializeField] Image solder;
    [SerializeField] RectTransform needle;
    [SerializeField] RectTransform tuningKnob;
    [SerializeField] float needleStart = -50f, needleStep = 20f, station = 50f;

    [Header("Yardim")]
    [SerializeField] RectTransform helpButton;
    [SerializeField] GameObject helpOverlay;
    [SerializeField] Text helpStep;
    [SerializeField] Text helpPick;
    [SerializeField] Text helpClick;
    [SerializeField] Image helpIcon;
    [SerializeField] RectTransform slotHint;
    [SerializeField] RectTransform targetHint;
    [Tooltip("Bu kadar saniye ilerleme olmazsa ipucu parlar")]
    [SerializeField] float idleHintDelay = 20f;
    [SerializeField] int wrongBeforeHint = 2;
    [SerializeField] int wrongBeforeCard = 4;

    static readonly string[] ItemNames =
        { "screwdriver", "cloth", "pliers", "soldering iron", "polished glass", "copper wire", "sanded wood", "rope", "filament" };
    static readonly string[] ItemNamesTr =
        { "tornavida", "bez", "pense", "havya", "cilalı cam", "bakır tel", "zımparalı tahta", "halat", "filament" };
    static readonly string[] ItemNamesTrAcc = // "Önce ... seç"
        { "tornavidayı", "bezi", "penseyi", "havyayı", "cilalı camı", "bakır teli", "zımparalı tahtayı", "halatı", "filamenti" };

    Item selected = Item.None;
    int step, phase, count;
    readonly int[] stock = { 1, 1, 1, 1, 1 };
    float needleAngle;
    bool busy, started, auto, hinting, cardShown;
    int wrongCount;
    float idle;

    /// <summary>Oyuncu tiklamalari: animasyon, otomatik yardim ya da yardim karti acikken yok sayilir.</summary>
    bool Locked => busy || auto || helpOverlay.activeSelf;
    Vector2 lidHome;

    void Awake()
    {
        lidHome = lid.rectTransform.anchoredPosition;
    }

    /// <summary>Tezgah ilk acildiginda cagrilir; sonraki acilislarda kalinan yerden devam eder.</summary>
    public void Begin()
    {
        if (started) { ShowHint(); return; }
        started = true;
        step = phase = count = 0;
        selected = Item.None;
        slotSelected.gameObject.SetActive(false);
        foreach (var s in screws) Show(s, true);
        foreach (var d in dust) Show(d, true);
        Show(glassCracked, true);
        Show(glassClear, false);
        Show(looseWire, true);
        Show(copperWire, false);
        Show(solder, false);
        Show(lid, true);
        needleAngle = needleStart;
        needle.localRotation = Quaternion.Euler(0, 0, -needleAngle);
        for (int i = 0; i < shelfCounts.Length; i++) UpdateStock(i);
        helpOverlay.SetActive(false);
        ShowHints(false);
        RefreshSteps();
        ShowHint();
    }

    public void Back()
    {
        if (Locked) return;
        host.ReturnToWorkshop();
    }

    // ---------------- Secim ----------------

    public void Select(int item)
    {
        if (Locked) return;
        SelectItem((Item)item);
    }

    void SelectItem(Item it)
    {
        int item = (int)it;
        if (it == selected)
        {
            selected = Item.None;
            slotSelected.gameObject.SetActive(false);
            if (hinting) PlaceHints();
            return;
        }
        if (IsMaterial(it) && stock[item - (int)Item.Glass] == 0)
        {
            Say(Loc.F("There is no more {0} on the shelf.", ItemName(it, false)));
            return;
        }
        selected = it;
        var slot = (RectTransform)slots[item].transform;
        slotSelected.gameObject.SetActive(true);
        slotSelected.anchoredPosition = slot.anchoredPosition;
        if (hinting) PlaceHints();
    }

    static bool IsMaterial(Item it) => it >= Item.Glass;

    // ---------------- Radyodaki noktalar ----------------

    public void ClickScrew(int i)
    {
        if (Locked) return;
        if (step == 0)
        {
            if (!Need(Item.Screwdriver) || !screws[i].gameObject.activeSelf) return;
            StartCoroutine(RemoveScrew(i));
        }
        else if (step == 4 && phase == 1)
        {
            if (!Need(Item.Screwdriver) || screws[i].gameObject.activeSelf) return;
            StartCoroutine(AddScrew(i));
        }
        else Wrong();
    }

    public void ClickDust(int i)
    {
        if (Locked) return;
        if (step != 1) { Wrong(); return; }
        if (!Need(Item.Cloth)) return;
        StartCoroutine(Wipe(i));
    }

    public void ClickCase()
    {
        if (Locked) return;
        if (step == 4 && phase == 0)
        {
            if (!Need(Item.Wood)) return;
            StartCoroutine(PutLid());
        }
        else if (step == 0 || (step == 4 && phase == 1)) Say(L("Use the screwdriver on the screws in the corners."));
        else Wrong();
    }

    public void ClickDial()
    {
        if (Locked) return;
        if (step != 2) { Wrong(); return; }
        if (phase == 0)
        {
            if (!Need(Item.Pliers)) return;
            StartCoroutine(PullGlass());
        }
        else
        {
            if (!Need(Item.Glass)) return;
            StartCoroutine(InsertGlass());
        }
    }

    public void ClickWire()
    {
        if (Locked) return;
        if (step != 3) { Wrong(); return; }
        if (phase == 0)
        {
            if (!Need(Item.Copper)) return;
            StartCoroutine(AddCopper());
        }
        else
        {
            if (!Need(Item.Solder)) return;
            StartCoroutine(SolderJoint());
        }
    }

    public void ClickKnob()
    {
        if (Locked) return;
        if (step != 5)
        {
            Say(L("Not yet! Finish the other steps before tuning in."));
            TwSes.Efekt(Ses.Hata, 0.6f);
            return;
        }
        StartCoroutine(Tune());
    }

    // ---------------- Adimlar ----------------

    IEnumerator RemoveScrew(int i)
    {
        busy = true;
        TwSes.EfektCesitli(Ses.Vida);
        var rt = screws[i].rectTransform;
        var home = rt.anchoredPosition;
        yield return Tween(0.4f, t =>
        {
            rt.localRotation = Quaternion.Euler(0, 0, -540f * t);
            rt.anchoredPosition = home + new Vector2(0, 30f * t);
            SetAlpha(screws[i], 1f - t * t);
        });
        rt.anchoredPosition = home;
        rt.localRotation = Quaternion.identity;
        Show(screws[i], false);
        busy = false;
        Progress();
        if (++count < screws.Length) yield break;

        // Kapak kalkar ve kenara cekilir
        busy = true;
        TwSes.Efekt(Ses.ParcaTak);
        yield return Tween(0.5f, t =>
        {
            lid.rectTransform.anchoredPosition = lidHome + new Vector2(0, 40f * Mathf.Sin(t * Mathf.PI)) + new Vector2(-260f, -120f) * (t * t);
            SetAlpha(lid, 1f - t);
        });
        Show(lid, false);
        busy = false;
        Progress();
        NextStep(L("The case is open... and so dusty! Step 2: wipe the dust away with the cloth."));
    }

    IEnumerator Wipe(int i)
    {
        if (!dust[i].gameObject.activeSelf) yield break;
        busy = true;
        TwSes.EfektCesitli(Ses.Zimpara, 0.7f);
        var rt = dust[i].rectTransform;
        float a = dust[i].color.a;
        yield return Tween(0.35f, t =>
        {
            SetAlpha(dust[i], a * (1f - t));
            rt.localScale = Vector3.one * (1f + 0.25f * t);
        });
        rt.localScale = Vector3.one;
        Show(dust[i], false);
        busy = false;
        Progress();
        if (++count < dust.Length) yield break;
        NextStep(L("Clean as new. Now the dial: its glass is cracked. Step 3: pull it out with the pliers."));
    }

    IEnumerator PullGlass()
    {
        busy = true;
        TwSes.Efekt(Ses.ParcaTak);
        var rt = glassCracked.rectTransform;
        var home = rt.anchoredPosition;
        yield return Tween(0.5f, t =>
        {
            rt.localScale = Vector3.one * (1f + 0.15f * t);
            rt.anchoredPosition = home + new Vector2(80f * t, -140f * t * t);
            rt.localRotation = Quaternion.Euler(0, 0, -25f * t);
            SetAlpha(glassCracked, 1f - t);
        });
        Show(glassCracked, false);
        rt.anchoredPosition = home;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        phase = 1;
        busy = false;
        Progress();
        Say(L("Good! Now put in the polished glass from the shelf. It's clear, so the dial is easy to read."));
    }

    IEnumerator InsertGlass()
    {
        busy = true;
        UseStock(Item.Glass);
        TwSes.Efekt(Ses.ParcaTak);
        Show(glassClear, true);
        yield return Tween(0.35f, t =>
        {
            glassClear.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, EaseOut(t));
            SetAlpha(glassClear, t);
        });
        busy = false;
        Progress();
        NextStep(L("The dial is clear again. Step 4: the speaker wire came loose. Connect it with copper wire: it conducts."));
    }

    IEnumerator AddCopper()
    {
        busy = true;
        UseStock(Item.Copper);
        TwSes.Efekt(Ses.ParcaTak);
        Show(copperWire, true);
        SetAlpha(copperWire, 0f);
        yield return Tween(0.35f, t =>
        {
            SetAlpha(looseWire, 1f - t);
            SetAlpha(copperWire, t);
        });
        Show(looseWire, false);
        phase = 1;
        busy = false;
        Progress();
        Say(L("Now melt the joint with the soldering iron so the wire holds."));
    }

    IEnumerator SolderJoint()
    {
        busy = true;
        TwSes.Efekt(Ses.MakineEritici, 0.7f);
        Show(solder, true);
        yield return Tween(0.4f, t => solder.rectTransform.localScale = Vector3.one * EaseOutBack(t));
        busy = false;
        Progress();
        NextStep(L("Almost there! Step 5: make a new lid. Put the sanded wood on the case."));
    }

    IEnumerator PutLid()
    {
        busy = true;
        UseStock(Item.Wood);
        lid.sprite = lidNew;
        Show(lid, true);
        var from = lidHome + new Vector2(-260f, -120f);
        TwSes.Efekt(Ses.Cekic, 0.8f);
        yield return Tween(0.45f, t =>
        {
            lid.rectTransform.anchoredPosition = Vector2.Lerp(from, lidHome, EaseOut(t));
            SetAlpha(lid, t);
        });
        phase = 1;
        count = 0;
        busy = false;
        Progress();
        Say(L("A perfect fit! Now screw the lid back on with the screwdriver."));
    }

    IEnumerator AddScrew(int i)
    {
        busy = true;
        TwSes.EfektCesitli(Ses.Vida);
        Show(screws[i], true);
        var rt = screws[i].rectTransform;
        yield return Tween(0.35f, t =>
        {
            rt.localRotation = Quaternion.Euler(0, 0, 540f * (1f - t));
            rt.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, t);
            SetAlpha(screws[i], t);
        });
        busy = false;
        Progress();
        if (++count < screws.Length) yield break;
        NextStep(L("Last step: turn the tuning knob until the needle finds the station."));
    }

    IEnumerator Tune()
    {
        busy = true;
        TwSes.Efekt(Ses.Tik);
        float from = needleAngle, to = Mathf.Min(station, needleAngle + needleStep);
        float knobFrom = tuningKnob.localEulerAngles.z;
        yield return Tween(0.25f, t =>
        {
            needleAngle = Mathf.Lerp(from, to, EaseOut(t));
            needle.localRotation = Quaternion.Euler(0, 0, -needleAngle);
            tuningKnob.localRotation = Quaternion.Euler(0, 0, knobFrom - 40f * EaseOut(t));
        });
        if (needleAngle < station - 0.01f)
        {
            Say(L(needleAngle >= station - needleStep - 0.01f ? "So close... just a little more!" : "Only static... keep turning."));
            busy = false;
            Progress();
            yield break;
        }

        TwSes.Efekt(Ses.RadyoAcilis);
        step = 6;
        RefreshSteps();
        Say(L("It works! The fishing news is on again. Hatice Teyze is so happy!"));
        yield return Wait(0.8f);
        TwSes.Efekt(Ses.OnarimTamam);
        yield return Wait(2.2f);
        busy = false;
        Progress();
        host.RepairFinished();
    }


    // ---------------- Yardim ----------------

    void Update()
    {
        if (!started || step >= 6) return;
        bool waiting = !busy && !auto && !helpOverlay.activeSelf;
        if (waiting) idle += Time.unscaledDeltaTime;
        if (waiting && !hinting && GameSettings.Hints && (idle > idleHintDelay || wrongCount >= wrongBeforeHint))
            ShowHints(true);
        // Ipucu varken "?" hafifce zipla
        float s = hinting && !GameSettings.ReduceMotion ? 1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) : 1f;
        helpButton.localScale = new Vector3(s, s, 1f);
    }

    /// <summary>Basarili her hamlede cagrilir: bekleme sayaci sifirlanir, ipucu yeni hedefe gecer.</summary>
    void Progress()
    {
        idle = 0f;
        if (hinting) PlaceHints();
    }

    void CountWrong()
    {
        wrongCount++;
        if (wrongCount >= wrongBeforeCard && !cardShown) OpenHelp();
    }

    Item Required() => step switch
    {
        0 => Item.Screwdriver,
        1 => Item.Cloth,
        2 => phase == 0 ? Item.Pliers : Item.Glass,
        3 => phase == 0 ? Item.Copper : Item.Solder,
        4 => phase == 0 ? Item.Wood : Item.Screwdriver,
        _ => Item.None,
    };

    /// <summary>Simdi tiklanmasi gereken yer (artboard'ta konum ve boyut).</summary>
    bool Target(out Vector2 pos, out Vector2 size)
    {
        RectTransform t = null;
        switch (step)
        {
            case 0: t = FirstScrew(true); break;
            case 1: foreach (var d in dust) if (d.gameObject.activeSelf) { t = d.rectTransform; break; } break;
            case 2: t = needle; break;
            case 3: t = phase == 0 ? looseWire.rectTransform : copperWire.rectTransform; break;
            case 4:
                if (phase == 0) { pos = lidHome; size = lid.rectTransform.sizeDelta * 0.8f; return true; }
                t = FirstScrew(false); break;
            case 5: t = tuningKnob; break;
        }
        pos = t ? t.anchoredPosition : Vector2.zero;
        size = t ? Vector2.one * Mathf.Max(90f, Mathf.Max(t.sizeDelta.x, t.sizeDelta.y) * 1.3f) : Vector2.zero;
        return t;
    }

    RectTransform FirstScrew(bool present)
    {
        foreach (var s in screws) if (s.gameObject.activeSelf == present) return s.rectTransform;
        return null;
    }

    void ShowHints(bool on)
    {
        hinting = on && step < 6;
        if (hinting) PlaceHints();
        else
        {
            slotHint.gameObject.SetActive(false);
            targetHint.gameObject.SetActive(false);
        }
    }

    void PlaceHints()
    {
        var need = Required();
        bool slotOn = need != Item.None && selected != need;
        slotHint.gameObject.SetActive(slotOn);
        if (slotOn)
        {
            var slot = (RectTransform)slots[(int)need].transform;
            slotHint.anchoredPosition = slot.anchoredPosition;
            slotHint.sizeDelta = slot.sizeDelta * 1.15f;
        }
        bool targetOn = (need == Item.None || selected == need) && Target(out _, out _);
        targetHint.gameObject.SetActive(targetOn);
        if (targetOn && Target(out var pos, out var size))
        {
            targetHint.anchoredPosition = pos;
            targetHint.sizeDelta = size;
        }
    }

    /// <summary>"?" butonu ya da cok yanlis denemeden sonra: bu adimin yardim karti.</summary>
    public void OpenHelp()
    {
        if (busy || auto || step >= 6) return;
        cardShown = true;
        var need = Required();
        helpStep.text = Loc.F("Step {0} of {1}: {2}", step + 1, stepRows.Length, Loc.T(StepTitlesEn[step]));
        helpIcon.gameObject.SetActive(need != Item.None);
        if (need != Item.None)
        {
            helpIcon.sprite = ((Image)slots[(int)need].targetGraphic).sprite;
            helpPick.text = Loc.F("1. Pick the {0}.", ItemName(need, true));
        }
        else helpPick.text = L("No tool needed.");
        helpClick.text = "2. " + (step == 5 ? L("Click the tuning knob on the right.") : L(StepTools()));
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
        if (busy || auto || step >= 6) return;
        StartCoroutine(AutoStep());
    }

    /// <summary>Hatice Teyze bu adimi oyuncunun gozu onunde yapar.</summary>
    IEnumerator AutoStep()
    {
        auto = true;
        ShowHints(false);
        int s = step;
        Say(L("Let me show you, dear..."));
        yield return Wait(0.6f);
        while (step == s && step < 6)
        {
            var need = Required();
            if (need != Item.None && selected != need)
            {
                SelectItem(need);
                yield return Wait(0.45f);
            }
            switch (step)
            {
                case 0: yield return RemoveScrew(System.Array.IndexOf(screws, FirstScrew(true).GetComponent<Image>())); break;
                case 1:
                    for (int i = 0; i < dust.Length; i++)
                        if (dust[i].gameObject.activeSelf) { yield return Wipe(i); break; }
                    break;
                case 2: yield return phase == 0 ? PullGlass() : InsertGlass(); break;
                case 3: yield return phase == 0 ? AddCopper() : SolderJoint(); break;
                case 4:
                    if (phase == 0) yield return PutLid();
                    else yield return AddScrew(System.Array.IndexOf(screws, FirstScrew(false).GetComponent<Image>()));
                    break;
                case 5: yield return Tune(); break;
            }
            yield return Wait(0.25f);
        }
        auto = false;
    }

    static readonly string[] StepTitlesEn =
        { "Open the case", "Clean the dust", "Replace the dial glass", "Rewire the speaker", "Close it up", "Tune in" };

    // ---------------- Yardimcilar ----------------

    bool Need(Item item)
    {
        if (selected == item) return true;
        if (selected == Item.None) { CountWrong(); Say(Loc.F("Pick the {0} first. ", ItemName(item, true)) + Loc.T(StepTools())); }
        else Wrong();
        return false;
    }

    void Wrong()
    {
        CountWrong();
        TwSes.Efekt(Ses.Hata, 0.6f);
        string why = selected == Item.Rope ? Loc.T("Rope is flexible, but it can't carry electricity or hold glass. ")
                   : selected == Item.Filament ? Loc.T("Filament is strong, but this step needs something else. ")
                   : selected == Item.None ? "" : Capitalize(Loc.F("The {0} won't help here. ", ItemName(selected, false)));
        Say(why + Loc.T(Hint()));
    }

    string StepTools() => step switch
    {
        0 => "Then click the 4 screws.",
        1 => "Then click the dusty spots.",
        2 => "Then click the dial.",
        3 => "Then click the loose wire next to the speaker.",
        4 => phase == 0 ? "Then click the case." : "Then click the 4 screw holes.",
        _ => "",
    };

    string Hint() => step switch
    {
        0 => "Step 1: pick the screwdriver and take out the 4 screws.",
        1 => "Step 2: wipe the dust away with the cloth.",
        2 => phase == 0 ? "Step 3: pull the cracked glass out with the pliers." : "Put the polished glass into the dial.",
        3 => phase == 0 ? "Step 4: connect the speaker with copper wire." : "Melt the joint with the soldering iron.",
        4 => phase == 0 ? "Step 5: put the sanded wood on the case as a new lid." : "Screw the lid back on with the screwdriver.",
        5 => "Turn the tuning knob until the needle finds the station.",
        _ => "",
    };

    void ShowHint()
    {
        if (step == 0 && count == 0)
            Say(L("Let's have a look inside. Step 1: pick the screwdriver and take out the 4 screws."));
        else
            Say(L(Hint()));
    }

    void NextStep(string text)
    {
        wrongCount = 0;
        idle = 0f;
        cardShown = false;
        ShowHints(false);
        TwSes.Efekt(Ses.Onay);
        step++;
        phase = count = 0;
        selected = Item.None;
        slotSelected.gameObject.SetActive(false);
        RefreshSteps();
        Say(text);
    }

    void RefreshSteps()
    {
        int done = Mathf.Min(step, stepRows.Length);
        for (int i = 0; i < stepRows.Length; i++)
        {
            bool isDone = i < step, isCurrent = i == step;
            stepRows[i].gameObject.SetActive(isDone || isCurrent);
            stepRows[i].sprite = isDone ? rowDone : rowCurrent;
            stepCircles[i].sprite = isDone ? circleDone : isCurrent ? circleCurrent : circlePending;
            stepNumbers[i].gameObject.SetActive(!isDone);
            stepTitles[i].color = isDone ? titleDone : titleNormal;
        }
        progressText.text = $"{done} / {stepRows.Length}";
        progressFill.sizeDelta = new Vector2(progressWidth * done / stepRows.Length, progressFill.sizeDelta.y);
    }

    void UseStock(Item item)
    {
        int i = item - Item.Glass;
        stock[i] = Mathf.Max(0, stock[i] - 1);
        UpdateStock(i);
        selected = Item.None;
        slotSelected.gameObject.SetActive(false);
    }

    void UpdateStock(int i)
    {
        shelfCounts[i].text = "×" + stock[i];
        slots[(int)Item.Glass + i].targetGraphic.color = stock[i] > 0 ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
    }

    /// <summary>Mesaj cubugu (metin cevrilmis olarak gelir; ingilizce sabitler icin L kullanin).</summary>
    void Say(string text) => message.text = text;

    static string L(string en) => Loc.T(en);

    static string ItemName(Item item, bool accusative) =>
        !Loc.Turkish ? ItemNames[(int)item] : accusative ? ItemNamesTrAcc[(int)item] : ItemNamesTr[(int)item];

    static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    static void Show(Graphic g, bool on)
    {
        g.gameObject.SetActive(on);
        SetAlpha(g, on ? 1f : 0f);
        g.rectTransform.localScale = Vector3.one;
    }

    static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    static IEnumerator Tween(float duration, Action<float> stepFn)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            stepFn(t / duration);
            yield return null;
        }
        stepFn(1f);
    }

    static IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
    }

    static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    static float EaseOutBack(float t)
    {
        const float c = 1.7f;
        float u = t - 1f;
        return 1f + (c + 1f) * u * u * u + c * u * u;
    }
}
