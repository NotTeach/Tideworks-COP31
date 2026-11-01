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
using static MainMenuSceneBuilder;

/// <summary>
/// Start'tan sonra acilan giris / yukleme ekranini (Assets/Scenes/Intro.unity) kodla kurar.
/// Arka plan tasarim gorselidir (yazilari silinmis); yazilar, kartlar ve butonlar Unity'de cizilir ki keskin olsun.
/// Gorseller: Assets/Sprites/Intro (uretici: Tools/MenuArt/make_intro_art.py)
/// Builder surumu degisince proje acildiginda bir kez otomatik calisir.
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Intro Scene
/// (DIKKAT: sahnede elle yaptiginiz degisiklikler silinir.)
/// </summary>
[InitializeOnLoad]
public static class IntroSceneBuilder
{
    const int BuilderVersion = 2;
    const string ScenePath = "Assets/Scenes/Intro.unity";
    const string SpriteDir = "Assets/Sprites/Intro/";
    const string FontDir = "Assets/Fonts/";
    const string AutoBuildKey = "Tideworks.IntroAutoBuild.v2";
    static string VersionKey => "Tideworks.IntroBuilderVersion." + PlayerSettings.productGUID;

    // Tasarim 1280x720; artboard 2x (2560x1440). Asagidaki tum koordinatlar 1280x720'dedir.
    const float K = 2f;
    static readonly Vector2 ArtSize = new Vector2(2560, 1440);

    static readonly Color Background = new Color32(14, 61, 72, 255);
    static readonly Color Yellow = new Color32(244, 192, 74, 255);
    static readonly Color YellowShadow = new Color32(190, 110, 40, 255);
    static readonly Color White = new Color32(250, 246, 236, 255);
    static readonly Color Body = new Color32(237, 226, 200, 255);
    static readonly Color CardLabel = new Color32(196, 229, 225, 255);
    static readonly Color HandInk = new Color32(92, 60, 40, 255);
    static readonly Color Night = new Color32(5, 24, 30, 255);

    static readonly string[] NoteLines =
    {
        "Kaş, summer of 1998",
        "This beach is the last place on the",
        "coast where turtles still nest.",
        "If it isn't cleaned for one more",
        "summer, they won't come back.",
        "The workshop key is above the door.",
    };

    static Font fontBody, fontHeavy, fontTitle, fontHand;

    static IntroSceneBuilder()
    {
        if (File.Exists(ScenePath) && EditorPrefs.GetInt(VersionKey, 0) >= BuilderVersion) return;
        SceneAutoBuild.Request(AutoBuildKey, () => Build(askToSave: true));
    }

    [MenuItem("Tideworks/Rebuild Intro Scene")]
    static void RebuildMenu()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild Intro",
                "Intro sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
                "Rebuild", "Cancel"))
            return;
        Build(askToSave: true);
    }

    static void Build(bool askToSave)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConfigureSprites(SpriteDir);
        var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fontBody = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-Bold.ttf") ?? legacy;
        fontHeavy = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-ExtraBold.ttf") ?? legacy;
        fontTitle = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Baloo2-ExtraBold.ttf") ?? legacy;
        fontHand = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Caveat-Bold.ttf") ?? legacy;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Kamera: arka plan rengi gorselin zemin rengiyle ayni, kenar bosluklari belli olmaz ---
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        camGo.transform.position = new Vector3(0, 0, -10);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // --- Canvas ---
        var canvasGo = new GameObject("IntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var controller = canvasGo.AddComponent<IntroController>();

        var artboard = new GameObject("Artboard", typeof(RectTransform), typeof(ArtboardFitter)).transform as RectTransform;
        artboard.SetParent(canvasGo.transform, false);
        artboard.GetComponent<ArtboardFitter>().artSize = ArtSize;
        artboard.sizeDelta = ArtSize;

        var bg = MakeImage(artboard, "Background", LoadSprite("IntroBackground", SpriteDir));
        Place(bg.rectTransform, new Rect(Vector2.zero, ArtSize));

        // --- Defterdeki not (her satir defter cizgisinin ustunde) ---
        var note = new GameObject("Note", typeof(RectTransform)).transform as RectTransform;
        note.SetParent(artboard, false);
        Stretch(note);
        var lines = NoteLines.Select((line, i) => HandLine(note, "Line" + (i + 1), line, 183, 165 + i * 44)).ToList();
        lines.Add(HandLine(note, "Signature", "Grandpa Yusuf", 338, 429));

        // --- Baslik ve hikaye ---
        var title = Label(artboard, "Title", "Tidework", fontTitle, 74, Yellow, 960, 104, 700);
        var titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = YellowShadow;
        titleShadow.effectDistance = new Vector2(0, -5 * K);
        Label(artboard, "Subtitle", "The Last Beach", fontTitle, 64, White, 960, 180, 700);
        var story = Label(artboard, "Story",
            "Deniz opens her grandpa's closed workshop and finds a dusty notebook. Nesting season is almost over.",
            fontHeavy, 20, Body, 960, 284, 490, 100);
        story.lineSpacing = 1.13f;
        story.horizontalOverflow = HorizontalWrapMode.Wrap;

        // --- Kartlar ---
        var timeLeft = MakeCard(artboard, "TimeLeftCard", 708, "TIME LEFT", "3 days");
        Icon(artboard, "IconHourglass");
        var nests = MakeCard(artboard, "NestsCard", 972, "NESTS ON THE BEACH", "12 nests");
        Icon(artboard, "IconNest");

        // --- Butonlar ---
        var open = MakeButton(artboard, "OpenWorkshopButton", "ButtonOrange", new Rect(708, 586, 241, 75),
            "Open the workshop", fontTitle, 26, controller.OpenWorkshop, "IconKey");
        var later = MakeButton(artboard, "ReadLaterButton", "ButtonOutline", new Rect(972, 587, 241, 68),
            "Read the notebook later", fontHeavy, 19, controller.ReadLater, "IconBook");

        // --- Ekran gecisi ---
        var faderImg = MakeImage(canvasGo.transform, "ScreenFader", null);
        faderImg.color = Night;
        faderImg.raycastTarget = true;
        Stretch(faderImg.rectTransform);
        var fader = faderImg.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 0f;
        faderImg.gameObject.SetActive(false);

        SetRefs(controller,
            ("timeLeftValue", timeLeft), ("nestsValue", nests),
            ("openWorkshopButton", open), ("readLaterButton", later), ("screenFader", fader));
        var so = new SerializedObject(controller);
        var arr = so.FindProperty("noteLines");
        arr.arraySize = lines.Count;
        for (int i = 0; i < lines.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = lines[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        ArtLocalization.Apply(canvasGo.transform);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        EditorPrefs.SetInt(VersionKey, BuilderVersion);
        Debug.Log("[Tideworks] Intro scene created: " + ScenePath);
    }

    // ---------------- Yardimcilar ----------------

    /// <summary>Intro'yu ana menunun hemen arkasina koyar.</summary>
    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        int menu = scenes.FindIndex(s => s.path == "Assets/Scenes/MainMenu.unity");
        scenes.Insert(menu + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static Rect R(float x, float y, float w, float h) => new Rect(x * K, y * K, w * K, h * K);

    /// <summary>Tasarimdaki merkez noktasina ortalanmis yazi (koordinatlar 1280x720).</summary>
    static Text Label(Transform parent, string name, string content, Font font, int size, Color color,
        float cx, float cy, float width, float height = 0)
    {
        if (height <= 0) height = size * 1.6f;
        var t = MakeText(parent, name, content, Mathf.RoundToInt(size * K), font, color);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Place(t.rectTransform, R(cx - width / 2, cy - height / 2, width, height));
        return t;
    }

    /// <summary>El yazisi satiri; (x, baseline) tasarimdaki sol alt yazi cizgisi.</summary>
    static Text HandLine(Transform parent, string name, string content, float x, float baseline)
    {
        var t = MakeText(parent, name, content, Mathf.RoundToInt(30 * K), fontHand, HandInk, TextAnchor.MiddleLeft);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Place(t.rectTransform, R(x, baseline - 10 - 25, 400, 50));
        return t;
    }

    static Text MakeCard(Transform parent, string name, float x, string label, string value)
    {
        var card = MakeImage(parent, name, LoadSprite("Card", SpriteDir));
        Place(card.rectTransform, R(x, 405, 241, 122));
        float cx = x + 120;
        Label(card.transform.parent, name + "Label", label, fontHeavy, 15, CardLabel, cx, 444, 230);
        return Label(card.transform.parent, name + "Value", value, fontTitle, 50, Yellow, cx, 481, 230);
    }

    static void Icon(Transform parent, string name, Transform under = null, Rect owner = default)
    {
        var rect = IconRects[name];
        var img = MakeImage(under ? under : parent, name, LoadSprite(name, SpriteDir));
        Place(img.rectTransform, R(rect.x - owner.x, rect.y - owner.y, rect.width, rect.height));
    }

    // make_intro_art.py ciktisi (rects.json)
    static readonly System.Collections.Generic.Dictionary<string, Rect> IconRects = new System.Collections.Generic.Dictionary<string, Rect>
    {
        { "IconHourglass", new Rect(905, 382, 55, 64) },
        { "IconNest", new Rect(1151, 392, 74, 41) },
        { "IconKey", new Rect(726, 566, 66, 31) },
        { "IconBook", new Rect(1066, 556, 51, 44) },
    };

    static Button MakeButton(Transform parent, string name, string sprite, Rect rect, string label, Font font, int size,
        UnityAction onClick, string icon)
    {
        var img = MakeImage(parent, name, LoadSprite(sprite, SpriteDir));
        img.raycastTarget = true;
        Place(img.rectTransform, R(rect.x, rect.y, rect.width, rect.height));

        var button = img.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        img.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.04f;

        // Yazi ve ikon butonun cocugu: buton buyuyunce onlar da buyur
        var t = MakeText(img.transform, "Label", label, Mathf.RoundToInt(size * K), font, White);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Place(t.rectTransform, R(0, 0, rect.width, 66));
        Icon(parent, icon, img.transform, rect);

        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }
}
