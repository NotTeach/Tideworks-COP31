using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Dukkan: urun katalogu (Resources/Shop), satin alma ve sahip olunanlar.
/// Kural: her urun bir kez alinir ve kalicidir; tuketilen bir sey yoktur.
/// Kayit: PlayerPrefs ("shop.owned" virgullu liste, "shop.introSeen").
/// </summary>
public static class ShopManager
{
    public enum BuyResult { Bought, AlreadyOwned, NotEnoughCoins }

    const string OwnedKey = "shop.owned";
    const string IntroKey = "shop.introSeen";

    public static event Action<ShopItem> OnPurchased;

    static List<ShopItem> catalog;
    static HashSet<string> owned;

    public static IReadOnlyList<ShopItem> Catalog
    {
        get
        {
            if (catalog == null)
                catalog = Resources.LoadAll<ShopItem>("Shop").OrderBy(i => i.order).ToList();
            return catalog;
        }
    }

    static HashSet<string> Owned
    {
        get
        {
            if (owned == null)
                owned = new HashSet<string>(PlayerPrefs.GetString(OwnedKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            return owned;
        }
    }

    public static ShopItem Find(string id) => Catalog.FirstOrDefault(i => i.id == id);

    public static bool Owns(string id) => Owned.Contains(id);

    /// <summary>Dukkan 2. gunden itibaren acik.</summary>
    public static bool IsOpen => GameProgress.CurrentDay >= 2;

    public static bool IntroSeen
    {
        get => PlayerPrefs.GetInt(IntroKey, 0) == 1;
        set { PlayerPrefs.SetInt(IntroKey, value ? 1 : 0); GameSettings.Save(); }
    }

    /// <summary>Kontroller, altini dusme, kaydetme ve olay burada.</summary>
    public static BuyResult TryBuy(ShopItem item)
    {
        if (Owns(item.id)) return BuyResult.AlreadyOwned;
        if (!PlayerWallet.Spend(item.price)) return BuyResult.NotEnoughCoins;
        Grant(item.id);
        if (item.type == ShopItem.ItemType.Tool && !string.IsNullOrEmpty(item.toolSlot))
            EquipmentState.Equip(item.toolSlot);
        OnPurchased?.Invoke(item);
        return BuyResult.Bought;
    }

    /// <summary>Ucretsiz verir (or. Elif'in babaannesinin dikis seti).</summary>
    public static void Grant(string id)
    {
        if (!Owned.Add(id)) return;
        PlayerPrefs.SetString(OwnedKey, string.Join(",", Owned));
        GameSettings.Save();
        var item = Find(id);
        if (item) OnPurchased?.Invoke(item);
    }

    /// <summary>Yeni oyun: dukkan kaydi silinir.</summary>
    public static void ResetSave()
    {
        PlayerPrefs.DeleteKey(OwnedKey);
        PlayerPrefs.DeleteKey(IntroKey);
        owned = null;
        EquipmentState.ResetSave();
    }
}
