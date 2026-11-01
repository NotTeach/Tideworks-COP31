using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static MainMenuSceneBuilder;

/// <summary>
/// 2. gun kumsali (Assets/Scenes/Day2Beach.unity): gun batiminda kayalik koy, aga dolanmis caretta, isim secimi,
/// gece, poset toplama, 10 yavru ve gun sonu karti. 2. gunde Recycling bu sahneyi yukler.
/// Gorseller: Assets/Sprites/Day2Beach (Tools/MenuArt/fishing/day2_beach_art.py).
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Day 2 Beach Scene
/// </summary>
public static partial class FishingSceneBuilder
{
    const int Day2BeachBuilderVersion = 1;
    const string Day2BeachScenePath = "Assets/Scenes/Day2Beach.unity";
    const string Day2BeachDir = "Assets/Sprites/Day2Beach/";
    const string Day2Dir = "Assets/Sprites/Day2/";
    const string Day2BeachAutoBuildKey = "Tideworks.Day2BeachAutoBuild.v1";
    static string Day2BeachVersionKey => "Tideworks.Day2BeachBuilderVersion." + PlayerSettings.productGUID;

    static void RequestDay2Beach()
    {
        if (File.Exists(Day2BeachScenePath) && EditorPrefs.GetInt(Day2BeachVersionKey, 0) >= Day2BeachBuilderVersion) return;
        SceneAutoBuild.Request(Day2BeachAutoBuildKey, () => BuildDay2Beach(askToSave: true));
    }

    [MenuItem("Tideworks/Rebuild Day 2 Beach Scene")]
    static void RebuildDay2BeachMenu()
    {
        if (File.Exists(Day2BeachScenePath) && !EditorUtility.DisplayDialog("Rebuild Day 2 Beach",
                "Day2Beach sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
                "Rebuild", "Cancel"))
            return;
        BuildDay2Beach(askToSave: true);
    }

    static void BuildDay2Beach(bool askToSave)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConfigureSprites(UiDir);
        ConfigureSprites(BeachDir);
        ConfigureSprites(Day2BeachDir);
        ConfigureSprites(Day2Dir);
        LoadFonts();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Night;
        camGo.transform.position = new Vector3(0, 0, -10);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("Day2BeachCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var ctl = canvasGo.AddComponent<Day2BeachController>();
        Page(canvasGo.transform, "Day2BeachPage", "Day2Beach", out var beach, Day2BeachDir);

        // Caretta ve uc parcali ag; dugumler: on yuzgec, boyun, kabuk (caretta gorselindeki konumlar)
        var turtle = Img(beach, "Caretta", Day2BeachDir, "Caretta", new Rect(940, 600, 680, 640));
        var nets = new Image[3];
        var knots = new Button[3];
        Vector2[] knotAt = { new Vector2(150, 215), new Vector2(340, 190), new Vector2(420, 400) };
        for (int i = 0; i < 3; i++)
            nets[i] = Img(turtle.transform, "Net" + i, Day2BeachDir, "CarettaNet" + i, new Rect(0, 0, 680, 640));
        for (int i = 0; i < 3; i++)
        {
            var k = Img(turtle.transform, "Knot" + i, Day2Dir, "NetKnot", new Rect(knotAt[i].x - 48, knotAt[i].y - 48, 96, 96));
            k.raycastTarget = true;
            knots[i] = k.gameObject.AddComponent<Button>();
            knots[i].transition = Selectable.Transition.None;
            k.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.2f;
            var pulse = k.gameObject.AddComponent<MenuPulse>();
            pulse.minAlpha = 0.85f;
            pulse.maxAlpha = 1f;
            pulse.speed = 2.4f;
            pulse.phase = i;
            pulse.scaleAmount = 0f;     // olcegi MenuButtonFx yonetir
        }

        // Gece: kararma, ay, yuva, posetler, yavrular
        var night = MakeImage(beach, "Night", null);
        night.color = new Color(0.03f, 0.05f, 0.18f, 0f);
        Place(night.rectTransform, new Rect(0, 0, Art.x, Art.y));
        var moon = Img(beach, "Moon", Day2BeachDir, "Moon", new Rect(420, 10, 300, 300));
        var nest = Img(beach, "Nest", BeachDir, "Nest", new Rect(1590, 1150, 220, 110));
        var bags = new Button[2];
        Rect[] bagRects = { new Rect(1520, 930, 150, 150), new Rect(1760, 760, 150, 150) };
        for (int i = 0; i < 2; i++)
        {
            var b = Img(beach, "Bag" + i, ArtDir, "TrashPlasticBag", bagRects[i]);
            b.preserveAspect = true;
            b.raycastTarget = true;
            bags[i] = b.gameObject.AddComponent<Button>();
            bags[i].transition = Selectable.Transition.None;
            b.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.1f;
        }
        var hatchling = Img(beach, "HatchlingTemplate", Day2BeachDir, "CarettaBaby", new Rect(0, 0, 63, 60));

        // Ust bar ve ipucu
        var menu = Img(beach, "MenuButton", UiDir, "HudTeal", new Rect(40, 40, 110, 110));
        Img(menu.transform, "Icon", UiDir, "IconWaves", new Rect(23, 23, 64, 64));
        Clickable(menu, ctl.BackToMenu);
        var coins = Img(beach, "Coins", UiDir, "HudDark220", new Rect(180, 45, 230, 100));
        Img(coins.transform, "Coin", CoinPath, "Coin", new Rect(22, 20, 60, 60));
        var coinText = Label(coins.transform, "Count", "0", fontTitle, 48, Color.white, new Rect(96, 2, 130, 100));
        var tip = Img(beach, "Tooltip", BeachDir, "BeachTooltip", new Rect(745, 40, 1070, 128));
        Img(tip.transform, "Icon", Day2BeachDir, "CarettaBaby", new Rect(30, 32, 70, 66));
        var tipText = Label(tip.transform, "Text", "", fontHeavy, 30, Ink, new Rect(124, 8, 920, 112));
        tipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tipText.resizeTextForBestFit = true;
        tipText.resizeTextMaxSize = 30;
        tipText.resizeTextMinSize = 20;

        var radio = Img(beach, "Radio", BeachDir, "RadioBox", new Rect(60, 1160, 1068, 243));
        Img(radio.transform, "Hatice", BeachDir, "HaticePortrait", new Rect(30, 52, 140, 140));
        var radioText = Label(radio.transform, "Text", "", fontBody, 32, new Color32(48, 43, 40, 255), new Rect(196, 20, 846, 203));
        radioText.horizontalOverflow = HorizontalWrapMode.Wrap;
        radioText.resizeTextForBestFit = true;
        radioText.resizeTextMaxSize = 32;
        radioText.resizeTextMinSize = 22;

        var nightBtn = Img(beach, "NightButton", UiDir, "GoButton", new Rect(1050, 1270, 460, 124));
        Label(nightBtn.transform, "Text", "Wait for night", fontTitle, 48, Color.white, new Rect(0, -6, 460, 124), TextAnchor.MiddleCenter);
        Clickable(nightBtn, ctl.WaitForNight);

        // Makas: dugumun uzerine gelir (orta capa: konum kod tarafindan verilir)
        var scissors = Img(beach, "Scissors", Day2Dir, "ToolScissors", new Rect(0, 0, 170, 170));
        scissors.preserveAspect = true;
        scissors.rectTransform.anchorMin = scissors.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // Isim karti
        var nameDim = MakeImage(beach, "NameOverlay", null);
        nameDim.color = new Color(0.02f, 0.08f, 0.1f, 0.5f);
        nameDim.raycastTarget = true;
        Place(nameDim.rectTransform, new Rect(-400, -400, Art.x + 800, Art.y + 800));
        var nameCard = Img(nameDim.transform, "Card", UiDir, "FinishCard", new Rect(400 + 730, 400 + 340, 1100, 760));
        var nameTitle = Label(nameCard.transform, "Title", "", fontTitle, 72, Ink, new Rect(60, 60, 980, 120), TextAnchor.MiddleCenter);
        nameTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameTitle.resizeTextForBestFit = true;
        nameTitle.resizeTextMaxSize = 72;
        nameTitle.resizeTextMinSize = 40;
        Img(nameCard.transform, "Caretta", Day2BeachDir, "Caretta", new Rect(440, 190, 220, 207)).preserveAspect = true;
        Label(nameCard.transform, "Hint", "It will remember this beach and come back one day to lay its eggs.",
            fontBody, 30, Brown, new Rect(90, 410, 920, 90), TextAnchor.MiddleCenter).horizontalOverflow = HorizontalWrapMode.Wrap;
        string[] names = { "Umut", "Mavi", "Yusuf" };
        for (int i = 0; i < 3; i++)
        {
            var nb = Img(nameCard.transform, "Name" + i, UiDir, "GoButton", new Rect(70 + i * 330, 560, 300, 124));
            Label(nb.transform, "Text", names[i], fontTitle, 52, Color.white, new Rect(0, -6, 300, 124), TextAnchor.MiddleCenter);
            nb.raycastTarget = true;
            var btn = nb.gameObject.AddComponent<Button>();
            nb.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.05f;
            UnityEventTools.AddIntPersistentListener(btn.onClick, ctl.ChooseName, i);
        }

        // Gun sonu karti
        var complete = MakeImage(beach, "CompleteOverlay", null);
        complete.color = new Color(0.02f, 0.08f, 0.1f, 0.55f);
        complete.raycastTarget = true;
        Place(complete.rectTransform, new Rect(-400, -400, Art.x + 800, Art.y + 800));
        var card = Img(complete.transform, "Card", BeachDir, "DayCard", new Rect(400 + 680, 400 + 170, 1200, 1070));
        var ct = card.transform;
        var kicker = Label(ct, "Kicker", "", fontHeavy, 26, new Color32(30, 120, 124, 255), new Rect(0, 46, 1200, 44), TextAnchor.MiddleCenter);
        var title = Label(ct, "Title", "", fontTitle, 76, Ink, new Rect(70, 92, 1060, 190), TextAnchor.MiddleCenter);
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.lineSpacing = 0.82f;
        title.resizeTextForBestFit = true;
        title.resizeTextMaxSize = 76;
        title.resizeTextMinSize = 44;
        Text Stat(string name, string sprite, float x, string label, Color labelInk, Color valueInk)
        {
            var box = Img(ct, name, BeachDir, sprite, new Rect(x, 300, 344, 142));
            Label(box.transform, "Label", label, fontHeavy, 22, labelInk, new Rect(0, 18, 344, 36), TextAnchor.MiddleCenter);
            return Label(box.transform, "Value", "", fontTitle, 50, valueInk, new Rect(0, 56, 344, 72), TextAnchor.MiddleCenter);
        }
        var hatchStat = Stat("Hatchlings", "StatBox", 67, "HATCHLINGS", Brown, Ink);
        var healthStat = Stat("OceanHealth", "StatBox", 428, "OCEAN HEALTH", Brown, Ink);
        var rewardStat = Stat("Reward", "StatBoxDark", 789, "REWARD", SoftTeal, Gold);
        var cRadio = Img(ct, "Radio", BeachDir, "RadioBox", new Rect(67, 468, 1068, 243));
        Img(cRadio.transform, "Hatice", BeachDir, "HaticePortrait", new Rect(30, 52, 140, 140));
        var cRadioText = Label(cRadio.transform, "Text",
            "<b>On Hatice Teyze's radio tonight:</b> “To a turtle, a floating bag looks just like a jellyfish. Keep plastic out of the sea, neighbours!” " +
            "Not a single bag was left between the nests and the waves.",
            fontBody, 32, new Color32(48, 43, 40, 255), new Rect(196, 20, 846, 203));
        cRadioText.horizontalOverflow = HorizontalWrapMode.Wrap;
        cRadioText.lineSpacing = 1.05f;
        cRadioText.resizeTextForBestFit = true;
        cRadioText.resizeTextMaxSize = 32;
        cRadioText.resizeTextMinSize = 22;
        var note = Label(ct, "Note", "Elif waves from the rocks. Tomorrow the amber lamps go up!", fontHand, 52,
            new Color32(110, 62, 40, 255), new Rect(90, 726, 1020, 140), TextAnchor.MiddleCenter);
        note.horizontalOverflow = HorizontalWrapMode.Wrap;
        note.lineSpacing = 0.9f;
        var next = Img(ct, "NextDay", BeachDir, "DayButton", new Rect(256, 884, 400, 122));
        var nextLabel = Label(next.transform, "Text", "", fontTitle, 46, Color.white, new Rect(0, -6, 344, 122), TextAnchor.MiddleCenter);
        Img(next.transform, "Arrow", CoinPath, "ArrowWhite", new Rect(330, 40, 48, 30));
        Clickable(next, ctl.StartNextDay);
        var map = Img(ct, "SeasonMap", BeachDir, "MapButton", new Rect(676, 884, 280, 122));
        Label(map.transform, "Text", "Season map", fontHeavy, 36, Ink, new Rect(0, -2, 280, 122), TextAnchor.MiddleCenter);
        Clickable(map, ctl.SeasonMap);

        var faderImg = MakeImage(canvasGo.transform, "ScreenFader", null);
        faderImg.color = Night;
        faderImg.raycastTarget = true;
        Stretch(faderImg.rectTransform);
        var fader = faderImg.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 1f;

        SetRefs(ctl, ("screenFader", fader), ("turtle", turtle.rectTransform), ("scissors", scissors.rectTransform),
            ("nameOverlay", nameDim.gameObject), ("nameCard", nameCard.rectTransform), ("nameTitle", nameTitle),
            ("nightButton", nightBtn.gameObject), ("night", night), ("moon", moon.rectTransform),
            ("radio", radio.gameObject), ("radioText", radioText), ("nest", nest.rectTransform),
            ("hatchlingTemplate", hatchling), ("tooltip", tipText), ("coinText", coinText),
            ("completeOverlay", complete.gameObject), ("completeCard", card.rectTransform), ("completeKicker", kicker),
            ("completeTitle", title), ("hatchlingsText", hatchStat), ("healthText", healthStat), ("rewardText", rewardStat),
            ("nextDayLabel", nextLabel));
        SetArray(ctl, "knots", knots);
        SetArray(ctl, "netParts", nets);
        SetArray(ctl, "bags", bags);

        Directory.CreateDirectory(Path.GetDirectoryName(Day2BeachScenePath));
        ArtLocalization.Apply(canvasGo.transform);
        EditorSceneManager.SaveScene(scene, Day2BeachScenePath);
        var scenes = EditorBuildSettings.scenes.Where(sc => sc.path != Day2BeachScenePath).ToList();
        int after = scenes.FindIndex(sc => sc.path == BeachScenePath);
        scenes.Insert(after + 1, new EditorBuildSettingsScene(Day2BeachScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorPrefs.SetInt(Day2BeachVersionKey, Day2BeachBuilderVersion);
        Debug.Log("[Tideworks] Day 2 beach scene created: " + Day2BeachScenePath);
    }
}
