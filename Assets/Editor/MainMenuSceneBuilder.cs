using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Giris ekrani sahnesini (Assets/Scenes/MainMenu.unity) kodla kurar: "Grandpa's notebook" tasarimi.
/// Arka plan tasarim gorselinin kendisidir; butonlar ayni gorselden kesilmis parcalardir ve
/// gorseldeki yerlerinin tam ustune oturur (Assets/Sprites/MainMenu, uretici: Tools/MenuArt).
/// Builder surumu degisince proje acildiginda bir kez otomatik calisir.
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Main Menu Scene
/// (DIKKAT: sahnede elle yaptiginiz degisiklikler silinir.)
/// </summary>
[InitializeOnLoad]
public static class MainMenuSceneBuilder
{
    const int BuilderVersion = 7;
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    const string SpriteDir = "Assets/Sprites/MainMenu/";
    const string SettingsDir = "Assets/Sprites/Settings/";
    const string FontDir = "Assets/Fonts/";
    const string AutoBuildKey = "Tideworks.MainMenuAutoBuild.v7";
    static string VersionKey => "Tideworks.MainMenuBuilderVersion." + PlayerSettings.productGUID;

    // Gorseldeki konumlar (piksel, sol ust koseden): x, y, genislik, yukseklik
    static readonly Vector2 ArtSize = new Vector2(2752, 1536);
    static readonly Rect StartRect = new Rect(246, 730, 722, 171);
    static readonly Rect ContinueRect = new Rect(251, 885, 721, 147);
    static readonly Rect BadgeRect = new Rect(752, 925, 171, 48);
    static readonly Rect HowToPlayRect = new Rect(257, 1032, 352, 118);
    static readonly Rect SettingsRect = new Rect(623, 1019, 352, 119);
    static readonly Rect NotebookRect = new Rect(60, 55, 1040, 1396);
    static readonly Rect PageContentRect = new Rect(240, 175, 720, 1150);
    // Settings sayfasi (ayni 2752x1536 olcek)
    static readonly Rect SettingsBackRect = new Rect(57, 42, 137, 138);
    static readonly Rect SettingsSaveRect = new Rect(2085, 1390, 457, 138);
    static readonly Rect SettingsResetRect = new Rect(292, 1120, 416, 116);
    static readonly float[] SliderRows = { 473, 603, 732 };          // Music, Sound effects, Ocean waves (merkez y)
    static readonly float[] SwitchRows = { 876, 1010, 1143, 1277 }; // Cozy, Hints, Material labels, Reduce motion
    static readonly Vector2 MoonCenter = new Vector2(2450, 230);
    static readonly Vector3[] Stars = // x, y, boyut
    {
        new Vector3(1633, 403, 90), new Vector3(1548, 78, 44), new Vector3(1204, 121, 40), new Vector3(2174, 78, 40),
        new Vector3(1291, 445, 36), new Vector3(1720, 227, 34), new Vector3(2064, 248, 34), new Vector3(2663, 399, 36),
    };

    static readonly Color Ink = new Color(0.29f, 0.17f, 0.10f);        // koyu kahve yazi
    static readonly Color InkSoft = new Color(0.42f, 0.29f, 0.19f);
    static readonly Color Teal = new Color(0.06f, 0.27f, 0.31f);       // baslik
    static readonly Color Rust = new Color(0.72f, 0.26f, 0.13f);       // "The Last Beach" kirmizisi
    static readonly Color SegmentInk = new Color32(60, 36, 22, 255);
    static readonly Color PercentRust = new Color32(175, 73, 33, 255);
    static readonly Color Night = new Color(0.04f, 0.13f, 0.20f);

    static Font fontBody, fontHeavy, fontHand;

    static MainMenuSceneBuilder()
    {
        if (File.Exists(ScenePath) && EditorPrefs.GetInt(VersionKey, 1) >= BuilderVersion) return;
        SceneAutoBuild.Request(AutoBuildKey, () => Build(askToSave: true));
    }

    [MenuItem("Tideworks/Rebuild Main Menu Scene")]
    static void RebuildMenu()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild Main Menu",
                "MainMenu sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
                "Rebuild", "Cancel"))
            return;
        Build(askToSave: true);
    }

    [MenuItem("Tideworks/Reset Saved Progress")]
    static void ResetProgress()
    {
        GameProgress.Clear();
        Debug.Log("[Tideworks] Kayitli gun ilerlemesi silindi.");
    }

    static void Build(bool askToSave)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConfigureSprites();
        ConfigureSprites(SettingsDir);
        var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fontBody = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-Bold.ttf") ?? legacy;
        fontHeavy = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-ExtraBold.ttf") ?? legacy;
        fontHand = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Kalam-Bold.ttf") ?? legacy;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Kamera ---
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Night;
        camGo.transform.position = new Vector3(0, 0, -10);

        // --- EventSystem (Input System) ---
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // --- Canvas ---
        var canvasGo = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var controller = canvasGo.AddComponent<MainMenuController>();

        // Ekran orani gorselden farkliysa kenarlarda kalan bosluk: gorselin bulanik, karartilmis kopyasi
        var backdropRt = new GameObject("Backdrop", typeof(RectTransform), typeof(ArtboardFitter)).transform as RectTransform;
        backdropRt.SetParent(canvasGo.transform, false);
        var backdropFit = backdropRt.GetComponent<ArtboardFitter>();
        backdropFit.artSize = ArtSize;
        backdropFit.mode = ArtboardFitter.FitMode.Cover;
        var backdrop = MakeImage(backdropRt, "Image", LoadSprite("MenuBackdrop"));
        Stretch(backdrop.rectTransform);

        // Tum menu, tasarim gorselinin piksel koordinatlarinda duran bir "artboard" icinde
        var artboard = new GameObject("Artboard", typeof(RectTransform), typeof(ArtboardFitter)).transform as RectTransform;
        artboard.SetParent(canvasGo.transform, false);
        artboard.GetComponent<ArtboardFitter>().artSize = ArtSize;
        artboard.sizeDelta = ArtSize;

        var bg = MakeImage(artboard, "Background", LoadSprite("MenuBackground"));
        Place(bg.rectTransform, new Rect(Vector2.zero, ArtSize));

        // --- Canlilik: ay isiltisi + yildiz parlamalari ---
        var glow = MakeImage(artboard, "MoonGlow", LoadSprite("Glow"));
        Place(glow.rectTransform, new Rect(MoonCenter.x - 380, MoonCenter.y - 380, 760, 760));
        Pulse(glow, 0.15f, 0.4f, 0.18f, 0f, 0.06f);
        for (int i = 0; i < Stars.Length; i++)
        {
            var s = Stars[i];
            var star = MakeImage(artboard, "Sparkle" + i, LoadSprite("Sparkle"));
            Place(star.rectTransform, new Rect(s.x - s.z / 2, s.y - s.z / 2, s.z, s.z));
            Pulse(star, 0f, 0.85f, 0.25f + 0.07f * (i % 4), i * 0.37f % 1f, 0.3f);
        }

        // --- Ana sayfa: gorseldeki butonlarin ustune oturan gercek butonlar ---
        var main = MakePage(artboard, "MainPage");
        var startBtn = MakeArtButton(main.transform, "StartButton", "BtnStart", StartRect, controller.StartGame);
        var continueBtn = MakeArtButton(main.transform, "ContinueButton", "BtnContinue", ContinueRect, controller.ContinueGame);
        var badge = MakeText(continueBtn.transform, "DayBadge", "Day 2 of 3", 30, fontHeavy, Ink);
        Place(badge.rectTransform, new Rect(BadgeRect.x - ContinueRect.x, BadgeRect.y - ContinueRect.y, BadgeRect.width, BadgeRect.height));
        MakeArtButton(main.transform, "HowToPlayButton", "BtnHowToPlay", HowToPlayRect, controller.ShowHowToPlay);
        MakeArtButton(main.transform, "SettingsButton", "BtnSettings", SettingsRect, controller.ShowSettings);

        // --- How to play: defterin bos sayfasi ---
        var howTo = MakePage(artboard, "HowToPlayPage");
        var howToCol = MakeNotebookPage(howTo, "How to play", "Three days to bring the beach back.");
        MakeSpacer(howToCol, 18);
        MakeHowToRow(howToCol, "IconFix", "Fix",
            "Fix up grandpa's workshop and boat, one part at a time.");
        MakeHowToRow(howToCol, "IconCollect", "Collect",
            "Sail out with the net and pull plastic, glass, metal, wood and ghost nets from the sea.");
        MakeHowToRow(howToCol, "IconSave", "Save",
            "Recycle what you find and keep the shore clean so the turtle hatchlings reach the water.");
        MakeSpacer(howToCol, 10);
        MakeText(howToCol, "Motto", "Every piece counts!", 46, fontHand, Rust).gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
        MakeFlexibleSpacer(howToCol);
        var howToBack = MakeCreamButton(howToCol, "Back", controller.ShowMain, 340);

        // --- Settings: dedenin acik defteri (tam ekran). Etiketler gorselde, kontroller ustunde ---
        var settings = MakePage(artboard, "SettingsPage");
        var settingsBg = MakeImage(settings.transform, "Background", LoadSprite("SettingsBackground", SettingsDir));
        settingsBg.raycastTarget = true; // alttaki ana sayfa butonlarina tiklanmasin
        Place(settingsBg.rectTransform, new Rect(Vector2.zero, ArtSize));
        var settingsMenu = settings.gameObject.AddComponent<SettingsMenu>();

        var music = MakeArtSlider(settings.transform, "Music", SliderRows[0], out var musicValue);
        var effects = MakeArtSlider(settings.transform, "SoundEffects", SliderRows[1], out var effectsValue);
        var waves = MakeArtSlider(settings.transform, "OceanWaves", SliderRows[2], out var wavesValue);
        // "Sound" basliginin saginda: Auto = onerilen ses dengesi, Manual = kaydiricilar
        var audioMode = MakeSegmented(settings.transform, "AudioMode", new Rect(792, 330, 350, 88),
            new[] { "Auto", "Manual" }, new[] { 30, 30 });
        var language = MakeSegmented(settings.transform, "Language", new Rect(301, 941, 989, 127),
            new[] { "English", "Türkçe" }, new[] { 36, 36 });
        var timeOfDay = MakeSegmented(settings.transform, "TimeOfDay", new Rect(1780, 421, 679, 114),
            new[] { "Auto", "Day", "Night" }, new[] { 34, 34, 34 });
        var textSize = MakeSegmented(settings.transform, "TextSize", new Rect(1780, 556, 679, 113),
            new[] { "Aa", "Aa", "Aa" }, new[] { 26, 34, 46 });
        var cozy = MakeSwitch(settings.transform, "CozyMode", SwitchRows[0]);
        var hints = MakeSwitch(settings.transform, "Hints", SwitchRows[1]);
        var materialLabels = MakeSwitch(settings.transform, "MaterialLabels", SwitchRows[2]);
        var reduceMotion = MakeSwitch(settings.transform, "ReduceMotion", SwitchRows[3]);

        MakeArtButton(settings.transform, "ResetButton", "BtnReset", SettingsResetRect, settingsMenu.ResetToDefaults, SettingsDir)
            .GetComponent<MenuButtonFx>().hoverScale = 1.03f;
        var saveBack = MakeArtButton(settings.transform, "SaveBackButton", "BtnSaveBack", SettingsSaveRect, settingsMenu.Save, SettingsDir);
        UnityEventTools.AddPersistentListener(saveBack.onClick, controller.ShowMain);
        // Geri ok kaydetmeden cikar (SettingsMenu sayfa kapaninca eski ayarlara doner)
        MakeArtButton(settings.transform, "BackButton", "BtnBack", SettingsBackRect, controller.ShowMain, SettingsDir);

        SetRefs(settingsMenu,
            ("audioMode", audioMode), ("musicSlider", music), ("effectsSlider", effects), ("wavesSlider", waves),
            ("musicValue", musicValue), ("effectsValue", effectsValue), ("wavesValue", wavesValue),
            ("language", language), ("timeOfDay", timeOfDay), ("textSize", textSize),
            ("cozyMode", cozy), ("hints", hints), ("materialLabels", materialLabels), ("reduceMotion", reduceMotion));

        // --- Ekran gecisi icin karartma (sahne acilirken / oyuna gecerken) ---
        var faderImg = MakeImage(canvasGo.transform, "ScreenFader", null);
        faderImg.color = Night;
        faderImg.raycastTarget = true;
        Stretch(faderImg.rectTransform);
        var fader = faderImg.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 0f;
        faderImg.gameObject.SetActive(false);

        SetRefs(controller,
            ("mainPage", main), ("howToPlayPage", howTo), ("settingsPage", settings), ("screenFader", fader),
            ("continueButton", continueBtn), ("continueBadge", badge),
            ("mainFirstSelected", startBtn), ("howToPlayFirstSelected", howToBack), ("settingsFirstSelected", music));

        foreach (var page in new[] { howTo, settings })
        {
            page.alpha = 0f;
            page.gameObject.SetActive(false);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        ArtLocalization.Apply(canvasGo.transform);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        EditorPrefs.SetInt(VersionKey, BuilderVersion);
        Debug.Log("[Tideworks] Main menu scene created: " + ScenePath);
    }

    // ---------------- Gorseller ----------------

    internal static void ConfigureSprites(string dir = SpriteDir)
    {
        foreach (var path in Directory.GetFiles(dir, "*.png").Select(p => p.Replace('\\', '/')))
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            bool big = name.EndsWith("Background") || name == "NotebookPage";
            var border = SpriteBorders.TryGetValue(name, out var b) ? b : Vector4.zero;

            if (ti.textureType == TextureImporterType.Sprite && ti.maxTextureSize == 4096 && ti.spriteBorder == border) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 4096;
            ti.textureCompression = big ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            ti.spriteBorder = border;
            ti.SaveAndReimport();
        }
    }

    // 9-slice kenarlari (sol, alt, sag, ust)
    static readonly System.Collections.Generic.Dictionary<string, Vector4> SpriteBorders =
        new System.Collections.Generic.Dictionary<string, Vector4>
    {
        { "BtnCreamBlank", new Vector4(48, 42, 48, 30) },
        { "SegFrame", new Vector4(40, 40, 40, 40) },
        { "SegPill", new Vector4(26, 26, 26, 26) },
        { "SliderTrack", new Vector4(16, 0, 16, 0) },
        { "SliderFill", new Vector4(16, 0, 16, 0) },
    };

    internal static Sprite LoadSprite(string name, string dir = SpriteDir)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(dir + name + ".png");
        if (!sprite) Debug.LogWarning("[Tideworks] Sprite bulunamadi: " + dir + name + ".png");
        return sprite;
    }

    // ---------------- Sayfalar ----------------

    static CanvasGroup MakePage(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        return go.GetComponent<CanvasGroup>();
    }

    /// <summary>Bos defter sayfasini koyar, basligi yazar ve icerik kolonunu dondurur.</summary>
    static RectTransform MakeNotebookPage(CanvasGroup page, string title, string subtitle)
    {
        var paper = MakeImage(page.transform, "Notebook", LoadSprite("NotebookPage"));
        paper.raycastTarget = true; // alttaki ana sayfa butonlarina tiklanmasin
        Place(paper.rectTransform, NotebookRect);

        var col = MakeColumn(page.transform, "Content", 16);
        Place(col, PageContentRect);
        MakeText(col, "Title", title, 104, fontHand, Teal).gameObject.AddComponent<LayoutElement>().preferredHeight = 130;
        MakeText(col, "Subtitle", subtitle, 40, fontHand, Rust).gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
        return col;
    }

    static void MakeHowToRow(Transform parent, string icon, string heading, string body)
    {
        var row = MakeRow(parent, heading + "Row", 34);
        row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

        var img = MakeImage(row, "Icon", LoadSprite(icon));
        img.preserveAspect = true;
        var ile = img.gameObject.AddComponent<LayoutElement>();
        ile.preferredWidth = 120;
        ile.preferredHeight = 120;

        var texts = MakeColumn(row, "Text", 2);
        texts.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
        texts.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
        texts.gameObject.AddComponent<LayoutElement>().preferredWidth = 560;
        MakeText(texts, "Heading", heading, 44, fontHeavy, Ink, TextAnchor.MiddleLeft);
        var b = MakeText(texts, "Body", body, 30, fontBody, InkSoft, TextAnchor.UpperLeft);
        b.lineSpacing = 1.05f;
    }

    // ---------------- Yardimcilar ----------------

    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        if (File.Exists(GameScenePath) && scenes.All(s => s.path != GameScenePath))
            scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    internal static void SetRefs(Object target, params (string field, Object value)[] refs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in refs)
            so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Pulse(Image img, float minAlpha, float maxAlpha, float speed, float phase, float scale)
    {
        var p = img.gameObject.AddComponent<MenuPulse>();
        p.minAlpha = minAlpha;
        p.maxAlpha = maxAlpha;
        p.speed = speed;
        p.phase = phase;
        p.scaleAmount = scale;
        img.color = new Color(1, 1, 1, maxAlpha);
    }

    /// <summary>Gorsel koordinatina (sol ust kose orijinli) yerlestirir; pivot ortada kalir ki olcekleme yerinde olsun.</summary>
    internal static void Place(RectTransform rt, Rect r)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(r.x + r.width / 2, -(r.y + r.height / 2));
        rt.sizeDelta = r.size;
    }

    internal static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    internal static Image MakeImage(Transform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    static Button MakeArtButton(Transform parent, string name, string sprite, Rect rect, UnityAction onClick,
        string dir = SpriteDir)
    {
        var img = MakeImage(parent, name, LoadSprite(sprite, dir));
        img.raycastTarget = true;
        Place(img.rectTransform, rect);

        var button = img.gameObject.AddComponent<Button>();
        // Alttaki gorselde ayni buton var: renkler opak kalmali yoksa altindaki gorunur
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = new Color(0.62f, 0.64f, 0.68f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        img.gameObject.AddComponent<MenuButtonFx>();
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }

    static Button MakeCreamButton(Transform parent, string label, UnityAction onClick, float width)
    {
        var img = MakeImage(parent, label + "Button", LoadSprite("BtnCreamBlank"));
        img.type = Image.Type.Sliced;
        img.raycastTarget = true;
        var le = img.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = 112;

        var button = img.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        img.gameObject.AddComponent<MenuButtonFx>();

        var text = MakeText(img.transform, "Label", label, 40, fontHeavy, Ink);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(0, 10); // alttaki golge payi
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }

    static RectTransform MakeColumn(Transform parent, string name, float spacing)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        return (RectTransform)go.transform;
    }

    static RectTransform MakeRow(Transform parent, string name, float spacing)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        return (RectTransform)go.transform;
    }

    static void MakeSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
    }

    static void MakeFlexibleSpacer(Transform parent)
    {
        var go = new GameObject("FlexibleSpacer", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().flexibleHeight = 1;
    }

    internal static Text MakeText(Transform parent, string name, string content, int size, Font font, Color color,
        TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.raycastTarget = false;
        return t;
    }

    // ---------------- Settings kontrolleri ----------------

    /// <summary>Yatayda ebeveyne yayilan, dikeyde ortalanmis sabit yukseklikte serit.</summary>
    static void Band(RectTransform rt, float left, float right, float height)
    {
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(-(left + right), height);
        rt.anchoredPosition = new Vector2((left - right) / 2, 0);
    }

    static Slider MakeArtSlider(Transform parent, string name, float cy, out Text valueText)
    {
        var go = new GameObject(name + "Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        Place(rt, new Rect(656, cy - 27, 486, 54));

        var track = MakeImage(rt, "Track", LoadSprite("SliderTrack", SettingsDir));
        track.type = Image.Type.Sliced;
        track.raycastTarget = true; // raya tiklayinca deger oraya atlar
        Band(track.rectTransform, 0, 0, 26);

        // Dolu kisim tutamacin ortasina kadar uzansin (tutamac yari genisligi = 18)
        var fillArea = new GameObject("Fill Area", typeof(RectTransform)).transform as RectTransform;
        fillArea.SetParent(rt, false);
        Band(fillArea, 0, 18, 26);
        var fill = MakeImage(fillArea, "Fill", LoadSprite("SliderFill", SettingsDir));
        fill.type = Image.Type.Sliced;
        var frt = fill.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(0.7f, 1f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = new Vector2(18, 0);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).transform as RectTransform;
        handleArea.SetParent(rt, false);
        Band(handleArea, 18, 18, 54);
        var knob = MakeImage(handleArea, "Handle", LoadSprite("SliderKnob", SettingsDir));
        knob.raycastTarget = true;
        var krt = knob.rectTransform;
        krt.anchorMin = krt.anchorMax = new Vector2(0.7f, 0.5f);
        krt.sizeDelta = new Vector2(36, 54);

        var slider = go.GetComponent<Slider>();
        slider.fillRect = frt;
        slider.handleRect = krt;
        slider.targetGraphic = knob;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0.7f;
        var colors = slider.colors;
        colors.highlightedColor = colors.selectedColor = new Color(1f, 0.88f, 0.8f);
        colors.pressedColor = new Color(0.85f, 0.75f, 0.7f);
        slider.colors = colors;

        valueText = MakeText(parent, name + "Value", "70%", 40, fontHeavy, PercentRust);
        Place(valueText.rectTransform, new Rect(1188, cy - 30, 120, 60));
        return slider;
    }

    static SegmentedControl MakeSegmented(Transform parent, string name, Rect r, string[] options, int[] sizes)
    {
        var frame = MakeImage(parent, name, LoadSprite("SegFrame", SettingsDir));
        frame.type = Image.Type.Sliced;
        Place(frame.rectTransform, r);

        const float inset = 14;
        float w = (r.width - 2 * inset) / options.Length, h = r.height - 2 * inset;
        var pill = MakeImage(frame.transform, "Pill", LoadSprite("SegPill", SettingsDir));
        pill.type = Image.Type.Sliced;
        Place(pill.rectTransform, new Rect(inset + 2, inset, w - 4, h));

        var buttons = new Button[options.Length];
        var labels = new Text[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            var hit = MakeImage(frame.transform, "Option" + i, null);
            hit.color = new Color(1, 1, 1, 0);
            hit.raycastTarget = true;
            Place(hit.rectTransform, new Rect(inset + i * w + 2, inset, w - 4, h));
            buttons[i] = hit.gameObject.AddComponent<Button>();
            buttons[i].transition = Selectable.Transition.None;
            hit.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.06f;

            labels[i] = MakeText(hit.transform, "Label", options[i], sizes[i], fontHeavy, SegmentInk);
            Stretch(labels[i].rectTransform);
        }

        var control = frame.gameObject.AddComponent<SegmentedControl>();
        control.segments = buttons;
        control.labels = labels;
        control.pill = pill.rectTransform;
        control.normalColor = SegmentInk;
        return control;
    }

    static Toggle MakeSwitch(Transform parent, string name, float cy)
    {
        var track = MakeImage(parent, name + "Switch", LoadSprite("SwitchOn", SettingsDir));
        track.raycastTarget = true;
        Place(track.rectTransform, new Rect(2320, cy - 41, 140, 82));

        var knob = MakeImage(track.transform, "Knob", LoadSprite("SwitchKnob", SettingsDir));
        var krt = knob.rectTransform;
        krt.anchorMin = krt.anchorMax = new Vector2(0, 0.5f);
        krt.sizeDelta = new Vector2(62, 62);
        krt.anchoredPosition = new Vector2(99, 0);

        var toggle = track.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = track;
        toggle.graphic = null;
        toggle.isOn = true;
        var colors = toggle.colors;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.fadeDuration = 0.08f;
        toggle.colors = colors;
        track.gameObject.AddComponent<MenuButtonFx>();

        var sw = track.gameObject.AddComponent<SwitchToggle>();
        sw.track = track;
        sw.knob = krt;
        sw.onSprite = LoadSprite("SwitchOn", SettingsDir);
        sw.offSprite = LoadSprite("SwitchOff", SettingsDir);
        return toggle;
    }
}
