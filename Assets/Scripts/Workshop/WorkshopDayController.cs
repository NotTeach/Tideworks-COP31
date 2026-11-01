using System;
using System.Collections;
using Tideworks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 2. ve sonraki gunlerin atolye ici: musteri bozuk bir esyayla gelir (2. gun Elif ve Pamuk, 3. gun Mert ve pusula).
/// Repliklerden sonra "Let's fix it!" tezgaha (RepairBench: TeddyRepairController / CompassRepairController) gecer.
/// Tamirden sonra: musterinin sevinci (replikler, kalpler), odul karti (+50 altin), gunun gorev paneli;
/// "Set sail" denize cikarir, "Later" diyaloga "Head out" koyar. Metinler ve gorevler sahne kurucusundan gelir.
/// </summary>
public class WorkshopDayController : MonoBehaviour
{
    [FormerlySerializedAs("day2SeaSceneName")]
    [SerializeField] string seaSceneName = "Day2Sea";
    [SerializeField] string menuSceneName = "MainMenu";
    [Tooltip("Tamirden once dukkandan alinmasi gereken urun (bos: yok). Altin yetmezse musteri kendisininkini verir.")]
    [SerializeField] string requiredItem = "sewing_kit";
    [Tooltip("Tamir bitmeden \"Head out\"a basilinca")]
    [SerializeField] string helpFirstLine = "Can you help Pamuk first? Please?";
    [Tooltip("Gun basinda tamamlanmis gorev sayisi (onceki gunun tamiri)")]
    [SerializeField] int tasksDoneAtStart = 1;

    [Header("Sayfalar")]
    [SerializeField] CanvasGroup interiorPage;
    [SerializeField] CanvasGroup benchPage;
    [SerializeField] CanvasGroup screenFader;
    [FormerlySerializedAs("teddy")]
    [SerializeField] RepairBench bench;

    [Header("Ust bar")]
    [SerializeField] Text subtitle;
    [SerializeField] Text healthText;
    [SerializeField] Text coinText;

    [Header("Diyalog")]
    [SerializeField] Text dialogue;
    [SerializeField, TextArea(2, 4)] string[] elifLines;
    [SerializeField, TextArea(2, 4)] string[] thanksLines;
    [SerializeField] float charsPerSecond = 55f;
    [SerializeField] Button tellMoreButton;
    [SerializeField] Button letsFixButton;
    [SerializeField] Button dialogueHeadOutButton;
    [SerializeField] int rewardCoins = 50;
    [SerializeField] Text floatingText;

    [Header("Tamirden sonra")]
    [SerializeField] LocalizedImage interiorBackground;
    [SerializeField] Sprite afterBackground;
    [SerializeField] Sprite afterBackgroundTr;
    [SerializeField] Button nextButton;
    [SerializeField] Button welcomeButton;
    [SerializeField] Image heartTemplate;
    [SerializeField] Vector2 elifHead = new Vector2(1440, 600);
    [SerializeField] GameObject rewardOverlay;
    [SerializeField] RectTransform rewardCard;
    [SerializeField] RectTransform rewardCoinIcon;
    [SerializeField] RectTransform coinCounterIcon;
    [SerializeField] Image coinTemplate;
    [SerializeField] GameObject missionOverlay;
    [SerializeField] RectTransform missionCard;

    [Header("Gorev paneli")]
    [SerializeField] GameObject tasksButtonActive;
    [SerializeField] RectTransform tasksPanel;
    [SerializeField] float panelSlide = 860f;
    [SerializeField] Text taskCount;
    [SerializeField] Text taskBadge;
    [SerializeField] Image[] taskChecks;
    [SerializeField] Text[] taskTitles;
    [SerializeField] RectTransform[] taskStrikes;
    [SerializeField] RectTransform highlight;
    [SerializeField] Sprite checkOn, checkOff;
    [SerializeField] int tasksTotal = 5;

    readonly bool[] done = new bool[5];
    int line;
    bool busy, panelOpen, metCustomer, repaired;
    Vector2 panelShown, floatHome;
    Coroutine typing, panelMove;
    string typingText;

    void Awake()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (!button.GetComponent<TwButonSesi>())
                button.gameObject.AddComponent<TwButonSesi>();
        panelShown = tasksPanel.anchoredPosition;
        floatHome = floatingText.rectTransform.anchoredPosition;
    }

    void Start()
    {
        Time.timeScale = 1f;
        interiorPage.gameObject.SetActive(true);
        benchPage.gameObject.SetActive(false);
        subtitle.text = Loc.F("Grandpa's workshop · Day {0}", GameProgress.CurrentDay);
        healthText.text = GameProgress.OceanHealth + "%";
        coinText.text = GameProgress.Coins.ToString();
        dialogueHeadOutButton.gameObject.SetActive(false);
        floatingText.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        welcomeButton.gameObject.SetActive(false);
        heartTemplate.gameObject.SetActive(false);
        coinTemplate.gameObject.SetActive(false);
        rewardOverlay.SetActive(false);
        missionOverlay.SetActive(false);
        tasksPanel.anchoredPosition = panelShown + new Vector2(panelSlide, 0);
        tasksPanel.gameObject.SetActive(false);
        tasksButtonActive.SetActive(false);
        for (int i = 0; i < tasksDoneAtStart; i++) done[i] = true;     // onceki gunlerin tamiri
        RefreshTasks();
        TwSes.EkranGecisi(Ses.MuzikTamirciCaz, Ses.AmbiyansAtolye, null);
        StartCoroutine(Fade(1f, 0f));
        line = 0;
        Say(elifLines[0]);
    }

    void Update()
    {
        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                 || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (back && panelOpen && !busy) SetPanel(false);
    }

    // ---------------- Diyalog ----------------

    public void TellMore()
    {
        if (busy) return;
        if (typing != null) { FinishTyping(); return; }
        if (line + 1 >= elifLines.Length) return;
        line++;
        Say(elifLines[line]);
        if (line + 1 >= elifLines.Length) tellMoreButton.gameObject.SetActive(false);
    }

    public void LetsFix()
    {
        if (busy || repaired) return;
        busy = true;
        StartCoroutine(GoToBench());
    }

    IEnumerator GoToBench()
    {
        if (typing != null) FinishTyping();
        // Tamir icin gereken urun (2. gun dikis seti): yoksa dukkana yonlendir; altin yetmiyorsa Elif babaannesininkini verir
        if (!string.IsNullOrEmpty(requiredItem) && !ShopManager.Owns(requiredItem))
        {
            var kit = ShopManager.Find(requiredItem);
            int price = kit ? kit.price : 40;
            if (PlayerWallet.Coins < price)
            {
                Say("Oh! My grandma has a sewing kit. Here, take it!");
                while (typing != null) yield return null;
                yield return Wait(1.6f);
                ShopManager.Grant(requiredItem);
                TwSes.Efekt(Ses.Onay);
            }
            else
            {
                Say("We need a sewing kit for Pamuk. Grandpa's shop has one!");
                while (typing != null) yield return null;
                bool closed = false;
                ShopPanel.Open(() => closed = true);
                while (!closed) yield return null;
                if (!ShopManager.Owns(requiredItem))
                {
                    Say("Pamuk needs a needle and thread... Can you get the sewing kit from the shop?");
                    busy = false;
                    yield break;
                }
            }
        }
        if (!metCustomer)
        {
            metCustomer = true;
            if (!panelOpen) yield return PanelRoutine(true);
            yield return Wait(0.25f);
            yield return Tick(1);
            yield return Wait(0.9f);
            yield return PanelRoutine(false);
        }
        yield return SwitchTo(benchPage);
        bench.Begin();
        busy = false;
    }

    /// <summary>Tezgahtaki geri oku / "Workshop" sekmesi.</summary>
    public void ReturnToInterior()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(Back());
    }

    IEnumerator Back()
    {
        yield return SwitchTo(interiorPage);
        busy = false;
    }

    /// <summary>Tamir bitti: atolyeye don, gorevi isaretle, musteri tesekkur etsin.</summary>
    public void RepairFinished()
    {
        repaired = true;
        busy = true;
        StartCoroutine(AfterRepair());
    }

    IEnumerator AfterRepair()
    {
        tellMoreButton.gameObject.SetActive(false);
        letsFixButton.gameObject.SetActive(false);
        dialogue.text = "";
        // Musteri artik onarilmis esyayi tutuyor
        if (interiorBackground && afterBackground) interiorBackground.SetSprites(afterBackground, afterBackgroundTr);
        yield return SwitchTo(interiorPage);
        yield return PanelRoutine(true);
        yield return Wait(0.25f);
        yield return Tick(2);
        yield return Wait(0.9f);
        yield return PanelRoutine(false);

        // Musterinin sevinci: kalpler ve replikler ("Next" ile)
        TwSes.Efekt(Ses.OnarimTamam);
        StartCoroutine(Hearts(12f));
        for (int i = 0; i < thanksLines.Length; i++)
        {
            Say(thanksLines[i]);
            bool last = i == thanksLines.Length - 1;
            var button = last ? welcomeButton : nextButton;
            advance = false;
            button.gameObject.SetActive(true);
            busy = false;
            while (!advance) yield return null;
            busy = true;
            button.gameObject.SetActive(false);
        }
        yield return Reward();
        missionOverlay.SetActive(true);
        TwSes.Efekt(Ses.GecisDalga, 0.7f);
        yield return Tween(0.35f, t => missionCard.localScale = Vector3.one * EaseOutBack(t));
    }

    bool advance, rewardContinued;

    /// <summary>Diyalogdaki "Next" ve "You're welcome!": yazi bitmediyse tamamlar, bittiyse ilerler.</summary>
    public void NextLine()
    {
        if (typing != null) { FinishTyping(); return; }
        advance = true;
    }

    IEnumerator Hearts(float seconds)
    {
        for (float t = 0f; t < seconds; t += 0.5f)
        {
            if (!GameSettings.ReduceMotion) StartCoroutine(OneHeart());
            yield return Wait(0.5f);
        }
    }

    IEnumerator OneHeart()
    {
        var img = Instantiate(heartTemplate, heartTemplate.transform.parent);
        img.gameObject.SetActive(true);
        var rt = img.rectTransform;
        var start = new Vector2(elifHead.x + UnityEngine.Random.Range(-110f, 110f), -elifHead.y);
        float drift = UnityEngine.Random.Range(-50f, 50f), size = UnityEngine.Random.Range(0.6f, 1.1f);
        yield return Tween(2.2f, t =>
        {
            rt.anchoredPosition = start + new Vector2(drift * t + Mathf.Sin(t * 8f) * 12f, 220f * t);
            rt.localScale = Vector3.one * size * Mathf.Min(1f, t * 4f);
            var c = img.color; c.a = t < 0.7f ? 1f : (1f - t) / 0.3f; img.color = c;
        });
        Destroy(img.gameObject);
    }

    /// <summary>Odul karti: altinlar karttan ust bardaki sayaca ucar; "Continue" ile kapanir.</summary>
    IEnumerator Reward()
    {
        rewardContinued = false;
        rewardOverlay.SetActive(true);
        yield return Tween(0.35f, t => rewardCard.localScale = Vector3.one * EaseOutBack(t));
        yield return Wait(0.3f);
        int start = GameProgress.Coins, shown = start;
        PlayerWallet.Add(rewardCoins);
        GameSettings.Save();
        var parent = (RectTransform)coinTemplate.transform.parent;
        Vector2 from = LocalIn(parent, rewardCoinIcon), to = LocalIn(parent, coinCounterIcon);
        const int flying = 8;
        for (int i = 0; i < flying; i++)
        {
            int amount = rewardCoins * (i + 1) / flying - rewardCoins * i / flying;
            StartCoroutine(FlyCoin(from, to, () =>
            {
                shown += amount;
                coinText.text = shown.ToString();
                coinCounterIcon.localScale = Vector3.one * 1.25f;
                TwSes.EfektCesitli(Ses.AtikTopla, 0.5f);
            }));
            yield return Wait(0.09f);
        }
        yield return Wait(0.9f);
        coinCounterIcon.localScale = Vector3.one;
        coinText.text = GameProgress.Coins.ToString();
        while (!rewardContinued) yield return null;
        yield return Tween(0.2f, t => rewardCard.localScale = Vector3.one * (1f - t * t));
        rewardOverlay.SetActive(false);
    }

    /// <summary>Odul kartindaki "Continue".</summary>
    public void RewardContinue() => rewardContinued = true;

    IEnumerator FlyCoin(Vector2 from, Vector2 to, Action arrived)
    {
        var coin = Instantiate(coinTemplate, coinTemplate.transform.parent);
        coin.gameObject.SetActive(true);
        var rt = coin.rectTransform;
        var control = (from + to) / 2 + new Vector2(UnityEngine.Random.Range(-160f, 160f), 260f);
        yield return Tween(0.7f, t =>
        {
            float e = t * t * (3f - 2f * t);
            rt.anchoredPosition = Vector2.Lerp(Vector2.Lerp(from, control, e), Vector2.Lerp(control, to, e), e);
            rt.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.7f, e);
        });
        arrived();
        Destroy(coin.gameObject);
    }

    static Vector2 LocalIn(RectTransform parent, RectTransform target)
    {
        Vector2 p = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
        return p - new Vector2(parent.rect.xMin, parent.rect.yMax);
    }

    /// <summary>Gorev paneli ("Clear the ghost nets!" / "Clean the rock pools!") > Set sail</summary>
    public void SetSail()
    {
        missionOverlay.SetActive(false);
        busy = true;
        StartCoroutine(Load(seaSceneName));
    }

    /// <summary>"Clear the ghost nets!" > Later: diyalogda "Head out" kalir.</summary>
    public void MissionLater()
    {
        missionOverlay.SetActive(false);
        dialogueHeadOutButton.gameObject.SetActive(true);
        busy = false;
    }

    /// <summary>Ust bar ya da diyalogdaki "Head out".</summary>
    public void HeadOut()
    {
        if (busy) return;
        if (!repaired) { Say(helpFirstLine); return; }
        busy = true;
        StartCoroutine(Load(seaSceneName));
    }

    public void BackToMenu()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(Load(menuSceneName));
    }

    // ---------------- Gorev paneli ----------------

    public void ToggleTasks() { if (!busy) SetPanel(!panelOpen); }

    /// <summary>Ust bardaki "Shop" butonu.</summary>
    public void OpenShop() { if (!busy) ShopPanel.Open(); }

    void OnEnable() => PlayerWallet.OnChanged += OnCoins;

    void OnDisable() => PlayerWallet.OnChanged -= OnCoins;

    void OnCoins(int coins) { if (coinText && !rewardOverlay.activeSelf) coinText.text = coins.ToString(); }

    public void CloseTasks() { if (!busy) SetPanel(false); }

    void SetPanel(bool open)
    {
        if (panelMove != null) StopCoroutine(panelMove);
        panelMove = StartCoroutine(PanelRoutine(open));
    }

    IEnumerator PanelRoutine(bool open)
    {
        panelOpen = open;
        tasksButtonActive.SetActive(open);
        tasksPanel.gameObject.SetActive(true);
        TwSes.Efekt(Ses.GecisKabarcik, 0.6f);
        Vector2 from = tasksPanel.anchoredPosition, to = panelShown + (open ? Vector2.zero : new Vector2(panelSlide, 0));
        yield return Tween(0.35f, t => tasksPanel.anchoredPosition = Vector2.LerpUnclamped(from, to, open ? EaseOutBack(t) : t * t));
        if (!open) tasksPanel.gameObject.SetActive(false);
        panelMove = null;
    }

    IEnumerator Tick(int task)
    {
        done[task] = true;
        TwSes.Efekt(Ses.Onay);
        RefreshTasks();
        var strike = taskStrikes[task];
        float width = Mathf.Min(taskTitles[task].preferredWidth, taskTitles[task].rectTransform.rect.width) + 10f;
        yield return Tween(0.3f, t => strike.sizeDelta = new Vector2(width * t, strike.sizeDelta.y));
    }

    void RefreshTasks()
    {
        int count = 0, current = -1;
        for (int i = 0; i < done.Length; i++)
        {
            if (done[i]) count++;
            else if (current < 0) current = i;
            taskChecks[i].sprite = done[i] ? checkOn : checkOff;
            var c = taskTitles[i].color; c.a = done[i] ? 0.55f : 1f; taskTitles[i].color = c;
            if (!done[i]) taskStrikes[i].sizeDelta = new Vector2(0, taskStrikes[i].sizeDelta.y);
            else if (taskStrikes[i].sizeDelta.x <= 0f && i == 0)
                taskStrikes[i].sizeDelta = new Vector2(Mathf.Min(taskTitles[i].preferredWidth, taskTitles[i].rectTransform.rect.width) + 10f, taskStrikes[i].sizeDelta.y);
        }
        highlight.gameObject.SetActive(current >= 0);
        if (current >= 0)
            highlight.anchoredPosition = new Vector2(highlight.anchoredPosition.x, ((RectTransform)taskChecks[current].transform).anchoredPosition.y);
        taskCount.text = Loc.F("{0} of {1} done · Day {2}", count, tasksTotal, GameProgress.CurrentDay);
        taskBadge.text = (tasksTotal - count).ToString();
    }

    // ---------------- Yardimcilar ----------------

    void Say(string en)
    {
        if (typing != null) StopCoroutine(typing);
        typingText = Loc.T(en);
        typing = StartCoroutine(Type(typingText));
    }

    void FinishTyping()
    {
        StopCoroutine(typing);
        typing = null;
        dialogue.text = typingText;
    }

    IEnumerator Type(string text)
    {
        dialogue.text = "";
        float shown = 0f;
        while (shown < text.Length)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            dialogue.text = text.Substring(0, Mathf.Min(text.Length, Mathf.FloorToInt(shown)));
            yield return null;
        }
        dialogue.text = text;
        typing = null;
    }

    IEnumerator Float(string text)
    {
        var t = floatingText;
        t.gameObject.SetActive(true);
        t.text = text;
        var rt = t.rectTransform;
        Vector2 start = floatHome;
        yield return Tween(1.3f, k =>
        {
            rt.anchoredPosition = start + new Vector2(0, 60f * k);
            var c = t.color; c.a = 1f - k * k; t.color = c;
        });
        t.gameObject.SetActive(false);
    }

    IEnumerator SwitchTo(CanvasGroup page)
    {
        yield return Fade(0f, 1f, true);
        interiorPage.gameObject.SetActive(page == interiorPage);
        benchPage.gameObject.SetActive(page == benchPage);
        yield return Fade(1f, 0f);
    }

    IEnumerator Load(string scene)
    {
        GameSettings.Save();
        yield return Fade(0f, 1f, true);
        SceneManager.LoadScene(scene);
    }

    IEnumerator Fade(float from, float to, bool keep = false)
    {
        if (!screenFader) yield break;
        screenFader.gameObject.SetActive(true);
        screenFader.blocksRaycasts = true;
        yield return Tween(0.45f, t => screenFader.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
        if (to == 0f && !keep) screenFader.gameObject.SetActive(false);
    }

    static IEnumerator Wait(float s)
    {
        for (float t = 0f; t < s; t += Time.unscaledDeltaTime) yield return null;
    }

    static float EaseOutBack(float t)
    {
        const float c = 1.7f;
        float u = t - 1f;
        return 1f + (c + 1f) * u * u * u + c * u * u;
    }

    static IEnumerator Tween(float duration, Action<float> step)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            step(t / duration);
            yield return null;
        }
        step(1f);
    }
}
