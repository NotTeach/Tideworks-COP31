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

    const string HealthKey = "progress.oceanHealth";
    const string FishKey = "progress.fishReturned";

    /// <summary>Okyanus sagligi (yuzde): 1. gun 0'dan baslar, her temizlik gunu ~35 artar.</summary>
    public static int OceanHealth
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(HealthKey, 0), 0, 100);
        set => PlayerPrefs.SetInt(HealthKey, Mathf.Clamp(value, 0, 100));
    }

    /// <summary>Temiz suya geri donen balik turu sayisi (TrashCatalog.FishReturnOrder sirasiyla).</summary>
    public static int FishReturned
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(FishKey, 0), 0, TrashCatalog.FishSprites.Length);
        set => PlayerPrefs.SetInt(FishKey, Mathf.Clamp(value, 0, TrashCatalog.FishSprites.Length));
    }

    public static void NewGame()
    {
        PlayerPrefs.SetInt(DayKey, 1);
        PlayerPrefs.SetInt(CoinsKey, StartCoins);
        PlayerPrefs.SetInt(NestsKey, 0);
        PlayerPrefs.SetInt(HealthKey, 0);
        PlayerPrefs.SetInt(FishKey, 0);
        ShopManager.ResetSave();
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
        PlayerPrefs.DeleteKey(HealthKey);
        PlayerPrefs.DeleteKey(FishKey);
        ShopManager.ResetSave();
        PlayerPrefs.Save();
    }
}
