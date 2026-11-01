using System;
using UnityEngine;

/// <summary>Denizde kusanilan alet: "rod" (olta) ya da "net" (buyuk ag). PlayerPrefs'te saklanir.</summary>
public static class EquipmentState
{
    public const string Rod = "rod";
    public const string Net = "net";
    const string Key = "shop.tool";

    public static event Action<string> OnChanged;

    public static string CurrentTool
    {
        get
        {
            var t = PlayerPrefs.GetString(Key, Rod);
            return t == Net && !ShopManager.Owns("big_net") ? Rod : t;
        }
    }

    public static void Equip(string tool)
    {
        if (tool == Net && !ShopManager.Owns("big_net")) return;
        PlayerPrefs.SetString(Key, tool);
        GameSettings.Save();
        OnChanged?.Invoke(tool);
    }

    public static void ResetSave() => PlayerPrefs.DeleteKey(Key);
}
