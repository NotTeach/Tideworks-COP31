using System;
using System.Collections;
using Tideworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 2. gun kumsali (Day2Beach): gun batiminda kayalik koyda hayalet aga dolanmis caretta.
/// Akis: 3 dugumu makasla kes (+5 her biri) -> isim sec (Umut / Mavi / Yusuf) -> caretta denize yurur, Elif konusur
/// -> "Wait for night": ekran kararir, ay doğar, Hatice Teyze radyoda poşet uyarisi yapar -> yuvayla deniz arasindaki
/// 2 poseti topla -> 10 yavru denize kosar -> gun sonu karti (+30, "Start Day 3" / "Season map").
/// </summary>
public class Day2BeachController : MonoBehaviour
{
    [SerializeField] string menuSceneName = "MainMenu";
    [SerializeField] string nextDaySceneName = "Workshop";
    [SerializeField] CanvasGroup screenFader;
    [SerializeField] int knotReward = 5;
    [SerializeField] int dayReward = 30;
    [SerializeField] int hatchlings = 10;
    [Tooltip("Denizin kiyi cizgisi (artboard y)")]
    [SerializeField] float seaLine = 640f;

    [Header("Caretta")]
    [SerializeField] RectTransform turtle;
    [SerializeField] Button[] knots;
    [SerializeField] Image[] netParts;
    [SerializeField] RectTransform scissors;

    [Header("Isim")]
    [SerializeField] GameObject nameOverlay;
    [SerializeField] RectTransform nameCard;
    [SerializeField] Text nameTitle;

    [Header("Gece")]
    [SerializeField] GameObject nightButton;
    [SerializeField] Image night;
    [SerializeField] float nightAlpha = 0.62f;
    [SerializeField] RectTransform moon;
    [SerializeField] GameObject radio;
    [SerializeField] Text radioText;
    [SerializeField] RectTransform nest;
    [SerializeField] Button[] bags;
    [SerializeField] Image hatchlingTemplate;

    [Header("Arayuz")]
    [SerializeField] Text tooltip;
    [SerializeField] Text coinText;

    [Header("Gun sonu karti")]
    [SerializeField] GameObject completeOverlay;
    [SerializeField] RectTransform completeCard;
    [SerializeField] Text completeKicker;
    [SerializeField] Text completeTitle;
    [SerializeField] Text hatchlingsText;
    [SerializeField] Text healthText;
    [SerializeField] Text rewardText;
    [SerializeField] Text nextDayLabel;

    public const string NameKey = "caretta.name";
    static readonly string[] Names = { "Umut", "Mavi", "Yusuf" };

    readonly bool[] cut = new bool[3];
    int bagsCollected;
    bool busy, freed, leaving, rewarded;
    Vector2 turtleHome;

    /// <summary>Oyuncunun carettaya verdigi isim (secilmediyse "Umut").</summary>
    public static string CarettaName => PlayerPrefs.GetString(NameKey, Names[0]);

    void Awake()
    {
        for (int i = 0; i < knots.Length; i++) { int k = i; knots[i].onClick.AddListener(() => Cut(k)); }
        for (int i = 0; i < bags.Length; i++) { int k = i; bags[i].onClick.AddListener(() => PickBag(k)); }
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (!button.GetComponent<TwButonSesi>())
                button.gameObject.AddComponent<TwButonSesi>();
    }

    void OnEnable() => PlayerWallet.OnChanged += OnCoins;

    void OnDisable() => PlayerWallet.OnChanged -= OnCoins;

    void OnCoins(int coins) => coinText.text = coins.ToString();

    void Start()
    {
        Time.timeScale = 1f;
        turtleHome = turtle.anchoredPosition;
        scissors.gameObject.SetActive(false);
        nameOverlay.SetActive(false);
        nightButton.SetActive(false);
        SetAlpha(night, 0f);
        moon.gameObject.SetActive(false);
        radio.SetActive(false);
        nest.gameObject.SetActive(false);
        foreach (var b in bags) b.gameObject.SetActive(false);
        hatchlingTemplate.gameObject.SetActive(false);
        completeOverlay.SetActive(false);
        coinText.text = PlayerWallet.Coins.ToString();
        Say("Elif: Look! A caretta is tangled in a ghost net! Cut the knots with the scissors.");
        TwSes.EkranGecisi(Ses.MuzikAnaTema, Ses.AmbiyansDeniz, null);
        if (screenFader) StartCoroutine(FadeIn());
    }

    void Update()
    {
        // Agdaki caretta kipirdanir
        if (freed) return;
        float t = Time.time;
        turtle.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 5f) * 3f * (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 0.7f))));
        turtle.anchoredPosition = turtleHome + new Vector2(Mathf.Sin(t * 9f) * 2f, 0);
    }

    // ---------------- Dugumler ----------------

    void Cut(int i)
    {
        if (busy || cut[i]) return;
        StartCoroutine(CutRoutine(i));
    }

    IEnumerator CutRoutine(int i)
    {
        busy = true;
        cut[i] = true;
        var knot = (RectTransform)knots[i].transform;
        // Makas dugume gider, iki kez kapanir
        Vector2 target = (Vector2)scissors.parent.InverseTransformPoint(knot.position);
        Vector2 from = target + new Vector2(260f, 200f);
        scissors.gameObject.SetActive(true);
        yield return Tween(0.25f, t => scissors.anchoredPosition = Vector2.Lerp(from, target, EaseOut(t)));
        for (int k = 0; k < 2; k++)
        {
            TwSes.Efekt(Ses.Tik);
            yield return Tween(0.1f, t => scissors.localRotation = Quaternion.Euler(0, 0, -18f * t));
            yield return Tween(0.1f, t => scissors.localRotation = Quaternion.Euler(0, 0, -18f * (1f - t)));
        }
        scissors.gameObject.SetActive(false);

        PlayerWallet.Add(knotReward);
        TwSes.Efekt(Ses.AtikTopla, 0.8f);
        knots[i].gameObject.SetActive(false);
        var part = netParts[i];
        var start = part.rectTransform.anchoredPosition;
        yield return Tween(0.5f, t =>
        {
            part.rectTransform.anchoredPosition = start + new Vector2(0, -90f * t * t);
            SetAlpha(part, 1f - t);
        });
        part.gameObject.SetActive(false);

        int done = Array.FindAll(cut, c => c).Length;
        string[] lines =
        {
            "Snip! The flipper is free. +5",
            "Snip! The neck is free, it can breathe easy now. +5",
            "Snip! The shell is free. The net is gone! +5",
        };
        Say(lines[done - 1]);
        busy = false;
        if (done == cut.Length) StartCoroutine(Freed());
    }

    IEnumerator Freed()
    {
        busy = true;
        freed = true;
        turtle.localRotation = Quaternion.identity;
        TwSes.Efekt(Ses.OnarimTamam);
        yield return new WaitForSeconds(0.8f);
        nameTitle.text = Loc.T("Give your caretta a name!");
        nameOverlay.SetActive(true);
        yield return Tween(0.35f, t => nameCard.localScale = Vector3.one * EaseOutBack(t));
    }

    /// <summary>Isim butonlari (0 Umut, 1 Mavi, 2 Yusuf).</summary>
    public void ChooseName(int i)
    {
        if (!nameOverlay.activeSelf) return;
        PlayerPrefs.SetString(NameKey, Names[Mathf.Clamp(i, 0, Names.Length - 1)]);
        PlayerPrefs.Save();
        TwSes.Efekt(Ses.Onay);
        nameOverlay.SetActive(false);
        StartCoroutine(Farewell());
    }

    IEnumerator Farewell()
    {
        Say(Loc.F("Elif: Just like Pamuk, you're free from the net too! Bye, {0}!", CarettaName), false);
        // Caretta denize dogru yurur ve dalgalarda kaybolur
        var img = turtle.GetComponent<Image>();
        Vector2 from = turtle.anchoredPosition, to = new Vector2(from.x + 80f, -seaLine + 120f);
        yield return Tween(4f, t =>
        {
            turtle.anchoredPosition = Vector2.Lerp(from, to, t);
            turtle.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 30f) * 6f);
            turtle.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, t);
            SetAlpha(img, t < 0.75f ? 1f : (1f - t) / 0.25f);
        });
        turtle.gameObject.SetActive(false);
        TwSes.Efekt(Ses.GecisDalga, 0.8f);
        yield return new WaitForSeconds(0.6f);
        Say("The sun is setting. Tonight a nest on this beach will hatch.");
        nightButton.SetActive(true);
        busy = false;
    }

    // ---------------- Gece ----------------

    /// <summary>"Wait for night" butonu.</summary>
    public void WaitForNight()
    {
        if (busy) return;
        busy = true;
        nightButton.SetActive(false);
        StartCoroutine(NightRoutine());
    }

    IEnumerator NightRoutine()
    {
        TwSes.Efekt(Ses.GecisParilti, 0.7f);
        moon.gameObject.SetActive(true);
        var moonFrom = moon.anchoredPosition + new Vector2(0, -260f);
        var moonTo = moon.anchoredPosition;
        yield return Tween(2.2f, t =>
        {
            SetAlpha(night, nightAlpha * Mathf.SmoothStep(0f, 1f, t));
            moon.anchoredPosition = Vector2.Lerp(moonFrom, moonTo, Mathf.SmoothStep(0f, 1f, t));
        });
        nest.gameObject.SetActive(true);
        TwSes.Efekt(Ses.RadyoAcilis, 0.7f);
        radioText.text = Loc.T("Hatice Teyze (radio): Turtles think plastic bags are jellyfish and swallow them! Pick up the bags between the nest and the sea.");
        radio.SetActive(true);
        Say("Two bags lie between the nest and the sea. Tap them to pick them up!");
        foreach (var b in bags)
        {
            b.gameObject.SetActive(true);
            var rt = (RectTransform)b.transform;
            StartCoroutine(Tween(0.3f, t => rt.localScale = Vector3.one * EaseOutBack(t)));
        }
        busy = false;
    }

    void PickBag(int i)
    {
        if (busy || !bags[i].gameObject.activeSelf) return;
        bags[i].interactable = false;
        bagsCollected++;
        TwSes.Efekt(Ses.AtikTopla);
        var rt = (RectTransform)bags[i].transform;
        var g = bags[i].targetGraphic;
        var start = rt.anchoredPosition;
        StartCoroutine(Tween(0.4f, t =>
        {
            rt.anchoredPosition = start + new Vector2(0, 80f * t);
            SetAlpha(g, 1f - t);
        }, () => rt.gameObject.SetActive(false)));
        if (bagsCollected < bags.Length)
        {
            Say("One bag gone. One more!");
            return;
        }
        Say("The path is clear. Here they come!");
        busy = true;
        StartCoroutine(Hatch());
    }

    // ---------------- Yavrular ----------------

    IEnumerator Hatch()
    {
        yield return new WaitForSeconds(1f);
        radio.SetActive(false);
        TwSes.Efekt(Ses.GecisParilti);
        var rng = new System.Random(10);
        var home = nest.anchoredPosition;
        float longest = 0f;
        for (int i = 0; i < hatchlings; i++)
        {
            var start = home + new Vector2((float)rng.NextDouble() * 120f - 60f, (float)rng.NextDouble() * 20f);
            float speed = Mathf.Lerp(120f, 180f, (float)rng.NextDouble());
            float delay = (float)rng.NextDouble() * 1.6f;
            longest = Mathf.Max(longest, delay + (-seaLine - start.y) / speed);
            StartCoroutine(Walk(start, speed, delay, (float)rng.NextDouble() * 6f));
        }
        yield return new WaitForSeconds(longest + 0.8f);
        TwSes.Efekt(Ses.OnarimTamam);
        ShowDayCard();
    }

    IEnumerator Walk(Vector2 start, float speed, float delay, float phase)
    {
        yield return new WaitForSeconds(delay);
        var img = Instantiate(hatchlingTemplate, hatchlingTemplate.transform.parent);
        img.name = "Hatchling";
        img.gameObject.SetActive(true);
        var rt = img.rectTransform;
        var p = start;
        float drift = UnityEngine.Random.Range(-30f, 30f);
        while (p.y < -seaLine)
        {
            float t = Time.time * 7f + phase;
            p += new Vector2(drift * Time.deltaTime + Mathf.Sin(t) * 0.6f, speed * Time.deltaTime);
            rt.anchoredPosition = p;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * 12f);
            SetAlpha(img, 1f - Mathf.InverseLerp(-seaLine - 60f, -seaLine, p.y));
            yield return null;
        }
        Destroy(img.gameObject);
    }

    void ShowDayCard()
    {
        if (!rewarded)
        {
            rewarded = true;
            PlayerWallet.Add(dayReward);
            GameSettings.Save();
        }
        int day = GameProgress.CurrentDay;
        completeKicker.text = Loc.F("DAY {0} COMPLETE · ALL TASKS DONE", day);
        completeTitle.text = Loc.F("{0} is free and {1} hatchlings reached the sea!", CarettaName, hatchlings);
        hatchlingsText.text = hatchlings.ToString();
        healthText.text = GameProgress.OceanHealth + "%";
        rewardText.text = "+" + dayReward;
        nextDayLabel.text = day < GameProgress.TotalDays ? Loc.F("Start Day {0}", day + 1) : Loc.T("Finish the season");
        completeOverlay.SetActive(true);
        StartCoroutine(Tween(0.35f, t => completeCard.localScale = Vector3.one * EaseOutBack(t)));
    }

    /// <summary>"Start Day 3".</summary>
    public void StartNextDay()
    {
        if (leaving) return;
        leaving = true;
        bool last = GameProgress.CurrentDay >= GameProgress.TotalDays;
        GameProgress.CompleteDay();
        StartCoroutine(FadeOutAndLoad(last ? menuSceneName : nextDaySceneName));
    }

    /// <summary>"Season map": sezon haritasi henuz yok; gun biter ve ana menuye donulur.</summary>
    public void SeasonMap()
    {
        if (leaving) return;
        leaving = true;
        GameProgress.CompleteDay();
        StartCoroutine(FadeOutAndLoad(menuSceneName));
    }

    /// <summary>Ust bardaki menu butonu.</summary>
    public void BackToMenu()
    {
        if (leaving) return;
        leaving = true;
        StartCoroutine(FadeOutAndLoad(menuSceneName));
    }

    // ---------------- Yardimcilar ----------------

    void Say(string text, bool translate = true) => tooltip.text = translate ? Loc.T(text) : text;

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

    static void SetAlpha(Graphic g, float a)
    {
        if (!g) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

    static float EaseOutBack(float t)
    {
        const float c = 1.7f;
        float u = t - 1f;
        return 1f + (c + 1f) * u * u * u + c * u * u;
    }

    static IEnumerator Tween(float duration, Action<float> step, Action done = null)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            step(t / duration);
            yield return null;
        }
        step(1f);
        done?.Invoke();
    }
}
