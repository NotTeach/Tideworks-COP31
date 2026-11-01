using System;
using UnityEngine;

/// <summary>
/// Basit ceviri: kaynak dil Ingilizce, anahtar Ingilizce metnin kendisi.
/// Loc.T("Start") Turkce secilmisse "Başla" doner; tabloda yoksa Ingilizcesi kalir.
/// Ceviriler: LocTable.cs. Dil degisince LanguageChanged tetiklenir (LocalizedText / LocalizedImage dinler).
/// </summary>
public static partial class Loc
{
    public static event Action LanguageChanged;

    public static bool Turkish => GameSettings.Language == GameLanguage.Turkish;

    public static string T(string en)
    {
        if (string.IsNullOrEmpty(en) || !Turkish) return en;
        return Tr.TryGetValue(en, out var tr) ? tr : en;
    }

    public static string F(string en, params object[] args) => string.Format(T(en), args);

    /// <summary>Editor'deki sahne kuruculari icin: bu metnin cevirisi var mi?</summary>
    public static bool Has(string en) => !string.IsNullOrEmpty(en) && Tr.ContainsKey(en);

    static GameLanguage last;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        last = GameSettings.Language;
        GameSettings.Changed -= OnSettingsChanged;
        GameSettings.Changed += OnSettingsChanged;
    }

    static void OnSettingsChanged()
    {
        var now = GameSettings.Language;
        if (now == last) return;
        last = now;
        LanguageChanged?.Invoke();
    }
}
