using UnityEngine;

/// <summary>
/// Oyuncu ayarlarini (ses, ekran, kalite) PlayerPrefs ile saklar ve uygular.
/// Oyun acilir acilmaz kayitli ayarlar otomatik uygulanir, bu yuzden her sahnede gecerlidir.
/// Muzik / efekt calan scriptler GameSettings.MusicVolume ve GameSettings.SfxVolume degerlerini kullanmali.
/// </summary>
public static class GameSettings
{
    const string MasterKey = "settings.masterVolume";
    const string MusicKey = "settings.musicVolume";
    const string SfxKey = "settings.sfxVolume";
    const string FullscreenKey = "settings.fullscreen";
    const string QualityKey = "settings.quality";
    const string ResWidthKey = "settings.resolutionWidth";
    const string ResHeightKey = "settings.resolutionHeight";

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(MasterKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
            AudioListener.volume = Mathf.Clamp01(value);
        }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
            ApplyAudioLevels();
        }
    }

    /// <summary>Sound Effects: efektler + ambiyans (deniz, atolye, tekne motoru).</summary>
    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
            ApplyAudioLevels();
        }
    }

    static void ApplyAudioLevels() => Tideworks.TwSes.AyarlariUygula(MusicVolume, SfxVolume);

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

    public static void ResetToDefaults()
    {
        foreach (var key in new[] { MasterKey, MusicKey, SfxKey, FullscreenKey, QualityKey, ResWidthKey, ResHeightKey })
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        AudioListener.volume = 1f;
        ApplyAudioLevels();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplySavedSettings()
    {
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
