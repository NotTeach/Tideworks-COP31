using System;
using System.Collections;
using System.Collections.Generic;
using Tideworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 3. gun kumsali, sezon finali "Buyuk Gece" (Day3Beach; tasarim 34-41.png). Bu sefer oyuncu yalniz degil:
/// Hatice Teyze, Elif ve Mert kumsalda. Sag ustte 4 maddelik gorev listesi, sol altta konusma kutusu.
/// 1 Gun batimi: Hatice Teyze konusur ("Let's go!"). 2 "Who does what?": gorev secilir, sonra dogru yardimciya dokunulur
///   (ya da surukle-birak); dogru eslesme +5, yanlista yardimci kendisi soyler, basinin ustunde gorev etiketi belirir.
/// 3 Amber lamba dukkandan alindiysa sokak lambasina takilir (+10), alinmadiysa Mert "ay isigi yeter" der.
/// 4 Gece: yuvalar kipirdar, ruzgar kiyiya 2 poset ve 1 ag parcasi getirir; oyuncu toplar (+5). Sonra 30 yavru denize kosar.
/// 5 Umut (2. gunde kurtarilan caretta) dalgalarin arasinda belirir. 6 Dedenin defterine son not. 7 Sezon sonu karti, kapanis.
/// </summary>
public class Day3BeachController : MonoBehaviour
{
    [SerializeField] string menuSceneName = "MainMenu";
    [Tooltip("\"Play the season again\": yeni sezon giristen baslar")]
    [SerializeField] string introSceneName = "Intro";
    [SerializeField] CanvasGroup screenFader;
    [SerializeField] int jobReward = 5, lampReward = 10, trashReward = 5;
    [SerializeField] int hatchlingsPerNest = 10;
    [Tooltip("Yavrularin denize girdigi cizgi (artboard y)")]
    [SerializeField] float seaLine = 650f;

    [Header("Konusma kutusu")]
    [SerializeField] Image dialogPortrait;
    [SerializeField] Text dialogName, dialogText;
    [SerializeField] Sprite[] portraits;                    // 0 Hatice, 1 Elif, 2 Mert
    [SerializeField] Image dialogButton;
    [SerializeField] Text dialogButtonText;
    [SerializeField] Sprite buttonNarrow, buttonWide;
    [SerializeField] Text coinText;

    [Header("Gorev listesi")]
    [SerializeField] Image[] taskChecks;                    // 0 isler, 1 isik, 2 yol, 3 yavrular
    [SerializeField] Text[] taskLabels, taskCounts;
    [SerializeField] Image[] taskStrikes, taskTicks;
    [SerializeField] Sprite checkOn;

    [Header("Kim ne yapar?")]
    [SerializeField] GameObject jobsPanel;
    [SerializeField] DragChip[] chips;                      // 0 copler (Mert), 1 isiklar (Hatice), 2 cukurlar (Elif)
    [SerializeField] Image[] chipImages;
    [SerializeField] Sprite chipNormal, chipSelected;
    [SerializeField] RectTransform[] helpers;               // 0 Hatice, 1 Elif, 2 Mert
    [SerializeField] GameObject[] helperPills;
    [SerializeField] Text[] helperPillTexts;
    [SerializeField] Image[] helperPillTicks;

    [Header("Lamba")]
    [SerializeField] GameObject lampHint;
    [SerializeField] Image lampGlow, lampBulb;

    [Header("Gece")]
    [SerializeField] Image night;
    [SerializeField] Image[] nests;
    [SerializeField] Color nestNightTint = new Color(0.5f, 0.53f, 0.62f);
    [SerializeField] RectTransform hatchLayer;
    [SerializeField] Image hatchlingTemplate;
    [SerializeField] Button[] shoreTrash;                   // 2 poset, 1 ag parcasi (halkalariyla)
    [SerializeField] RectTransform umut;
    [SerializeField] GameObject umutLabel;
    [SerializeField] Text umutName;

    [Header("Defter ve sezon sonu")]
    [SerializeField] GameObject notebook;
    [SerializeField] RectTransform notebookCard;
    [SerializeField] Text notebookText;
    [SerializeField] GameObject notebookButton;
    [SerializeField] GameObject seasonCard;
    [SerializeField] RectTransform seasonCardRect;
    [SerializeField] Text statRepaired, statFish, statTurtles, statCoins;
    [Tooltip("Kapanis sayfasi: \"The story isn't over...\"")]
    [SerializeField] CanvasGroup epiloguePage;

    static readonly Color[] NameColors = { new Color32(52, 110, 40, 255), new Color32(180, 72, 40, 255), new Color32(23, 119, 122, 255) };
    static readonly string[] Names = { "Hatice Teyze", "Elif", "Mert" };
    static readonly int[] RightHelper = { 2, 0, 1 };       // gorev -> dogru yardimci
    static readonly string[] Pills = { "Scooping trash", "On the radio", "Filling holes" };
    static readonly string[] RightLines =
    {
        "On it! My net is ready for the last bits of trash.",
        "I'll tell every café on the radio: lights off by the shore!",
        "I love the sand! Pamuk and I will fill every hole.",
    };
    // [yardimci, gorev] yanlis eslesmede yardimcinin sozu
    static readonly string[,] WrongLines =
    {
        { "My back can't bend that far, dear! Mert has a net.", null,
          "Little hands are better in the sand. Elif loves playing in it!" },
        { "Mert has the net, not me!", "I don't have a radio! Shall we give this to my grandma?", null },
        { null, "A radio? I only have a compass!", "Elif is the sand expert, not me!" },
    };

    readonly bool[] assigned = new bool[3];
    readonly List<RectTransform> peeking = new List<RectTransform>();
    int selectedJob = -1, jobsDone, trashLeft, reached;
    bool busy, leaving, lampPlaced;
    Action onButton;
    static int startCoins = -1, startTrash, startEarned;

    void Awake()
    {
        foreach (var b in GetComponentsInChildren<Button>(true))
            if (!b.GetComponent<TwButonSesi>()) b.gameObject.AddComponent<TwButonSesi>();
        for (int i = 0; i < chips.Length; i++)
        {
            chips[i].index = i;
            chips[i].Clicked = SelectJob;
            chips[i].Dropped = DropJob;
        }
        for (int i = 0; i < shoreTrash.Length; i++) { int k = i; shoreTrash[i].onClick.AddListener(() => PickTrash(k)); }
    }

    void OnEnable() => PlayerWallet.OnChanged += OnCoins;
    void OnDisable() => PlayerWallet.OnChanged -= OnCoins;
    void OnCoins(int c) => coinText.text = c.ToString();

    void Start()
    {
        Time.timeScale = 1f;
        // Restart sahneyi yeniden yukler: bu sahnede kazanilanlar geri alinir, odul tekrar kasilamaz
        if (startCoins < 0) { startCoins = GameProgress.Coins; startTrash = GameProgress.TrashCollected; startEarned = GameProgress.CoinsEarned; }
        coinText.text = PlayerWallet.Coins.ToString();
        jobsPanel.SetActive(false);
        for (int i = 0; i < helperPills.Length; i++) { helperPills[i].SetActive(false); helperPillTicks[i].gameObject.SetActive(false); }
        for (int i = 0; i < taskChecks.Length; i++)
        {
            taskStrikes[i].gameObject.SetActive(false);
            taskTicks[i].gameObject.SetActive(false);
        }
        taskCounts[0].text = "0/3";
        taskCounts[1].text = "";
        taskCounts[2].text = "0/3";
        taskCounts[3].text = "";
        lampHint.SetActive(false);
        SetAlpha(lampGlow, 0f);
        SetAlpha(lampBulb, 0f);
        SetAlpha(night, 0f);
        hatchlingTemplate.gameObject.SetActive(false);
        foreach (var t in shoreTrash) t.gameObject.SetActive(false);
        umut.gameObject.SetActive(false);
        umutLabel.SetActive(false);
        notebook.SetActive(false);
        seasonCard.SetActive(false);
        TwSes.EkranGecisi(Ses.MuzikAkdeniz, Ses.AmbiyansDeniz, null);
        if (screenFader) StartCoroutine(FadeIn());
        Say(0, "The whole town switched off its lights tonight. All thanks to you, dear. The nests will hatch after dark. Shall we get ready together?",
            "Let's go!", OpenJobs);
    }

    // ---------------- Konusma kutusu ----------------

    void Say(int who, string text, string button = null, Action action = null, bool translate = true)
    {
        dialogPortrait.sprite = portraits[who];
        dialogName.text = Loc.T(Names[who]);
        dialogName.color = NameColors[who];
        dialogText.text = translate ? Loc.T(text) : text;
        onButton = action;
        bool show = !string.IsNullOrEmpty(button);
        dialogButton.gameObject.SetActive(show);
        bool wide = show && button.Length > 12;
        float textWidth = !show ? 1240f : wide ? 680f : 990f;
        dialogText.rectTransform.sizeDelta = new Vector2(textWidth, dialogText.rectTransform.sizeDelta.y);
        dialogText.rectTransform.anchoredPosition = new Vector2(230f + textWidth / 2f, dialogText.rectTransform.anchoredPosition.y);
        if (!show) return;
        dialogButton.sprite = wide ? buttonWide : buttonNarrow;
        var rt = dialogButton.rectTransform;
        float w = wide ? 540f : 224f;
        rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
        rt.anchoredPosition = new Vector2(1476f - w / 2f, rt.anchoredPosition.y);
        dialogButtonText.rectTransform.sizeDelta = new Vector2(w - 24f, dialogButtonText.rectTransform.sizeDelta.y);
        dialogButtonText.text = Loc.T(button);
    }

    /// <summary>Konusma kutusundaki buton ("Let's go!", "Wait for night", "Open Grandpa's notebook").</summary>
    public void DialogButton()
    {
        var a = onButton;
        if (a == null) return;
        onButton = null;
        dialogButton.gameObject.SetActive(false);
        a();
    }

    // ---------------- Gorev listesi ----------------

    void CompleteTask(int i, bool tick = false)
    {
        taskChecks[i].sprite = checkOn;
        var c = taskLabels[i].color;
        c.a = 0.55f;
        taskLabels[i].color = c;
        var s = taskStrikes[i].rectTransform;
        s.sizeDelta = new Vector2(taskLabels[i].preferredWidth, s.sizeDelta.y);
        s.anchoredPosition = new Vector2(taskLabels[i].rectTransform.anchoredPosition.x - taskLabels[i].rectTransform.sizeDelta.x / 2f
                                         + s.sizeDelta.x / 2f, s.anchoredPosition.y);
        taskStrikes[i].gameObject.SetActive(true);
        if (tick) { taskCounts[i].text = ""; taskTicks[i].gameObject.SetActive(true); }
        TwSes.Efekt(Ses.Onay, 0.7f);
        var rt = taskChecks[i].rectTransform;
        StartCoroutine(Tween(0.3f, t => rt.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * Mathf.PI))));
    }

    // ---------------- Kim ne yapar? ----------------

    void OpenJobs()
    {
        Say(2, "Three jobs, three helpers. Who does what? Pick a job, then tap one of us!");
        jobsPanel.SetActive(true);
        var p = (RectTransform)jobsPanel.transform;
        StartCoroutine(Tween(0.35f, t => p.localScale = Vector3.one * EaseOutBack(t)));
    }

    void SelectJob(DragChip chip)
    {
        if (busy) return;
        selectedJob = selectedJob == chip.index ? -1 : chip.index;
        RefreshChips();
        TwSes.Efekt(Ses.Tik);
    }

    void RefreshChips()
    {
        for (int i = 0; i < chipImages.Length; i++) chipImages[i].sprite = i == selectedJob ? chipSelected : chipNormal;
    }

    bool DropJob(DragChip chip, Vector2 screen)
    {
        if (busy) return false;
        for (int h = 0; h < helpers.Length; h++)
            if (RectTransformUtility.RectangleContainsScreenPoint(helpers[h], screen, null))
                return Assign(chip.index, h);
        return false;
    }

    /// <summary>Karaktere dokunuldu: once is secilir, sonra yardimci.</summary>
    public void ClickHelper(int h)
    {
        if (busy || !jobsPanel.activeSelf) return;
        if (selectedJob < 0)
        {
            Say(2, "First pick a job from the list, then tap one of us!");
            return;
        }
        if (Assign(selectedJob, h)) selectedJob = -1;
        RefreshChips();
    }

    bool Assign(int job, int helper)
    {
        if (assigned[job]) return false;
        if (RightHelper[job] != helper)
        {
            TwSes.Efekt(Ses.Hata, 0.6f);
            Say(helper, WrongLines[helper, job]);
            StartCoroutine(Shake(helpers[helper]));
            return false;
        }
        assigned[job] = true;
        jobsDone++;
        chips[job].Locked = true;
        chips[job].gameObject.SetActive(false);
        helperPills[helper].SetActive(true);
        helperPillTexts[helper].text = Loc.T(Pills[job]);
        var pill = (RectTransform)helperPills[helper].transform;
        StartCoroutine(Tween(0.3f, t => pill.localScale = Vector3.one * EaseOutBack(t)));
        PlayerWallet.Add(jobReward);
        TwSes.Efekt(Ses.Onay);
        Say(helper, RightLines[job]);
        taskCounts[0].text = jobsDone + "/3";
        var hr = helpers[helper];
        StartCoroutine(Tween(0.3f, t => hr.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * Mathf.PI))));
        if (jobsDone == chips.Length) StartCoroutine(JobsDone());
        return true;
    }

    IEnumerator JobsDone()
    {
        busy = true;
        CompleteTask(0);
        yield return Wait(1.8f);
        var p = (RectTransform)jobsPanel.transform;
        yield return Tween(0.25f, t => p.localScale = Vector3.one * (1f - t));
        jobsPanel.SetActive(false);
        if (ShopManager.Owns("amber_lamp"))
        {
            Say(0, "You brought the amber lamp! Tap the street lamp to fit it: warm light for people, not for turtles.");
            lampHint.SetActive(true);
            busy = false;
            yield break;
        }
        Say(2, "No amber lamp? No problem, moonlight is enough! The turtles will find the sea.");
        CompleteTask(1, tick: true);
        yield return Wait(2.6f);
        ReadyForNight();
    }

    // ---------------- Amber lamba ----------------

    /// <summary>Sokak lambasina amber lamba takilir.</summary>
    public void PlaceLamp()
    {
        if (busy || lampPlaced || !lampHint.activeSelf) return;
        lampPlaced = true;
        busy = true;
        lampHint.SetActive(false);
        PlayerWallet.Add(lampReward);
        TwSes.Efekt(Ses.ParcaTak);
        StartCoroutine(LampOn());
    }

    IEnumerator LampOn()
    {
        yield return Tween(0.8f, t => { SetAlpha(lampBulb, t); SetAlpha(lampGlow, 0.85f * t); });
        CompleteTask(1, tick: true);
        Say(0, "A warm amber light! People can see the path, and the hatchlings won't get confused. +10");
        yield return Wait(2.6f);
        ReadyForNight();
    }

    void ReadyForNight()
    {
        busy = false;
        Say(0, "Now we wait for the dark. Everyone ready?", "Wait for night", () => StartCoroutine(Night()));
    }

    // ---------------- Gece ----------------

    IEnumerator Night()
    {
        busy = true;
        TwSes.Efekt(Ses.GecisParilti, 0.7f);
        yield return Tween(2.4f, t =>
        {
            float k = Mathf.SmoothStep(0, 1, t);
            SetAlpha(night, k);
            foreach (var n in nests) n.color = Color.Lerp(Color.white, nestNightTint, k);
        });
        // Yuvalar kipirdar, yavrular basini cikarir; ruzgar kiyiya cop getirir
        TwSes.Efekt(Ses.Pop, 0.7f);
        for (int i = 0; i < nests.Length; i++)
            for (int k = 0; k < 4; k++)
            {
                var b = NewHatchling(NestTop(i) + new Vector2(-60f + k * 34f, (k % 2) * 6f));
                b.localScale = Vector3.one * 0.8f;
                peeking.Add(b);
            }
        trashLeft = shoreTrash.Length;
        foreach (var t in shoreTrash)
        {
            t.gameObject.SetActive(true);
            var rt = (RectTransform)t.transform;
            StartCoroutine(Tween(0.4f, k => rt.localScale = Vector3.one * EaseOutBack(k)));
        }
        Say(0, "The sand is moving! But the wind brought two bags and a piece of net to the shore. Pick them up quickly!");
        busy = false;
        StartCoroutine(Wiggle());
    }

    Vector2 NestTop(int i) => nests[i].rectTransform.anchoredPosition + new Vector2(-24f, 12f);

    RectTransform NewHatchling(Vector2 at)
    {
        var img = Instantiate(hatchlingTemplate, hatchLayer);
        img.name = "Hatchling";
        img.gameObject.SetActive(true);
        img.rectTransform.anchoredPosition = at;
        return img.rectTransform;
    }

    IEnumerator Wiggle()
    {
        while (peeking.Count > 0)
        {
            for (int i = 0; i < peeking.Count; i++)
                if (peeking[i]) peeking[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 9f + i) * 14f);
            for (int i = 0; i < nests.Length; i++)
                nests[i].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 18f + i) * 1.5f);
            yield return null;
        }
        foreach (var n in nests) n.rectTransform.localRotation = Quaternion.identity;
    }

    void PickTrash(int i)
    {
        var b = shoreTrash[i];
        if (busy || !b.interactable) return;
        b.interactable = false;
        trashLeft--;
        PlayerWallet.Add(trashReward);
        GameProgress.TrashCollected += 1;
        TwSes.EfektCesitli(Ses.AtikTopla);
        taskCounts[2].text = (3 - trashLeft) + "/3";
        var rt = (RectTransform)b.transform;
        var start = rt.anchoredPosition;
        var group = b.GetComponent<CanvasGroup>();
        StartCoroutine(Tween(0.45f, t =>
        {
            rt.anchoredPosition = start + new Vector2(0, 80f * t);
            group.alpha = 1f - t;
        }));
        if (trashLeft == 0) StartCoroutine(PathClear());
    }

    IEnumerator PathClear()
    {
        busy = true;
        CompleteTask(2);
        for (int i = 0; i < helperPillTicks.Length; i++) helperPillTicks[i].gameObject.SetActive(true);
        yield return Wait(0.6f);
        // 30 yavru denize kosar
        TwSes.Efekt(Ses.GecisParilti);
        Say(1, "Thirty of them! Look how fast they run to the sea!");
        var rng = new System.Random(30);
        int total = nests.Length * hatchlingsPerNest;
        reached = 0;
        taskCounts[3].text = "0";
        for (int n = 0; n < nests.Length; n++)
            for (int i = 0; i < hatchlingsPerNest; i++)
            {
                RectTransform rt;
                int slot = n * 4 + i;
                if (i < 4 && slot < peeking.Count) rt = peeking[slot];
                else rt = null;
                var start = NestTop(n) + new Vector2((float)rng.NextDouble() * 140f - 70f, (float)rng.NextDouble() * 24f);
                float speed = Mathf.Lerp(150f, 230f, (float)rng.NextDouble());
                float delay = i < 4 ? 0f : (float)rng.NextDouble() * 2.6f;
                float drift = (float)rng.NextDouble() * 120f - 60f;
                StartCoroutine(Run(rt, start, speed, delay, drift, (float)rng.NextDouble() * 6f));
            }
        peeking.Clear();
        while (reached < total) yield return null;
        CompleteTask(3);
        yield return Wait(1f);
        yield return Umut();
    }

    IEnumerator Run(RectTransform rt, Vector2 start, float speed, float delay, float drift, float phase)
    {
        yield return Wait(delay);
        if (!rt) rt = NewHatchling(start);
        var img = rt.GetComponent<Image>();
        var p = rt.anchoredPosition;
        Vector2 home = p;
        float scale = rt.localScale.x;
        while (p.y < -seaLine)
        {
            float t = Time.time * 10f + phase;
            p += new Vector2(drift * Time.deltaTime + Mathf.Sin(t) * 0.8f, speed * Time.deltaTime);
            rt.anchoredPosition = p;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * 14f);
            rt.localScale = Vector3.one * Mathf.Lerp(scale, 0.7f, Mathf.InverseLerp(home.y, -seaLine, p.y));
            yield return null;
        }
        reached++;
        taskCounts[3].text = reached.ToString();
        yield return Tween(0.35f, t => SetAlpha(img, 1f - t));
        Destroy(rt.gameObject);
    }

    // ---------------- Umut ----------------

    IEnumerator Umut()
    {
        umut.gameObject.SetActive(true);
        var img = umut.GetComponent<Image>();
        Vector2 home = umut.anchoredPosition;
        TwSes.Efekt(Ses.GecisDalga, 0.8f);
        yield return Tween(1.4f, t =>
        {
            SetAlpha(img, t);
            umut.anchoredPosition = home + new Vector2(0, -50f * (1f - t));
        });
        umutName.text = Day2BeachController.CarettaName;
        umutLabel.SetActive(true);
        Say(1, Loc.F("Wait... that big one in the waves... it's {0}! The caretta we saved from the net! It came back to swim with the babies!",
            Day2BeachController.CarettaName.ToUpper()), "Open Grandpa's notebook", () => StartCoroutine(Notebook()), translate: false);
        busy = false;
        StartCoroutine(Bob(home));
    }

    IEnumerator Bob(Vector2 home)
    {
        while (umut.gameObject.activeSelf && !notebook.activeSelf)
        {
            umut.anchoredPosition = home + new Vector2(Mathf.Sin(Time.time * 0.8f) * 14f, Mathf.Sin(Time.time * 1.6f) * 6f);
            umut.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 0.8f) * 4f);
            yield return null;
        }
    }

    // ---------------- Defter ve sezon sonu ----------------

    IEnumerator Notebook()
    {
        busy = true;
        // Umut yavrularla acik denize gider
        var img = umut.GetComponent<Image>();
        Vector2 from = umut.anchoredPosition;
        umutLabel.SetActive(false);
        yield return Tween(1.2f, t =>
        {
            umut.anchoredPosition = from + new Vector2(0, 110f * t);
            umut.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, t);
            SetAlpha(img, 1f - t);
        });
        umut.gameObject.SetActive(false);
        dialogText.text = "";
        notebook.SetActive(true);
        notebookButton.SetActive(false);
        notebookText.text = "";
        TwSes.Efekt(Ses.GecisParilti, 0.6f);
        yield return Tween(0.4f, t => notebookCard.localScale = Vector3.one * EaseOutBack(t));
        string note = Loc.T("Grandpa, we saved the beach. The fish came back, the turtles reached the sea, and Umut came to say goodbye. The workshop will stay open.");
        note = note.Replace("Umut", Day2BeachController.CarettaName);
        for (float shown = 0; shown < note.Length; shown += 26f * Time.unscaledDeltaTime)
        {
            notebookText.text = note.Substring(0, Mathf.Min(note.Length, (int)shown));
            yield return null;
        }
        notebookText.text = note;
        notebookButton.SetActive(true);
        busy = false;
    }

    /// <summary>"Close the notebook": sezon sonu karti.</summary>
    public void CloseNotebook()
    {
        if (seasonCard.activeSelf) return;
        notebook.SetActive(false);
        statRepaired.text = Loc.T("Radio · Pamuk · Compass");
        statFish.text = Loc.T("2 » 14 and a dolphin");
        statTurtles.text = Loc.F("{0} + every hatchling", Day2BeachController.CarettaName);
        statCoins.text = PlayerWallet.Coins.ToString();
        seasonCard.SetActive(true);
        TwSes.Efekt(Ses.OnarimTamam);
        StartCoroutine(Tween(0.35f, t => seasonCardRect.localScale = Vector3.one * EaseOutBack(t)));
    }

    /// <summary>Sezon kartindaki "Continue": sezon tamamlanir, kapanis sayfasi acilir.</summary>
    public void FinishSeason()
    {
        if (leaving || epiloguePage.gameObject.activeSelf) return;
        GameProgress.SeasonComplete = true;
        GameProgress.CompleteDay();
        GameSettings.Save();
        startCoins = -1;
        StartCoroutine(ShowEpilogue());
    }

    IEnumerator ShowEpilogue()
    {
        screenFader.gameObject.SetActive(true);
        screenFader.blocksRaycasts = true;
        yield return Tween(0.45f, t => screenFader.alpha = Mathf.SmoothStep(0f, 1f, t));
        seasonCard.SetActive(false);
        epiloguePage.gameObject.SetActive(true);
        TwSes.Efekt(Ses.GecisParilti, 0.7f);
        yield return FadeIn();
    }

    /// <summary>Kapanis sayfasi: "Main menu".</summary>
    public void EpilogueMenu() => BackToMenu();

    /// <summary>Kapanis sayfasi: "Play the season again": yeni oyun, giris sahnesinden.</summary>
    public void PlaySeasonAgain()
    {
        if (leaving) return;
        leaving = true;
        startCoins = -1;
        GameProgress.NewGame();
        StartCoroutine(FadeOutAndLoad(introSceneName));
    }

    /// <summary>Ust bardaki "Restart": sahne bastan, bu sahnede kazanilan altin ve copler geri alinir.</summary>
    public void Restart()
    {
        if (leaving) return;
        leaving = true;
        if (startCoins >= 0 && !GameProgress.SeasonComplete)
        {
            GameProgress.Coins = startCoins;
            GameProgress.TrashCollected = startTrash;
            GameProgress.CoinsEarned = startEarned;
        }
        StartCoroutine(FadeOutAndLoad(SceneManager.GetActiveScene().name));
    }

    public void BackToMenu()
    {
        if (leaving) return;
        leaving = true;
        startCoins = -1;
        StartCoroutine(FadeOutAndLoad(menuSceneName));
    }

    // ---------------- Yardimcilar ----------------

    IEnumerator Shake(RectTransform rt)
    {
        var home = rt.anchoredPosition;
        yield return Tween(0.35f, t => rt.anchoredPosition = home + new Vector2(Mathf.Sin(t * 60f) * 12f * (1f - t), 0));
        rt.anchoredPosition = home;
    }

    IEnumerator FadeIn()
    {
        screenFader.gameObject.SetActive(true);
        yield return Tween(0.45f, t => screenFader.alpha = 1f - Mathf.SmoothStep(0f, 1f, t));
        screenFader.gameObject.SetActive(false);
    }

    IEnumerator FadeOutAndLoad(string scene)
    {
        GameSettings.Save();
        if (screenFader)
        {
            screenFader.gameObject.SetActive(true);
            screenFader.blocksRaycasts = true;
            yield return Tween(0.45f, t => screenFader.alpha = Mathf.SmoothStep(0f, 1f, t));
        }
        SceneManager.LoadScene(scene);
    }

    static IEnumerator Wait(float s)
    {
        for (float t = 0f; t < s; t += Time.unscaledDeltaTime) yield return null;
    }

    static void SetAlpha(Graphic g, float a)
    {
        if (!g) return;
        var c = g.color;
        c.a = a;
        g.color = c;
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
