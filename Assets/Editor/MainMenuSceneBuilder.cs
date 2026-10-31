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
    const int BuilderVersion = 4;
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    const string SpriteDir = "Assets/Sprites/MainMenu/";
    const string FontDir = "Assets/Fonts/";
    const string AutoBuildKey = "Tideworks.MainMenuAutoBuildTried.v4";
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
    static readonly Vector2 MoonCenter = new Vector2(2450, 230);
    static readonly Vector3[] Stars = // x, y, boyut
    {
        new Vector3(1633, 403, 90), new Vector3(1548, 78, 44), new Vector3(1204, 121, 40), new Vector3(2174, 78, 40),
        new Vector3(1291, 445, 36), new Vector3(1720, 227, 34), new Vector3(2064, 248, 34), new Vector3(2663, 399, 36),
    };

    static readonly Color Ink = new Color(0.29f, 0.17f, 0.10f);        // koyu kahve yazi
    static readonly Color InkSoft = new Color(0.42f, 0.29f, 0.19f);
    static readonly Color Teal = new Color(0.06f, 0.27f, 0.31f);       // baslik
    static readonly Color TealFill = new Color(0.11f, 0.45f, 0.47f);
    static readonly Color Rust = new Color(0.72f, 0.26f, 0.13f);       // "The Last Beach" kirmizisi
    static readonly Color Cream = new Color(0.98f, 0.94f, 0.85f);
    static readonly Color Night = new Color(0.04f, 0.13f, 0.20f);

    static Font fontBody, fontHeavy, fontHand;
    static DefaultControls.Resources resources;

    static MainMenuSceneBuilder()
    {
        if (File.Exists(ScenePath) && EditorPrefs.GetInt(VersionKey, 1) >= BuilderVersion) return;
        if (SessionState.GetBool(AutoBuildKey, false)) return;
        SessionState.SetBool(AutoBuildKey, true);
        EditorApplication.delayCall += () => Build(askToSave: true);
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
        var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fontBody = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-Bold.ttf") ?? legacy;
        fontHeavy = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-ExtraBold.ttf") ?? legacy;
        fontHand = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Kalam-Bold.ttf") ?? legacy;
        resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };

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

        // --- Settings: defterin bos sayfasi ---
        var settings = MakePage(artboard, "SettingsPage");
        var settingsCol = MakeNotebookPage(settings, "Settings", "Make it sound and look just right.");
        MakeSpacer(settingsCol, 18);
        var master = MakeSliderRow(settingsCol, "Master", out var masterValue);
        var music = MakeSliderRow(settingsCol, "Music", out var musicValue);
        var sfx = MakeSliderRow(settingsCol, "Effects", out var sfxValue);
        MakeSpacer(settingsCol, 6);
        var fullscreen = MakeToggleRow(settingsCol, "Fullscreen");
        var resolution = MakeDropdownRow(settingsCol, "Resolution");
        var quality = MakeDropdownRow(settingsCol, "Quality");
        MakeFlexibleSpacer(settingsCol);
        var settingsButtons = MakeRow(settingsCol, "Buttons", 30);
        var settingsMenu = settings.gameObject.AddComponent<SettingsMenu>();
        MakeCreamButton(settingsButtons, "Reset", settingsMenu.ResetToDefaults, 320);
        MakeCreamButton(settingsButtons, "Back", controller.ShowMain, 320);

        SetRefs(settingsMenu,
            ("masterVolumeSlider", master), ("musicVolumeSlider", music), ("sfxVolumeSlider", sfx),
            ("masterVolumeValue", masterValue), ("musicVolumeValue", musicValue), ("sfxVolumeValue", sfxValue),
            ("fullscreenToggle", fullscreen), ("resolutionDropdown", resolution), ("qualityDropdown", quality));

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
            ("mainFirstSelected", startBtn), ("howToPlayFirstSelected", howToBack), ("settingsFirstSelected", master));

        foreach (var page in new[] { howTo, settings })
        {
            page.alpha = 0f;
            page.gameObject.SetActive(false);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
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
            var border = name == "BtnCreamBlank" ? new Vector4(48, 42, 48, 30) : Vector4.zero;

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

    static Button MakeArtButton(Transform parent, string name, string sprite, Rect rect, UnityAction onClick)
    {
        var img = MakeImage(parent, name, LoadSprite(sprite));
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

    static RectTransform MakeLabeledRow(Transform parent, string label)
    {
        var row = MakeRow(parent, label.Replace(" ", "") + "Row", 20);
        var text = MakeText(row, "Label", label, 36, fontHeavy, Ink, TextAnchor.MiddleLeft);
        var le = text.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 250;
        le.preferredHeight = 76;
        return row;
    }

    static Slider MakeSliderRow(Transform parent, string label, out Text valueText)
    {
        var row = MakeLabeledRow(parent, label);
        var go = DefaultControls.CreateSlider(resources);
        go.name = "Slider";
        go.transform.SetParent(row, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 340;
        le.preferredHeight = 40;

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        var bgImage = go.transform.Find("Background")?.GetComponent<Image>();
        if (bgImage) bgImage.color = new Color(0.55f, 0.40f, 0.25f, 0.35f);
        if (slider.fillRect) slider.fillRect.GetComponent<Image>().color = TealFill;
        if (slider.handleRect)
        {
            slider.handleRect.GetComponent<Image>().color = Rust;
            slider.handleRect.sizeDelta = new Vector2(44, 4);
        }
        var colors = slider.colors;
        colors.highlightedColor = colors.selectedColor = new Color(1f, 0.85f, 0.75f);
        slider.colors = colors;

        valueText = MakeText(row, "Value", "100%", 32, fontBody, InkSoft, TextAnchor.MiddleRight);
        var vle = valueText.gameObject.AddComponent<LayoutElement>();
        vle.preferredWidth = 90;
        vle.preferredHeight = 76;
        return slider;
    }

    static Toggle MakeToggleRow(Transform parent, string label)
    {
        var row = MakeLabeledRow(parent, label);
        var go = DefaultControls.CreateToggle(resources);
        go.name = "Toggle";
        go.transform.SetParent(row, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 450;
        le.preferredHeight = 56;

        var toggleLabel = go.transform.Find("Label");
        if (toggleLabel) Object.DestroyImmediate(toggleLabel.gameObject);

        var box = (RectTransform)go.transform.Find("Background");
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(0, 0.5f);
        box.anchoredPosition = Vector2.zero;
        box.sizeDelta = new Vector2(52, 52);
        box.GetComponent<Image>().color = Cream;
        var check = (RectTransform)box.Find("Checkmark");
        check.anchorMin = Vector2.zero;
        check.anchorMax = Vector2.one;
        check.offsetMin = new Vector2(5, 5);
        check.offsetMax = new Vector2(-5, -5);
        check.GetComponent<Image>().color = TealFill;
        return go.GetComponent<Toggle>();
    }

    static Dropdown MakeDropdownRow(Transform parent, string label)
    {
        var row = MakeLabeledRow(parent, label);
        var go = DefaultControls.CreateDropdown(resources);
        go.name = "Dropdown";
        go.transform.SetParent(row, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 450;
        le.preferredHeight = 80;

        var frame = go.GetComponent<Image>();
        frame.sprite = LoadSprite("BtnCreamBlank");
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 1.4f;

        var dd = go.GetComponent<Dropdown>();
        dd.captionText.font = fontBody;
        dd.captionText.fontSize = 30;
        dd.captionText.color = Ink;
        dd.captionText.rectTransform.offsetMin = new Vector2(26, 12);
        dd.captionText.rectTransform.offsetMax = new Vector2(-50, -6);
        dd.itemText.font = fontBody;
        dd.itemText.fontSize = 28;
        dd.itemText.color = Ink;
        dd.options.Clear();

        var arrow = (RectTransform)go.transform.Find("Arrow");
        arrow.GetComponent<Image>().color = Ink;
        arrow.sizeDelta = new Vector2(30, 30);
        arrow.anchoredPosition = new Vector2(-30, 4);

        var template = dd.template;
        template.sizeDelta = new Vector2(template.sizeDelta.x, 320);
        template.GetComponent<Image>().color = Cream;
        var item = (RectTransform)template.Find("Viewport/Content/Item");
        item.sizeDelta = new Vector2(item.sizeDelta.x, 52);
        var itemToggle = item.GetComponent<Toggle>();
        var itemColors = itemToggle.colors;
        itemColors.normalColor = Cream;
        itemColors.highlightedColor = itemColors.selectedColor = new Color(0.90f, 0.82f, 0.66f);
        itemToggle.colors = itemColors;
        var itemCheck = item.Find("Item Checkmark")?.GetComponent<Image>();
        if (itemCheck) itemCheck.color = TealFill;
        var content = (RectTransform)item.parent;
        content.sizeDelta = new Vector2(content.sizeDelta.x, 56);
        return dd;
    }
}
