using System;

/// <summary>
/// Oyuncunun altinlari. Butun kazanma / harcama noktalari buradan gecer; sayaclar OnChanged'i dinler.
/// Deger GameProgress.Coins'te (PlayerPrefs) saklanir.
/// </summary>
public static class PlayerWallet
{
    public static event Action<int> OnChanged;

    public static int Coins => GameProgress.Coins;

    public static void Add(int amount)
    {
        if (amount == 0) return;
        GameProgress.Coins = Coins + amount;
        OnChanged?.Invoke(Coins);
    }

    /// <summary>Yeterli altin varsa duser ve true doner; yoksa hicbir sey degismez.</summary>
    public static bool Spend(int amount)
    {
        if (amount < 0 || Coins < amount) return false;
        GameProgress.Coins = Coins - amount;
        GameSettings.Save();
        OnChanged?.Invoke(Coins);
        return true;
    }
}
