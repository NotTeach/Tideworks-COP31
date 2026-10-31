using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Start'tan sonra acilan giris / yukleme ekrani: dedenin notu satir satir belirir,
/// bu sirada oyun sahnesi arka planda yuklenir. "Open the workshop" veya
/// "Read the notebook later" oyuna gecer (yukleme bitmediyse bitmesini bekler).
/// </summary>
public class IntroController : MonoBehaviour
{
    [Header("Yuklenecek sahne (Build Settings'te ekli olmali)")]
    [SerializeField] string gameSceneName = "SampleScene";

    [Header("Defterdeki not: satirlar sirayla belirir")]
    [SerializeField] Text[] noteLines;
    [SerializeField] float firstLineDelay = 0.5f;
    [SerializeField] float lineDelay = 0.35f;
    [SerializeField] float lineFade = 0.45f;

    [Header("Kartlar")]
    [SerializeField] Text timeLeftValue;
    [SerializeField] Text nestsValue;
    [SerializeField] int nestsOnBeach = 12;

    [Header("Butonlar")]
    [SerializeField] Button openWorkshopButton;
    [SerializeField] Button readLaterButton;

    [Header("Ekran gecisi")]
    [SerializeField] CanvasGroup screenFader;
    [SerializeField] float screenFade = 0.6f;

    AsyncOperation loading;
    bool leaving;

    void Awake()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (!button.GetComponent<Tideworks.TwButonSesi>())
                button.gameObject.AddComponent<Tideworks.TwButonSesi>();
    }

    void Start()
    {
        Time.timeScale = 1f;

        int daysLeft = GameProgress.TotalDays - GameProgress.CurrentDay + 1;
        if (timeLeftValue) timeLeftValue.text = daysLeft == 1 ? "1 day" : $"{daysLeft} days";
        if (nestsValue) nestsValue.text = nestsOnBeach == 1 ? "1 nest" : $"{nestsOnBeach} nests";

        foreach (var line in noteLines) SetAlpha(line, 0f);
        StartCoroutine(RevealNote());

        if (screenFader) StartCoroutine(Fade(screenFader, 1f, 0f));

        // Oyun sahnesini simdiden yukle, ama oyuncu butona basana kadar acma
        loading = SceneManager.LoadSceneAsync(gameSceneName);
        if (loading != null) loading.allowSceneActivation = false;
        else Debug.LogError($"[Tideworks] '{gameSceneName}' sahnesi yuklenemedi. Build Settings'e ekli mi?");
    }

    public void OpenWorkshop() => Proceed();

    public void ReadLater() => Proceed();

    void Proceed()
    {
        if (leaving || loading == null) return;
        leaving = true;
        if (openWorkshopButton) openWorkshopButton.interactable = false;
        if (readLaterButton) readLaterButton.interactable = false;
        GameSettings.Save();
        Tideworks.TwSes.Efekt(Tideworks.Ses.GecisDalga, 0.8f);
        StartCoroutine(Leave());
    }

    IEnumerator Leave()
    {
        if (screenFader) yield return Fade(screenFader, 0f, 1f);
        while (loading.progress < 0.9f) yield return null;
        loading.allowSceneActivation = true;
    }

    IEnumerator RevealNote()
    {
        yield return new WaitForSecondsRealtime(firstLineDelay);
        foreach (var line in noteLines)
        {
            StartCoroutine(FadeText(line));
            yield return new WaitForSecondsRealtime(lineDelay);
        }
    }

    IEnumerator FadeText(Text text)
    {
        for (float t = 0f; t < lineFade; t += Time.unscaledDeltaTime)
        {
            SetAlpha(text, t / lineFade);
            yield return null;
        }
        SetAlpha(text, 1f);
    }

    IEnumerator Fade(CanvasGroup group, float from, float to)
    {
        group.gameObject.SetActive(true);
        group.blocksRaycasts = true;
        for (float t = 0f; t < screenFade; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.SmoothStep(from, to, t / screenFade);
            yield return null;
        }
        group.alpha = to;
        if (to == 0f) group.gameObject.SetActive(false);
    }

    static void SetAlpha(Graphic g, float a)
    {
        if (!g) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
