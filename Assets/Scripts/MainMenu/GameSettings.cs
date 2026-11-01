using System;
using UnityEngine;

public enum TimeOfDayMode { Auto, Day, Night }
public enum TextSizeMode { Small, Medium, Large }
public enum GameLanguage { English, Turkish }

/// <summary>
/// Oyuncu ayarlarini PlayerPrefs ile saklar ve uygular.
/// Oyun acilir acilmaz kayitli ayarlar otomatik uygulanir, bu yuzden her sahnede gecerlidir.
/// Oyun kodu ayarlari buradan okur (or. GameSettings.CozyMode); degisiklikleri dinlemek icin GameSettings.Changed.
/// </summary>
public static class GameSettings
{
    const string MasterKey = "settings.masterVolume";
    const string MusicKey = "settings.musicVolume";
    const string SfxKey = "settings.sfxVolume";
    const string AmbienceKey = "settings.ambienceVolume";
    const string AudioAutoKey = "settings.audioAuto";
    const string LanguageKey = "settings.language";
    const string TimeOfDayKey = "settings.timeOfDay";
    const string TextSizeKey = "settings.textSize";
    const string CozyKey = "settings.cozyMode";
    const string HintsKey = "settings.hints";
    const string MaterialLabelsKey = "settings.materialLabels";
    const string ReduceMotionKey = "settings.reduceMotion";
    const string FullscreenKey = "settings.fullscreen";
    const string QualityKey = "settings.quality";
    const string ResWidthKey = "settings.resolutionWidth";
    const string ResHeightKey = "settings.resolutionHeight";

    // Varsayilanlar (Reset to default bunlara doner)
    const float DefaultMusic = 0.7f, DefaultSfx = 0.8f, DefaultAmbience = 0.6f;
    const bool DefaultCozy = true, DefaultHints = true, DefaultMaterialLabels = false, DefaultReduceMotion = false;

    /// <summary>Herhangi bir ayar degisince cagrilir.</summary>
    public static event Action Changed;

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(MasterKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
            AudioListener.volume = Mathf.Clamp01(value);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Auto: oyun onerilen ses dengesini kullanir (Music 70%, Sound effects 80%, Ocean waves 60%).
    /// Manual: oyuncunun kaydiricilarla ayarladigi seviyeler. Elle ayarlanan degerler Auto'dayken de saklanir.
    /// </summary>
    public static bool AudioAuto
    {
        get => PlayerPrefs.GetInt(AudioAutoKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(AudioAutoKey, value ? 1 : 0);
            ApplyAudioLevels();
            Changed?.Invoke();
        }
    }

    // Asagidaki uc ses seviyesi okunurken Auto'daysa onerilen degeri, degilse elle ayarlanani verir;
    // yazilinca her zaman elle ayarlanan degeri gunceller.
    public static float MusicVolume
    {
        get => AudioAuto ? DefaultMusic : PlayerPrefs.GetFloat(MusicKey, DefaultMusic);
        set => SetVolume(MusicKey, value);
    }

    /// <summary>Sound effects: tiklama, toplama, tamir sesleri.</summary>
    public static float SfxVolume
    {
        get => AudioAuto ? DefaultSfx : PlayerPrefs.GetFloat(SfxKey, DefaultSfx);
        set => SetVolume(SfxKey, value);
    }

    /// <summary>Ocean waves: ambiyans dongusu (deniz, atolye) ve tekne motoru.</summary>
    public static float AmbienceVolume
    {
        get => AudioAuto ? DefaultAmbience : PlayerPrefs.GetFloat(AmbienceKey, DefaultAmbience);
        set => SetVolume(AmbienceKey, value);
    }

    static void SetVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        ApplyAudioLevels();
        Changed?.Invoke();
    }

    static void ApplyAudioLevels() => Tideworks.TwSes.AyarlariUygula(MusicVolume, SfxVolume, AmbienceVolume);

    public static GameLanguage Language
    {
        get => (GameLanguage)PlayerPrefs.GetInt(LanguageKey, (int)GameLanguage.English);
        set => SetInt(LanguageKey, (int)value);
    }

    public static TimeOfDayMode TimeOfDay
    {
        get => (TimeOfDayMode)PlayerPrefs.GetInt(TimeOfDayKey, (int)TimeOfDayMode.Auto);
        set => SetInt(TimeOfDayKey, (int)value);
    }

    public static TextSizeMode TextSize
    {
        get => (TextSizeMode)PlayerPrefs.GetInt(TextSizeKey, (int)TextSizeMode.Medium);
        set => SetInt(TextSizeKey, (int)value);
    }

    /// <summary>Cozy mode: denize acilinca zamanlayici yok.</summary>
    public static bool CozyMode
    {
        get => PlayerPrefs.GetInt(CozyKey, DefaultCozy ? 1 : 0) == 1;
        set => SetInt(CozyKey, value ? 1 : 0);
    }

    /// <summary>Hints: hangi malzemenin hangi parcaya uydugunu goster.</summary>
    public static bool Hints
    {
        get => PlayerPrefs.GetInt(HintsKey, DefaultHints ? 1 : 0) == 1;
        set => SetInt(HintsKey, value ? 1 : 0);
    }

    /// <summary>Material labels: malzemeleri sadece renkle degil yazi ve ikonla da goster.</summary>
    public static bool MaterialLabels
    {
        get => PlayerPrefs.GetInt(MaterialLabelsKey, DefaultMaterialLabels ? 1 : 0) == 1;
        set => SetInt(MaterialLabelsKey, value ? 1 : 0);
    }

    /// <summary>Reduce motion: dalga, tekne ve parilti animasyonlarini sakinlestir.</summary>
    public static bool ReduceMotion
    {
        get
        {
            if (reduceMotionCache < 0) reduceMotionCache = PlayerPrefs.GetInt(ReduceMotionKey, DefaultReduceMotion ? 1 : 0);
            return reduceMotionCache == 1;
        }
        set
        {
            reduceMotionCache = value ? 1 : 0;
            SetInt(ReduceMotionKey, reduceMotionCache);
        }
    }
    static int reduceMotionCache = -1; // her karede okunuyor, PlayerPrefs'e gitmesin

    static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        Changed?.Invoke();
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        set
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }

    public static int Quality
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
        set
        {
            PlayerPrefs.SetInt(QualityKey, value);
            QualitySettings.SetQualityLevel(value, true);
        }
    }

    public static void SetResolution(int width, int height)
    {
        PlayerPrefs.SetInt(ResWidthKey, width);
        PlayerPrefs.SetInt(ResHeightKey, height);
        Screen.SetResolution(width, height, Screen.fullScreenMode);
    }

    public static void Save() => PlayerPrefs.Save();

    /// <summary>Settings sayfasindaki ayarlar (Reset to default bunlari sifirlar).</summary>
    public static void ResetToDefaults()
    {
        foreach (var key in new[] { AudioAutoKey, MusicKey, SfxKey, AmbienceKey, LanguageKey, TimeOfDayKey, TextSizeKey,
                     CozyKey, HintsKey, MaterialLabelsKey, ReduceMotionKey })
            PlayerPrefs.DeleteKey(key);
        reduceMotionCache = -1;
        ApplyAudioLevels();
        Changed?.Invoke();
    }

    /// <summary>Settings sayfasi acilirken alinir; kaydetmeden cikilirsa Restore ile geri yuklenir.</summary>
    public struct Snapshot
    {
        public bool audioAuto;
        public float music, sfx, ambience; // elle ayarlanan seviyeler
        public GameLanguage language;
        public TimeOfDayMode timeOfDay;
        public TextSizeMode textSize;
        public bool cozy, hints, materialLabels, reduceMotion;
    }

    public static Snapshot Capture() => new Snapshot
    {
        audioAuto = AudioAuto,
        music = PlayerPrefs.GetFloat(MusicKey, DefaultMusic),
        sfx = PlayerPrefs.GetFloat(SfxKey, DefaultSfx),
        ambience = PlayerPrefs.GetFloat(AmbienceKey, DefaultAmbience),
        language = Language, timeOfDay = TimeOfDay, textSize = TextSize,
        cozy = CozyMode, hints = Hints, materialLabels = MaterialLabels, reduceMotion = ReduceMotion,
    };

    public static void Restore(Snapshot s)
    {
        MusicVolume = s.music;
        SfxVolume = s.sfx;
        AmbienceVolume = s.ambience;
        AudioAuto = s.audioAuto;
        Language = s.language;
        TimeOfDay = s.timeOfDay;
        TextSize = s.textSize;
        CozyMode = s.cozy;
        Hints = s.hints;
        MaterialLabels = s.materialLabels;
        ReduceMotion = s.reduceMotion;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplySavedSettings()
    {
        reduceMotionCache = -1;
        AudioListener.volume = MasterVolume;
        ApplyAudioLevels();

        if (PlayerPrefs.HasKey(QualityKey))
            QualitySettings.SetQualityLevel(Quality, true);

        // Editor'de ekran/cozunurluk ayarlari bir ise yaramaz, sadece build'de uygula.
        if (Application.isEditor) return;

        var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (PlayerPrefs.HasKey(ResWidthKey) && PlayerPrefs.HasKey(ResHeightKey))
            Screen.SetResolution(PlayerPrefs.GetInt(ResWidthKey), PlayerPrefs.GetInt(ResHeightKey), mode);
        else if (PlayerPrefs.HasKey(FullscreenKey))
            Screen.fullScreenMode = mode;
    }
}
