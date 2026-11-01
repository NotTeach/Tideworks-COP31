using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings sayfasi (dedenin acik defteri): Sound, Language, Look &amp; feel, Play.
/// Degisiklikler aninda uygulanir. "Save &amp; back" kaydeder; sol ustteki geri oku veya Escape
/// kaydetmeden cikar ve sayfa acildigindaki ayarlara geri doner.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("Sound")]
    [Tooltip("0 = Auto, 1 = Manual")]
    [SerializeField] SegmentedControl audioMode;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider effectsSlider;
    [SerializeField] Slider wavesSlider;
    [SerializeField] Text musicValue;
    [SerializeField] Text effectsValue;
    [SerializeField] Text wavesValue;

    [Header("Language / Look & feel")]
    [SerializeField] SegmentedControl language;
    [SerializeField] SegmentedControl timeOfDay;
    [SerializeField] SegmentedControl textSize;

    [Header("Play")]
    [SerializeField] Toggle cozyMode;
    [SerializeField] Toggle hints;
    [SerializeField] Toggle materialLabels;
    [SerializeField] Toggle reduceMotion;

    GameSettings.Snapshot opened;
    bool saved;

    void Awake()
    {
        audioMode.ValueChanged += i => { GameSettings.AudioAuto = i == 0; RefreshSound(); };
        musicSlider.onValueChanged.AddListener(v => { SwitchToManual(); GameSettings.MusicVolume = v; SetPercent(musicValue, v); });
        effectsSlider.onValueChanged.AddListener(v => { SwitchToManual(); GameSettings.SfxVolume = v; SetPercent(effectsValue, v); });
        wavesSlider.onValueChanged.AddListener(v => { SwitchToManual(); GameSettings.AmbienceVolume = v; SetPercent(wavesValue, v); });
        language.ValueChanged += i => GameSettings.Language = (GameLanguage)i;
        timeOfDay.ValueChanged += i => GameSettings.TimeOfDay = (TimeOfDayMode)i;
        textSize.ValueChanged += i => GameSettings.TextSize = (TextSizeMode)i;
        cozyMode.onValueChanged.AddListener(v => GameSettings.CozyMode = v);
        hints.onValueChanged.AddListener(v => GameSettings.Hints = v);
        materialLabels.onValueChanged.AddListener(v => GameSettings.MaterialLabels = v);
        reduceMotion.onValueChanged.AddListener(v => GameSettings.ReduceMotion = v);
    }

    void OnEnable()
    {
        opened = GameSettings.Capture();
        saved = false;
        Refresh();
    }

    void OnDisable()
    {
        if (!saved) GameSettings.Restore(opened);
    }

    /// <summary>"Save &amp; back": ayarlari kalici yap (sayfayi controller kapatir).</summary>
    public void Save()
    {
        saved = true;
        GameSettings.Save();
    }

    public void ResetToDefaults()
    {
        GameSettings.ResetToDefaults();
        Refresh();
    }

    /// <summary>Auto'dayken bir kaydirici oynatilirsa Manual'e gec; diger kaydiricilar oldugu yerde kalsin.</summary>
    void SwitchToManual()
    {
        if (!GameSettings.AudioAuto) return;
        float music = GameSettings.MusicVolume, sfx = GameSettings.SfxVolume, waves = GameSettings.AmbienceVolume;
        GameSettings.AudioAuto = false;
        GameSettings.MusicVolume = music;
        GameSettings.SfxVolume = sfx;
        GameSettings.AmbienceVolume = waves;
        audioMode.SetValueWithoutNotify(1);
    }

    void Refresh()
    {
        RefreshSound();

        language.SetValueWithoutNotify((int)GameSettings.Language);
        timeOfDay.SetValueWithoutNotify((int)GameSettings.TimeOfDay);
        textSize.SetValueWithoutNotify((int)GameSettings.TextSize);

        cozyMode.SetIsOnWithoutNotify(GameSettings.CozyMode);
        hints.SetIsOnWithoutNotify(GameSettings.Hints);
        materialLabels.SetIsOnWithoutNotify(GameSettings.MaterialLabels);
        reduceMotion.SetIsOnWithoutNotify(GameSettings.ReduceMotion);
    }

    void RefreshSound()
    {
        audioMode.SetValueWithoutNotify(GameSettings.AudioAuto ? 0 : 1);
        musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        effectsSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        wavesSlider.SetValueWithoutNotify(GameSettings.AmbienceVolume);
        SetPercent(musicValue, GameSettings.MusicVolume);
        SetPercent(effectsValue, GameSettings.SfxVolume);
        SetPercent(wavesValue, GameSettings.AmbienceVolume);
    }

    static void SetPercent(Text label, float value)
    {
        if (label) label.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
