using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Giris ekrani (dedenin defteri): Start / Continue / How to play / Settings.
/// How to play ve Settings defterin bos sayfasi olarak acilir; Escape (veya gamepad B) ile geri donulur.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Acilacak sahneler (Build Settings'te ekli olmali)")]
    [Tooltip("Start: once dedenin notu / yukleme ekrani")]
    [SerializeField] string introSceneName = "Intro";
    [Tooltip("Continue: dogrudan oyun")]
    [SerializeField] string gameSceneName = "Workshop";

    [Header("Sayfalar")]
    [SerializeField] CanvasGroup mainPage;
    [SerializeField] CanvasGroup howToPlayPage;
    [SerializeField] CanvasGroup settingsPage;
    [SerializeField] CanvasGroup screenFader;

    [Header("Continue butonu ve 'Day X of 3' rozeti")]
    [SerializeField] Button continueButton;
    [SerializeField] Text continueBadge;

    [Header("Klavye / gamepad icin ilk secilecek butonlar")]
    [SerializeField] Selectable mainFirstSelected;
    [SerializeField] Selectable howToPlayFirstSelected;
    [SerializeField] Selectable settingsFirstSelected;

    [Header("Gecisler (saniye)")]
    [SerializeField] float pageFade = 0.25f;
    [SerializeField] float screenFade = 0.7f;

    [Header("Ses (Assets/Tideworks/Resources/TideworksSes)")]
    [SerializeField] bool playMenuAudio = true;
    [SerializeField] string menuMusic = Tideworks.Ses.MuzikAnaTema;
    [SerializeField] string menuAmbience = Tideworks.Ses.AmbiyansDeniz;

    CanvasGroup currentPage;
    Coroutine pageRoutine;
    bool leaving;
    bool usingKeys;

    void Awake()
    {
        // Menudeki tum butonlara tiklama / uzerine gelme sesi ekle
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (!button.GetComponent<Tideworks.TwButonSesi>())
                button.gameObject.AddComponent<Tideworks.TwButonSesi>();
    }

    void Start()
    {
        Time.timeScale = 1f;
        RefreshContinue();

        foreach (var page in new[] { howToPlayPage, settingsPage })
            if (page) { page.alpha = 0f; page.gameObject.SetActive(false); }
        SetInteractive(mainPage, true);
        currentPage = mainPage;

        if (screenFader)
            StartCoroutine(FadeScreen(1f, 0f, null));

        if (playMenuAudio)
        {
            if (!string.IsNullOrEmpty(menuMusic)) Tideworks.TwSes.Muzik(menuMusic);
            Tideworks.TwSes.Ambiyans(menuAmbience);
        }
    }

    void Update()
    {
        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                 || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        if (back && currentPage != mainPage && !leaving)
            ShowMain();

        // Fareyle oynayanda hicbir sey secili durmasin; ok tusu / gamepad gelince ilk butonu sec.
        bool nav = NavigationPressed();
        if (nav) usingKeys = true;
        else if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 4f) usingKeys = false;

        if (nav && pageRoutine == null && EventSystem.current && !EventSystem.current.currentSelectedGameObject)
            Select(currentPage == howToPlayPage ? howToPlayFirstSelected
                 : currentPage == settingsPage ? settingsFirstSelected
                 : (continueButton && continueButton.interactable ? continueButton : mainFirstSelected));
    }

    static bool NavigationPressed()
    {
        var k = Keyboard.current;
        var g = Gamepad.current;
        return (k != null && (k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame
                           || k.leftArrowKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame
                           || k.tabKey.wasPressedThisFrame))
            || (g != null && (g.dpad.up.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame
                           || g.leftStick.up.wasPressedThisFrame || g.leftStick.down.wasPressedThisFrame));
    }

    public void StartGame()
    {
        if (leaving) return;
        GameProgress.NewGame();
        LoadScene(introSceneName);
    }

    public void ContinueGame()
    {
        if (leaving || !GameProgress.HasSave) return;
        LoadScene(gameSceneName);
    }

    public void ShowMain() => ShowPage(mainPage, mainFirstSelected);

    public void ShowHowToPlay() => ShowPage(howToPlayPage, howToPlayFirstSelected);

    public void ShowSettings() => ShowPage(settingsPage, settingsFirstSelected);

    public void QuitGame()
    {
        GameSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void RefreshContinue()
    {
        bool hasSave = GameProgress.HasSave;
        if (continueButton) continueButton.interactable = hasSave;
        if (continueBadge)
            continueBadge.text = hasSave ? Loc.F("Day {0} of {1}", GameProgress.CurrentDay, GameProgress.TotalDays) : Loc.T("No save");
    }

    void OnEnable() => Loc.LanguageChanged += RefreshContinue;

    void OnDisable() => Loc.LanguageChanged -= RefreshContinue;

    void LoadScene(string sceneName)
    {
        leaving = true;
        GameSettings.Save();
        SetInteractive(mainPage, false);
        Tideworks.TwSes.Efekt(Tideworks.Ses.GecisDalga, 0.8f);
        if (screenFader) StartCoroutine(FadeScreen(0f, 1f, () => SceneManager.LoadScene(sceneName)));
        else SceneManager.LoadScene(sceneName);
    }

    void ShowPage(CanvasGroup page, Selectable firstSelected)
    {
        if (page == null || page == currentPage || leaving) return;
        if (page != mainPage) Tideworks.TwSes.Efekt(Tideworks.Ses.GecisParilti, 0.6f);

        if (pageRoutine != null) StopCoroutine(pageRoutine);
        pageRoutine = StartCoroutine(SwitchPage(currentPage, page, firstSelected));
        currentPage = page;
    }

    IEnumerator SwitchPage(CanvasGroup from, CanvasGroup to, Selectable firstSelected)
    {
        SetInteractive(from, false);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

        // Alt sayfalar ana sayfanin ustune biner: acilirken belirir, kapanirken kaybolur.
        var fading = to != mainPage ? to : from;
        float target = to != mainPage ? 1f : 0f;
        if (target > 0f) fading.gameObject.SetActive(true);

        float start = fading.alpha;
        for (float t = 0f; t < pageFade; t += Time.unscaledDeltaTime)
        {
            fading.alpha = Mathf.Lerp(start, target, t / pageFade);
            yield return null;
        }
        fading.alpha = target;
        if (target == 0f) fading.gameObject.SetActive(false);

        SetInteractive(to, true);
        if (usingKeys) Select(firstSelected);
        pageRoutine = null;
    }

    IEnumerator FadeScreen(float from, float to, System.Action done)
    {
        screenFader.gameObject.SetActive(true);
        screenFader.blocksRaycasts = true;
        for (float t = 0f; t < screenFade; t += Time.unscaledDeltaTime)
        {
            screenFader.alpha = Mathf.SmoothStep(from, to, t / screenFade);
            yield return null;
        }
        screenFader.alpha = to;
        if (to == 0f) screenFader.gameObject.SetActive(false);
        done?.Invoke();
    }

    static void SetInteractive(CanvasGroup group, bool on)
    {
        if (!group) return;
        group.interactable = on;
        group.blocksRaycasts = on;
    }

    static void Select(Selectable s)
    {
        if (EventSystem.current != null && s != null && s.IsInteractable())
            EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}
