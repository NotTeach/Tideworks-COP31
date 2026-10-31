using UnityEngine;

/// <summary>
/// Oyuncunun kac gun ilerledigini PlayerPrefs ile saklar.
/// Giris ekranindaki "Continue" butonu ve "Day X of 3" rozeti buradan beslenir.
/// Oyun sahnesi bir gun bittiginde GameProgress.CompleteDay() cagirmali.
/// </summary>
public static class GameProgress
{
    public const int TotalDays = 3;
    const string DayKey = "progress.day";

    public static bool HasSave => PlayerPrefs.HasKey(DayKey);

    public static int CurrentDay => Mathf.Clamp(PlayerPrefs.GetInt(DayKey, 1), 1, TotalDays);

    public static void NewGame()
    {
        PlayerPrefs.SetInt(DayKey, 1);
        PlayerPrefs.Save();
    }

    public static void CompleteDay()
    {
        PlayerPrefs.SetInt(DayKey, Mathf.Min(CurrentDay + 1, TotalDays));
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(DayKey);
        PlayerPrefs.Save();
    }
}
