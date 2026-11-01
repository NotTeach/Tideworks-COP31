using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static MainMenuSceneBuilder;

/// <summary>
/// Gorev 3 - Carettalara yardim sahnesini (Assets/Scenes/Beach.unity) kurar.
/// Ayristirmadan sonra Fishing sahnesi bu sahneyi yukler; "Start Day 2" tekrar Fishing'e doner.
/// Gorseller: Assets/Sprites/Beach (Tools/MenuArt/fishing/beach_art.py).
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Beach Scene
/// </summary>
public static partial class FishingSceneBuilder
{
    const int BeachBuilderVersion = 1;
    const string BeachScenePath = "Assets/Scenes/Beach.unity";
    const string BeachAutoBuildKey = "Tideworks.BeachAutoBuild.v1";
    static string BeachVersionKey => "Tideworks.BeachBuilderVersion." + PlayerSettings.productGUID;

    [MenuItem("Tideworks/Rebuild Beach Scene")]
    static void RebuildBeachMenu()
    {
        if (File.Exists(BeachScenePath) && !EditorUtility.DisplayDialog("Rebuild Beach",
                "Beach sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
                "Rebuild", "Cancel"))
            return;
        BuildBeach(askToSave: true);
    }

    static void BuildBeach(bool askToSave)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConfigureSprites(UiDir);
        ConfigureSprites(BeachDir);
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

        var canvasGo = new GameObject("BeachCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var beachCtl = canvasGo.AddComponent<BeachController>();
        Page(canvasGo.transform, "BeachPage", "Beach", out var beach, BeachDir);

        float[] lampX = { 429, 1261, 2439 };
        var pools = new Image[3];
        for (int i = 0; i < 3; i++)
            pools[i] = Img(beach, "LightPool" + i, BeachDir, "LightPool", new Rect(lampX[i] + 40 - 280, 1160 - 110, 560, 220));

        var chair = BeachButton(beach, "Chair", new Rect(880 - 120, 830 - 85, 240, 170));
        var umbrella = BeachButton(beach, "Umbrella", new Rect(2162 - 120, 760 - 120, 240, 240));
        var holeFilled = Img(beach, "HoleFilled", BeachDir, "HoleFilled", new Rect(1503 - 130, 890 - 75, 260, 150));
        var hole = BeachButton(beach, "Hole", new Rect(1503 - 130, 890 - 75, 260, 150));
        Vector2[] nestPos = { new Vector2(873, 1074), new Vector2(1514, 1136), new Vector2(2148, 1046) };
        var nests = new Button[3];
        var cages = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            nests[i] = BeachButton(beach, "Nest" + i, new Rect(nestPos[i].x - 110, nestPos[i].y - 55, 220, 110));
            cages[i] = Img(beach, "Cage" + i, BeachDir, "Cage", new Rect(nestPos[i].x - 120, nestPos[i].y - 36 - 105, 240, 210)).rectTransform;
        }
        var hatchling = Img(beach, "HatchlingTemplate", BeachDir, "Hatchling", new Rect(0, 0, 64, 72));
        var lamps = new Button[3];
        var bulbs = new Image[3];
        var glows = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            glows[i] = Img(beach, "LampGlow" + i, BeachDir, "LampGlow", new Rect(lampX[i] + 68 - 150, 1140 - 150, 300, 300));
            lamps[i] = BeachButton(beach, "Lamp" + i, new Rect(lampX[i] - 110, 1070, 220, 360));
            bulbs[i] = Img(lamps[i].transform, "Bulb", BeachDir, "LampBulb", new Rect(0, 0, 220, 360));
        }

        // Lambalar sondukce kumsal kararir (ust bar ve gorev karti aydinlik kalir)
        var dark = MakeImage(beach, "Darkness", null);
        dark.color = new Color(0.04f, 0.06f, 0.16f, 0f);
        Place(dark.rectTransform, new Rect(0, 0, Art.x, Art.y));

        var bMenu = Img(beach, "MenuButton", UiDir, "HudTeal", new Rect(40, 40, 110, 110));
        Img(bMenu.transform, "Icon", UiDir, "IconWaves", new Rect(23, 23, 64, 64));
        Clickable(bMenu, beachCtl.BackToMenu);
        var bRestart = Img(beach, "RestartButton", UiDir, "RestartButton", new Rect(174, 45, 260, 100));
        Label(bRestart.transform, "Text", "Restart", fontHeavy, 38, Ink, new Rect(0, -2, 260, 100), TextAnchor.MiddleCenter);
        Clickable(bRestart, beachCtl.Restart);
        var bCoins = Img(beach, "Coins", UiDir, "HudDark220", new Rect(456, 45, 230, 100));
        Img(bCoins.transform, "Coin", CoinPath, "Coin", new Rect(22, 20, 60, 60));
        var bCoinText = Label(bCoins.transform, "Count", "0", fontTitle, 48, Color.white, new Rect(96, 2, 130, 100));

        var bTip = Img(beach, "Tooltip", BeachDir, "BeachTooltip", new Rect(720, 40, 1070, 128));
        Img(bTip.transform, "Icon", BeachDir, "Hatchling", new Rect(34, 28, 64, 72));
        var bTipText = Label(bTip.transform, "Text", "", fontHeavy, 30, Ink, new Rect(124, 8, 920, 112));
        bTipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bTipText.resizeTextForBestFit = true;
        bTipText.resizeTextMaxSize = 30;
        bTipText.resizeTextMinSize = 20;

        var taskCard = Img(beach, "TaskCard", BeachDir, "TaskFrame", new Rect(1866, 40, 650, 580));
        Label(taskCard.transform, "Title", "TASK 3 · HELP THE CARETTAS", fontHeavy, 24, new Color32(30, 120, 124, 255), new Rect(44, 34, 560, 34));
        Label(taskCard.transform, "Subtitle", "Tonight the nests hatch!", fontHand, 56, Ink, new Rect(44, 66, 560, 72));
        string[] taskTitles = { "Switch off the lights", "Clear the path", "Protect the nests" };
        string[] taskSubs = { "Hatchlings follow the brightest light.", "Move chairs, fill holes.", "A cage over every nest." };
        var checks = new Image[3];
        var counts = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            float y = 160 + i * 132;
            checks[i] = Img(taskCard.transform, "Check" + i, BeachDir, "CheckOff", new Rect(44, y + 8, 52, 52));
            var tt = Label(taskCard.transform, "TaskTitle" + i, taskTitles[i], fontTitle, 36, Ink, new Rect(114, y - 4, 400, 52));
            tt.resizeTextForBestFit = true;
            tt.resizeTextMaxSize = 36;
            tt.resizeTextMinSize = 22;
            tt.horizontalOverflow = HorizontalWrapMode.Wrap;
            var sub = Label(taskCard.transform, "TaskSub" + i, taskSubs[i], fontBody, 24, new Color32(170, 96, 52, 255), new Rect(114, y + 46, 400, 60), TextAnchor.UpperLeft);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            counts[i] = Label(taskCard.transform, "Count" + i, "0/3", fontTitle, 36, Ink, new Rect(516, y + 4, 92, 56), TextAnchor.MiddleRight);
        }

        var hatchImg = Img(beach, "HatchButton", UiDir, "GoButton", new Rect(1050, 660, 460, 124));
        Label(hatchImg.transform, "Text", "Let them hatch!", fontTitle, 50, Color.white, new Rect(0, -6, 460, 124), TextAnchor.MiddleCenter);
        Clickable(hatchImg, beachCtl.Hatch);

        // Gun sonu karti
        var complete = MakeImage(beach, "CompleteOverlay", null);
        complete.color = new Color(0.02f, 0.08f, 0.1f, 0.55f);
        complete.raycastTarget = true;
        Place(complete.rectTransform, new Rect(-400, -400, Art.x + 800, Art.y + 800));
        var cCard = Img(complete.transform, "Card", BeachDir, "DayCard", new Rect(400 + 680, 400 + 170, 1200, 1070));
        var ct = cCard.transform;
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
        var nestsSafe = Stat("NestsSafe", "StatBox", 67, "NESTS SAFE", Brown, Ink);
        var beachReady = Stat("BeachReady", "StatBox", 428, "BEACH READY", Brown, Ink);
        var earnedText = Stat("Earned", "StatBoxDark", 789, "EARNED", SoftTeal, Gold);
        var radio = Img(ct, "Radio", BeachDir, "RadioBox", new Rect(67, 468, 1068, 243));
        Img(radio.transform, "Hatice", BeachDir, "HaticePortrait", new Rect(30, 52, 140, 140));
        var radioText = Label(radio.transform, "Text",
            "<b>On Hatice Teyze's radio tonight:</b> \u201cNeighbours, switch off your lights by the shore! The turtles are coming!\u201d " +
            "The whole town of Ka\u015f went dark, and the hatchlings found their way to the waves.",
            fontBody, 32, new Color32(48, 43, 40, 255), new Rect(196, 20, 846, 203));
        radioText.horizontalOverflow = HorizontalWrapMode.Wrap;
        radioText.lineSpacing = 1.05f;
        radioText.resizeTextForBestFit = true;
        radioText.resizeTextMaxSize = 32;
        radioText.resizeTextMinSize = 22;
        var waiting = Label(ct, "Waiting", "", fontHand, 52, new Color32(110, 62, 40, 255), new Rect(90, 726, 1020, 140), TextAnchor.MiddleCenter);
        waiting.horizontalOverflow = HorizontalWrapMode.Wrap;
        waiting.lineSpacing = 0.9f;
        var next = Img(ct, "NextDay", BeachDir, "DayButton", new Rect(256, 884, 400, 122));
        var nextLabel = Label(next.transform, "Text", "", fontTitle, 46, Color.white, new Rect(0, -6, 344, 122), TextAnchor.MiddleCenter);
        Img(next.transform, "Arrow", CoinPath, "ArrowWhite", new Rect(330, 40, 48, 30));
        Clickable(next, beachCtl.StartNextDay);
        var map = Img(ct, "SeasonMap", BeachDir, "MapButton", new Rect(676, 884, 280, 122));
        Label(map.transform, "Text", "Season map", fontHeavy, 36, Ink, new Rect(0, -2, 280, 122), TextAnchor.MiddleCenter);
        Clickable(map, beachCtl.SeasonMap);

        SetRefs(beachCtl, ("darkness", dark), ("chair", chair), ("umbrella", umbrella), ("hole", hole),
            ("holeFilled", holeFilled.gameObject), ("hatchlingTemplate", hatchling), ("tooltip", bTipText),
            ("checkOn", LoadSprite("CheckOn", BeachDir)), ("checkOff", LoadSprite("CheckOff", BeachDir)),
            ("coinText", bCoinText), ("hatchButton", hatchImg.gameObject), ("completeOverlay", complete.gameObject),
            ("completeCard", cCard.rectTransform), ("completeKicker", kicker), ("completeTitle", title),
            ("nestsSafeText", nestsSafe), ("beachReadyText", beachReady), ("earnedText", earnedText),
            ("waitingText", waiting), ("nextDayLabel", nextLabel));
        SetArray(beachCtl, "lamps", lamps);
        SetArray(beachCtl, "lampBulbs", bulbs);
        SetArray(beachCtl, "lampGlows", glows);
        SetArray(beachCtl, "lightPools", pools);
        SetArray(beachCtl, "nests", nests);
        SetArray(beachCtl, "cages", cages);
        SetArray(beachCtl, "taskChecks", checks);
        SetArray(beachCtl, "taskCounts", counts);

        var faderImg = MakeImage(canvasGo.transform, "ScreenFader", null);
        faderImg.color = Night;
        faderImg.raycastTarget = true;
        Stretch(faderImg.rectTransform);
        var fader = faderImg.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 1f;
        SetRefs(beachCtl, ("screenFader", fader));

        Directory.CreateDirectory(Path.GetDirectoryName(BeachScenePath));
        ArtLocalization.Apply(canvasGo.transform);
        EditorSceneManager.SaveScene(scene, BeachScenePath);
        var scenes = EditorBuildSettings.scenes.Where(sc => sc.path != BeachScenePath).ToList();
        int after = scenes.FindIndex(sc => sc.path == ScenePath);
        scenes.Insert(after + 1, new EditorBuildSettingsScene(BeachScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorPrefs.SetInt(BeachVersionKey, BeachBuilderVersion);
        Debug.Log("[Tideworks] Beach scene created: " + BeachScenePath);
    }
}
