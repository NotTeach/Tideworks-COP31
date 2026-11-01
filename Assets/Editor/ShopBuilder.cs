using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static MainMenuSceneBuilder;

/// <summary>
/// Dukkan urunlerini (Assets/Resources/Shop/*.asset) ve dukkan penceresi prefab'ini
/// (Assets/Resources/UI/ShopPanel.prefab) kodla uretir. Gorseller: Assets/Sprites/Shop (Tools/MenuArt/shop/shop_art.py).
/// Tekrar uretmek icin: Unity menusu > Tideworks > Rebuild Shop
/// </summary>
[InitializeOnLoad]
public static class ShopBuilder
{
    const int BuilderVersion = 1;
    const string ItemsDir = "Assets/Resources/Shop/";
    const string PrefabPath = "Assets/Resources/UI/ShopPanel.prefab";
    const string ShopDir = "Assets/Sprites/Shop/";
    const string CoinDir = "Assets/Sprites/Workshop/";
    const string FontDir = "Assets/Fonts/";
    const string AutoBuildKey = "Tideworks.ShopAutoBuild.v1";
    static string VersionKey => "Tideworks.ShopBuilderVersion." + PlayerSettings.productGUID;

    static readonly Color Ink = new Color32(18, 62, 72, 255);
    static readonly Color Brown = new Color32(120, 90, 62, 255);

    static Font fontBody, fontHeavy, fontTitle;

    static ShopBuilder()
    {
        if (File.Exists(PrefabPath) && EditorPrefs.GetInt(VersionKey, 0) >= BuilderVersion) return;
        SceneAutoBuild.Request(AutoBuildKey, Build);
    }

    [MenuItem("Tideworks/Rebuild Shop")]
    static void Build()
    {
        ConfigureSprites(ShopDir);
        var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fontBody = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-Bold.ttf") ?? legacy;
        fontHeavy = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-ExtraBold.ttf") ?? legacy;
        fontTitle = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Baloo2-ExtraBold.ttf") ?? legacy;

        // ---------------- Urunler ----------------
        Directory.CreateDirectory(ItemsDir);
        Item("sewing_kit", "Sewing kit", "Needle, thread and patches for mending soft toys.", "Thread made from old rope",
            40, "ItemSewingKit", ShopItem.ItemType.Material, 2, 2, 0, "");
        Item("big_net", "Big net", "Catches all the trash around your cast in one go.", "Woven from collected ghost nets",
            80, "ItemBigNet", ShopItem.ItemType.Tool, 2, 0, 1, EquipmentState.Net);
        Item("sturdy_rod", "Sturdy rod", "Reels in twice as fast.", "",
            60, "ItemSturdyRod", ShopItem.ItemType.Tool, 2, 0, 2, EquipmentState.Rod);
        Item("amber_lamp", "Amber lamp", "A warm light that doesn't confuse the turtles.", "Glass made from sea glass",
            120, "ItemAmberLamp", ShopItem.ItemType.Material, 2, 3, 3, "");
        Item("solar_motor", "Solar motor", "Your boat goes faster and burns no fuel.", "Powered by the sun",
            150, "ItemSolarMotor", ShopItem.ItemType.Upgrade, 2, 0, 4, "");
        AssetDatabase.SaveAssets();

        // ---------------- Pencere prefab'i ----------------
        var root = new GameObject("ShopPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var panel = root.AddComponent<ShopPanel>();

        var dim = MakeImage(root.transform, "Dim", null);
        dim.color = new Color(0.02f, 0.08f, 0.1f, 0.6f);
        dim.raycastTarget = true;
        Stretch(dim.rectTransform);
        var board = new GameObject("Artboard", typeof(RectTransform), typeof(ArtboardFitter)).transform as RectTransform;
        board.SetParent(root.transform, false);
        board.GetComponent<ArtboardFitter>().artSize = new Vector2(2560, 1440);
        board.sizeDelta = new Vector2(2560, 1440);

        var modal = Img(board, "Modal", "ShopModal", new Rect(380, 130, 1800, 1180));
        modal.raycastTarget = true;
        var m = modal.transform;
        var kicker = Label(m, "Kicker", "", fontHeavy, 28, new Color32(200, 60, 46, 255), new Rect(84, 24, 900, 40));
        var title = Label(m, "Title", "", fontTitle, 88, Ink, new Rect(80, 56, 1100, 110));
        var subtitle = Label(m, "Subtitle", "", fontHeavy, 26, new Color32(30, 120, 124, 255), new Rect(86, 162, 900, 40));
        var pill = Img(m, "Coins", "ShopCoinPill", new Rect(1480, 62, 260, 96));
        var coin = MakeImage(pill.transform, "Coin", AssetDatabase.LoadAssetAtPath<Sprite>(CoinDir + "Coin.png"));
        Place(coin.rectTransform, new Rect(22, 18, 60, 60));
        var coinText = Label(pill.transform, "Count", "0", fontTitle, 48, Color.white, new Rect(96, 0, 150, 96));

        var cards = new ShopCard[6];
        for (int i = 0; i < 6; i++)
            cards[i] = Card(m, i, new Rect(75 + (i % 3) * 555, 228 + (i / 3) * 420, 520, 400));

        var info = Label(m, "Info", "", fontBody, 32, Ink, new Rect(84, 1074, 1290, 76));
        info.horizontalOverflow = HorizontalWrapMode.Wrap;
        var back = Img(m, "Back", "ShopBack", new Rect(1440, 1060, 300, 100));
        Label(back.transform, "Text", "Back", fontTitle, 40, Ink, new Rect(0, -2, 300, 100), TextAnchor.MiddleCenter, true);
        Button(back, panel.Close);
        var great = Img(m, "Great", "ShopGreat", new Rect(1400, 1050, 340, 110));
        Label(great.transform, "Text", "Great!", fontTitle, 46, Color.white, new Rect(0, -6, 340, 110), TextAnchor.MiddleCenter, true);
        Button(great, panel.Close);

        SetRefs(panel, ("modal", modal.rectTransform), ("kicker", kicker), ("title", title), ("subtitle", subtitle),
            ("coinText", coinText), ("infoText", info), ("backButton", back.gameObject), ("greatButton", great.gameObject));
        var so = new SerializedObject(panel);
        var arr = so.FindProperty("cards");
        arr.arraySize = cards.Length;
        for (int i = 0; i < cards.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        EditorPrefs.SetInt(VersionKey, BuilderVersion);
        Debug.Log("[Tideworks] Shop items and panel created: " + PrefabPath);
    }

    static void Item(string id, string name, string desc, string note, int price, string icon, ShopItem.ItemType type,
        int unlockDay, int neededOnDay, int order, string slot)
    {
        string path = ItemsDir + id + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<ShopItem>(path);
        if (!item)
        {
            item = ScriptableObject.CreateInstance<ShopItem>();
            AssetDatabase.CreateAsset(item, path);
        }
        item.id = id;
        item.displayName = name;
        item.description = desc;
        item.recycleNote = note;
        item.price = price;
        item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(ShopDir + icon + ".png");
        item.type = type;
        item.unlockDay = unlockDay;
        item.neededOnDay = neededOnDay;
        item.order = order;
        item.toolSlot = slot;
        EditorUtility.SetDirty(item);
    }

    static ShopCard Card(Transform parent, int index, Rect r)
    {
        var bg = Img(parent, "Card" + index, "ShopCard", r);
        var t = bg.transform;
        var card = bg.gameObject.AddComponent<ShopCard>();
        var frame = Img(t, "Selected", "ShopCardSelected", new Rect(-6, -6, 532, 412));
        var icon = MakeImage(t, "Icon", null);
        icon.preserveAspect = true;
        Place(icon.rectTransform, new Rect(24, 24, 150, 150));
        var name = Label(t, "Name", "", fontTitle, 40, Ink, new Rect(190, 20, 310, 64));
        name.resizeTextForBestFit = true;
        name.resizeTextMaxSize = 40;
        name.resizeTextMinSize = 26;
        name.horizontalOverflow = HorizontalWrapMode.Wrap;
        var desc = Label(t, "Description", "", fontBody, 24, Brown, new Rect(190, 86, 312, 118), TextAnchor.UpperLeft);
        desc.horizontalOverflow = HorizontalWrapMode.Wrap;
        var note = Img(t, "Note", "ShopNote", new Rect(24, 212, 472, 48));
        var noteText = Label(note.transform, "Text", "", fontHeavy, 22, new Color32(40, 110, 60, 255), new Rect(0, 0, 472, 48), TextAnchor.MiddleCenter);
        var btn = Img(t, "Button", "ShopBtnYellow", new Rect(24, 276, 472, 100));
        var btnLabel = Label(btn.transform, "Label", "", fontTitle, 40, Ink, new Rect(0, -6, 472, 100), TextAnchor.MiddleCenter);
        btn.raycastTarget = true;
        var button = btn.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = Color.white;
        button.colors = colors;
        var needed = Img(t, "Needed", "ShopNeeded", new Rect(286, -22, 220, 46));
        Label(needed.transform, "Text", "NEEDED TODAY", fontHeavy, 22, Color.white, new Rect(0, 0, 220, 46), TextAnchor.MiddleCenter, true);
        var fb = Label(t, "Feedback", "", fontTitle, 56, new Color32(60, 150, 90, 255), new Rect(0, 110, 520, 90), TextAnchor.MiddleCenter);
        fb.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.9f);
        SetRefs(card, ("icon", icon), ("nameText", name), ("description", desc), ("notePill", note.gameObject),
            ("noteText", noteText), ("button", button), ("buttonImage", btn), ("buttonLabel", btnLabel),
            ("neededBadge", needed.gameObject), ("selectedFrame", frame.gameObject), ("feedback", fb),
            ("yellow", Sprite("ShopBtnYellow")), ("grey", Sprite("ShopBtnGrey")), ("teal", Sprite("ShopBtnTeal")));
        return card;
    }

    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ShopDir + name + ".png");

    static Image Img(Transform parent, string name, string sprite, Rect r)
    {
        var img = MakeImage(parent, name, Sprite(sprite));
        Place(img.rectTransform, r);
        return img;
    }

    static Text Label(Transform parent, string name, string content, Font font, int size, Color color, Rect r,
        TextAnchor anchor = TextAnchor.MiddleLeft, bool localized = false)
    {
        var t = MakeText(parent, name, content, size, font, color, anchor);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Place(t.rectTransform, r);
        if (localized) t.gameObject.AddComponent<LocalizedText>().Setup(content, false);
        return t;
    }

    static void Button(Image img, UnityEngine.Events.UnityAction onClick)
    {
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        b.colors = colors;
        img.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.04f;
        UnityEventTools.AddPersistentListener(b.onClick, onClick);
    }
}
