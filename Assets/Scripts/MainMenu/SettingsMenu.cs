using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings paneli: ses, tam ekran, cozunurluk ve grafik kalitesi.
/// Degisiklikler aninda uygulanir ve GameSettings uzerinden kaydedilir.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Text masterVolumeValue;
    [SerializeField] Text musicVolumeValue;
    [SerializeField] Text sfxVolumeValue;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] Dropdown resolutionDropdown;
    [SerializeField] Dropdown qualityDropdown;

    readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    void Awake()
    {
        masterVolumeSlider.onValueChanged.AddListener(v => { GameSettings.MasterVolume = v; SetPercent(masterVolumeValue, v); });
        musicVolumeSlider.onValueChanged.AddListener(v => { GameSettings.MusicVolume = v; SetPercent(musicVolumeValue, v); });
        sfxVolumeSlider.onValueChanged.AddListener(v => { GameSettings.SfxVolume = v; SetPercent(sfxVolumeValue, v); });
        fullscreenToggle.onValueChanged.AddListener(v => GameSettings.Fullscreen = v);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        qualityDropdown.onValueChanged.AddListener(i => GameSettings.Quality = i);
    }

    void OnEnable() => Refresh();

    void OnDisable() => GameSettings.Save();

    public void ResetToDefaults()
    {
        GameSettings.ResetToDefaults();
        Refresh();
    }

    void Refresh()
    {
        masterVolumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        musicVolumeSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        sfxVolumeSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        SetPercent(masterVolumeValue, GameSettings.MasterVolume);
        SetPercent(musicVolumeValue, GameSettings.MusicVolume);
        SetPercent(sfxVolumeValue, GameSettings.SfxVolume);

        fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(QualitySettings.names.ToList());
        qualityDropdown.SetValueWithoutNotify(GameSettings.Quality);
        qualityDropdown.RefreshShownValue();

        resolutions.Clear();
        resolutions.AddRange(Screen.resolutions
            .Select(r => new Vector2Int(r.width, r.height))
            .Distinct()
            .OrderByDescending(r => r.x).ThenByDescending(r => r.y));
        var current = new Vector2Int(Screen.width, Screen.height);
        if (!resolutions.Contains(current)) resolutions.Insert(0, current);

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resolutions.Select(r => $"{r.x} x {r.y}").ToList());
        resolutionDropdown.SetValueWithoutNotify(resolutions.IndexOf(current));
        resolutionDropdown.RefreshShownValue();
    }

    void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= resolutions.Count) return;
        GameSettings.SetResolution(resolutions[index].x, resolutions[index].y);
    }

    static void SetPercent(Text label, float value)
    {
        if (label) label.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
