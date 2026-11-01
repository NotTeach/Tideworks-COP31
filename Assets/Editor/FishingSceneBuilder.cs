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
/// Denize acilis sahnesini (Assets/Scenes/Fishing.unity) kodla kurar:
/// Gorev 1 - tekneyle oltayla cop ve balik toplama, Gorev 2 - copleri dogru kutulara ayirma.
/// Gorev 3 (carettalara yardim) ayri sahnedir: Assets/Scenes/Beach.unity (FishingSceneBuilder.Beach.cs).
/// Oyun gorselleri: Assets/Art/Fishing (Tools/MenuArt/fishing/cut_assets.py),
/// arayuz ve zeminler: Assets/Sprites/Fishing (Tools/MenuArt/fishing/fishing_ui.py).
/// Builder surumu degisince proje acildiginda bir kez otomatik calisir.
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Fishing Scene
/// </summary>
[InitializeOnLoad]
public static partial class FishingSceneBuilder
{
    const int BuilderVersion = 6;
    const string ScenePath = "Assets/Scenes/Fishing.unity";
    const string ArtDir = "Assets/Art/Fishing/";
    const string UiDir = "Assets/Sprites/Fishing/";
    const string BeachDir = "Assets/Sprites/Beach/";
    const string CoinPath = "Assets/Sprites/Workshop/";
    const string FontDir = "Assets/Fonts/";
    const string AutoBuildKey = "Tideworks.FishingAutoBuild.v6";
    static string VersionKey => "Tideworks.FishingBuilderVersion." + PlayerSettings.productGUID;

    static readonly Vector2 Art = new Vector2(2560, 1440);
    static readonly Color Night = new Color32(5, 24, 30, 255);
    static readonly Color Ink = new Color32(18, 62, 72, 255);
    static readonly Color Brown = new Color32(120, 90, 62, 255);
    static readonly Color SoftTeal = new Color32(150, 210, 205, 255);
    static readonly Color Gold = new Color32(244, 192, 74, 255);

    static Font fontBody, fontHeavy, fontTitle, fontHand;

    static FishingSceneBuilder()
    {
        RequestBeach();
        if (File.Exists(ScenePath) && EditorPrefs.GetInt(VersionKey, 0) >= BuilderVersion) return;
        SceneAutoBuild.Request(AutoBuildKey, () => Build(askToSave: true));
    }

    static void RequestBeach()
    {
        if (File.Exists(BeachScenePath) && EditorPrefs.GetInt(BeachVersionKey, 0) >= BeachBuilderVersion) return;
        SceneAutoBuild.Request(BeachAutoBuildKey, () => BuildBeach(askToSave: true));
    }

    [MenuItem("Tideworks/Rebuild Fishing Scene")]
    static void RebuildMenu()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild Fishing",
                "Fishing sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
                "Rebuild", "Cancel"))
            return;
        Build(askToSave: true);
    }

    static void Build(bool askToSave)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConfigureSprites(ArtDir);
        ConfigureSprites(UiDir);
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

        var canvasGo = new GameObject("FishingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var c = canvasGo.AddComponent<FishingController>();

        // ================= Gorev 1: deniz =================
        var seaPage = Page(canvasGo.transform, "SeaPage", "Sea", out var sea);
        var murk = Img(sea, "Murk", UiDir, "SeaMurk", new Rect(0, 0, Art.x, Art.y));
        var items = new GameObject("Catches", typeof(RectTransform)).transform as RectTransform;
        items.SetParent(sea, false);
        Place(items, new Rect(0, 0, Art.x, Art.y));
        var template = Img(items, "ItemTemplate", ArtDir, "TrashTinCan", new Rect(0, 0, 100, 60));
        template.raycastTarget = true;
        template.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

        var line = MakeImage(items, "Line", null);
        line.color = new Color(1f, 1f, 1f, 0.9f);
        Place(line.rectTransform, new Rect(0, 0, 10, 4));
        line.rectTransform.pivot = new Vector2(0f, 0.5f);
        var bobber = Img(items, "Bobber", UiDir, "Bobber", new Rect(0, 0, 48, 48));
        var splash = Img(items, "Splash", UiDir, "Splash", new Rect(0, 0, 200, 200));
        var waveTemplate = Img(items, "WaveTemplate", UiDir, "WaveLine", new Rect(0, 0, 150, 34));
        var wakeTemplate = Img(items, "WakeTemplate", UiDir, "Wake", new Rect(0, 0, 120, 120));
        wakeTemplate.gameObject.SetActive(false);
        var boat = Img(items, "Boat", ArtDir, "BoatTop", new Rect(1070, 664, 360, 192));
        var floating = Label(sea, "FloatingText", "", fontTitle, 52, Color.white, new Rect(0, 0, 900, 80), TextAnchor.MiddleCenter);
        floating.gameObject.AddComponent<Outline>().effectColor = new Color(0.06f, 0.2f, 0.24f, 0.9f);

        // Ust bar
        var menu = Img(sea, "MenuButton", UiDir, "HudTeal", new Rect(50, 40, 110, 110));
        Img(menu.transform, "Icon", UiDir, "IconWaves", new Rect(23, 23, 64, 64));
        Clickable(menu, c.BackToMenu);
        var restart = Img(sea, "RestartButton", UiDir, "RestartButton", new Rect(184, 45, 260, 100));
        Label(restart.transform, "Text", "Restart", fontHeavy, 38, Ink, new Rect(0, -2, 260, 100), TextAnchor.MiddleCenter);
        Clickable(restart, c.Restart);
        var help = Img(sea, "HelpButton", UiDir, "HudTeal", new Rect(458, 45, 100, 100));
        Label(help.transform, "Mark", "?", fontTitle, 60, Color.white, new Rect(0, 2, 100, 100), TextAnchor.MiddleCenter);
        Clickable(help, c.ShowHowTo);

        var coins = Img(sea, "Coins", UiDir, "HudDark220", new Rect(880, 45, 230, 100));
        Img(coins.transform, "Coin", CoinPath, "Coin", new Rect(22, 20, 60, 60));
        var coinText = Label(coins.transform, "Count", "120", fontTitle, 48, Color.white, new Rect(96, 2, 130, 100));

        var health = Img(sea, "Health", UiDir, "HudDark420", new Rect(1136, 40, 430, 112));
        Label(health.transform, "Label", "Ocean health", fontHeavy, 26, Color.white, new Rect(26, 10, 260, 40));
        var healthText = Label(health.transform, "Percent", "0%", fontTitle, 36, Color.white, new Rect(300, 6, 104, 46), TextAnchor.MiddleRight);
        Img(health.transform, "Track", UiDir, "BarTrack", new Rect(26, 66, 340, 18));
        var fill = Img(health.transform, "Fill", UiDir, "BarFill", new Rect(26, 66, 0, 18));
        LeftPivot(fill.rectTransform, 26);
        fill.type = Image.Type.Simple;

        var task = Img(sea, "Task", UiDir, "HudDark520", new Rect(1600, 36, 560, 120));
        Label(task.transform, "Title", "TASK 1 · CLEAN THE OCEAN", fontHeavy, 24, SoftTeal, new Rect(26, 12, 380, 36));
        var taskText = Label(task.transform, "Progress", "", fontTitle, 34, Color.white, new Rect(26, 50, 380, 56));
        var fishPill = Img(task.transform, "FishPill", UiDir, "HudPill", new Rect(394, 30, 150, 62));
        Img(fishPill.transform, "Fish", ArtDir, "FishAnchovy", new Rect(10, 17, 70, 27));
        var fishText = Label(fishPill.transform, "Count", "0/5", fontTitle, 32, Ink, new Rect(80, 2, 66, 62));

        var tip = Img(sea, "Tooltip", UiDir, "Tooltip", new Rect(890, 176, 780, 140));
        var tooltip = tip.gameObject.AddComponent<CanvasGroup>();
        tooltip.blocksRaycasts = false;
        var tipLine = Label(tip.transform, "Line", "", fontHeavy, 32, Ink, new Rect(32, 14, 716, 60));
        tipLine.horizontalOverflow = HorizontalWrapMode.Wrap;
        tipLine.resizeTextForBestFit = true;
        tipLine.resizeTextMaxSize = 32;
        tipLine.resizeTextMinSize = 20;
        Label(tip.transform, "Rewards", "<color=#B4472A>Trash +5</color> and a healthier ocean · <color=#17777A>Fish +10</color>",
            fontBody, 28, Brown, new Rect(32, 78, 716, 44));

        // Alt bar
        var equipped = Img(sea, "Equipped", UiDir, "EquippedCard", new Rect(40, 1280, 380, 130));
        Img(equipped.transform, "Rod", ArtDir, "Rod", new Rect(16, 10, 110, 110));
        Label(equipped.transform, "Label", "EQUIPPED", fontHeavy, 22, Brown, new Rect(140, 22, 230, 30));
        Label(equipped.transform, "Name", "Grandpa's rod", fontTitle, 38, Ink, new Rect(140, 52, 230, 56));

        var bagStrip = Img(sea, "Bag", UiDir, "BagStrip", new Rect(800, 1296, 980, 124));
        var bagIcons = new Image[9];
        for (int i = 0; i < bagIcons.Length; i++)
        {
            var slot = Img(bagStrip.transform, "Slot" + i, UiDir, "BagSlot", new Rect(20 + i * 92, 20, 84, 84));
            bagIcons[i] = Img(slot.transform, "Icon", ArtDir, "TrashTinCan", new Rect(6, 14, 72, 56));
            bagIcons[i].preserveAspect = true;
        }
        var bagCount = Label(bagStrip.transform, "Count", "0 items", fontTitle, 30, Color.white, new Rect(850, 0, 120, 124), TextAnchor.MiddleCenter);
        bagCount.horizontalOverflow = HorizontalWrapMode.Wrap;

        var sortImg = Img(sea, "SortButton", UiDir, "SortButton", new Rect(1870, 1292, 400, 116));
        Label(sortImg.transform, "Text", "Sort the catch", fontTitle, 40, new Color32(70, 48, 30, 255), new Rect(0, -6, 400, 116), TextAnchor.MiddleCenter);
        var sortButton = Clickable(sortImg, c.SortTheCatch);
        var colors = sortButton.colors;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        sortButton.colors = colors;

        // Telefon: joystick + olta butonu
        var mobile = new GameObject("MobileControls", typeof(RectTransform)).transform as RectTransform;
        mobile.SetParent(sea, false);
        Place(mobile, new Rect(0, 0, Art.x, Art.y));
        var stick = Img(mobile, "Joystick", UiDir, "JoystickBase", new Rect(70, 900, 320, 320));
        stick.raycastTarget = true;
        var knob = Img(stick.transform, "Knob", UiDir, "JoystickKnob", new Rect(90, 90, 140, 140));
        knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        knob.rectTransform.anchoredPosition = Vector2.zero;
        var joystick = stick.gameObject.AddComponent<VirtualJoystick>();
        SetRefs(joystick, ("knob", knob.rectTransform));
        var rodBtn = Img(mobile, "RodButton", UiDir, "RodButton", new Rect(2290, 1000, 230, 230));
        Img(rodBtn.transform, "Rod", ArtDir, "Rod", new Rect(35, 35, 160, 160));
        Clickable(rodBtn, c.CastButton);

        // "How to play" karti (oyun basinda acilir, "?" ile tekrar acilir)
        var howTo = MakeImage(sea, "HowToOverlay", null);
        howTo.color = new Color(0.02f, 0.08f, 0.1f, 0.6f);
        howTo.raycastTarget = true;
        Place(howTo.rectTransform, new Rect(-400, -400, Art.x + 800, Art.y + 800));
        var howCard = Img(howTo.transform, "Card", UiDir, "HowToCard", new Rect(400 + 580, 400 + 220, 1400, 1000));
        Label(howCard.transform, "Title", "How to play", fontTitle, 96, Ink, new Rect(0, 36, 1400, 120), TextAnchor.MiddleCenter);
        var desktop = Group(howCard.transform, "DesktopRows");
        var hr = HowRow(desktop, 0);
        string[] arrows = { "KeyLeft", "KeyUp", "KeyDown", "KeyRight" };
        for (int i = 0; i < 4; i++) Img(hr, arrows[i], UiDir, arrows[i], new Rect(20 + i * 88, 20, 84, 84));
        Label(hr, "Or", "/", fontTitle, 44, Brown, new Rect(372, 20, 40, 84), TextAnchor.MiddleCenter);
        string[] wasd = { "W", "A", "S", "D" };
        for (int i = 0; i < 4; i++) Key(hr, wasd[i], 416 + i * 88);
        Label(hr, "Text", "Row & steer", fontTitle, 44, Ink, new Rect(790, 12, 440, 60));
        Label(hr, "Sub", "Up/Down row · Left/Right steer", fontBody, 26, Brown, new Rect(790, 70, 440, 40));
        hr = HowRow(desktop, 1);
        Key(hr, "E", 20);
        Label(hr, "Or", "or", fontHeavy, 32, Brown, new Rect(108, 20, 60, 84), TextAnchor.MiddleCenter);
        var space = Img(hr, "Space", UiDir, "KeyCapWide", new Rect(170, 20, 230, 84));
        Label(space.transform, "Text", "Space", fontTitle, 36, Ink, new Rect(0, -6, 230, 84), TextAnchor.MiddleCenter);
        Label(hr, "Text", "Cast your rod", fontTitle, 44, Ink, new Rect(430, 12, 790, 60));
        Label(hr, "Sub", "or click on trash or a fish that is close enough", fontBody, 28, Brown, new Rect(430, 70, 790, 40));
        CommonRows(desktop);

        var mobileRows = Group(howCard.transform, "MobileRows");
        hr = HowRow(mobileRows, 0);
        var stickIcon = Img(hr, "Joystick", UiDir, "JoystickBase", new Rect(30, 6, 116, 116));
        Img(stickIcon.transform, "Knob", UiDir, "JoystickKnob", new Rect(33, 33, 50, 50));
        Label(hr, "Text", "Drag the joystick to hr", fontTitle, 44, Ink, new Rect(190, 20, 1030, 84));
        hr = HowRow(mobileRows, 1);
        var rodIcon = Img(hr, "Rod", UiDir, "RodButton", new Rect(30, 6, 116, 116));
        Img(rodIcon.transform, "Icon", ArtDir, "Rod", new Rect(18, 18, 80, 80));
        Label(hr, "Text", "Tap the rod button to cast", fontTitle, 44, Ink, new Rect(190, 12, 1030, 60));
        Label(hr, "Sub", "or tap on trash or a fish that is close enough", fontBody, 28, Brown, new Rect(190, 70, 1030, 40));
        CommonRows(mobileRows);

        var go = Img(howCard.transform, "Go", UiDir, "GoButton", new Rect(470, 826, 460, 124));
        Label(go.transform, "Text", "Let's go!", fontTitle, 54, Color.white, new Rect(0, -6, 460, 124), TextAnchor.MiddleCenter);
        Clickable(go, c.CloseHowTo);
        howTo.gameObject.SetActive(false);

        // ================= Gorev 2: ayristirma =================
        var sortPage = Page(canvasGo.transform, "SortPage", "Sort", out var yard);
        var sorter = sortPage.gameObject.AddComponent<SortController>();
        Img(yard, "TopBar", UiDir, "TopBar", new Rect(0, 0, 2560, 150));
        var yMenu = Img(yard, "MenuButton", UiDir, "HudTeal", new Rect(40, 20, 110, 110));
        Img(yMenu.transform, "Icon", UiDir, "IconWaves", new Rect(23, 23, 64, 64));
        Clickable(yMenu, c.BackToMenu);
        Label(yard, "Brand", "Tidework", fontTitle, 56, Gold, new Rect(176, 14, 400, 70));
        Label(yard, "Place", "Recycling yard · Day 1", fontHeavy, 28, Color.white, new Rect(176, 84, 520, 40));
        var taskPill = Img(yard, "TaskPill", UiDir, "TaskPill", new Rect(1020, 27, 520, 96));
        Label(taskPill.transform, "Text", "Task 2 · Sort the catch", fontTitle, 38, Color.white, new Rect(0, 0, 520, 96), TextAnchor.MiddleCenter);
        Img(yard, "Track", UiDir, "BarTrack", new Rect(1880, 66, 300, 18));
        Img(yard, "Fill", UiDir, "BarFill", new Rect(1880, 66, 300, 18));
        Label(yard, "Health", "100%", fontTitle, 36, Color.white, new Rect(2190, 50, 110, 50));
        var yCoins = Img(yard, "Coins", UiDir, "HudDark220", new Rect(2300, 25, 230, 100));
        Img(yCoins.transform, "Coin", CoinPath, "Coin", new Rect(22, 20, 60, 60));
        var yCoinText = Label(yCoins.transform, "Count", "0", fontTitle, 48, Color.white, new Rect(96, 2, 130, 100));

        var bins = Img(yard, "Bins", UiDir, "BinsPanel", new Rect(60, 190, 680, 1170));
        Label(bins.transform, "Title", "Recycling bins", fontHand, 70, Ink, new Rect(40, 16, 600, 96));
        var binRows = new Image[5];
        var binCounts = new Text[5];
        string[] binIcons = { "BinMetal", "BinGlass", "BinPaper", "BinPlastic", "BinHousehold" };
        for (int i = 0; i < 5; i++)
        {
            var row = binRows[i] = Img(bins.transform, "Bin" + i, UiDir, "BinRow", new Rect(40, 122 + i * 204, 600, 186));
            row.raycastTarget = true;
            var b = row.gameObject.AddComponent<Button>();
            var bc = b.colors;
            bc.highlightedColor = new Color(0.95f, 0.95f, 0.92f, 1f);
            bc.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            b.colors = bc;
            row.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.03f;
            UnityEventTools.AddIntPersistentListener(b.onClick, sorter.Choose, i);
            Img(row.transform, "Icon", UiDir, binIcons[i], new Rect(24, 28, 110, 130));
            var name = Label(row.transform, "Name", TrashCatalog.BinNames[i], fontTitle, 46, Ink, new Rect(150, 30, 340, 64));
            name.resizeTextForBestFit = true;
            name.resizeTextMaxSize = 46;
            name.resizeTextMinSize = 28;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            Label(row.transform, "Hint", TrashCatalog.BinHints[i], fontBody, 26, Brown, new Rect(150, 98, 360, 44));
            var count = Img(row.transform, "Count", UiDir, binIcons[i] + "Count", new Rect(504, 61, 64, 64));
            binCounts[i] = Label(count.transform, "Text", "0", fontTitle, 32, Color.white, new Rect(0, 0, 64, 64), TextAnchor.MiddleCenter);
        }

        var itemIndex = Label(yard, "ItemIndex", "", fontHeavy, 34, new Color32(30, 120, 124, 255), new Rect(780, 184, 1000, 50), TextAnchor.MiddleCenter);
        var itemName = Label(yard, "ItemName", "", fontTitle, 84, Ink, new Rect(780, 232, 1000, 110), TextAnchor.MiddleCenter);
        var question = Label(yard, "Question", "", fontBody, 36, Brown, new Rect(780, 346, 1000, 56), TextAnchor.MiddleCenter);
        Img(yard, "Plate", UiDir, "ItemPlate", new Rect(980, 420, 600, 600));
        var itemImage = Img(yard, "Item", ArtDir, "TrashWaterBottle", new Rect(1120, 640, 320, 120));
        itemImage.preserveAspect = true;

        var score = Img(yard, "Score", UiDir, "ScoreCard", new Rect(1890, 190, 610, 300));
        Label(score.transform, "Title", "SCORE", fontHeavy, 26, SoftTeal, new Rect(34, 18, 300, 40));
        var rightBox = Img(score.transform, "Right", UiDir, "ScoreRight", new Rect(34, 66, 262, 150));
        Label(rightBox.transform, "Label", "Right · +3", fontHeavy, 26, Color.white, new Rect(20, 12, 230, 40));
        var rightCount = Label(rightBox.transform, "Count", "0", fontTitle, 60, Color.white, new Rect(20, 56, 230, 80));
        var wrongBox = Img(score.transform, "Wrong", UiDir, "ScoreWrong", new Rect(314, 66, 262, 150));
        Label(wrongBox.transform, "Label", "Wrong · -2", fontHeavy, 26, Color.white, new Rect(20, 12, 230, 40));
        var wrongCount = Label(wrongBox.transform, "Count", "0", fontTitle, 60, Color.white, new Rect(20, 56, 230, 80));
        var soFar = Label(score.transform, "SoFar", "", fontTitle, 40, Gold, new Rect(34, 228, 540, 56));

        var bagPanel = Img(yard, "BagPanel", UiDir, "BagPanel", new Rect(1890, 520, 610, 790));
        var bagTitle = Label(bagPanel.transform, "Title", "", fontHeavy, 26, Brown, new Rect(30, 20, 550, 44));
        var tiles = new Image[15];
        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i] = Img(bagPanel.transform, "Tile" + i, UiDir, "BagTile", new Rect(30 + (i % 3) * 186, 78 + (i / 3) * 136, 160, 110));
            var ic = Img(tile.transform, "Icon", ArtDir, "TrashTinCan", new Rect(14, 10, 132, 90));
            ic.preserveAspect = true;
        }
        var sortFloat = Label(yard, "FloatingText", "", fontTitle, 72, Color.white, new Rect(0, 0, 300, 100), TextAnchor.MiddleCenter);

        // Sonuc karti
        var finish = MakeImage(yard, "FinishOverlay", null);
        finish.color = new Color(0.02f, 0.08f, 0.1f, 0.6f);
        finish.raycastTarget = true;
        Place(finish.rectTransform, new Rect(-400, -400, Art.x + 800, Art.y + 800));
        var card = Img(finish.transform, "Card", UiDir, "FinishCard", new Rect(400 + 730, 400 + 340, 1100, 760));
        Label(card.transform, "Title", "All sorted!", fontTitle, 100, Ink, new Rect(0, 50, 1100, 130), TextAnchor.MiddleCenter);
        var finishCoins = Label(card.transform, "Coins", "", fontTitle, 72, new Color32(200, 140, 30, 255), new Rect(0, 190, 1100, 100), TextAnchor.MiddleCenter);
        var finishScore = Label(card.transform, "Score", "", fontHeavy, 42, Brown, new Rect(0, 300, 1100, 60), TextAnchor.MiddleCenter);
        var thanks = Label(card.transform, "Thanks", "The beach is cleaner. The turtles thank you!", fontHand, 60,
            new Color32(110, 62, 40, 255), new Rect(80, 380, 940, 150), TextAnchor.MiddleCenter);
        thanks.horizontalOverflow = HorizontalWrapMode.Wrap;
        var done = Img(card.transform, "Done", UiDir, "FinishButton", new Rect(290, 580, 520, 124));
        Label(done.transform, "Text", "Continue", fontTitle, 50, Color.white, new Rect(0, -6, 520, 124), TextAnchor.MiddleCenter);
        Clickable(done, sorter.Done);
        finish.gameObject.SetActive(false);

        SetRefs(sorter, ("host", c));

        // ================= Ekran gecisi =================
        var faderImg = MakeImage(canvasGo.transform, "ScreenFader", null);
        faderImg.color = Night;
        faderImg.raycastTarget = true;
        Stretch(faderImg.rectTransform);
        var fader = faderImg.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 1f;

        SetRefs(c, ("seaPage", seaPage), ("sortPage", sortPage), ("screenFader", fader), ("sort", sorter),
            ("boat", boat.rectTransform), ("itemsParent", items), ("itemTemplate", template), ("murk", murk),
            ("line", line.rectTransform), ("bobber", bobber.rectTransform), ("splash", splash.rectTransform),
            ("floatingText", floating), ("coinText", coinText), ("healthFill", fill.rectTransform),
            ("healthText", healthText), ("taskText", taskText), ("fishText", fishText),
            ("tooltip", tooltip), ("tooltipLine", tipLine), ("bagCount", bagCount), ("sortButton", sortButton),
            ("mobileControls", mobile.gameObject), ("joystick", joystick),
            ("waveTemplate", waveTemplate), ("wakeTemplate", wakeTemplate),
            ("howTo", howTo.gameObject), ("howToCard", howCard.rectTransform),
            ("desktopRows", desktop.gameObject), ("mobileRows", mobileRows.gameObject));
        SetArray(c, "bagIcons", bagIcons);
        SetArray(c, "trashSprites", TrashCatalog.Trash.Select(t => (Object)LoadSprite(t.sprite, ArtDir)).ToArray());
        SetArray(c, "fishSprites", TrashCatalog.FishSprites.Select(f => (Object)LoadSprite(f, ArtDir)).ToArray());

        SetRefs(sorter, ("itemIndex", itemIndex), ("itemName", itemName), ("question", question), ("itemImage", itemImage),
            ("rightCount", rightCount), ("wrongCount", wrongCount), ("coinsSoFar", soFar), ("coinText", yCoinText),
            ("bagTitle", bagTitle), ("floatingText", sortFloat), ("finishOverlay", finish.gameObject),
            ("finishCard", card.rectTransform), ("finishCoins", finishCoins), ("finishScore", finishScore),
            ("rowNormal", LoadSprite("BinRow", UiDir)), ("rowRight", LoadSprite("BinRowRight", UiDir)),
            ("rowWrong", LoadSprite("BinRowWrong", UiDir)));
        SetArray(sorter, "binRows", binRows);
        SetArray(sorter, "binCounts", binCounts);
        SetArray(sorter, "bagTiles", tiles);
        sortPage.gameObject.SetActive(false);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        ArtLocalization.Apply(canvasGo.transform);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        EditorPrefs.SetInt(VersionKey, BuilderVersion);
        Debug.Log("[Tideworks] Fishing scene created: " + ScenePath);
    }

    // ---------------- Yardimcilar ----------------

    static void LoadFonts()
    {
        var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fontBody = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-Bold.ttf") ?? legacy;
        fontHeavy = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Nunito-ExtraBold.ttf") ?? legacy;
        fontTitle = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Baloo2-ExtraBold.ttf") ?? legacy;
        fontHand = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Caveat-Bold.ttf") ?? legacy;
    }

    static RectTransform Group(Transform parent, string name)
    {
        var rt = new GameObject(name, typeof(RectTransform)).transform as RectTransform;
        rt.SetParent(parent, false);
        Place(rt, new Rect(0, 0, 1400, 1000));
        return rt;
    }

    /// <summary>Kartta bir satir zemini (i = 0..3).</summary>
    static Transform HowRow(Transform group, int i) =>
        Img(group, "Row" + i, UiDir, "HowToRow", new Rect(80, 170 + i * 150, 1240, 128)).transform;

    static void Key(Transform row, string letter, float x)
    {
        var cap = Img(row, "Key" + letter, UiDir, "KeyCap", new Rect(x, 20, 84, 84));
        Label(cap.transform, "Text", letter, fontTitle, 44, Ink, new Rect(0, -6, 84, 84), TextAnchor.MiddleCenter);
    }

    /// <summary>Iki duzende de ayni: oduller ve kurallar.</summary>
    static void CommonRows(Transform group)
    {
        var row = HowRow(group, 2);
        var trash = Img(row, "Trash", ArtDir, "TrashWaterBottle", new Rect(20, 30, 139, 51));
        trash.preserveAspect = true;
        Img(row, "Fish", ArtDir, "FishAnchovy", new Rect(170, 30, 150, 57)).preserveAspect = true;
        var reward = Label(row, "Text", "<color=#B4472A>Trash +5</color> coins and a cleaner ocean · <color=#17777A>Fish +10</color> coins",
            fontHeavy, 32, Ink, new Rect(350, 14, 870, 100));
        reward.horizontalOverflow = HorizontalWrapMode.Wrap;
        row = HowRow(group, 3);
        var warn = Img(row, "Warn", UiDir, "HudTeal", new Rect(30, 14, 100, 100));
        Label(warn.transform, "Mark", "!", fontTitle, 60, Color.white, new Rect(0, 2, 100, 100), TextAnchor.MiddleCenter);
        var rules = Label(row, "Text", "Steer clear of the islands and the beach. Fish out all 13 pieces of trash, then sort them!",
            fontBody, 32, Ink, new Rect(170, 14, 1050, 100));
        rules.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        int after = scenes.FindIndex(s => s.path == "Assets/Scenes/Workshop.unity");
        scenes.Insert(after + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    /// <summary>Sayfa: bulanik dolgu (Cover) + 2560x1440 artboard (Contain) + zemin gorseli.</summary>
    static CanvasGroup Page(Transform parent, string name, string art, out Transform artboard, string dir = UiDir)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        var backdrop = MakeImage(go.transform, "Backdrop", LoadSprite(art + "Backdrop", dir));
        var fit = backdrop.gameObject.AddComponent<ArtboardFitter>();
        fit.artSize = Art;
        fit.mode = ArtboardFitter.FitMode.Cover;
        backdrop.rectTransform.sizeDelta = Art;
        var board = new GameObject("Artboard", typeof(RectTransform), typeof(ArtboardFitter)).transform as RectTransform;
        board.SetParent(go.transform, false);
        board.GetComponent<ArtboardFitter>().artSize = Art;
        board.sizeDelta = Art;
        Img(board, "Background", dir, art + "Background", new Rect(0, 0, Art.x, Art.y));
        artboard = board;
        return go.GetComponent<CanvasGroup>();
    }

    /// <summary>Kumsalda tiklanabilir nesne (lamba, sezlong, yuva...): renk tonu yok, dinleyiciyi controller ekler.</summary>
    static Button BeachButton(Transform parent, string name, Rect r)
    {
        var img = Img(parent, name, BeachDir, name.TrimEnd('0', '1', '2'), r);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        img.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.05f;
        return b;
    }

    static Image Img(Transform parent, string name, string dir, string sprite, Rect r)
    {
        var img = MakeImage(parent, name, LoadSprite(sprite, dir));
        Place(img.rectTransform, r);
        return img;
    }

    static Button Clickable(Image img, UnityAction onClick)
    {
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.fadeDuration = 0.08f;
        b.colors = colors;
        img.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.05f;
        UnityEventTools.AddPersistentListener(b.onClick, onClick);
        return b;
    }

    static Text Label(Transform parent, string name, string content, Font font, int size, Color color, Rect r,
        TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var t = MakeText(parent, name, content, size, font, color, anchor);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        Place(t.rectTransform, r);
        return t;
    }

    /// <summary>Soldan saga dolan cubuk: pivot sol kenarda.</summary>
    static void LeftPivot(RectTransform rt, float x)
    {
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
    }

    static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
