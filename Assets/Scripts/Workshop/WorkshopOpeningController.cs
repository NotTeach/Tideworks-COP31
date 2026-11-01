using System;
using System.Collections;
using Tideworks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Intro'dan sonraki acilis: (1) kapinin ustundeki anahtari al, anahtar kapiyi acsin,
/// (2) Day 1 gorev listesi ("Open the workshop" isaretlenir), (3) atolyede ilk musteri Hatice Teyze.
/// "Let's fix it!" gorevi isaretler ve tamir tezgahina (RepairController) gecer; radyo bitince
/// atolyeye donulur, "Head out" denize cikar.
/// </summary>
public class WorkshopOpeningController : MonoBehaviour
{
    [Header("Sahneler (Build Settings'te ekli olmali)")]
    [SerializeField] string seaSceneName = "Fishing";
    [SerializeField] string menuSceneName = "MainMenu";

    [Header("Sayfalar")]
    [SerializeField] CanvasGroup exteriorPage;
    [SerializeField] CanvasGroup dayPlanPage;
    [SerializeField] CanvasGroup workshopPage;
    [SerializeField] CanvasGroup workbenchPage;
    [SerializeField] CanvasGroup screenFader;
    [SerializeField] float screenFade = 0.5f;

    [Header("1 - Anahtar ve kapi")]
    [SerializeField] Text questStep;
    [SerializeField] Text questTitle;
    [SerializeField] Text questBody;
    [SerializeField] Button takeKeyButton;
    [SerializeField] RectTransform key;
    [SerializeField] Graphic keyGlow;
    [SerializeField] Graphic keySparkle;
    [SerializeField] RectTransform doorLeft;
    [SerializeField] RectTransform doorRight;
    [Tooltip("Anahtar ucunun girdigi kilit (artboard koordinati, sol ust orijin)")]
    [SerializeField] Vector2 keyhole = new Vector2(1922, 1222);
    [Tooltip("Anahtar sprite'inda ucun yeri (pivot)")]
    [SerializeField] Vector2 keyTipPivot = new Vector2(0.47f, 0.05f);

    [Header("Gune gore dis cephe (2. gun ve sonrasi)")]
    [Tooltip("Kapi acilinca 2. gun ve sonrasi bu sahneye gecer (gun plani ve Hatice Teyze sadece 1. gun)")]
    [SerializeField] string laterDaysAfterDoor = "Fishing";
    [SerializeField] LocalizedImage exteriorBackground;
    [SerializeField] Sprite day2Background;
    [SerializeField] Sprite day2BackgroundTr;
    [SerializeField] GameObject[] trashHiddenOnDay2;
    [SerializeField] GameObject shop;
    [SerializeField] Text toast;
    [SerializeField] RectTransform sign;
    [SerializeField] Text signText;

    [Header("2 - Gun plani")]
    [SerializeField] RectTransform firstCheck;
    [SerializeField] RectTransform doneBadge;
    [SerializeField] Text firstTitle;
    [SerializeField] Text firstSubtitle;
    [SerializeField] RectTransform firstStrike;
    [SerializeField] Button startDayButton;

    [Header("3 - Atolye")]
    [SerializeField] Text dialogue;
    [SerializeField, TextArea(2, 4)] string[] dialogueLines;
    [SerializeField, TextArea(2, 4)] string headOutLine = "Before you head out, dear... would you take a look at my radio?";
    [SerializeField] float charsPerSecond = 55f;
    [SerializeField] Button tellMoreButton;
    [SerializeField] Button letsFixButton;
    [SerializeField] GameObject tasksButtonActive;
    [SerializeField] RectTransform tasksPanel;
    [SerializeField] float panelSlide = 860f;
    [SerializeField] Text taskCount;
    [SerializeField] Text taskBadge;
    [SerializeField] RectTransform meetCheck;
    [SerializeField] RectTransform meetStrike;
    [SerializeField] Graphic meetFade;
    [SerializeField] float meetStrikeWidth = 300f;
    [SerializeField] RectTransform repairCheck;
    [SerializeField] RectTransform repairStrike;
    [SerializeField] Graphic repairFade;
    [SerializeField] float repairStrikeWidth = 266f;

    [Header("4 - Tamir tezgahi")]
    [SerializeField] RepairController repair;
    [SerializeField] int tasksTotal = 5;

    [Header("5 - Tamirden sonra: Hatice Teyze, odul, deniz paneli")]
    [Tooltip("Her biri ayri diyalog kutusu; sonuncudan sonra odul karti gelir")]
    [SerializeField, TextArea(2, 4)] string[] afterRepairLines;
    [SerializeField] Button nextButton;
    [SerializeField] Button dialogueHeadOutButton;
    [SerializeField] RectTransform rewardCard;
    [SerializeField] RectTransform rewardCoinIcon;
    [SerializeField] Image coinTemplate;
    [Tooltip("Odul kartinin ustundeki \"120 > 160\" hapi")]
    [SerializeField] Text counterPillFrom;
    [SerializeField] Text counterPillTo;
    [SerializeField] RectTransform counterPillCoin;
    [SerializeField] Text coinCounter;
    [SerializeField] int rewardCoins = 40;
    [SerializeField] GameObject rewardOverlay;
    [SerializeField] GameObject missionOverlay;
    [SerializeField] RectTransform missionCard;
    [SerializeField] Image[] floatingSprites;
    [Tooltip("Notalar ve kalpler radyonun ustunden yukselir (artboard koordinati, sol ust orijin)")]
    [SerializeField] Vector2 radioPosition = new Vector2(1480, 900);

    static readonly string[,] Quest =
    {
        { "Find the key", "The key is above the door, just like Grandpa wrote." },
        { "Unlock the door", "Grandpa's old key still fits the lock." },
        { "Step inside", "The workshop is open again!" },
    };

    int tasksDone = 1, line, day = 1;
    bool busy, panelOpen, metHatice, radioFixed;
    Vector2 panelShown;
    Coroutine typing, panelMove;
    string typingText;

    void Awake()
    {
        // Sahne bu scriptten eski bir kurucu surumuyle kurulduysa bazi alanlar bos kalir: cokmek yerine ne yapilacagini soyle
        if (!coinCounter || !nextButton || !rewardOverlay || !missionOverlay || !counterPillTo || !repair || !signText)
        {
            Debug.LogError("[Tideworks] Workshop sahnesi eski. Unity menusu > Tideworks > Rebuild Workshop Scene ile yeniden kurun.", this);
            enabled = false;
            return;
        }

        foreach (var button in GetComponentsInChildren<Button>(true))
            if (!button.GetComponent<TwButonSesi>())
                button.gameObject.AddComponent<TwButonSesi>();

        // Kanatlar dis kenarlarindaki menteseden donsun
        SetPivot(doorLeft, new Vector2(0f, 0.5f));
        SetPivot(doorRight, new Vector2(1f, 0.5f));
        panelShown = tasksPanel.anchoredPosition;
    }

    void Start()
    {
        Time.timeScale = 1f;
        ShowOnly(exteriorPage);
        day = GameProgress.CurrentDay;
        SetupDay();
        SetQuest(0);
        TwSes.Ambiyans(Ses.AmbiyansDeniz);

        firstCheck.localScale = Vector3.zero;
        doneBadge.localScale = Vector3.zero;
        SetWidth(firstStrike, 0f);
        meetCheck.localScale = Vector3.zero;
        SetWidth(meetStrike, 0f);
        SetAlpha(meetFade, 0f);
        repairCheck.localScale = Vector3.zero;
        SetWidth(repairStrike, 0f);
        SetAlpha(repairFade, 0f);
        tasksPanel.anchoredPosition = panelShown + new Vector2(panelSlide, 0);
        tasksPanel.gameObject.SetActive(false);
        tasksButtonActive.SetActive(false);
        UpdateTaskCounts();
        coinCounter.text = GameProgress.Coins.ToString();
        nextButton.gameObject.SetActive(false);
        dialogueHeadOutButton.gameObject.SetActive(false);
        rewardOverlay.SetActive(false);
        missionOverlay.SetActive(false);
        coinTemplate.gameObject.SetActive(false);
        foreach (var f in floatingSprites) f.gameObject.SetActive(false);

        if (screenFader) StartCoroutine(Fade(screenFader, 1f, 0f));
    }

    void Update()
    {
        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                 || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (back && panelOpen && !busy) SetPanel(false);
    }

    // ---------------- 1: anahtar ----------------

    public void TakeKey()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(OpenDoor());
    }

    IEnumerator OpenDoor()
    {
        takeKeyButton.interactable = false;
        if (key.TryGetComponent(out Button keyButton)) keyButton.interactable = false;
        TwSes.Efekt(Ses.Pop);

        // Etiket kuculup kaybolur, alttan "OPEN" tabelasi gorunur; isilti soner
        var pill = takeKeyButton.transform;
        if (pill.TryGetComponent(out MenuButtonFx fx)) fx.enabled = false; // olcegi o da degistiriyor
        foreach (var pulse in new[] { keyGlow, keySparkle })
            if (pulse.TryGetComponent(out MenuPulse p)) p.enabled = false;
        float glowA = keyGlow.color.a, sparkA = keySparkle.color.a;
        yield return Tween(0.25f, t =>
        {
            pill.localScale = Vector3.one * (1f - EaseIn(t));
            SetAlpha(keyGlow, glowA * (1f - t));
            SetAlpha(keySparkle, sparkA * (1f - t));
        });
        pill.gameObject.SetActive(false);
        SetQuest(1);

        // Anahtar ucu kilide: once hafifce kalkar, sonra yay cizerek iner
        SetPivot(key, keyTipPivot);
        Vector2 from = key.anchoredPosition, to = new Vector2(keyhole.x, -keyhole.y);
        yield return Tween(0.18f, t => key.localScale = Vector3.one * (1f + 0.12f * EaseOut(t)));
        yield return Tween(0.7f, t =>
        {
            float e = EaseInOut(t);
            key.anchoredPosition = Vector2.Lerp(from, to, e) + new Vector2(-60f, 0) * Mathf.Sin(e * Mathf.PI);
            key.localScale = Vector3.one * Mathf.Lerp(1.12f, 0.9f, e);
        });
        TwSes.Efekt(Ses.Vida);
        yield return Tween(0.35f, t => key.localRotation = Quaternion.Euler(0, 0, -90f * EaseInOut(t)));
        yield return Wait(0.15f);
        yield return Tween(0.2f, t => SetAlpha(key.GetComponent<Graphic>(), 1f - t));

        // Kapi acilir: kanatlar menteseden iceri katlanir
        TwSes.Efekt(Ses.GecisParilti, 0.8f);
        yield return Tween(0.8f, t =>
        {
            float sx = Mathf.Lerp(1f, 0.14f, EaseOut(t));
            doorLeft.localScale = new Vector3(sx, 1f, 1f);
            doorRight.localScale = new Vector3(sx, 1f, 1f);
        });
        yield return FlipSign();
        SetQuest(2);
        yield return Wait(1.1f);

        if (day >= 2)
        {
            // Gun plani ve Hatice Teyze'nin ziyareti 1. gune ozel: sonraki gunler denize acilir
            StartCoroutine(LoadAfterFade(laterDaysAfterDoor));
            yield break;
        }
        yield return SwitchPage(dayPlanPage);
        busy = false;
        StartCoroutine(MarkWorkshopOpen());
    }

    /// <summary>
    /// Gune gore dis cephe: 2. gunden itibaren kumsal daha temiz (daha az cop, daha berrak deniz),
    /// yaliya iki ev yapilmis ve magaza acilmis. Tabela kapi acilana kadar CLOSED.
    /// </summary>
    void SetupDay()
    {
        bool later = day >= 2;
        if (later && exteriorBackground && day2Background) exteriorBackground.SetSprites(day2Background, day2BackgroundTr);
        foreach (var t in trashHiddenOnDay2) if (t) t.SetActive(!later);
        if (shop) shop.SetActive(later);
        if (toast) toast.gameObject.SetActive(false);
        SetSign(false);
    }

    void SetSign(bool open)
    {
        signText.text = Loc.T(open ? "OPEN" : "CLOSED");
        signText.color = open ? new Color32(60, 140, 90, 255) : new Color32(196, 64, 48, 255);
    }

    /// <summary>Kapi acilinca tabela doner: CLOSED -> OPEN.</summary>
    IEnumerator FlipSign()
    {
        TwSes.Efekt(Ses.Tik, 0.7f);
        yield return Tween(0.18f, t => sign.localScale = new Vector3(1f - t, 1f, 1f));
        SetSign(true);
        yield return Tween(0.22f, t => sign.localScale = new Vector3(EaseOutBack(t), 1f, 1f));
    }

    /// <summary>Magaza binasina tiklaninca.</summary>
    public void OpenShop()
    {
        if (!toast) return;
        StopCoroutine(nameof(ShowToast));
        StartCoroutine(nameof(ShowToast), Loc.T("The shop opens soon! Keep saving your coins."));
    }

    IEnumerator ShowToast(string text)
    {
        toast.text = text;
        toast.gameObject.SetActive(true);
        TwSes.Efekt(Ses.Pop, 0.7f);
        yield return Tween(2.6f, t => SetAlpha(toast, t < 0.8f ? 1f : (1f - t) / 0.2f));
        toast.gameObject.SetActive(false);
    }

    // ---------------- 2: gun plani ----------------

    IEnumerator MarkWorkshopOpen()
    {
        yield return Wait(0.45f);
        TwSes.Efekt(Ses.Onay);
        yield return Pop(firstCheck);
        SetWidth(firstStrike, 0f);
        float strike = firstTitle.preferredWidth + 12f;
        yield return Tween(0.3f, t =>
        {
            SetWidth(firstStrike, strike * EaseOut(t));
            SetAlpha(firstTitle, Mathf.Lerp(1f, 0.55f, t));
            SetAlpha(firstSubtitle, Mathf.Lerp(1f, 0.55f, t));
        });
        yield return Pop(doneBadge);
    }

    public void StartDay()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(EnterWorkshop());
    }

    IEnumerator EnterWorkshop()
    {
        startDayButton.interactable = false;
        TwSes.EkranGecisi(Ses.MuzikTamir, Ses.AmbiyansAtolye);
        dialogue.text = "";
        yield return SwitchPage(workshopPage);
        busy = false;
        line = 0;
        Say(dialogueLines.Length > 0 ? dialogueLines[0] : "");
    }

    // ---------------- 3: atolye ----------------

    public void TellMore()
    {
        if (busy) return;
        if (typing != null) { FinishTyping(); return; }
        if (line + 1 >= dialogueLines.Length) return;
        line++;
        Say(dialogueLines[line]);
        if (line + 1 >= dialogueLines.Length) tellMoreButton.gameObject.SetActive(false);
    }

    public void HeadOut()
    {
        if (busy) return;
        if (!radioFixed) { Say(headOutLine); return; }
        busy = true;
        StartCoroutine(LoadAfterFade(seaSceneName));
    }

    public void LetsFix()
    {
        if (busy || radioFixed) return;
        busy = true;
        StartCoroutine(metHatice ? OpenWorkbench() : StartRepair());
    }

    IEnumerator StartRepair()
    {
        if (typing != null) FinishTyping();
        letsFixButton.interactable = false;
        tellMoreButton.interactable = false;

        if (!panelOpen) yield return SetPanelRoutine(true);
        yield return Wait(0.3f);

        TwSes.Efekt(Ses.Onay);
        tasksDone++;
        UpdateTaskCounts();
        yield return Pop(meetCheck);
        float meetWidth = StrikeWidth("Meet Hatice Teyze", meetStrikeWidth);
        yield return Tween(0.3f, t =>
        {
            SetWidth(meetStrike, meetWidth * EaseOut(t));
            SetAlpha(meetFade, 0.5f * t);
        });
        metHatice = true;
        yield return Wait(1.1f);
        yield return OpenWorkbench();
    }

    IEnumerator OpenWorkbench()
    {
        busy = true;
        if (typing != null) FinishTyping();
        if (panelOpen) yield return SetPanelRoutine(false);
        yield return SwitchPage(workbenchPage);
        repair.Begin();
        letsFixButton.interactable = true;
        busy = false;
    }

    /// <summary>Tezgahtaki geri oku / "Workshop" sekmesi: tamir yarim kalir, "Let's fix it!" geri getirir.</summary>
    public void ReturnToWorkshop()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(Return());
    }

    IEnumerator Return()
    {
        yield return SwitchPage(workshopPage);
        busy = false;
    }

    /// <summary>Radyo calisti: atolyeye don, gorevi isaretle, Hatice Teyze tesekkur etsin.</summary>
    public void RepairFinished()
    {
        radioFixed = true;
        busy = true;
        StartCoroutine(AfterRepair());
    }

    IEnumerator AfterRepair()
    {
        tellMoreButton.gameObject.SetActive(false);
        letsFixButton.gameObject.SetActive(false);
        dialogue.text = "";
        yield return SwitchPage(workshopPage);
        TwSes.EkranGecisi(Ses.MuzikAnaTema, Ses.AmbiyansAtolye, null);
        yield return SetPanelRoutine(true);
        yield return Wait(0.3f);
        TwSes.Efekt(Ses.Onay);
        tasksDone++;
        UpdateTaskCounts();
        yield return Pop(repairCheck);
        float repairWidth = StrikeWidth("Repair the radio", repairStrikeWidth);
        yield return Tween(0.3f, t =>
        {
            SetWidth(repairStrike, repairWidth * EaseOut(t));
            SetAlpha(repairFade, 0.5f * t);
        });
        yield return Wait(0.9f);
        yield return SetPanelRoutine(false);

        // Radyo calar: cizirti, ardindan haberler; Hatice Teyze'nin etrafinda notalar ve kalpler
        TwSes.Efekt(Ses.RadyoAcilis);
        StartCoroutine(FloatAroundRadio(14f));
        yield return Wait(0.8f);

        for (int i = 0; i < afterRepairLines.Length; i++)
        {
            Say(afterRepairLines[i]);
            while (typing != null) yield return null;
            if (i == afterRepairLines.Length - 1) break;
            advance = false;
            nextButton.gameObject.SetActive(true);
            while (!advance) yield return null;
            nextButton.gameObject.SetActive(false);
        }
        yield return Wait(0.8f);
        yield return Reward();
        yield return ShowMission();
        busy = false;
    }

    bool advance;

    /// <summary>Diyalogdaki "Next": yazi bitmediyse tamamlar, bittiyse siradaki konusma.</summary>
    public void NextLine()
    {
        if (typing != null) { FinishTyping(); return; }
        advance = true;
    }

    /// <summary>Odul karti; altinlar karttan ust bardaki sayaca ucar.</summary>
    IEnumerator Reward()
    {
        int start = GameProgress.Coins, shown = start;
        counterPillFrom.text = counterPillTo.text = start.ToString();
        rewardContinued = false;
        rewardOverlay.SetActive(true);
        TwSes.Efekt(Ses.OnarimTamam);
        yield return Pop(rewardCard);
        yield return Wait(0.4f);

        // Altinlar yigindan ustteki sayac hapina ucar: "120 > 160"
        const int flying = 8;
        GameProgress.Coins = start + rewardCoins;
        GameSettings.Save();
        var parent = (RectTransform)coinTemplate.transform.parent;
        Vector2 from = LocalIn(parent, rewardCoinIcon), to = LocalIn(parent, counterPillCoin);
        for (int i = 0; i < flying; i++)
        {
            int amount = rewardCoins * (i + 1) / flying - rewardCoins * i / flying;
            StartCoroutine(FlyCoin(from, to, () =>
            {
                shown += amount;
                counterPillTo.text = shown.ToString();
                counterPillCoin.localScale = Vector3.one * 1.25f;
                TwSes.EfektCesitli(Ses.AtikTopla, 0.5f);
            }));
            yield return Wait(0.09f);
        }
        yield return Wait(0.9f);
        counterPillCoin.localScale = Vector3.one;
        counterPillTo.text = coinCounter.text = GameProgress.Coins.ToString();

        while (!rewardContinued) yield return null;
        yield return Tween(0.2f, t => rewardCard.localScale = Vector3.one * (1f - EaseIn(t)));
        rewardOverlay.SetActive(false);
    }

    bool rewardContinued;

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
            float e = EaseInOut(t);
            rt.anchoredPosition = Vector2.Lerp(Vector2.Lerp(from, control, e), Vector2.Lerp(control, to, e), e);
            rt.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.7f, e);
        });
        arrived();
        Destroy(coin.gameObject);
    }

    /// <summary>target'in merkezini, parent'in sol ust kosesine baglanmis bir cocugun anchoredPosition'ina cevirir.</summary>
    static Vector2 LocalIn(RectTransform parent, RectTransform target)
    {
        Vector2 p = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
        return p - new Vector2(parent.rect.xMin, parent.rect.yMax);
    }

    IEnumerator ShowMission()
    {
        missionOverlay.SetActive(true);
        TwSes.Efekt(Ses.GecisDalga, 0.7f);
        yield return Pop(missionCard);
    }

    /// <summary>"Let's Save the Seas!" > Head out</summary>
    public void MissionHeadOut()
    {
        missionOverlay.SetActive(false);
        busy = true;
        StartCoroutine(LoadAfterFade(seaSceneName));
    }

    /// <summary>"Let's Save the Seas!" > Later: atolyede kal, diyalogdaki "Head out" ile denize cikilir.</summary>
    public void MissionLater()
    {
        missionOverlay.SetActive(false);
        dialogueHeadOutButton.gameObject.SetActive(true);
        busy = false;
    }

    /// <summary>Hatice Teyze ve radyonun ustunden notalar ve kalpler yukselir.</summary>
    IEnumerator FloatAroundRadio(float seconds)
    {
        if (floatingSprites.Length == 0) yield break;
        for (float t = 0f; t < seconds; t += 0.55f)
        {
            if (!GameSettings.ReduceMotion)
                StartCoroutine(FloatOne(floatingSprites[UnityEngine.Random.Range(0, floatingSprites.Length)]));
            yield return Wait(0.55f);
        }
    }

    IEnumerator FloatOne(Image template)
    {
        var img = Instantiate(template, template.transform.parent);
        img.gameObject.SetActive(true);
        var rt = img.rectTransform;
        var start = new Vector2(radioPosition.x + UnityEngine.Random.Range(-120f, 120f), -radioPosition.y);
        float drift = UnityEngine.Random.Range(-60f, 60f), size = UnityEngine.Random.Range(0.7f, 1.1f);
        yield return Tween(2.2f, t =>
        {
            rt.anchoredPosition = start + new Vector2(drift * t + Mathf.Sin(t * 9f) * 14f, 240f * t);
            rt.localScale = Vector3.one * size * Mathf.Min(1f, t * 4f);
            SetAlpha(img, t < 0.7f ? 1f : (1f - t) / 0.3f);
        });
        Destroy(img.gameObject);
    }

    public void ToggleTasks()
    {
        if (busy) return;
        SetPanel(!panelOpen);
    }

    public void CloseTasks()
    {
        if (busy) return;
        SetPanel(false);
    }

    public void BackToMenu()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(LoadAfterFade(menuSceneName));
    }

    IEnumerator LoadAfterFade(string scene)
    {
        if (screenFader) yield return Fade(screenFader, 0f, 1f);
        SceneManager.LoadScene(scene);
    }

    void SetPanel(bool open)
    {
        if (panelMove != null) StopCoroutine(panelMove);
        panelMove = StartCoroutine(SetPanelRoutine(open));
    }

    IEnumerator SetPanelRoutine(bool open)
    {
        panelOpen = open;
        tasksButtonActive.SetActive(open);
        tasksPanel.gameObject.SetActive(true);
        TwSes.Efekt(Ses.GecisKabarcik, 0.6f);
        Vector2 from = tasksPanel.anchoredPosition;
        Vector2 to = panelShown + (open ? Vector2.zero : new Vector2(panelSlide, 0));
        yield return Tween(0.35f, t => tasksPanel.anchoredPosition = Vector2.LerpUnclamped(from, to, open ? EaseOutBack(t) : EaseIn(t)));
        if (!open) tasksPanel.gameObject.SetActive(false);
        panelMove = null;
    }

    void UpdateTaskCounts()
    {
        taskCount.text = Loc.F("{0} of {1} done · Day {2}", tasksDone, tasksTotal, GameProgress.CurrentDay);
        taskBadge.text = (tasksTotal - tasksDone).ToString();
    }

    void Say(string text)
    {
        if (typing != null) StopCoroutine(typing);
        typingText = text = Loc.T(text);
        typing = StartCoroutine(Type(text));
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

    // ---------------- Yardimcilar ----------------

    void SetQuest(int step)
    {
        questStep.text = Loc.F("DAY {0} · MORNING · STEP {1} OF 3", day, step + 1);
        questTitle.text = Loc.T(Quest[step, 0]);
        questBody.text = Loc.T(day >= 2 && step == 0 ? "The key is above the door, right where you left it." : Quest[step, 1]);
    }

    void ShowOnly(CanvasGroup page)
    {
        foreach (var p in new[] { exteriorPage, dayPlanPage, workshopPage, workbenchPage })
        {
            bool on = p == page;
            p.gameObject.SetActive(on);
            p.alpha = on ? 1f : 0f;
            p.interactable = p.blocksRaycasts = on;
        }
    }

    IEnumerator SwitchPage(CanvasGroup to)
    {
        if (screenFader) yield return Fade(screenFader, 0f, 1f, keepActive: true);
        ShowOnly(to);
        if (screenFader) yield return Fade(screenFader, 1f, 0f);
    }

    IEnumerator Fade(CanvasGroup group, float from, float to, bool keepActive = false)
    {
        group.gameObject.SetActive(true);
        group.blocksRaycasts = true;
        yield return Tween(screenFade, t => group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
        if (to == 0f && !keepActive) group.gameObject.SetActive(false);
    }

    static IEnumerator Pop(Transform target)
    {
        yield return Tween(0.32f, t => target.localScale = Vector3.one * EaseOutBack(t));
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

    static IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
    }

    static float EaseIn(float t) => t * t;
    static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    static float EaseInOut(float t) => t * t * (3f - 2f * t);
    static float EaseOutBack(float t)
    {
        const float c = 1.7f;
        float u = t - 1f;
        return 1f + (c + 1f) * u * u * u + c * u * u;
    }

    /// <summary>Pivotu degistirir ama ekrandaki yeri kaymaz.</summary>
    static void SetPivot(RectTransform rt, Vector2 pivot)
    {
        var shift = Vector2.Scale(pivot - rt.pivot, rt.rect.size);
        shift = Vector2.Scale(shift, rt.localScale);
        rt.pivot = pivot;
        rt.anchoredPosition += shift;
    }

    static void SetWidth(RectTransform rt, float width) => rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);

    /// <summary>Paneldeki gorev yazisinin ustunu cizecek uzunluk: Turkce'de ustteki ceviri etiketine gore.</summary>
    float StrikeWidth(string task, float english)
    {
        if (!Loc.Turkish) return english;
        foreach (var label in tasksPanel.GetComponentsInChildren<Text>())
            if (label.name == "TR_" + task)
                return Mathf.Min(label.preferredWidth, label.rectTransform.rect.width) * label.rectTransform.localScale.x + 10f;
        return english;
    }

    static void SetAlpha(Graphic g, float a)
    {
        if (!g) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
