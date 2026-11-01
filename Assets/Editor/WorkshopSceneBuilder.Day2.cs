using System;
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
/// 2. ve sonraki gunlerin atolye sahneleri (Assets/Scenes/DayNWorkshop.unity): musteri ve bozuk esyasi, gunun gorev paneli,
/// tamir tezgahi, tesekkur, odul karti ve gunun gorev kartı. Ortak kurulum BuildWorkshopDay; gune ozel veriler WorkshopDay'de.
/// 2. gun (bu dosya): Elif ve oyuncak ayisi Pamuk. 3. gun: WorkshopSceneBuilder.Day3.cs.
/// Gorseller: Assets/Sprites/Day2 (Tools/MenuArt/workshop/day2_workshop_art.py) + 1. gunun buton / panel parcalari.
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Day 2 Workshop Scene
/// </summary>
public static partial class WorkshopSceneBuilder
{
    /// <summary>Bir atolye gununun sahneye giren verileri.</summary>
    class WorkshopDay
    {
        public int day;
        public string scenePath, artDir;
        public string customer, ownerSub, rewardLine, helperPill, quote;
        public string[] lines, thanks;
        public (string title, string sub)[] tasks;
        public int progressTask;                 // ilerleme cubugu olan gorev satiri
        public string progressLabel;             // "0 / 6"
        public string missionTitle, missionBody;
        public (string color, string icon, string text)[] missionRows;
        public string seaScene, requiredItem, helpFirstLine;
        public Vector2 customerHead;
        public string[] steps, stepSubs;
        public string[] slotSprites, toolLabels;
        public (string name, string source, string bin)[] shelf;
        /// <summary>Tezgah sayfasina tamir controller'ini ekler.</summary>
        public Func<GameObject, RepairBench> addBench;
        /// <summary>Tamir edilen esyayi (katmanlar, tiklanan noktalar) kurar ve controller'a baglar.</summary>
        public Action<RepairBench, Transform> buildSubject;
        public int version;
        public string autoBuildKey;
        public string VersionKey => $"Tideworks.Day{day}BuilderVersion." + PlayerSettings.productGUID;
    }

    const string Day2ScenePath = "Assets/Scenes/Day2Workshop.unity";
    const string Day2Dir = "Assets/Sprites/Day2/";
    const string FishingUiDir = "Assets/Sprites/Fishing/";
    const string BeachUiDir = "Assets/Sprites/Beach/";

    static readonly string[] ElifLines =
    {
        "Hello... Are you Grandpa Yusuf's grandchild? My grandma Hatice says you fixed her radio. " +
        "This is Pamuk. I found him on the beach this morning, all tangled up in a net.",
        "His arm came right off, his button eye fell out, and he's soaking wet inside. " +
        "They told me to throw him away, but... Pamuk is my best friend. Can you make him better?",
    };

    // Pamuk iyilesince (her biri ayri diyalog kutusu, "Next" ile; sonuncusundan sonra "You're welcome!")
    static readonly string[] ElifThanks =
    {
        "Pamuk! Is it really him? His arm is back... and look at his eye, it's blue like the sea!",
        "What did you stuff him with? Plastic bottles? So the bottles we pulled out of the sea are inside Pamuk now? That's amazing!",
        "Grandma always says, \u2018Don't throw the old away, mend it.\u2019 Now I get it: everything we throw away ends up somewhere, " +
        "and most of it ends up in the sea.",
        "This is from my piggy bank, take it. Thank you so much! Today I'll tell all my friends: don't leave anything on the beach. " +
        "A net caught Pamuk... it could catch a caretta too!",
    };
    static readonly (string title, string sub)[] Day2Tasks =
    {
        ("Fix Hatice Teyze's radio", "It plays the fishing news again."),
        ("Meet Elif", "Hatice Teyze's granddaughter, holding a soaked teddy bear."),
        ("Repair Pamuk the teddy bear", "Needs: sewing kit, recycled stuffing, a sea-glass button."),
        ("Clear the ghost nets by boat", "The storm dragged lost nets to the shore."),
        ("Free the tangled caretta", "A turtle is caught in a net near the rocks."),
    };

    static readonly string[] TeddySteps = { "Cut the net away", "Wash off the sea", "Re-stuff the shoulder", "Sew the arm back on", "Give him a new eye", "Patch it with love" };
    static readonly string[] TeddySubs = { "Scissors · 3 knots", "Sponge · 4 spots", "Tweezers, then recycled fiber", "Needle · rope thread", "Sea-glass button", "Sail-cloth heart" };

    static WorkshopDay Day2 => new WorkshopDay
    {
        day = 2, scenePath = Day2ScenePath, artDir = Day2Dir, version = 4, autoBuildKey = "Tideworks.Day2AutoBuild.v4",
        customer = "Elif", ownerSub = "Pamuk, her best friend",
        lines = ElifLines, thanks = ElifThanks, tasks = Day2Tasks, progressTask = 3, progressLabel = "0 / 6",
        rewardLine = "Elif joins as a helper · +50 coins", helperPill = "Elif is now your helper",
        quote = "\u201cA mended toy keeps its story. A new one only keeps its price tag.\u201d",
        missionTitle = "Clear the ghost nets!",
        missionBody = "Last night's storm washed lost fishing nets onto the shore. These ghost nets keep catching fish " +
                      "and carettas for years. Let's pull them out of the sea!",
        missionRows = new[] { ("Teal", "IconBoatWhite", "Collect the nets"), ("Green", "IconRecycleWhite", "Sort them"),
                              ("Orange", "IconTurtleWhite", "Free the caretta") },
        seaScene = "Day2Sea", requiredItem = "sewing_kit", helpFirstLine = "Can you help Pamuk first? Please?",
        customerHead = new Vector2(1440, 600),
        steps = TeddySteps, stepSubs = TeddySubs,
        slotSprites = new[] { "ToolScissors", "ToolSponge", "ToolTweezers", "ToolNeedle", "ShelfFiber", "ShelfThread", "ShelfButton", "ShelfHeart" },
        toolLabels = new[] { "Scissors", "Sponge", "Tweezers", "Needle" },
        shelf = new[]
        {
            ("Recycled fiber", "from 6 PET bottles", "Plastic bin"), ("Rope thread", "from old rope", "Rope bin"),
            ("Sea-glass button", "from a green bottle", "Glass bin"), ("Sail-cloth heart", "from an old sail", "Nets bin"),
        },
        addBench = go => go.AddComponent<TeddyRepairController>(),
        buildSubject = (b, bench) => BuildTeddy((TeddyRepairController)b, bench),
    };

    static void RequestWorkshopDay(WorkshopDay d)
    {
        if (File.Exists(d.scenePath) && EditorPrefs.GetInt(d.VersionKey, 0) >= d.version) return;
        SceneAutoBuild.Request(d.autoBuildKey, () => BuildWorkshopDay(d, askToSave: true));
    }

    static void RequestDay2() => RequestWorkshopDay(Day2);

    [MenuItem("Tideworks/Rebuild Day 2 Workshop Scene")]
    static void RebuildDay2Menu() => RebuildWorkshopDayMenu(Day2);

    static void RebuildWorkshopDayMenu(WorkshopDay d)
    {
        if (File.Exists(d.scenePath) && !EditorUtility.DisplayDialog($"Rebuild Day {d.day}",
                $"{Path.GetFileNameWithoutExtension(d.scenePath)} sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
                "Rebuild", "Cancel"))
            return;
        BuildWorkshopDay(d, askToSave: true);
    }

    static void BuildWorkshopDay(WorkshopDay d, bool askToSave)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConfigureSprites(SpriteDir);
        ConfigureSprites(BenchDir);
        ConfigureSprites(Day2Dir);
        ConfigureSprites(d.artDir);
        ConfigureSprites("Assets/Sprites/Shop/");
        if (Directory.Exists(d.artDir + "TR/")) ConfigureSprites(d.artDir + "TR/");
        ConfigureSprites(FishingUiDir);
        ConfigureSprites(BeachUiDir);
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

        var canvasGo = new GameObject($"Day{d.day}Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var c = canvasGo.AddComponent<WorkshopDayController>();

        // ================= Atolye ici =================
        var interior = MakePage(canvasGo.transform, "InteriorPage", $"InteriorDay{d.day}", WideArt, out var shop, d.artDir);
        ArtButton(shop, "MenuButton", c.BackToMenu);
        ArtButton(shop, "HeadOutButton", c.HeadOut);
        var tasks = ArtButton(shop, "TasksButton", c.ToggleTasks);
        var tasksActive = MakeImage(tasks.transform, "Active", LoadSprite("TasksButtonActive", SpriteDir));
        PlaceIn(tasksActive.rectTransform, Art["TasksButtonActive"], Art["TasksButton"]);
        var badge = Cut(shop, "TaskBadge");
        var badgeText = Label(badge.transform, "Count", "4", fontHeavy, 26, BadgeInk, new Rect(0, -2, 52, 52), TextAnchor.MiddleCenter);
        var subtitle = Label(shop, "Subtitle", "", fontHeavy, 26, Color.white, R2(84, 39, 320, 24));
        var health = Label(shop, "Health", "", fontTitle, 36, Color.white, R2(500, 24, 70, 26));
        var coinIcon = MakeImage(shop, "CoinIcon", LoadSprite("Coin", SpriteDir));
        Place(coinIcon.rectTransform, Centered(1807, 73, 30));
        var coins = Label(shop, "CoinCount", "", fontHeavy, 34, Color.white, new Rect(1842, 50, 120, 46));
        // Dukkan (2. gunden itibaren acik)
        var shopBtn = Img2(shop, "ShopButton", "Assets/Sprites/Shop/", "ShopButtonHud", new Rect(1540, 26, 210, 92));
        Label(shopBtn.transform, "Text", "Shop", fontTitle, 40, new Color32(60, 40, 20, 255), new Rect(0, -2, 210, 92), TextAnchor.MiddleCenter);
        shopBtn.raycastTarget = true;
        UnityEventTools.AddPersistentListener(shopBtn.gameObject.AddComponent<Button>().onClick, c.OpenShop);
        shopBtn.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.05f;
        var floating = Label(shop, "FloatingCoins", "", fontTitle, 56, Yellow, new Rect(1760, 140, 200, 80), TextAnchor.MiddleCenter);
        floating.gameObject.AddComponent<Outline>().effectColor = new Color(0.06f, 0.2f, 0.24f, 0.9f);

        var pill = MakeImage(shop, "NamePill", LoadSprite("NamePillYellow", Day2Dir));
        Place(pill.rectTransform, new Rect(110, 1022, 300, 92));
        Label(pill.transform, "Name", d.customer, fontTitle, 38, new Color32(60, 40, 30, 255), new Rect(0, -2, 300, 92), TextAnchor.MiddleCenter);
        var dialogue = Label(shop, "Dialogue", "", fontBody, 38, DialogueInk, R2(58, 549, 780, 70), TextAnchor.UpperLeft);
        dialogue.lineSpacing = 1.12f;
        dialogue.horizontalOverflow = HorizontalWrapMode.Wrap;
        var tellMore = ArtButton(shop, "TellMoreButton", c.TellMore);
        var letsFix = ArtButton(shop, "LetsFixButton", c.LetsFix);
        var headOut2 = CardButton(shop, "DialogueHeadOutButton", "HelpButtonTeal", R2(642, 627, 182, 54), "Head out", Color.white, 38, c.HeadOut);

        // Gorev paneli: bos panel + gunun satirlari
        var panelRect = Art["TasksPanel"];
        var panel = MakeImage(shop, "TasksPanel", LoadSprite("TasksPanelBlank", Day2Dir));
        panel.raycastTarget = true;
        Place(panel.rectTransform, panelRect);
        var pt = panel.transform;
        Label(pt, "Title", "Today's tasks", fontHand, 72, QuestTeal, new Rect(56, 44, 520, 100));
        var count = Label(pt, "Count", "", fontHeavy, 26, PanelInk, new Rect(60, 150, 560, 40));
        var hl = MakeImage(pt, "Highlight", LoadSprite("TaskHighlight", Day2Dir));
        var checks = new Image[5];
        var titles = new Text[5];
        var strikes = new RectTransform[5];
        for (int i = 0; i < d.tasks.Length; i++)
        {
            float y = 214 + i * 172;
            if (i == 0) Place(hl.rectTransform, new Rect(62, y - 8, 660, 136));
            checks[i] = MakeImage(pt, "Check" + i, LoadSprite("CheckOff", BeachUiDir));
            Place(checks[i].rectTransform, new Rect(84, y + 34, 52, 52));
            titles[i] = Label(pt, "TaskTitle" + i, d.tasks[i].title, fontHeavy, 32, RowInk, new Rect(152, y + 4, 540, 48));
            titles[i].resizeTextForBestFit = true;
            titles[i].resizeTextMaxSize = 32;
            titles[i].resizeTextMinSize = 22;
            titles[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            var sub = Label(pt, "TaskSub" + i, d.tasks[i].sub, fontBody, 23, PanelInk, new Rect(152, y + 52, 540, 64), TextAnchor.UpperLeft);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            var strike = MakeImage(pt, "Strike" + i, null);
            strike.color = RowInk;
            PlaceLeft(strike.rectTransform, new Rect(148, y + 28, 0, 4));
            strikes[i] = strike.rectTransform;
        }
        Img2(pt, "ProgressTrack", FishingUiDir, "BarTrack", new Rect(152, 214 + d.progressTask * 172 + 118, 420, 18));
        Label(pt, "ProgressCount", d.progressLabel, fontHeavy, 24, RowInk, new Rect(590, 214 + d.progressTask * 172 + 104, 100, 44));
        Label(pt, "RewardTitle", "DAY REWARD", fontHeavy, 22, new Color32(150, 210, 205, 255), new Rect(150, 1122, 520, 30));
        var reward = Label(pt, "Reward", d.rewardLine, fontHeavy, 26, Color.white, new Rect(150, 1152, 560, 70), TextAnchor.UpperLeft);
        reward.horizontalOverflow = HorizontalWrapMode.Wrap;
        var close = Spot(pt, "CloseButton", new Rect(626, 72, 82, 82), c.CloseTasks);

        // Tamirden sonra: Next / You're welcome, kalpler
        var next = CardButton(shop, "NextButton", "HelpButtonOutline", R2(642, 627, 182, 54), "Next", DialogueInk, 38, c.NextLine);
        var welcome = CardButton(shop, "WelcomeButton", "HelpButtonOrange", R2(586, 627, 238, 54), "You're welcome!", Color.white, 36, c.NextLine);
        var heartTpl = MakeImage(shop, "HeartTemplate", LoadSprite("Heart", SpriteDir));
        Place(heartTpl.rectTransform, Centered(0, 0, 80));

        // Odul karti (1. gunun karti, gunun odulu)
        var rewardOverlay = Dim(shop, "RewardOverlay");
        var rewardCard = MakeImage(rewardOverlay.transform, "RewardCard", LoadSprite("RewardCard", SpriteDir));
        Place(rewardCard.rectTransform, new Rect(400 + 1280 - 530, 400 + 180, 1060, 1160));
        var rt = rewardCard.transform;
        var pile = MakeImage(rt, "CoinPile", LoadSprite("CoinPile", SpriteDir));
        pile.SetNativeSize();
        Place(pile.rectTransform, new Rect(530 - pile.rectTransform.sizeDelta.x / 2, 66, pile.rectTransform.sizeDelta.x, pile.rectTransform.sizeDelta.y));
        Label(rt, "Title", "Repair complete!", fontTitle, 96, RewardInk, new Rect(40, 396, 980, 130), TextAnchor.MiddleCenter);
        var coinsPill = MakeImage(rt, "CoinsPill", LoadSprite("RewardPillCoins", SpriteDir));
        Place(coinsPill.rectTransform, new Rect(200, 554, 300, 100));
        Label(coinsPill.transform, "Text", "+50 coins", fontTitle, 52, Yellow, new Rect(0, 2, 300, 100), TextAnchor.MiddleCenter);
        var helperPill = MakeImage(rt, "HelperPill", LoadSprite("RewardPillHelper", SpriteDir));
        Place(helperPill.rectTransform, new Rect(520, 561, 360, 86));
        var helper = Label(helperPill.transform, "Text", d.helperPill, fontTitle, 36, Color.white, new Rect(14, 2, 332, 86), TextAnchor.MiddleCenter);
        helper.resizeTextForBestFit = true;
        helper.resizeTextMaxSize = 36;
        helper.resizeTextMinSize = 22;
        helper.horizontalOverflow = HorizontalWrapMode.Wrap;
        var quote = Label(rt, "Quote", d.quote, fontHand, 60,
            QuoteInk, new Rect(90, 690, 880, 220), TextAnchor.MiddleCenter);
        quote.horizontalOverflow = HorizontalWrapMode.Wrap;
        quote.lineSpacing = 0.9f;
        CardButton(rt, "Continue", "ButtonOrange", new Rect(350, 958, 360, 120), "Continue", Color.white, 56, c.RewardContinue, SpriteDir, fontTitle);

        // Gunun gorev karti ("Clear the ghost nets!", "Clean the rock pools!")
        var missionOverlay = Dim(shop, "MissionOverlay");
        var mission = MakeImage(missionOverlay.transform, "MissionFrame", LoadSprite("MissionFrame", SpriteDir));
        Place(mission.rectTransform, new Rect(400 + 1280 - 1124, 400 + 740 - 634, 2248, 1268));
        var mt = mission.transform;
        var beachArt = MakeImage(mt, "BeachArt", LoadSprite("BeachArt", SpriteDir));
        Place(beachArt.rectTransform, new Rect(84, 84, 1000, 1100));
        var right = MakeImage(mt, "Card", LoadSprite("MissionCardRight", SpriteDir));
        Place(right.rectTransform, new Rect(1114, 74, 1050, 1120));
        var ct = right.transform;
        var mTitle = Label(ct, "Title", d.missionTitle, fontTitle, 108, RewardInk, new Rect(60, 40, 930, 270), TextAnchor.UpperLeft);
        mTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        mTitle.lineSpacing = 0.8f;
        var body = Label(ct, "Body", d.missionBody, fontBody, 38, DialogueInk, new Rect(60, 312, 930, 236), TextAnchor.UpperLeft);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.lineSpacing = 1.08f;
        var mrows = d.missionRows;
        for (int i = 0; i < mrows.Length; i++)
        {
            var (color, icon, text) = mrows[i];
            var row = MakeImage(ct, "Row" + color, LoadSprite("MissionRow" + color, SpriteDir));
            Place(row.rectTransform, new Rect(60, 560 + i * 136, 930, 120));
            var ic = MakeImage(row.transform, "Icon", LoadSprite(icon, SpriteDir));
            Place(ic.rectTransform, new Rect(44, 24, 72, 72));
            Label(row.transform, "Text", text, fontTitle, 52, Color.white, new Rect(150, 2, 760, 120));
        }
        var sail = CardButton(ct, "SetSail", "ButtonOrangeWide", new Rect(60, 974, 560, 120), "Set sail", Color.white, 54,
            c.SetSail, SpriteDir, fontTitle);
        var sailIcon = MakeImage(sail.transform, "Icon", LoadSprite("IconBoatWhite", SpriteDir));
        Place(sailIcon.rectTransform, new Rect(120, 22, 64, 64));
        sail.transform.Find("Label").GetComponent<RectTransform>().anchoredPosition += new Vector2(36, 0);
        CardButton(ct, "Later", "ButtonOutline", new Rect(660, 974, 330, 120), "Later", DialogueInk, 44, c.MissionLater, SpriteDir, fontHeavy);

        var flyingCoin = MakeImage(shop, "FlyingCoin", LoadSprite("Coin", SpriteDir));
        Place(flyingCoin.rectTransform, Centered(0, 0, 64));

        // ================= Tezgah =================
        var benchPage = MakePage(canvasGo.transform, "BenchPage", $"BenchDay{d.day}", WideArt, out var bench, d.artDir);
        var teddyCtl = d.addBench(benchPage.gameObject);
        Spot(bench, "BackSpot", R2(22, 12, 48, 48), teddyCtl.Back);
        Spot(bench, "WorkshopTabSpot", R2(338, 13, 122, 47), teddyCtl.Back);
        Label(bench, "TabWorkshop", "Workshop", fontTitle, 32, Color.white, R2(338, 13, 122, 47), TextAnchor.MiddleCenter);
        Label(bench, "TabWorkbench", "Workbench", fontTitle, 32, QuestTeal, R2(463, 13, 127, 47), TextAnchor.MiddleCenter);
        Label(bench, "Subtitle", $"Workbench · Day {d.day}", fontHeavy, 26, Color.white, R2(84, 39, 320, 24));
        var restart = Img2(bench, "Restart", FishingUiDir, "RestartButton", R2(918, 15, 100, 40));
        Label(restart.transform, "Text", "Restart", fontHeavy, 30, QuestTeal, new Rect(0, -2, 200, 80), TextAnchor.MiddleCenter);
        restart.raycastTarget = true;
        var rb = restart.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(rb.onClick, teddyCtl.Restart);
        Label(bench, "RepairLabel", "Repair", fontHeavy, 24, Color.white, R2(1022, 26, 50, 22));
        var track = MakeImage(bench, "RepairTrack", null);
        track.color = BarTrack;
        Place(track.rectTransform, R2(1071, 31, 139, 11));
        var fill = MakeImage(bench, "RepairFill", null);
        fill.color = BarFill;
        PlaceLeft(fill.rectTransform, R2(1071, 31, 0, 11));
        var progress = Label(bench, "RepairCount", "0 / 6", fontHeavy, 34, Color.white, R2(1221, 24, 60, 24));

        Label(bench, "OwnerName", d.customer, fontTitle, 34, new Color32(30, 30, 30, 255), R2(108, 120, 180, 28));
        Label(bench, "OwnerSub", d.ownerSub, fontBody, 22, StepSubInk, R2(108, 145, 185, 20));
        Label(bench, "StepsTitle", "REPAIR STEPS", fontHeavy, 22, StepSubInk, R2(40, 182, 240, 18));
        var rows = new Image[6]; var circles = new Image[6]; var numbers = new Text[6]; var stepTitles = new Text[6];
        for (int i = 0; i < 6; i++)
        {
            float yc = 233 + i * 53.8f;
            rows[i] = MakeImage(bench, "StepRow" + (i + 1), LoadSprite("RowCurrent", BenchDir));
            Place(rows[i].rectTransform, R2(40, yc - 25, 244, 50));
            circles[i] = MakeImage(bench, "StepCircle" + (i + 1), LoadSprite("StepPending", BenchDir));
            Place(circles[i].rectTransform, R2(51, yc - 14, 28, 28));
            numbers[i] = Label(circles[i].transform, "Number", (i + 1).ToString(), fontHeavy, 26, StepNumberInk, new Rect(0, 0, 56, 56), TextAnchor.MiddleCenter);
            stepTitles[i] = Label(bench, "StepTitle" + (i + 1), d.steps[i], fontHeavy, 26, BenchInk, R2(88, yc - 16, 200, 18));
            Label(bench, "StepSub" + (i + 1), d.stepSubs[i], fontBody, 23, StepSubInk, R2(88, yc + 1, 200, 16));
        }
        var message = Label(bench, "Message", "", fontHeavy, 30, BenchInk, R2(362, 503, 826, 38));
        message.horizontalOverflow = HorizontalWrapMode.Wrap;
        message.lineSpacing = 0.95f;
        Label(bench, "PartsTray", "PARTS TRAY", fontHeavy, 22, new Color32(100, 70, 46, 255), R2(1080, 233, 128, 18), TextAnchor.MiddleCenter);
        Label(bench, "Tools", "Tools", fontHand, 56, Color.white, R2(38, 615, 90, 50));
        Label(bench, "Shelf", "Shelf", fontHand, 56, Color.white, R2(592, 615, 90, 50));

        d.buildSubject(teddyCtl, bench);

        // Alet ve raf yuvalari
        string[] slotNames = d.slotSprites;
        string[] toolLabels = d.toolLabels;
        var shelfInfo = d.shelf;
        var slots = new Button[8];
        for (int i = 0; i < 8; i++)
        {
            bool isTool = i < 4;
            var r = isTool ? new Rect(220 + i * 228, 1176, 210, 200) : new Rect(1334 + (i - 4) * 290, 1176, 266, 200);
            var img = Img2(bench, slotNames[i], d.artDir, slotNames[i], r);
            img.raycastTarget = true;
            var b = slots[i] = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            img.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.04f;
            UnityEventTools.AddIntPersistentListener(b.onClick, teddyCtl.Select, i);
            if (isTool)
                Label(img.transform, "Label", toolLabels[i], fontHeavy, 26, BenchInk, new Rect(0, 134, 210, 44), TextAnchor.MiddleCenter);
            else
            {
                var info = shelfInfo[i - 4];
                var n = Label(img.transform, "Name", info.name, fontHeavy, 26, BenchInk, new Rect(80, 12, 180, 70), TextAnchor.MiddleLeft);
                n.horizontalOverflow = HorizontalWrapMode.Wrap;
                n.lineSpacing = 0.85f;
                var src = Img2(img.transform, "Source", Day2Dir, "SourcePill", new Rect(20, 92, 226, 40));
                Label(src.transform, "Text", info.source, fontBody, 20, new Color32(40, 90, 110, 255), new Rect(0, 0, 226, 40), TextAnchor.MiddleCenter);
                Label(img.transform, "Bin", info.bin, fontBody, 21, new Color32(120, 90, 62, 255), new Rect(22, 138, 230, 34));
            }
        }
        var selected = MakeImage(bench, "SlotSelected", LoadSprite("SlotSelected", BenchDir));
        Place(selected.rectTransform, new Rect(220, 1176, 222, 212));

        // Yardim: ipucu halkalari, "?" butonu ve yardim karti (radyo tezgahindakiyle ayni)
        var slotHint = Hint(bench, "SlotHint");
        var targetHint = Hint(bench, "TargetHint");
        var help = MakeImage(bench, "HelpButton", LoadSprite("HelpButton", BenchDir));
        help.raycastTarget = true;
        Place(help.rectTransform, R2(1201, 504, 38, 38));
        Label(help.transform, "Mark", "?", fontHeavy, 50, Color.white, new Rect(0, 2, 76, 76), TextAnchor.MiddleCenter);
        var helpButton = help.gameObject.AddComponent<Button>();
        helpButton.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(helpButton.onClick, teddyCtl.OpenHelp);

        var overlay = MakeImage(bench, "HelpOverlay", null);
        overlay.color = new Color(0.02f, 0.08f, 0.1f, 0.55f);
        overlay.raycastTarget = true;
        Place(overlay.rectTransform, new Rect(-400, -400, WideArt.x + 800, WideArt.y + 800));
        var card = MakeImage(overlay.transform, "HelpCard", LoadSprite("HelpCard", BenchDir));
        card.raycastTarget = true;
        Place(card.rectTransform, new Rect(400 + 1280 - 656, 400 + 700 - 386, 1312, 772));
        var cardT = card.transform;
        Label(cardT, "Title", "Need a hand?", fontHand, 96, new Color32(16, 70, 80, 255), new Rect(100, 60, 1000, 120));
        var helpStep = Label(cardT, "Step", "", fontHeavy, 44, BenchInk, new Rect(100, 180, 1110, 64));
        var frame = MakeImage(cardT, "IconFrame", LoadSprite("HelpIconFrame", BenchDir));
        Place(frame.rectTransform, new Rect(100, 272, 240, 232));
        var helpIcon = MakeImage(cardT, "Icon", null);
        helpIcon.preserveAspect = true;
        Place(helpIcon.rectTransform, new Rect(118, 290, 204, 196));
        var helpPick = Label(cardT, "Pick", "", fontBody, 42, BenchInk, new Rect(380, 290, 830, 90));
        var helpClick = Label(cardT, "Click", "", fontBody, 42, BenchInk, new Rect(380, 392, 830, 112));
        helpPick.horizontalOverflow = helpClick.horizontalOverflow = HorizontalWrapMode.Wrap;
        HelpCardButton(cardT, "ShowMe", "HelpButtonTeal", 96, "Show me", Color.white, teddyCtl.ShowMe);
        HelpCardButton(cardT, "DoItForMe", "HelpButtonOrange", 471, "Do it for me", Color.white, teddyCtl.DoItForMe);
        HelpCardButton(cardT, "GotIt", "HelpButtonOutline", 846, "Got it", BenchInk, teddyCtl.CloseHelp);
        overlay.gameObject.SetActive(false);

        // ================= Ekran gecisi =================
        var faderImg = MakeImage(canvasGo.transform, "ScreenFader", null);
        faderImg.color = Night;
        faderImg.raycastTarget = true;
        Stretch(faderImg.rectTransform);
        var fader = faderImg.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 1f;

        SetRefs(c, ("interiorPage", interior), ("benchPage", benchPage), ("screenFader", fader), ("bench", teddyCtl),
            ("subtitle", subtitle), ("healthText", health), ("coinText", coins), ("dialogue", dialogue),
            ("tellMoreButton", tellMore), ("letsFixButton", letsFix), ("dialogueHeadOutButton", headOut2),
            ("floatingText", floating), ("tasksButtonActive", tasksActive.gameObject), ("tasksPanel", panel.rectTransform),
            ("taskCount", count), ("taskBadge", badgeText), ("highlight", hl.rectTransform),
            ("checkOn", LoadSprite("CheckOn", BeachUiDir)), ("checkOff", LoadSprite("CheckOff", BeachUiDir)),
            ("afterBackground", LoadSprite($"InteriorDay{d.day}AfterBackground", d.artDir)),
            ("afterBackgroundTr", LoadSprite($"InteriorDay{d.day}AfterBackground", d.artDir + "TR/")),
            ("nextButton", next), ("welcomeButton", welcome), ("heartTemplate", heartTpl),
            ("rewardOverlay", rewardOverlay.gameObject), ("rewardCard", rewardCard.rectTransform),
            ("rewardCoinIcon", pile.rectTransform), ("coinCounterIcon", coinIcon.rectTransform), ("coinTemplate", flyingCoin),
            ("missionOverlay", missionOverlay.gameObject), ("missionCard", mission.rectTransform));
        SetArray(c, "taskChecks", checks);
        SetArray(c, "taskTitles", titles);
        SetArray(c, "taskStrikes", strikes);
        var so = new SerializedObject(c);
        SetStrings(so.FindProperty("elifLines"), d.lines);
        SetStrings(so.FindProperty("thanksLines"), d.thanks);
        so.FindProperty("seaSceneName").stringValue = d.seaScene;
        so.FindProperty("requiredItem").stringValue = d.requiredItem;
        so.FindProperty("helpFirstLine").stringValue = d.helpFirstLine;
        so.FindProperty("elifHead").vector2Value = d.customerHead;
        so.ApplyModifiedPropertiesWithoutUndo();

        SetRefs(teddyCtl, ("host", c), ("progressText", progress), ("progressFill", fill.rectTransform), ("message", message),
            ("slotSelected", selected.rectTransform), ("helpButton", help.rectTransform), ("helpOverlay", overlay.gameObject), ("helpStep", helpStep),
            ("helpPick", helpPick), ("helpClick", helpClick), ("helpIcon", helpIcon),
            ("slotHint", slotHint.rectTransform), ("targetHint", targetHint.rectTransform),
            ("rowDone", LoadSprite("RowDone", BenchDir)), ("rowCurrent", LoadSprite("RowCurrent", BenchDir)),
            ("circleDone", LoadSprite("StepDone", BenchDir)), ("circleCurrent", LoadSprite("StepCurrent", BenchDir)),
            ("circlePending", LoadSprite("StepPending", BenchDir)));
        SetArray(teddyCtl, "stepRows", rows);
        SetArray(teddyCtl, "stepCircles", circles);
        SetArray(teddyCtl, "stepNumbers", numbers);
        SetArray(teddyCtl, "stepTitles", stepTitles);
        SetArray(teddyCtl, "slots", slots);
        benchPage.gameObject.SetActive(false);

        Directory.CreateDirectory(Path.GetDirectoryName(d.scenePath));
        ArtLocalization.Apply(canvasGo.transform);
        // Ic mekan zemini dile gore sprite degistirir; tamirden sonraki zemin de onun uzerinden verilir
        SetRefs(c, ("interiorBackground", shop.Find("Background").GetComponent<LocalizedImage>()));
        EditorSceneManager.SaveScene(scene, d.scenePath);
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != d.scenePath).ToList();
        int after = scenes.FindIndex(s => s.path == ScenePath);
        scenes.Insert(after + 1, new EditorBuildSettingsScene(d.scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorPrefs.SetInt(d.VersionKey, d.version);
        Debug.Log($"[Tideworks] Day {d.day} workshop scene created: " + d.scenePath);
    }

    /// <summary>Pamuk: hepsi ayni tuvalde ust uste katmanlar, tiklanan noktalar ve kopuk kol.</summary>
    static void BuildTeddy(TeddyRepairController teddyCtl, Transform bench)
    {
        // Pamuk: hepsi ayni tuvalde ust uste katmanlar
        var teddy = new GameObject("Pamuk", typeof(RectTransform)).transform as RectTransform;
        teddy.SetParent(bench, false);
        Place(teddy, new Rect(1300, 268, 620, 700));
        Image Layer(string name) => Img2(teddy, name, Day2Dir, name, new Rect(0, 0, 620, 700));
        Layer("TeddyBody");
        var armAttached = Layer("TeddyArmAttached");
        var stitches = Layer("TeddyStitches");
        var stuffingOld = Layer("TeddyStuffingOld");
        var stuffingNew = Layer("TeddyStuffingNew");
        var eye = Layer("TeddyEye");
        var heart = Layer("TeddyHeart");
        var shoulderSpot = Spot(teddy, "ShoulderSpot", new Rect(380, 280, 130, 130), teddyCtl.ClickShoulder);
        var eyeSpot = Spot(teddy, "EyeSpot", new Rect(316, 126, 100, 100), teddyCtl.ClickEye);
        var chestSpot = Spot(teddy, "ChestSpot", new Rect(230, 390, 160, 160), teddyCtl.ClickChest);
        Vector2[] mudPos = { new Vector2(200, 250), new Vector2(360, 300), new Vector2(260, 470), new Vector2(420, 560) };
        var mud = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            mud[i] = Img2(teddy, "Mud" + i, Day2Dir, "Mud" + i, new Rect(mudPos[i].x - 55, mudPos[i].y - 55, 110, 110));
            mud[i].raycastTarget = true;
            var b = mud[i].gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            UnityEventTools.AddIntPersistentListener(b.onClick, teddyCtl.ClickMud, i);
        }
        var net = Layer("TeddyNet");
        Vector2[] knotPos = { new Vector2(250, 300), new Vector2(380, 420), new Vector2(280, 540) };
        var knots = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            knots[i] = Img2(teddy, "Knot" + i, Day2Dir, "NetKnot", new Rect(knotPos[i].x - 34, knotPos[i].y - 34, 68, 68));
            knots[i].raycastTarget = true;
            var b = knots[i].gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            knots[i].gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.15f;
            UnityEventTools.AddIntPersistentListener(b.onClick, teddyCtl.ClickKnot, i);
        }
        var armLoose = Img2(bench, "ArmLoose", Day2Dir, "TeddyArmLoose", new Rect(1975, 760, 240, 140));
        armLoose.raycastTarget = true;
        var ab = armLoose.gameObject.AddComponent<Button>();
        ab.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(ab.onClick, teddyCtl.ClickArm);
        SetRefs(teddyCtl, ("net", net), ("stuffingOld", stuffingOld), ("stuffingNew", stuffingNew),
            ("armLoose", armLoose), ("armAttached", armAttached), ("stitches", stitches), ("eye", eye), ("heart", heart),
            ("teddy", teddy), ("shoulderSpot", (RectTransform)shoulderSpot.transform),
            ("eyeSpot", (RectTransform)eyeSpot.transform), ("chestSpot", (RectTransform)chestSpot.transform));
        SetArray(teddyCtl, "knots", knots);
        SetArray(teddyCtl, "mud", mud);
    }

    static Image Img2(Transform parent, string name, string dir, string sprite, Rect r)
    {
        var img = MakeImage(parent, name, LoadSprite(sprite, dir));
        Place(img.rectTransform, r);
        return img;
    }
}
