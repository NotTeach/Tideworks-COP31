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

    const string CoinsKey = "progress.coins";
    public const int StartCoins = 120;

    /// <summary>Atolyenin ust barindaki altin sayaci.</summary>
    public static int Coins
    {
        get => PlayerPrefs.GetInt(CoinsKey, StartCoins);
        set => PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, value));
    }

    public static void NewGame()
    {
        PlayerPrefs.SetInt(DayKey, 1);
        PlayerPrefs.SetInt(CoinsKey, StartCoins);
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
        PlayerPrefs.DeleteKey(CoinsKey);
        PlayerPrefs.Save();
    }
}
