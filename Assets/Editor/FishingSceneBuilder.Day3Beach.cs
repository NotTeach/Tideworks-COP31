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
/// 3. gun kumsali, sezon finali "Buyuk Gece" (Assets/Scenes/Day3Beach.unity). 3. gunde Recycling bu sahneyi yukler.
/// Tasarim 34-41.png: gun batimi kumsali, ayakta uc yardimci, sokak lambasi, sag ustte gorev listesi, sol altta konusma kutusu,
/// "Who does what?" paneli, gece, yuvalar, Umut, defter ve sezon sonu karti. Gorseller: Assets/Sprites/Day3Beach (day3_night_art.py).
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Day 3 Beach Scene
/// </summary>
public static partial class FishingSceneBuilder
{
    const int Day3BeachBuilderVersion = 4;
    const string Day3BeachScenePath = "Assets/Scenes/Day3Beach.unity";
    const string Day3BeachDir = "Assets/Sprites/Day3Beach/";
    const string Day3BeachAutoBuildKey = "Tideworks.Day3BeachAutoBuild.v4";
    static string Day3BeachVersionKey => "Tideworks.Day3BeachBuilderVersion." + PlayerSettings.productGUID;

    static void RequestDay3Beach()
    {
        if (File.Exists(Day3BeachScenePath) && EditorPrefs.GetInt(Day3BeachVersionKey, 0) >= Day3BeachBuilderVersion) return;
        SceneAutoBuild.Request(Day3BeachAutoBuildKey, () => BuildDay3Beach(askToSave: true));
    }

    [MenuItem("Tideworks/Rebuild Day 3 Beach Scene")]
    static void RebuildDay3BeachMenu()
    {
        if (File.Exists(Day3BeachScenePath) && !EditorUtility.DisplayDialog("Rebuild Day 3 Beach",
                "Day3Beach sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
                "Rebuild", "Cancel"))
            return;
        BuildDay3Beach(askToSave: true);
    }

    static void BuildDay3Beach(bool askToSave)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConfigureSprites(UiDir);
        ConfigureSprites(BeachDir);
        ConfigureSprites(Day2BeachDir);
        ConfigureSprites(Day3BeachDir);
        ConfigureSprites(ShopDir);
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

        var canvasGo = new GameObject("Day3BeachCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var ctl = canvasGo.AddComponent<Day3BeachController>();
        // Tasarim 34-41.png 1280x720; artboard 2560x1440 (her olcu 2 kat)
        Page(canvasGo.transform, "Day3BeachPage", "Day3Beach", out var beach, Day3BeachDir);
        var dark = new Color32(14, 62, 72, 255);
        var cream = new Color32(251, 248, 241, 255);
        var text = new Color32(50, 40, 34, 255);
        var rust = new Color32(180, 72, 40, 255);

        // Gece: ayni sahnenin gece hali ustune yavasca gelir
        var night = Img(beach, "Night", Day3BeachDir, "NightBackground", new Rect(0, 0, Art.x, Art.y));
        night.color = new Color(1f, 1f, 1f, 0f);
        var moon = Img(beach, "Moon", Day2BeachDir, "Moon", new Rect(860, 20, 230, 230)).rectTransform;
        Vector2[] starAt = { new Vector2(760, 70), new Vector2(1120, 40), new Vector2(1480, 90), new Vector2(1700, 190), new Vector2(980, 250),
                             new Vector2(420, 220), new Vector2(240, 250), new Vector2(1250, 150), new Vector2(1420, 230), new Vector2(620, 160) };
        var stars = starAt.Select((p, i) => Img(beach, "Star" + i, CoinPath, "Sparkle", new Rect(p.x - 22, p.y - 22, 44, 44))).ToArray();

        // Kafe isik dizisi: Hatice Teyze radyodan seslenince tek tek soner
        var lightString = Img(beach, "CafeLights", Day3BeachDir, "LightString", new Rect(0, 660, 740, 200));
        var cafeBulbs = new Image[8];
        var cafeGlows = new Image[8];
        for (int i = 0; i < 8; i++)
        {
            float t = (i + 1) / 9f;
            float x = 30 + 670 * t, y = 690 + 240 * t * (1 - t);
            cafeGlows[i] = Img(beach, "CafeGlow" + i, BeachDir, "LampGlow", new Rect(x - 60, y - 38, 120, 120));
            cafeGlows[i].color = new Color(1f, 0.78f, 0.4f, 0.65f);
            cafeBulbs[i] = Img(beach, "CafeBulb" + i, Day3BeachDir, "Bulb", new Rect(x - 18, y - 4, 36, 48));
        }

        // Sokak lambasi ve amber isik
        var lampGlow = Img(beach, "LampGlow", BeachDir, "LampGlow", new Rect(1374, 232, 520, 520));
        lampGlow.color = new Color(1f, 0.68f, 0.26f, 0f);
        Img(beach, "StreetLamp", Day3BeachDir, "StreetLamp", new Rect(1540, 448, 128, 388));
        var bulb = Img(beach, "LampBulb", BeachDir, "LampGlow", new Rect(1600, 456, 68, 68));
        bulb.color = new Color(1f, 0.78f, 0.36f, 0f);
        var lampHint = Img(beach, "LampHint", ShopDir, "ItemAmberLamp", new Rect(1690, 330, 130, 130));
        var pulse = lampHint.gameObject.AddComponent<MenuPulse>();
        pulse.minAlpha = 0.6f;
        pulse.maxAlpha = 1f;
        pulse.speed = 3f;
        pulse.scaleAmount = 0.08f;
        Clickable(lampHint, ctl.PlaceLamp);
        var lampSpot = MakeImage(beach, "LampSpot", null);       // lambanin basina dokunmak da yeter
        lampSpot.color = new Color(1f, 1f, 1f, 0f);
        Place(lampSpot.rectTransform, new Rect(1560, 430, 140, 120));
        lampSpot.raycastTarget = true;
        UnityEventTools.AddPersistentListener(lampSpot.gameObject.AddComponent<Button>().onClick, ctl.PlaceLamp);

        // Bayrakli yuvalar (gece rengi koyulasir) ve yavrular
        Vector2[] nestAt = { new Vector2(376, 868), new Vector2(796, 948), new Vector2(1176, 848) };
        var nests = nestAt.Select((p, i) => Img(beach, "Nest" + i, Day3BeachDir, "NestFlag", new Rect(p.x, p.y, 288, 144))).ToArray();
        // Elif'in dolduracagi cukurlar ve Mert'in toplayacagi kucuk copler (gece belirir)
        Vector2[] holeAt = { new Vector2(560, 830), new Vector2(930, 700), new Vector2(1260, 720) };
        var holes = holeAt.Select((p, i) => Img(beach, "Hole" + i, Day3BeachDir, "SandHole", new Rect(p.x - 80, p.y - 35, 160, 70))).ToArray();
        var mertTrash = new[]
        {
            Img(beach, "MertTrash0", ArtDir, "TrashBottleCap", new Rect(1734, 676, 52, 52)),
            Img(beach, "MertTrash1", ArtDir, "TrashStraw", new Rect(1560, 700, 100, 32)),
        };
        foreach (var m in mertTrash) m.preserveAspect = true;
        RectTransform Layer(string name)
        {
            var layer = new GameObject(name, typeof(RectTransform)).transform as RectTransform;
            layer.SetParent(beach, false);
            Place(layer, new Rect(0, 0, Art.x, Art.y));
            return layer;
        }
        var trackLayer = Layer("Tracks");
        var track = Img(trackLayer, "TrackTemplate", Day3BeachDir, "Track", new Rect(0, 0, 14, 10));
        var hatchLayer = Layer("Hatchlings");
        var hatchling = Img(hatchLayer, "HatchlingTemplate", Day2BeachDir, "CarettaBaby", new Rect(0, 0, 42, 40));
        var splash = Img(hatchLayer, "SplashTemplate", UiDir, "Splash", new Rect(0, 0, 90, 90));
        var sparkle = Img(hatchLayer, "SparkleTemplate", CoinPath, "Sparkle", new Rect(0, 0, 64, 64));
        var flipRing = Img(hatchLayer, "RingTemplate", Day3BeachDir, "PickRing", new Rect(0, 0, 110, 110));

        // Kiyiya vuran 2 poset ve 1 ag parcasi (sari halkalariyla)
        string[] shoreSprites = { "TrashPlasticBag", "TrashPlasticBag", "TrashGhostNet" };
        Vector2[] shoreAt = { new Vector2(760, 780), new Vector2(1120, 840), new Vector2(1440, 784) };
        var shore = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var ring = Img(beach, "ShoreTrash" + i, Day3BeachDir, "PickRing", new Rect(shoreAt[i].x - 100, shoreAt[i].y - 100, 200, 200));
            var item = Img(ring.transform, "Item", ArtDir, shoreSprites[i], new Rect(30, 40, 140, 120));
            item.preserveAspect = true;
            ring.raycastTarget = true;
            ring.gameObject.AddComponent<CanvasGroup>();
            shore[i] = ring.gameObject.AddComponent<Button>();
            shore[i].transition = Selectable.Transition.None;
            ring.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.1f;
        }

        // Umut dalgalarin arasinda (adi ustunde)
        var umut = Img(beach, "Umut", Day3BeachDir, "CarettaUmut", new Rect(1150, 310, 330, 310)).rectTransform;
        var umutLabel = Img(beach, "UmutLabel", Day3BeachDir, "NamePill", new Rect(1240, 266, 150, 54));
        var umutName = Label(umutLabel.transform, "Name", "", fontTitle, 28, text, new Rect(0, -2, 150, 54), TextAnchor.MiddleCenter);

        // Yardimcilar: Hatice Teyze (radyo), Elif (Pamuk), Mert (pusula ve kepce); basinin ustunde gorev etiketi
        string[] who = { "Hatice", "Elif", "Mert" };
        Rect[] bodies = { new Rect(1756, 716, 224, 510), new Rect(2016, 796, 196, 430), new Rect(2236, 796, 172, 432) };
        Rect[] pillAt = { new Rect(1748, 640, 240, 50), new Rect(1994, 732, 236, 50), new Rect(2206, 672, 236, 50) };
        var helpers = new RectTransform[3];
        var pills = new GameObject[3];
        var pillTexts = new Text[3];
        var pillTicks = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var b = bodies[i];
            Img(beach, who[i] + "Shadow", Day3BeachDir, "CharShadow", new Rect(b.x + b.width / 2 - 110, b.yMax - 26, 220, 40));
            var body = Img(beach, who[i], Day3BeachDir, who[i], b);
            body.raycastTarget = true;
            var btn = body.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            body.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.03f;
            UnityEventTools.AddIntPersistentListener(btn.onClick, ctl.ClickHelper, i);
            helpers[i] = body.rectTransform;
        }
        for (int i = 0; i < 3; i++)
        {
            var pill = Img(beach, who[i] + "Pill", Day3BeachDir, "HelperPill", pillAt[i]);
            pillTicks[i] = Img(pill.transform, "Tick", BeachDir, "CheckOn", new Rect(12, 9, 32, 32));
            pillTexts[i] = Label(pill.transform, "Text", "", fontHeavy, 24, Color.white, new Rect(46, 0, pillAt[i].width - 58, 50), TextAnchor.MiddleCenter);
            pillTexts[i].resizeTextForBestFit = true;
            pillTexts[i].resizeTextMaxSize = 24;
            pillTexts[i].resizeTextMinSize = 14;
            pills[i] = pill.gameObject;
        }

        // Ust bar: menu, Restart, altin
        var menu = Img(beach, "MenuButton", UiDir, "HudTeal", new Rect(44, 36, 124, 124));
        Img(menu.transform, "Icon", UiDir, "IconWaves", new Rect(28, 28, 68, 68));
        Clickable(menu, ctl.BackToMenu);
        var restart = Img(beach, "RestartButton", UiDir, "RestartButton", new Rect(186, 42, 256, 112));
        Label(restart.transform, "Text", "Restart", fontHeavy, 38, Ink, new Rect(0, -2, 256, 112), TextAnchor.MiddleCenter);
        Clickable(restart, ctl.Restart);
        var coins = Img(beach, "Coins", UiDir, "HudDark220", new Rect(472, 46, 232, 100));
        Img(coins.transform, "Coin", CoinPath, "Coin", new Rect(22, 20, 60, 60));
        var coinText = Label(coins.transform, "Count", "0", fontTitle, 48, Color.white, new Rect(96, 2, 130, 100));

        // Gorev listesi (sag ust)
        var panelT = Img(beach, "TaskPanel", Day3BeachDir, "TaskPanel", new Rect(1856, 36, 660, 346));
        Label(panelT.transform, "Title", "DAY 3 · THE BIG NIGHT", fontHeavy, 24, SoftTeal, new Rect(32, 14, 600, 46));
        string[] taskNames = { "Give each helper a job", "Light for people, not for turtles", "Clear the hatchlings' path", "30 hatchlings to the sea" };
        var taskChecks = new Image[4];
        var taskLabels = new Text[4];
        var taskCounts = new Text[4];
        var strikes = new Image[4];
        var ticks = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            float yc = 88 + i * 67;
            taskChecks[i] = Img(panelT.transform, "Check" + i, Day3BeachDir, "CheckBox", new Rect(30, yc - 26, 52, 52));
            taskLabels[i] = Label(panelT.transform, "Task" + i, taskNames[i], fontHeavy, 30, Color.white, new Rect(100, yc - 26, 440, 52));
            taskLabels[i].resizeTextForBestFit = true;
            taskLabels[i].resizeTextMaxSize = 30;
            taskLabels[i].resizeTextMinSize = 20;
            taskLabels[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            strikes[i] = MakeImage(panelT.transform, "Strike" + i, null);
            strikes[i].color = new Color(1f, 1f, 1f, 0.7f);
            Place(strikes[i].rectTransform, new Rect(100, yc - 1, 440, 4));
            taskCounts[i] = Label(panelT.transform, "Count" + i, "", fontHeavy, 28, Gold, new Rect(540, yc - 26, 92, 52), TextAnchor.MiddleRight);
            ticks[i] = Img(panelT.transform, "Tick" + i, BeachDir, "CheckOn", new Rect(596, yc - 18, 36, 36));
            ticks[i].color = Gold;
        }

        // Konusma kutusu (sol alt): portre, isim, yazi, buton
        var box = Img(beach, "DialogBox", Day3BeachDir, "DialogBox", new Rect(44, 1132, 1520, 276));
        var portrait = MakeImage(box.transform, "Portrait", LoadSprite("HaticePortrait", BeachDir));
        Place(portrait.rectTransform, new Rect(44, 56, 152, 152));
        var speakerName = Label(box.transform, "Name", "", fontTitle, 32, new Color32(52, 110, 40, 255), new Rect(230, 30, 900, 50));
        var say = Label(box.transform, "Text", "", fontBody, 34, text, new Rect(230, 80, 1240, 160), TextAnchor.UpperLeft);
        say.horizontalOverflow = HorizontalWrapMode.Wrap;
        say.verticalOverflow = VerticalWrapMode.Truncate;
        say.resizeTextForBestFit = true;
        say.resizeTextMaxSize = 34;
        say.resizeTextMinSize = 22;
        say.lineSpacing = 1.05f;
        var sayBtn = Img(box.transform, "Button", Day3BeachDir, "DialogButton", new Rect(1252, 80, 224, 108));
        var sayBtnText = Label(sayBtn.transform, "Text", "", fontTitle, 36, Color.white, new Rect(12, -6, 200, 108), TextAnchor.MiddleCenter);
        sayBtnText.resizeTextForBestFit = true;
        sayBtnText.resizeTextMaxSize = 36;
        sayBtnText.resizeTextMinSize = 22;
        Clickable(sayBtn, ctl.DialogButton);

        // "Who does what?": is secilir, sonra yardimciya dokunulur (ya da suruklenir)
        var panel = Img(beach, "JobsPanel", Day3BeachDir, "JobsPanel", new Rect(44, 198, 920, 502));
        Label(panel.transform, "Title", "Who does what?", fontHand, 64, dark, new Rect(40, 26, 840, 80));
        var hint = Label(panel.transform, "Hint", "Pick a job, then tap the helper who fits it best.", fontBody, 27, Brown, new Rect(42, 102, 840, 44));
        hint.horizontalOverflow = HorizontalWrapMode.Wrap;
        hint.resizeTextForBestFit = true;
        hint.resizeTextMaxSize = 27;
        hint.resizeTextMinSize = 18;
        string[] jobTitles = { "Pick up the last trash on the shore", "Remind the cafés to switch off lights", "Fill the holes on the hatchlings' path" };
        string[] tags = { "net", "radio", "sand" };
        var chips = new DragChip[3];
        var chipImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var chip = Img(panel.transform, "Job" + i, Day3BeachDir, "JobChip", new Rect(46, 162 + i * 108, 830, 90));
            chip.raycastTarget = true;
            var l = Label(chip.transform, "Text", jobTitles[i], fontHeavy, 28, text, new Rect(30, 0, 650, 90));
            l.horizontalOverflow = HorizontalWrapMode.Wrap;
            l.resizeTextForBestFit = true;
            l.resizeTextMaxSize = 28;
            l.resizeTextMinSize = 18;
            var tag = Img(chip.transform, "Tag", Day3BeachDir, "TagPill", new Rect(700, 25, 100, 40));
            var tl = Label(tag.transform, "Text", tags[i], fontHeavy, 22, Color.white, new Rect(0, -1, 100, 40), TextAnchor.MiddleCenter);
            tl.resizeTextForBestFit = true;
            tl.resizeTextMaxSize = 22;
            tl.resizeTextMinSize = 14;
            chip.gameObject.AddComponent<CanvasGroup>();
            chips[i] = chip.gameObject.AddComponent<DragChip>();
            chipImages[i] = chip;
        }

        // Dedenin defteri
        var nbDim = MakeImage(beach, "NotebookOverlay", null);
        nbDim.color = new Color(0.02f, 0.06f, 0.12f, 0.55f);
        nbDim.raycastTarget = true;
        Place(nbDim.rectTransform, new Rect(-400, -400, Art.x + 800, Art.y + 800));
        var nb = Img(nbDim.transform, "Notebook", Day3BeachDir, "NotebookCard", new Rect(400 + 656, 400 + 342, 1214, 752));
        var brownInk = new Color32(110, 76, 50, 255);
        Label(nb.transform, "Place", "Kaş, the last night of the season", fontHand, 34, brownInk, new Rect(70, 70, 1000, 48));
        var note = Label(nb.transform, "Note", "", fontHand, 54, new Color32(48, 36, 30, 255), new Rect(70, 122, 1070, 330), TextAnchor.UpperLeft);
        note.horizontalOverflow = HorizontalWrapMode.Wrap;
        note.lineSpacing = 1.02f;
        Label(nb.transform, "Signature", "— your grandchild", fontHand, 42, rust, new Rect(560, 458, 580, 56), TextAnchor.MiddleRight);
        var nbBtn = Img(nb.transform, "Close", BeachDir, "DayButton", new Rect(375, 548, 464, 112));
        var nbBtnText = Label(nbBtn.transform, "Text", "Close the notebook", fontTitle, 38, Color.white, new Rect(16, -8, 432, 112), TextAnchor.MiddleCenter);
        nbBtnText.resizeTextForBestFit = true;
        nbBtnText.resizeTextMaxSize = 38;
        nbBtnText.resizeTextMinSize = 24;
        Clickable(nbBtn, ctl.CloseNotebook);

        // Sezon sonu karti
        var sDim = MakeImage(beach, "SeasonOverlay", null);
        sDim.color = new Color(0.02f, 0.06f, 0.12f, 0.55f);
        sDim.raycastTarget = true;
        Place(sDim.rectTransform, new Rect(-400, -400, Art.x + 800, Art.y + 800));
        var card = Img(sDim.transform, "Card", Day3BeachDir, "SeasonCard", new Rect(400 + 645, 400 + 309, 1270, 822));
        var ct = card.transform;
        Label(ct, "Kicker", "SEASON COMPLETE", fontHeavy, 26, new Color32(30, 120, 124, 255), new Rect(0, 50, 1264, 44), TextAnchor.MiddleCenter);
        var title = Label(ct, "Title", "30 hatchlings reached the sea!", fontTitle, 70, dark, new Rect(40, 92, 1184, 100), TextAnchor.MiddleCenter);
        title.resizeTextForBestFit = true;
        title.resizeTextMaxSize = 70;
        title.resizeTextMinSize = 44;
        string[] statLabels = { "REPAIRED", "FISH IN THE SEA", "TURTLES", "COINS" };
        var values = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            bool coinBox = i == 3;
            var sb = Img(ct, "Stat" + i, BeachDir, coinBox ? "StatBoxDark" : "StatBox", new Rect(62 + (i % 2) * 580, 218 + (i / 2) * 156, 558, 136));
            Label(sb.transform, "Label", statLabels[i], fontHeavy, 22, coinBox ? SoftTeal : Brown, new Rect(28, 14, 500, 40));
            values[i] = Label(sb.transform, "Value", "", fontTitle, 38, coinBox ? Gold : (Color)text, new Rect(28, 54, 510, 66));
            values[i].resizeTextForBestFit = true;
            values[i].resizeTextMaxSize = 38;
            values[i].resizeTextMinSize = 24;
        }
        var info = Label(ct, "Info", "A caretta hatchling may return to the beach where it was born 25 to 30 years later to lay its own eggs. " +
            "The beach you protected today is their future.", fontBody, 28, text, new Rect(90, 528, 1084, 110), TextAnchor.MiddleCenter);
        info.horizontalOverflow = HorizontalWrapMode.Wrap;
        info.resizeTextForBestFit = true;
        info.resizeTextMaxSize = 28;
        info.resizeTextMinSize = 20;
        var finish = Img(ct, "Continue", BeachDir, "DayButton", new Rect(468, 660, 334, 112));
        Label(finish.transform, "Text", "Continue", fontTitle, 44, Color.white, new Rect(0, -8, 334, 112), TextAnchor.MiddleCenter);
        Clickable(finish, ctl.FinishSeason);

        // Kapanis sayfasi: "The story isn't over..." (tasarimdaki yazilar silindi, burada cevrilebilir metin olarak)
        var epiloguePage = Page(canvasGo.transform, "EpiloguePage", "Epilogue", out var epi, Day3BeachDir);
        Rect E(float x, float y, float w, float h) => new Rect(x * 2, y * 2, w * 2, h * 2);    // 1280x720 tasarim -> artboard
        var ink = new Color32(60, 44, 40, 255);
        Label(epi, "Kicker", "KA\u015e IS SAVED", fontHeavy, 26, rust, E(175, 100, 300, 20));
        var eTitle = Label(epi, "Title", "The story isn't over...", fontTitle, 84, dark, E(175, 120, 410, 98), TextAnchor.UpperLeft);
        eTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        eTitle.lineSpacing = 0.8f;
        var eBody = Label(epi, "Body", "The beach is safe, the fish are back and the hatchlings are at sea. The workshop stays open, " +
            "because there's so much more to mend.", fontBody, 30, ink, E(175, 232, 405, 82), TextAnchor.UpperLeft);
        eBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        eBody.lineSpacing = 1.1f;
        eBody.resizeTextForBestFit = true;
        eBody.resizeTextMaxSize = 30;
        eBody.resizeTextMinSize = 22;
        Label(epi, "YouCan", "You can do it too:", fontHand, 54, rust, E(178, 336, 300, 36));
        (string title, string sub)[] tips =
        {
            ("Repair before you throw away", "Fix a broken toy or tool with someone older."),
            ("Leave nothing on the beach", "And pick up one piece you find."),
            ("Keep nights dark for the turtles", "No flashlights near the nests."),
        };
        for (int i = 0; i < tips.Length; i++)
        {
            float y = 388 + i * 74;
            var t = Label(epi, "Tip" + i, tips[i].title, fontTitle, 34, dark, E(247, y - 2, 345, 26));
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = 34;
            t.resizeTextMinSize = 22;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var s = Label(epi, "TipSub" + i, tips[i].sub, fontBody, 24, Brown, E(247, y + 22, 345, 20));
            s.resizeTextForBestFit = true;
            s.resizeTextMaxSize = 24;
            s.resizeTextMinSize = 16;
            s.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
        Image Spot(string name, Rect r)
        {
            var img = MakeImage(epi, name, null);
            img.color = new Color(1f, 1f, 1f, 0f);
            Place(img.rectTransform, r);
            return img;
        }
        var toMenu = Spot("MainMenuButton", E(175, 592, 155, 54));
        var menuLabel = Label(toMenu.transform, "Text", "Main menu", fontTitle, 38, Color.white, new Rect(0, -6, 310, 108), TextAnchor.MiddleCenter);
        menuLabel.resizeTextForBestFit = true;
        menuLabel.resizeTextMaxSize = 38;
        menuLabel.resizeTextMinSize = 24;
        Clickable(toMenu, ctl.EpilogueMenu);
        var again = Spot("PlayAgainButton", E(343, 592, 220, 53));
        var againLabel = Label(again.transform, "Text", "Play the season again", fontHeavy, 32, ink, new Rect(0, -2, 440, 106), TextAnchor.MiddleCenter);
        againLabel.resizeTextForBestFit = true;
        againLabel.resizeTextMaxSize = 32;
        againLabel.resizeTextMinSize = 20;
        Clickable(again, ctl.PlaySeasonAgain);
        Label(epi, "PageTitle", "Grandpa's notebook · the last page", fontHand, 42, new Color32(120, 80, 50, 255), E(680, 99, 300, 28));
        var quote = Label(epi, "Quote", "\u201cYou found a map on the last page of the notebook... Grandpa Yusuf had another workshop.\u201d",
            fontHand, 50, ink, E(684, 458, 426, 108), TextAnchor.MiddleCenter);
        quote.horizontalOverflow = HorizontalWrapMode.Wrap;
        quote.resizeTextForBestFit = true;
        quote.resizeTextMaxSize = 50;
        quote.resizeTextMinSize = 32;
        var coast = Label(epi, "NewCoast", "NEW COAST UNLOCKING", fontHeavy, 24, SoftTeal, E(738, 583, 120, 40));
        coast.horizontalOverflow = HorizontalWrapMode.Wrap;
        coast.lineSpacing = 0.95f;
        var grows = Label(epi, "MapGrows", "The map grows", fontTitle, 46, Gold, E(918, 572, 130, 62));
        grows.horizontalOverflow = HorizontalWrapMode.Wrap;
        grows.lineSpacing = 0.8f;
        epiloguePage.gameObject.SetActive(false);

        var faderImg = MakeImage(canvasGo.transform, "ScreenFader", null);
        faderImg.color = Night;
        faderImg.raycastTarget = true;
        Stretch(faderImg.rectTransform);
        var fader = faderImg.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 1f;

        SetRefs(ctl, ("screenFader", fader), ("dialogPortrait", portrait), ("dialogName", speakerName), ("dialogText", say),
            ("dialogButton", sayBtn), ("dialogButtonText", sayBtnText),
            ("buttonNarrow", LoadSprite("DialogButton", Day3BeachDir)), ("buttonWide", LoadSprite("DialogButtonWide", Day3BeachDir)),
            ("coinText", coinText), ("checkOn", LoadSprite("CheckBoxOn", Day3BeachDir)),
            ("jobsPanel", panel.gameObject), ("chipNormal", LoadSprite("JobChip", Day3BeachDir)),
            ("chipSelected", LoadSprite("JobChipSelected", Day3BeachDir)),
            ("lampHint", lampHint.gameObject), ("lampGlow", lampGlow), ("lampBulb", bulb),
            ("night", night), ("hatchLayer", hatchLayer), ("hatchlingTemplate", hatchling),
            ("umut", umut), ("umutLabel", umutLabel.gameObject), ("umutName", umutName),
            ("notebook", nbDim.gameObject), ("notebookCard", nb.rectTransform), ("notebookText", note),
            ("notebookButton", nbBtn.gameObject), ("seasonCard", sDim.gameObject), ("seasonCardRect", card.rectTransform),
            ("statRepaired", values[0]), ("statFish", values[1]), ("statTurtles", values[2]), ("statCoins", values[3]),
            ("epiloguePage", epiloguePage));
        SetArray(ctl, "portraits", new Object[] { LoadSprite("HaticePortrait", BeachDir), LoadSprite("ElifPortrait", Day3BeachDir), LoadSprite("MertPortrait", Day3BeachDir) });
        SetArray(ctl, "taskChecks", taskChecks);
        SetArray(ctl, "taskLabels", taskLabels);
        SetArray(ctl, "taskCounts", taskCounts);
        SetArray(ctl, "taskStrikes", strikes);
        SetArray(ctl, "taskTicks", ticks);
        SetArray(ctl, "chips", chips);
        SetArray(ctl, "chipImages", chipImages);
        SetArray(ctl, "helpers", helpers);
        SetArray(ctl, "helperPills", pills);
        SetArray(ctl, "helperPillTexts", pillTexts);
        SetArray(ctl, "helperPillTicks", pillTicks);
        SetArray(ctl, "nests", nests);
        SetArray(ctl, "shoreTrash", shore);
        SetRefs(ctl, ("lightString", lightString), ("moon", moon), ("holeFilled", LoadSprite("SandHoleFilled", Day3BeachDir)),
            ("trackLayer", trackLayer), ("trackTemplate", track), ("splashTemplate", splash), ("sparkleTemplate", sparkle),
            ("ringTemplate", flipRing));
        SetArray(ctl, "cafeBulbs", cafeBulbs);
        SetArray(ctl, "cafeGlows", cafeGlows);
        SetArray(ctl, "stars", stars);
        SetArray(ctl, "holes", holes);
        SetArray(ctl, "mertTrash", mertTrash);

        Directory.CreateDirectory(Path.GetDirectoryName(Day3BeachScenePath));
        ArtLocalization.Apply(canvasGo.transform);
        EditorSceneManager.SaveScene(scene, Day3BeachScenePath);
        var scenes = EditorBuildSettings.scenes.Where(sc => sc.path != Day3BeachScenePath).ToList();
        int after = scenes.FindIndex(sc => sc.path == Day2BeachScenePath);
        scenes.Insert(after + 1, new EditorBuildSettingsScene(Day3BeachScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorPrefs.SetInt(Day3BeachVersionKey, Day3BeachBuilderVersion);
        Debug.Log("[Tideworks] Day 3 beach scene created: " + Day3BeachScenePath);
    }
}
