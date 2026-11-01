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
/// Gorev 2 - Toplananlari ayir sahnesini (Assets/Scenes/Recycling.unity) kurar.
/// Denizden (Fishing) sonra gelir, bitince kumsala (Beach) gecer.
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Recycling Scene
/// </summary>
public static partial class FishingSceneBuilder
{
    const int RecyclingBuilderVersion = 3;
    const string RecyclingScenePath = "Assets/Scenes/Recycling.unity";
    const string RecyclingAutoBuildKey = "Tideworks.RecyclingAutoBuild.v3";
    static string RecyclingVersionKey => "Tideworks.RecyclingBuilderVersion." + PlayerSettings.productGUID;

    [MenuItem("Tideworks/Rebuild Recycling Scene")]
    static void RebuildRecyclingMenu()
    {
        if (File.Exists(RecyclingScenePath) && !EditorUtility.DisplayDialog("Rebuild Recycling",
                "Recycling sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
                "Rebuild", "Cancel"))
            return;
        BuildRecycling(askToSave: true);
    }

    static void BuildRecycling(bool askToSave)
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

        var canvasGo = new GameObject("RecyclingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var sorter = canvasGo.AddComponent<SortController>();
        Page(canvasGo.transform, "SortPage", "Sort", out var yard);

        Img(yard, "TopBar", UiDir, "TopBar", new Rect(0, 0, 2560, 150));
        var yMenu = Img(yard, "MenuButton", UiDir, "HudTeal", new Rect(40, 20, 110, 110));
        Img(yMenu.transform, "Icon", UiDir, "IconWaves", new Rect(23, 23, 64, 64));
        Clickable(yMenu, sorter.BackToMenu);
        Label(yard, "Brand", "Tidework", fontTitle, 56, Gold, new Rect(176, 14, 400, 70));
        var place = Label(yard, "Place", "", fontHeavy, 28, Color.white, new Rect(176, 84, 520, 40));
        var taskPill = Img(yard, "TaskPill", UiDir, "TaskPill", new Rect(1020, 27, 520, 96));
        Label(taskPill.transform, "Text", "Task 2 · Sort the catch", fontTitle, 38, Color.white, new Rect(0, 0, 520, 96), TextAnchor.MiddleCenter);
        Img(yard, "Track", UiDir, "BarTrack", new Rect(1880, 66, 300, 18));
        var yFill = Img(yard, "Fill", UiDir, "BarFill", new Rect(1880, 66, 0, 18));
        LeftPivot(yFill.rectTransform, 1880);
        var yHealth = Label(yard, "Health", "0%", fontTitle, 36, Color.white, new Rect(2190, 50, 110, 50));
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

        var faderImg = MakeImage(canvasGo.transform, "ScreenFader", null);
        faderImg.color = Night;
        faderImg.raycastTarget = true;
        Stretch(faderImg.rectTransform);
        var fader = faderImg.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 1f;
        SetRefs(sorter, ("screenFader", fader));

        SetRefs(sorter, ("itemIndex", itemIndex), ("itemName", itemName), ("question", question), ("itemImage", itemImage),
            ("rightCount", rightCount), ("wrongCount", wrongCount), ("coinsSoFar", soFar), ("coinText", yCoinText),
            ("bagTitle", bagTitle), ("floatingText", sortFloat), ("finishOverlay", finish.gameObject),
            ("finishCard", card.rectTransform), ("finishCoins", finishCoins), ("finishScore", finishScore),
            ("rowNormal", LoadSprite("BinRow", UiDir)), ("rowRight", LoadSprite("BinRowRight", UiDir)),
            ("rowWrong", LoadSprite("BinRowWrong", UiDir)), ("placeText", place), ("healthFill", yFill.rectTransform),
            ("healthText", yHealth));
        SetArray(sorter, "trashSprites", TrashCatalog.Trash.Select(t => (Object)LoadSprite(t.sprite, ArtDir)).ToArray());
        SetArray(sorter, "binRows", binRows);
        SetArray(sorter, "binCounts", binCounts);
        SetArray(sorter, "bagTiles", tiles);

        Directory.CreateDirectory(Path.GetDirectoryName(RecyclingScenePath));
        ArtLocalization.Apply(canvasGo.transform);
        EditorSceneManager.SaveScene(scene, RecyclingScenePath);
        var scenes = EditorBuildSettings.scenes.Where(sc => sc.path != RecyclingScenePath).ToList();
        int after = scenes.FindIndex(sc => sc.path == ScenePath);
        scenes.Insert(after + 1, new EditorBuildSettingsScene(RecyclingScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorPrefs.SetInt(RecyclingVersionKey, RecyclingBuilderVersion);
        Debug.Log("[Tideworks] Recycling scene created: " + RecyclingScenePath);
    }
}
