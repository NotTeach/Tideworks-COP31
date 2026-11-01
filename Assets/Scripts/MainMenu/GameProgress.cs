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

    const string NestsKey = "progress.nestsSafe";
    /// <summary>Sezon boyunca kumsaldaki yuva sayisi (Intro: "12 nests").</summary>
    public const int TotalNests = 12;

    /// <summary>Korunup yavrulari denize ulasan yuva sayisi.</summary>
    public static int NestsSafe
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(NestsKey, 0), 0, TotalNests);
        set => PlayerPrefs.SetInt(NestsKey, Mathf.Clamp(value, 0, TotalNests));
    }

    public static void NewGame()
    {
        PlayerPrefs.SetInt(DayKey, 1);
        PlayerPrefs.SetInt(CoinsKey, StartCoins);
        PlayerPrefs.SetInt(NestsKey, 0);
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
        PlayerPrefs.DeleteKey(NestsKey);
        PlayerPrefs.Save();
    }
}
