using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static MainMenuSceneBuilder;

/// <summary>
/// Sahne kuruculari sahneyi kaydetmeden once Apply cagirir:
/// - Yazisi gorsele gomulu sprite'lara LocalizedImage (Turkce'de yazisi silinmis kopya: Assets/Sprites/&lt;Klasor&gt;/TR/)
///   ve her yazinin yerine sadece Turkce'de gorunen bir etiket (LocalizedText) ekler.
///   Yazilarin yeri/boyu/rengi: Assets/Localization/ArtLabels.json (uretici: Tools/Localization/erase_labels.py)
/// - Cevirisi olan (LocTable.cs) sabit yazilara LocalizedText ekler.
/// </summary>
public static class ArtLocalization
{
    const string LabelsPath = "Assets/Localization/ArtLabels.json";
    const string SpriteRoot = "Assets/Sprites/";
    const string FontDir = "Assets/Fonts/";

    [System.Serializable] class Label { public string en, font, align; public int size, r, g, b; public float x, y, w, h; }
    [System.Serializable] class SpriteLabels { public string sprite; public int width, height; public Label[] labels; }
    [System.Serializable] class Root { public SpriteLabels[] sprites; }

    public static void Apply(Transform root)
    {
        // Sabit yazilar (koddan doldurulanlar bos baslar, onlara dokunulmaz)
        foreach (var text in root.GetComponentsInChildren<Text>(true))
            if (Loc.Has(text.text) && !text.GetComponent<LocalizedText>())
                text.gameObject.AddComponent<LocalizedText>().Setup(text.text, false);

        if (!File.Exists(LabelsPath)) { Debug.LogWarning("[Tideworks] " + LabelsPath + " yok"); return; }
        var data = JsonUtility.FromJson<Root>(File.ReadAllText(LabelsPath));
        foreach (var dir in data.sprites.Select(s => SpriteRoot + s.sprite.Split('/')[0] + "/TR/").Distinct())
            if (Directory.Exists(dir)) ConfigureSprites(dir);

        var bySprite = data.sprites.ToDictionary(s => SpriteRoot + s.sprite + ".png");
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (!image.sprite) continue;
            var path = AssetDatabase.GetAssetPath(image.sprite);
            if (!bySprite.TryGetValue(path, out var entry) || image.GetComponent<LocalizedImage>()) continue;

            var parts = entry.sprite.Split('/');
            var turkish = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + parts[0] + "/TR/" + parts[1] + ".png");
            image.gameObject.AddComponent<LocalizedImage>().Setup(image.sprite, turkish);

            var size = image.rectTransform.rect.size;
            float sx = size.x / entry.width, sy = size.y / entry.height;
            foreach (var label in entry.labels)
            {
                var t = MakeText(image.transform, "TR_" + label.en, label.en, Mathf.RoundToInt(label.size * sy),
                    LoadFont(label.font), new Color32((byte)label.r, (byte)label.g, (byte)label.b, 255),
                    label.align == "c" ? TextAnchor.MiddleCenter : label.align == "r" ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Truncate;
                t.resizeTextForBestFit = true;
                t.resizeTextMaxSize = t.fontSize;
                t.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(t.fontSize * 0.55f));
                Place(t.rectTransform, new Rect(label.x * sx, label.y * sy, label.w * sx, label.h * sy));
                t.gameObject.AddComponent<LocalizedText>().Setup(label.en, true);
                t.enabled = false;
            }
        }
    }

    static readonly Dictionary<string, string> Fonts = new Dictionary<string, string>
    {
        { "N", "Nunito-ExtraBold.ttf" }, { "n", "Nunito-Bold.ttf" }, { "C", "Caveat-Bold.ttf" }, { "B", "Baloo2-ExtraBold.ttf" },
    };

    static Font LoadFont(string key) =>
        AssetDatabase.LoadAssetAtPath<Font>(FontDir + Fonts[key]) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
