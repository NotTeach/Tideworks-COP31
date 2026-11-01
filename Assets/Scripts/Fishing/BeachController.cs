using System;
using System.Collections;
using System.Collections.Generic;
using Tideworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gorev 3 - Carettalara yardim (aksam kumsali). Uc gorev:
/// isiklari sondur (3 lamba), yolu ac (sezlong, semsiye, cukur), yuvalari koru (3 kafes).
/// Her yardim +5 altin. Hepsi bitince "Let them hatch!": yavrular yuvalardan cikip denize yurur,
/// ardindan gun sonu karti (kurtarilan yuvalar, Hatice Teyze'nin radyo mesaji); "Start Day 2" yeni gunu baslatir.
/// </summary>
public class BeachController : MonoBehaviour
{
    [SerializeField] string menuSceneName = "MainMenu";
    [SerializeField] int reward = 5;

    [Header("Lambalar")]
    [SerializeField] Button[] lamps;
    [SerializeField] Graphic[] lampBulbs;
    [SerializeField] Graphic[] lampGlows;
    [SerializeField] Graphic[] lightPools;
    [SerializeField] Graphic darkness;
    [SerializeField] float darkAlpha = 0.32f;

    [Header("Yol")]
    [SerializeField] Button chair;
    [SerializeField] Button umbrella;
    [SerializeField] Button hole;
    [SerializeField] GameObject holeFilled;

    [Header("Yuvalar")]
    [SerializeField] Button[] nests;
    [SerializeField] RectTransform[] cages;
    [SerializeField] Image hatchlingTemplate;
    [SerializeField] int hatchlingsPerNest = 4;
    [SerializeField] float seaLine = 620f;

    [Header("Arayuz")]
    [SerializeField] Text tooltip;
    [SerializeField] Image[] taskChecks;
    [SerializeField] Text[] taskCounts;
    [SerializeField] Sprite checkOn, checkOff;
    [SerializeField] Text coinText;
    [SerializeField] GameObject hatchButton;
    [SerializeField] GameObject completeOverlay;
    [SerializeField] RectTransform completeCard;

    [Header("Gun sonu karti")]
    [Tooltip("Her yuvadan denize ulasan yavru (kartta yazan sayi)")]
    [SerializeField] int carettasPerNest = 12;
    [SerializeField] Text completeKicker;
    [SerializeField] Text completeTitle;
    [SerializeField] Text nestsSafeText;
    [SerializeField] Text beachReadyText;
    [SerializeField] Text earnedText;
    [SerializeField] Text waitingText;
    [SerializeField] Text nextDayLabel;

    readonly bool[] lampOff = new bool[3];
    readonly bool[] caged = new bool[3];
    bool chairGone, umbrellaGone, holeFilledDone, hatching, dayCounted;
    int earned;
    Vector2 chairHome, umbrellaHome;

    int LightsOff => Count(lampOff);
    int Protected => Count(caged);
    int Cleared => (chairGone ? 1 : 0) + (umbrellaGone ? 1 : 0) + (holeFilledDone ? 1 : 0);
    bool AllDone => LightsOff == 3 && Cleared == 3 && Protected == 3;

    void Awake()
    {
        chairHome = ((RectTransform)chair.transform).anchoredPosition;
        umbrellaHome = ((RectTransform)umbrella.transform).anchoredPosition;
        for (int i = 0; i < lamps.Length; i++) { int k = i; lamps[i].onClick.AddListener(() => ToggleLamp(k)); }
        for (int i = 0; i < nests.Length; i++) { int k = i; nests[i].onClick.AddListener(() => Protect(k)); }
        chair.onClick.AddListener(() => MoveAway(chair, ref chairGone, "Chair moved. The hatchlings' path is clear."));
        umbrella.onClick.AddListener(() => MoveAway(umbrella, ref umbrellaGone, "Umbrella packed away. Nothing in the way now."));
        hole.onClick.AddListener(FillHole);
    }

    /// <summary>Ayristirmadan sonra cagrilir.</summary>
    public void Begin()
    {
        ResetBeach();
    }

    public void Restart()
    {
        if (hatching) return;
        ResetBeach();
    }

    void ResetBeach()
    {
        StopAllCoroutines();
        earned = 0;
        hatching = false;
        for (int i = 0; i < 3; i++)
        {
            lampOff[i] = false;
            SetAlpha(lampBulbs[i], 1f); SetAlpha(lampGlows[i], 1f); SetAlpha(lightPools[i], 1f);
            caged[i] = false;
            cages[i].gameObject.SetActive(false);
        }
        SetAlpha(darkness, 0f);
        chairGone = umbrellaGone = holeFilledDone = false;
        Restore(chair, chairHome);
        Restore(umbrella, umbrellaHome);
        hole.gameObject.SetActive(true);
        holeFilled.SetActive(false);
        hatchButton.SetActive(false);
        completeOverlay.SetActive(false);
        hatchlingTemplate.gameObject.SetActive(false);
        foreach (Transform t in hatchlingTemplate.transform.parent)
            if (t != hatchlingTemplate.transform && t.name.StartsWith("Hatchling")) Destroy(t.gameObject);
        Say("Clear the path, protect the nests and switch off the lights. Tap things on the beach!");
        Refresh();
    }

    // ---------------- Gorevler ----------------

    void ToggleLamp(int i)
    {
        if (hatching || lampOff[i]) return;
        lampOff[i] = true;
        Earn();
        TwSes.Efekt(Ses.Tik);
        StartCoroutine(Tween(0.5f, t =>
        {
            SetAlpha(lampBulbs[i], 1f - t);
            SetAlpha(lampGlows[i], 1f - t);
            SetAlpha(lightPools[i], 1f - t);
        }));
        float from = darkness.color.a, to = darkAlpha * LightsOff / 3f;
        StartCoroutine(Tween(0.6f, t => SetAlpha(darkness, Mathf.Lerp(from, to, t))));
        Say(LightsOff < 3 ? "Light off! Hatchlings follow the brightest light, so a bright town pulls them the wrong way."
                          : "All the lights are off. Now the moonlit sea is the brightest thing on the beach.");
        Done();
    }

    void MoveAway(Button b, ref bool gone, string message)
    {
        if (hatching || gone) return;
        gone = true;
        Earn();
        TwSes.Efekt(Ses.ParcaTak);
        var rt = (RectTransform)b.transform;
        var g = b.targetGraphic;
        var start = rt.anchoredPosition;
        b.interactable = false;
        StartCoroutine(Tween(0.5f, t =>
        {
            rt.anchoredPosition = start + new Vector2(0, -260f * t * t);
            SetAlpha(g, 1f - t);
        }, () => b.gameObject.SetActive(false)));
        Say(message);
        Done();
    }

    void FillHole()
    {
        if (hatching || holeFilledDone) return;
        holeFilledDone = true;
        Earn();
        TwSes.Efekt(Ses.Zimpara);
        hole.gameObject.SetActive(false);
        holeFilled.SetActive(true);
        Say("Hole filled. Hatchlings can fall into holes and get stuck.");
        Done();
    }

    void Protect(int i)
    {
        if (hatching || caged[i]) return;
        caged[i] = true;
        Earn();
        TwSes.Efekt(Ses.Cekic, 0.8f);
        cages[i].gameObject.SetActive(true);
        StartCoroutine(Tween(0.35f, t => cages[i].localScale = Vector3.one * EaseOutBack(t)));
        Say(Protected < 3 ? "Nest protected. The cage keeps dogs and feet away from the eggs."
                          : "Every nest has a cage now.");
        Done();
    }

    void Done()
    {
        Refresh();
        if (!AllDone) return;
        Say("Everything is ready. Let them hatch!");
        hatchButton.SetActive(true);
        TwSes.Efekt(Ses.Onay);
    }

    void Earn()
    {
        earned += reward;
        GameProgress.Coins += reward;
    }

    void Refresh()
    {
        int[] counts = { LightsOff, Cleared, Protected };
        for (int i = 0; i < 3; i++)
        {
            taskCounts[i].text = counts[i] + "/3";
            taskChecks[i].sprite = counts[i] >= 3 ? checkOn : checkOff;
        }
        coinText.text = GameProgress.Coins.ToString();
    }

    // ---------------- Yumurtadan cikis ----------------

    /// <summary>"Let them hatch!" butonu.</summary>
    public void Hatch()
    {
        if (hatching || !AllDone) return;
        hatching = true;
        hatchButton.SetActive(false);
        StartCoroutine(HatchRoutine());
    }

    IEnumerator HatchRoutine()
    {
        GameSettings.Save();
        Say("Look! The hatchlings are heading for the sea!");
        TwSes.Efekt(Ses.GecisParilti);
        // Kafesler kalkar ki yavrular cikabilsin
        foreach (var c in cages)
        {
            var cage = c;
            StartCoroutine(Tween(0.4f, t => cage.localScale = Vector3.one * (1f - t), () => cage.gameObject.SetActive(false)));
        }
        yield return new WaitForSeconds(0.5f);

        var rng = new System.Random(3);
        var walkers = new List<Coroutine>();
        float longest = 0f;
        foreach (var nest in nests)
        {
            var home = ((RectTransform)nest.transform).anchoredPosition;
            for (int i = 0; i < hatchlingsPerNest; i++)
            {
                var start = home + new Vector2((float)rng.NextDouble() * 80f - 40f, (float)rng.NextDouble() * 20f);
                float speed = Mathf.Lerp(110f, 170f, (float)rng.NextDouble());
                float delay = (float)rng.NextDouble() * 1.2f;
                float time = (-seaLine - start.y) / speed;
                longest = Mathf.Max(longest, delay + time);
                StartCoroutine(Walk(start, speed, delay, (float)rng.NextDouble() * 6f));
            }
        }
        yield return new WaitForSeconds(longest + 0.8f);
        TwSes.Efekt(Ses.OnarimTamam);
        if (!dayCounted)
        {
            dayCounted = true;
            GameProgress.NestsSafe += nests.Length;
            GameSettings.Save();
        }
        FillDayCard();
        completeOverlay.SetActive(true);
        yield return Tween(0.35f, t => completeCard.localScale = Vector3.one * EaseOutBack(t));
    }

    IEnumerator Walk(Vector2 start, float speed, float delay, float phase)
    {
        yield return new WaitForSeconds(delay);
        var img = Instantiate(hatchlingTemplate, hatchlingTemplate.transform.parent);
        img.name = "Hatchling";
        img.gameObject.SetActive(true);
        var rt = img.rectTransform;
        var p = start;
        float drift = UnityEngine.Random.Range(-25f, 25f);
        while (p.y < -seaLine)
        {
            float t = Time.time * 7f + phase;
            p += new Vector2(drift * Time.deltaTime + Mathf.Sin(t) * 0.6f, speed * Time.deltaTime);
            rt.anchoredPosition = p;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * 12f);
            float toSea = Mathf.InverseLerp(-seaLine - 60f, -seaLine, p.y);
            SetAlpha(img, 1f - toSea);
            yield return null;
        }
        Destroy(img.gameObject);
    }

    void FillDayCard()
    {
        int day = GameProgress.CurrentDay;
        int safe = GameProgress.NestsSafe, total = GameProgress.TotalNests, waiting = total - safe;
        completeKicker.text = Loc.F("DAY {0} COMPLETE · ALL TASKS DONE", day);
        completeTitle.text = Loc.F("You did it! {0} little carettas reached the sea!", nests.Length * carettasPerNest);
        nestsSafeText.text = safe + " / " + total;
        beachReadyText.text = Mathf.RoundToInt(100f * safe / total) + "%";
        earnedText.text = "+" + earned;
        waitingText.text = waiting > 0
            ? Loc.F("{0} more nests are still waiting in the sand. They'll hatch soon. Keep going, the beach needs you!", waiting)
            : Loc.T("Every nest on the beach is safe. What a season!");
        nextDayLabel.text = day < GameProgress.TotalDays ? Loc.F("Start Day {0}", day + 1) : Loc.T("Finish the season");
    }

    /// <summary>"Start Day 2": gun biter, yeni gun denizde baslar (son gunse ana menu).</summary>
    public void StartNextDay()
    {
        bool last = GameProgress.CurrentDay >= GameProgress.TotalDays;
        GameProgress.CompleteDay();
        SceneManager.LoadScene(last ? menuSceneName : SceneManager.GetActiveScene().name);
    }

    /// <summary>"Season map": sezon haritasi henuz yok, gun biter ve ana menuye donulur.</summary>
    public void SeasonMap()
    {
        GameProgress.CompleteDay();
        SceneManager.LoadScene(menuSceneName);
    }

    // ---------------- Yardimcilar ----------------

    void Say(string en) => tooltip.text = Loc.T(en);

    static void Restore(Button b, Vector2 home)
    {
        b.gameObject.SetActive(true);
        b.interactable = true;
        ((RectTransform)b.transform).anchoredPosition = home;
        SetAlpha(b.targetGraphic, 1f);
    }

    static int Count(bool[] a)
    {
        int n = 0;
        foreach (var v in a) if (v) n++;
        return n;
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
