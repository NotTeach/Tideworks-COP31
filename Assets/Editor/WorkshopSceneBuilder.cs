using System.Collections.Generic;
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
/// Intro'dan sonraki atolye acilisini (Assets/Scenes/Workshop.unity) kodla kurar:
/// dis cephe (anahtar + kapi), Day 1 gorev listesi, atolye ici (Hatice Teyze) ve tamir tezgahi.
/// Gorseller: Assets/Sprites/Workshop (uretici: Tools/MenuArt/workshop/*.py)
/// Builder surumu degisince proje acildiginda bir kez otomatik calisir.
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Workshop Scene
/// (DIKKAT: sahnede elle yaptiginiz degisiklikler silinir.)
/// </summary>
[InitializeOnLoad]
public static partial class WorkshopSceneBuilder
{
    const int BuilderVersion = 4;
    const string ScenePath = "Assets/Scenes/Workshop.unity";
    const string SpriteDir = "Assets/Sprites/Workshop/";
    const string FontDir = "Assets/Fonts/";
    const string AutoBuildKey = "Tideworks.WorkshopAutoBuildTried.v4";
    static string VersionKey => "Tideworks.WorkshopBuilderVersion." + PlayerSettings.productGUID;

    // Dis cephe ve atolye tasarimi 1280x720 (artboard 2x); gun plani 2752x1536 (artboard 1x).
    static readonly Vector2 WideArt = new Vector2(2560, 1440);
    static readonly Vector2 PlanArt = new Vector2(2752, 1536);

    static readonly Color Night = new Color32(5, 24, 30, 255);
    static readonly Color QuestStepInk = new Color32(120, 92, 64, 255);
    static readonly Color QuestTitleInk = new Color32(176, 78, 44, 255);
    static readonly Color QuestBodyInk = new Color32(45, 40, 38, 255);
    static readonly Color RowInk = new Color32(53, 37, 21, 255);
    static readonly Color RowSubInk = new Color32(141, 99, 58, 255);
    static readonly Color DialogueInk = new Color32(48, 43, 40, 255);
    static readonly Color PanelInk = new Color32(134, 103, 69, 255);
    static readonly Color PanelHighlight = new Color32(251, 243, 220, 255);
    static readonly Color PanelPaper = new Color32(230, 203, 154, 255);
    static readonly Color BadgeInk = new Color32(251, 248, 241, 255);

    static readonly Color QuestTeal = new Color32(16, 70, 80, 255);

    // Radyo calistiktan sonra Hatice Teyze (her biri ayri diyalog kutusu)
    static readonly string[] AfterRepairLines =
    {
        "Ooh! Is that... the fishing news? Just like my late husband and I listened to every evening! " +
        "Bless your hands, dear. Yusuf would be proud of you.",
        "In our day, nothing got thrown away so easily. We fixed what broke and reused what was old. " +
        "Now everything goes straight in the bin, and most of that bin ends up in the sea.",
        "You fixed this radio with things you collected from the sea. That's recycling: " +
        "a bottle became glass, a net became rope, and this old radio spoke again.",
        "Here, take this, you've earned it. Tonight I'll tell the whole town on this radio: " +
        "switch off the lights by the shore at night, the turtles are coming!",
    };

    static readonly string[] DialogueLines =
    {
        "Good morning, dear! So Yusuf’s workshop is open again. I brought you something...\n" +
        "This was my husband’s radio. We listened to the fishing news on it every evening.",
        "It went quiet last winter. The dial glass cracked and a wire came loose inside.\n" +
        "Yusuf always said nothing is too old to be fixed. Will you try?",
    };

    // Tools/MenuArt/workshop ciktisi (*_rects.json): artboard pikseli, sol ust orijin
    static readonly Dictionary<string, Rect> Art = new Dictionary<string, Rect>
    {
        { "DoorLeafLeft", new Rect(1810, 926, 114, 390) }, { "DoorLeafRight", new Rect(1920, 926, 114, 390) },
        { "Wave0", new Rect(588, 876, 136, 40) }, { "Wave1", new Rect(68, 900, 136, 40) },
        { "Wave2", new Rect(1108, 912, 136, 40) }, { "Wave3", new Rect(376, 962, 118, 36) },
        { "Wave4", new Rect(1296, 1062, 118, 36) }, { "Wave5", new Rect(288, 1100, 136, 40) },
        { "Wave6", new Rect(108, 1360, 192, 40) }, { "Wave7", new Rect(1108, 1380, 136, 40) },
        { "Boat", new Rect(530, 1068, 490, 294) },
        // Kirli deniz: yuzen copler (Tools/MenuArt/workshop/pollute.py -> trash_rects.json)
        { "TrashBottle0", new Rect(252, 934, 96, 52) },
        { "TrashBag0", new Rect(108, 1148, 84, 64) },
        { "TrashCan0", new Rect(894, 919, 72, 52) },
        { "TrashPlank0", new Rect(480, 875, 160, 50) },
        { "TrashCup0", new Rect(438, 1180, 64, 60) },
        { "TrashBottle1", new Rect(1242, 904, 96, 52) },
        { "TrashNet0", new Rect(1204, 1177, 112, 66) },
        { "TrashBag1", new Rect(968, 1358, 84, 64) },
        { "TrashCan1", new Rect(54, 974, 72, 52) },
        { "TrashCup1", new Rect(1148, 1030, 64, 60) },
        { "Cloud0", new Rect(996, 36, 242, 106) }, { "Cloud1", new Rect(1700, 136, 194, 88) },
        { "Key", new Rect(1884, 924, 76, 168) }, { "TakeKeyButton", new Rect(2006, 942, 348, 100) },

        { "CheckOn", new Rect(849, 503, 84, 86) }, { "BadgeDone", new Rect(1766, 501, 140, 83) },
        { "StartDayButton", new Rect(817, 1295, 1156, 118) },

        { "TellMoreButton", new Rect(958, 1248, 310, 108) }, { "LetsFixButton", new Rect(1284, 1248, 366, 118) },
        { "MenuButton", new Rect(40, 20, 104, 104) }, { "HeadOutButton", new Rect(1948, 20, 308, 104) },
        { "TasksButton", new Rect(2270, 20, 218, 104) }, { "TasksButtonActive", new Rect(2270, 20, 252, 104) },
        { "TasksPanel", new Rect(1744, 184, 784, 1250) }, { "PanelCheck", new Rect(1846, 592, 70, 68) },
        { "TaskBadge", new Rect(2482, 8, 52, 52) },
    };

    static Font fontBody, fontHeavy, fontHand, fontTitle;

    static WorkshopSceneBuilder()
    {
        if (File.Exists(ScenePath) && EditorPrefs.GetInt(VersionKey, 0) >= BuilderVersion) return;
        if (SessionState.GetBool(AutoBuildKey, false)) return;
        SessionState.SetBool(AutoBuildKey, true);
        EditorApplication.delayCall += () => Build(askToSave: true);
    }

    [MenuItem("Tideworks/Rebuild Workshop Scene")]
    static void RebuildMenu()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild Workshop",
                "Workshop sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
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
        fontHand = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Caveat-Bold.ttf") ?? legacy;
        fontTitle = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Baloo2-ExtraBold.ttf") ?? legacy;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Night;
        camGo.transform.position = new Vector3(0, 0, -10);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("WorkshopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var c = canvasGo.AddComponent<WorkshopOpeningController>();

        // ================= 1: Dis cephe =================
        var exterior = MakePage(canvasGo.transform, "ExteriorPage", "Exterior", WideArt, out var ext);

        var sun = MakeImage(ext, "SunGlow", LoadSprite("Glow", SpriteDir));
        Place(sun.rectTransform, Centered(2220, 216, 330));
        Pulse(sun, 0.35f, 0.75f, 0.2f, 0f, 0.08f);
        for (int i = 0; i < 2; i++)
            AddBob(Cut(ext, "Cloud" + i), new Vector2(46, 0), 0f, 0.025f, i * 0.4f);
        for (int i = 0; i < 8; i++)
            AddBob(Cut(ext, "Wave" + i), new Vector2(16, 5), 0f, 0.2f, i * 0.137f);
        AddBob(Cut(ext, "Boat"), new Vector2(0, 7), 1.6f, 0.17f, 0.3f);
        int trashIndex = 0;
        foreach (var name in Art.Keys.Where(k => k.StartsWith("Trash")).ToList())
            AddBob(Cut(ext, name), new Vector2(9, 6), 7f, 0.14f + 0.02f * (trashIndex % 3), 0.23f * trashIndex++);

        var doorLeft = Cut(ext, "DoorLeafLeft");
        var doorRight = Cut(ext, "DoorLeafRight");

        var keyGlow = MakeImage(ext, "KeyGlow", LoadSprite("Glow", SpriteDir));
        Place(keyGlow.rectTransform, Centered(1920, 980, 190));
        Pulse(keyGlow, 0.55f, 0.95f, 0.6f, 0f, 0.1f);
        var key = Cut(ext, "Key");
        key.raycastTarget = true;
        var keyButton = key.gameObject.AddComponent<Button>();
        keyButton.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(keyButton.onClick, c.TakeKey);
        var sparkle = MakeImage(ext, "KeySparkle", LoadSprite("Sparkle", SpriteDir));
        Place(sparkle.rectTransform, Centered(1986, 966, 46));
        Pulse(sparkle, 0.15f, 1f, 0.8f, 0.25f, 0.35f);
        var takeKey = ArtButton(ext, "TakeKeyButton", c.TakeKey);

        var questStep = Label(ext, "QuestStep", "", fontHeavy, 28, QuestStepInk, R2(47, 37, 405, 22));
        var questTitle = Label(ext, "QuestTitle", "", fontHand, 62, QuestTitleInk, R2(47, 60, 405, 42));
        var questBody = Label(ext, "QuestBody", "", fontHeavy, 32, QuestBodyInk, R2(46, 103, 410, 28));

        // ================= 2: Gun plani =================
        var dayPlan = MakePage(canvasGo.transform, "DayPlanPage", "DayPlan", PlanArt, out var plan);
        var firstTitle = Label(plan, "FirstTaskTitle", "Open the workshop", fontHeavy, 37, RowInk, new Rect(952, 518, 700, 48));
        var firstSub = Label(plan, "FirstTaskPlace", "Grandpa’s workshop", fontBody, 27, RowSubInk, new Rect(954, 565, 700, 38));
        var firstStrike = MakeImage(plan, "FirstTaskStrike", null);
        firstStrike.color = RowInk;
        PlaceLeft(firstStrike.rectTransform, new Rect(946, 540, 0, 5));
        var firstCheck = Cut(plan, "CheckOn");
        var doneBadge = Cut(plan, "BadgeDone");
        var startDay = ArtButton(plan, "StartDayButton", c.StartDay, 1.02f);

        // ================= 3: Atolye =================
        var workshop = MakePage(canvasGo.transform, "WorkshopPage", "Workshop", WideArt, out var shop);
        ArtButton(shop, "MenuButton", c.BackToMenu);
        ArtButton(shop, "HeadOutButton", c.HeadOut);
        var tasks = ArtButton(shop, "TasksButton", c.ToggleTasks);
        var tasksActive = MakeImage(tasks.transform, "Active", LoadSprite("TasksButtonActive", SpriteDir));
        PlaceIn(tasksActive.rectTransform, Art["TasksButtonActive"], Art["TasksButton"]);
        var badge = Cut(shop, "TaskBadge");
        var badgeText = Label(badge.transform, "Count", "4", fontHeavy, 26, BadgeInk, new Rect(0, -2, 52, 52), TextAnchor.MiddleCenter);

        var dialogue = Label(shop, "Dialogue", "", fontBody, 38, DialogueInk, R2(58, 549, 780, 70), TextAnchor.UpperLeft);
        dialogue.lineSpacing = 1.12f;
        dialogue.horizontalOverflow = HorizontalWrapMode.Wrap;
        var tellMore = ArtButton(shop, "TellMoreButton", c.TellMore);
        var letsFix = ArtButton(shop, "LetsFixButton", c.LetsFix);

        var panelRect = Art["TasksPanel"];
        var panel = Cut(shop, "TasksPanel");
        panel.raycastTarget = true; // alttaki atolyeye tiklanmasin
        var count = Label(panel.transform, "Count", "", fontHeavy, 27, PanelInk,
            Local(R2(911, 175, 300, 22), panelRect));
        var meetFade = MakeImage(panel.transform, "MeetFade", null);
        meetFade.color = PanelHighlight;
        PlaceIn(meetFade.rectTransform, R2(965, 294, 240, 42), panelRect);
        var meetStrike = MakeImage(panel.transform, "MeetStrike", null);
        meetStrike.color = RowInk;
        PlaceLeft(meetStrike.rectTransform, Local(R2(965, 304.5f, 0, 2.2f), panelRect));
        var meetCheck = MakeImage(panel.transform, "MeetCheck", LoadSprite("PanelCheck", SpriteDir));
        PlaceIn(meetCheck.rectTransform, Art["PanelCheck"], panelRect);
        var repairFade = MakeImage(panel.transform, "RepairFade", null);
        repairFade.color = PanelPaper;
        PlaceIn(repairFade.rectTransform, R2(965, 368, 262, 58), panelRect);
        var repairStrike = MakeImage(panel.transform, "RepairStrike", null);
        repairStrike.color = RowInk;
        PlaceLeft(repairStrike.rectTransform, Local(R2(988, 381.5f, 0, 2.2f), panelRect));
        var repairCheck = MakeImage(panel.transform, "RepairCheck", LoadSprite("PanelCheck", SpriteDir));
        PlaceIn(repairCheck.rectTransform, new Rect(1846, 740, 70, 68), panelRect);
        var close = MakeImage(panel.transform, "CloseButton", null);
        close.color = new Color(1, 1, 1, 0);
        close.raycastTarget = true;
        PlaceIn(close.rectTransform, R2(1184, 127, 43, 43), panelRect);
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(closeButton.onClick, c.CloseTasks);

        // ================= 3b: Tamirden sonra =================
        var coinIcon = MakeImage(shop, "CoinIcon", LoadSprite("Coin", SpriteDir));
        Place(coinIcon.rectTransform, Centered(1807, 73, 30));
        var coinCounter = Label(shop, "CoinCount", "120", fontHeavy, 34, Color.white, new Rect(1842, 50, 120, 46));

        var next = CardButton(shop, "NextButton", "HelpButtonOutline", R2(642, 627, 182, 54), "Next", DialogueInk, 38, c.NextLine);
        var dialogueHeadOut = CardButton(shop, "DialogueHeadOutButton", "HelpButtonTeal", R2(642, 627, 182, 54), "Head out",
            Color.white, 38, c.HeadOut);

        var floating = new[] { "Note", "Heart", "Note" }.Select((n, i) =>
        {
            var img = MakeImage(shop, "Float" + n + i, LoadSprite(n, SpriteDir));
            Place(img.rectTransform, Centered(0, 0, 80));
            return img;
        }).ToArray();

        // Odul karti
        var rewardOverlay = Dim(shop, "RewardOverlay");
        var reward = MakeImage(rewardOverlay.transform, "RewardCard", LoadSprite("RewardCard", SpriteDir));
        Place(reward.rectTransform, new Rect(400 + 1280 - 536, 400 + 640 - 316, 1072, 632));
        var rt = reward.transform;
        Label(rt, "Title", "Repair complete!", fontTitle, 76, QuestTeal, new Rect(36, 60, 1000, 120), TextAnchor.MiddleCenter);
        var rewardCoin = MakeImage(rt, "Coin", LoadSprite("Coin", SpriteDir));
        Place(rewardCoin.rectTransform, Centered(536, 248, 72));
        Label(rt, "Reward", "+40 coins · Hatice Teyze is now your helper", fontHeavy, 40, DialogueInk,
            new Rect(66, 292, 940, 64), TextAnchor.MiddleCenter);
        var quote = Label(rt, "Quote", "“Fixing an old thing means less waste than buying a new one.”", fontHand, 54,
            QuestTitleInk, new Rect(96, 372, 880, 170), TextAnchor.MiddleCenter);
        quote.horizontalOverflow = HorizontalWrapMode.Wrap;

        // "Let's Save the Seas!" paneli
        var missionOverlay = Dim(shop, "MissionOverlay");
        var mission = MakeImage(missionOverlay.transform, "MissionCard", LoadSprite("MissionCard", SpriteDir));
        Place(mission.rectTransform, new Rect(400 + 1280 - 736, 400 + 720 - 526, 1472, 1052));
        var mt = mission.transform;
        Label(mt, "Title", "Let's Save the Seas!", fontTitle, 84, QuestTeal, new Rect(36, 64, 1400, 130), TextAnchor.MiddleCenter);
        var body = Label(mt, "Body", "Every piece of trash that washes ashore blocks a caretta hatchling's way to the sea. " +
            "Clean the sea, tidy up the beach and help the hatchlings reach the water.", fontBody, 40, DialogueInk,
            new Rect(136, 240, 1200, 200), TextAnchor.MiddleCenter);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.lineSpacing = 1.1f;
        var bullets = new[]
        {
            "<color=#17777A>Clean the sea:</color> collect trash by boat.",
            "<color=#17777A>Recycle:</color> sort every piece into the right bin.",
            "<color=#17777A>Protect the carettas:</color> open a safe path from the nests.",
        };
        for (int i = 0; i < bullets.Length; i++)
        {
            float y = 480 + i * 96;
            var tick = MakeImage(mt, "Tick" + i, LoadSprite("StepDone", BenchDir));
            Place(tick.rectTransform, new Rect(220, y + 12, 56, 56));
            var line = Label(mt, "Bullet" + i, bullets[i], fontHeavy, 40, DialogueInk, new Rect(300, y, 1060, 80));
            line.supportRichText = true;
        }
        CardButton(mt, "HeadOut", "HelpButtonTeal", new Rect(296, 820, 420, 112), "Head out", Color.white, 42, c.MissionHeadOut);
        CardButton(mt, "Later", "HelpButtonOutline", new Rect(756, 820, 420, 112), "Later", DialogueInk, 42, c.MissionLater);

        var coin = MakeImage(shop, "FlyingCoin", LoadSprite("Coin", SpriteDir));
        Place(coin.rectTransform, Centered(0, 0, 64));

        // ================= 4: Tamir tezgahi =================
        var workbench = BuildWorkbench(canvasGo.transform, c, out var repair);

        // ================= Ekran gecisi =================
        var faderImg = MakeImage(canvasGo.transform, "ScreenFader", null);
        faderImg.color = Night;
        faderImg.raycastTarget = true;
        Stretch(faderImg.rectTransform);
        var fader = faderImg.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 1f;

        SetRefs(c,
            ("exteriorPage", exterior), ("dayPlanPage", dayPlan), ("workshopPage", workshop), ("screenFader", fader),
            ("questStep", questStep), ("questTitle", questTitle), ("questBody", questBody),
            ("takeKeyButton", takeKey), ("key", key.rectTransform), ("keyGlow", keyGlow), ("keySparkle", sparkle),
            ("doorLeft", doorLeft.rectTransform), ("doorRight", doorRight.rectTransform),
            ("firstCheck", firstCheck.rectTransform), ("doneBadge", doneBadge.rectTransform),
            ("firstTitle", firstTitle), ("firstSubtitle", firstSub), ("firstStrike", firstStrike.rectTransform),
            ("startDayButton", startDay),
            ("dialogue", dialogue), ("tellMoreButton", tellMore), ("letsFixButton", letsFix),
            ("tasksButtonActive", tasksActive.gameObject), ("tasksPanel", panel.rectTransform),
            ("taskCount", count), ("taskBadge", badgeText),
            ("meetCheck", meetCheck.rectTransform), ("meetStrike", meetStrike.rectTransform), ("meetFade", meetFade),
            ("repairCheck", repairCheck.rectTransform), ("repairStrike", repairStrike.rectTransform), ("repairFade", repairFade),
            ("workbenchPage", workbench), ("repair", repair),
            ("nextButton", next), ("dialogueHeadOutButton", dialogueHeadOut),
            ("rewardCard", reward.rectTransform), ("rewardCoinIcon", rewardCoin.rectTransform), ("coinTemplate", coin),
            ("coinCounterIcon", coinIcon.rectTransform), ("coinCounter", coinCounter),
            ("rewardOverlay", rewardOverlay.gameObject), ("missionOverlay", missionOverlay.gameObject),
            ("missionCard", mission.rectTransform));
        SetArray(c, "floatingSprites", floating);
        var so = new SerializedObject(c);
        SetStrings(so.FindProperty("dialogueLines"), DialogueLines);
        SetStrings(so.FindProperty("afterRepairLines"), AfterRepairLines);
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        ArtLocalization.Apply(canvasGo.transform);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        EditorPrefs.SetInt(VersionKey, BuilderVersion);
        Debug.Log("[Tideworks] Workshop scene created: " + ScenePath);
    }

    // ---------------- Yardimcilar ----------------

    /// <summary>Workshop'u Intro'nun hemen arkasina koyar.</summary>
    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        int intro = scenes.FindIndex(s => s.path == "Assets/Scenes/Intro.unity");
        if (intro < 0) intro = scenes.FindIndex(s => s.path == "Assets/Scenes/MainMenu.unity");
        scenes.Insert(intro + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    /// <summary>Sayfa: bulanik dolgu (Cover) + tasarim artboard'u (Contain) + arka plan gorseli.</summary>
    static CanvasGroup MakePage(Transform parent, string name, string art, Vector2 size, out Transform artboard,
        string dir = SpriteDir)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);

        var backdrop = MakeImage(go.transform, "Backdrop", LoadSprite(art + "Backdrop", dir));
        var fit = backdrop.gameObject.AddComponent<ArtboardFitter>();
        fit.artSize = size;
        fit.mode = ArtboardFitter.FitMode.Cover;
        backdrop.rectTransform.sizeDelta = size;

        var board = new GameObject("Artboard", typeof(RectTransform), typeof(ArtboardFitter)).transform as RectTransform;
        board.SetParent(go.transform, false);
        board.GetComponent<ArtboardFitter>().artSize = size;
        board.sizeDelta = size;
        var bg = MakeImage(board, "Background", LoadSprite(art + "Background", dir));
        Place(bg.rectTransform, new Rect(Vector2.zero, size));

        artboard = board;
        return go.GetComponent<CanvasGroup>();
    }

    /// <summary>Kesilmis gorseli tasarimdaki yerine koyar.</summary>
    static Image Cut(Transform parent, string name)
    {
        var img = MakeImage(parent, name, LoadSprite(name, SpriteDir));
        Place(img.rectTransform, Art[name]);
        return img;
    }

    static Button ArtButton(Transform parent, string name, UnityAction onClick, float hover = 1.05f)
    {
        var img = Cut(parent, name);
        img.raycastTarget = true;
        var button = img.gameObject.AddComponent<Button>();
        // Alttaki gorselde ayni buton var: renkler opak kalmali yoksa altindaki gorunur
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        img.gameObject.AddComponent<MenuButtonFx>().hoverScale = hover;
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }

    static void AddBob(Image img, Vector2 move, float tilt, float speed, float phase)
    {
        var bob = img.gameObject.AddComponent<Bob>();
        bob.move = move;
        bob.tilt = tilt;
        bob.speed = speed;
        bob.phase = phase;
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

    static Text Label(Transform parent, string name, string content, Font font, int size, Color color, Rect r,
        TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var t = MakeText(parent, name, content, size, font, color, anchor);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Place(t.rectTransform, r);
        return t;
    }

    /// <summary>1280x720 tasarim koordinati -> 2x artboard.</summary>
    static Rect R2(float x, float y, float w, float h) => new Rect(x * 2, y * 2, w * 2, h * 2);

    static Rect Centered(float cx, float cy, float size) => new Rect(cx - size / 2, cy - size / 2, size, size);

    /// <summary>Artboard koordinatini ebeveynin sol ust kosesine gore yapar.</summary>
    static Rect Local(Rect r, Rect parent) => new Rect(r.x - parent.x, r.y - parent.y, r.width, r.height);

    static void PlaceIn(RectTransform rt, Rect r, Rect parent) => Place(rt, Local(r, parent));

    /// <summary>Soldan saga uzayan cizgi (ustu cizili gorev): pivot sol kenarda.</summary>
    static void SetStrings(SerializedProperty array, string[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    /// <summary>Ekrani karartan, arkaya tiklatmayan katman (icindeki kart ortalanir); baslangicta kapali.</summary>
    static Image Dim(Transform parent, string name)
    {
        var img = MakeImage(parent, name, null);
        img.color = new Color(0.02f, 0.08f, 0.1f, 0.6f);
        img.raycastTarget = true;
        Place(img.rectTransform, new Rect(-400, -400, WideArt.x + 800, WideArt.y + 800));
        img.gameObject.SetActive(false);
        return img;
    }

    /// <summary>Kart / diyalog butonu (Workbench klasorundeki yuvarlak buton gorselleri) + yazi.</summary>
    static Button CardButton(Transform parent, string name, string sprite, Rect r, string label, Color ink, int size,
        UnityAction onClick)
    {
        var img = MakeImage(parent, name, LoadSprite(sprite, BenchDir));
        img.raycastTarget = true;
        Place(img.rectTransform, r);
        var b = img.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.fadeDuration = 0.08f;
        b.colors = colors;
        img.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.04f;
        var t = Label(img.transform, "Label", label, fontHeavy, size, ink, new Rect(0, -r.height * 0.04f, r.width, r.height),
            TextAnchor.MiddleCenter);
        t.resizeTextForBestFit = true;
        t.resizeTextMaxSize = size;
        t.resizeTextMinSize = size / 2;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        UnityEventTools.AddPersistentListener(b.onClick, onClick);
        return b;
    }

    static void PlaceLeft(RectTransform rt, Rect r)
    {
        Place(rt, r);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(r.x, -(r.y + r.height / 2));
    }
}
