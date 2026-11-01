using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static MainMenuSceneBuilder;

/// <summary>
/// 3. gun denizi (Assets/Scenes/Day3Sea.unity): "Pusulanin Gosterdigi Koy". Ana kurulum FishingSceneBuilder.Build(day: 3);
/// bu dosya 3. gune ozel parcalari kurar (tasarimlar: 30-33.png): kayalik havuzlar ekrani, kiyisiz acik deniz, gizli koy ekrani,
/// Mert'in konusma kutusu, ust barin yerlesimi, "Grandpa's compass" gostergesi, yunus ve gun sonu karti.
/// Gorseller: Assets/Sprites/Day3Sea (Tools/MenuArt/fishing/day3_sea_art.py), Day3 (pusula), Day3Beach (Mert portresi).
/// </summary>
public static partial class FishingSceneBuilder
{
    const string Day3ScenePath = "Assets/Scenes/Day3Sea.unity";
    const string Day3SeaDir = "Assets/Sprites/Day3Sea/";
    const string Day3Dir = "Assets/Sprites/Day3/";
    const string Day3AutoBuildKey = "Tideworks.Day3SeaAutoBuild.v2";
    static string Day3VersionKey => "Tideworks.Day3SeaBuilderVersion." + PlayerSettings.productGUID;
    const int Day3SeaVersion = BuilderVersion + 1;

    static readonly string[] PoolCreatures = { "Crab", "Starfish", "Blenny", "Anemone" };
    static readonly Vector2[] PoolCenters = { new Vector2(540, 940), new Vector2(1200, 840), new Vector2(1800, 1040), new Vector2(2280, 1240) };
    // Havuz copleri (30.png'deki yerleri, artboard): sise, kutu / altili halka, terlik / sut kutusu, gazete / ip, ton kutusu
    static readonly (string sprite, Rect r)[] PoolTrashArt =
    {
        ("TrashWaterBottle", new Rect(375, 877, 170, 66)), ("TrashTinCan", new Rect(573, 940, 134, 60)),
        ("TrashSixPack", new Rect(1038, 785, 164, 70)), ("TrashFlipFlop", new Rect(1260, 798, 80, 84)),
        ("TrashMilkCarton", new Rect(1658, 960, 84, 100)), ("TrashNewspaper", new Rect(1825, 1012, 150, 76)),
        ("TrashRope", new Rect(2105, 1192, 190, 56)), ("TrashTunaCan", new Rect(2310, 1232, 120, 56)),
    };
    // Gizli koyun dibindeki copler (32.png): lastik, ag parcasi, boya kutusu, cam sise
    static readonly (string sprite, Rect r)[] HiddenArt =
    {
        ("TrashTire", new Rect(700, 740, 120, 120)), ("TrashGhostNet", new Rect(1590, 712, 218, 96)),
        ("TrashPaintCan", new Rect(1250, 1000, 100, 120)), ("TrashGlassBottle", new Rect(1924, 1090, 152, 62)),
    };

    /// <summary>3. gunun sahne parcalari (kurulum sirasinda toplanir, sonunda controller'a baglanir).</summary>
    class CoveUi
    {
        public GameObject poolsPage, covePage, coveCard, compassBadge;
        public Image coveGlow, bubblesTemplate, coveFishTemplate;
        public RectTransform scoop, mertPortrait, compassHud, compassNeedle, dolphin, mertOnBoat, coveCardRect;
        public Image[] poolTrash, poolRings, poolCreatures, hiddenItems, hiddenRings;
        public Text dialogText, cardPools, cardHidden, cardBabies, cardFish;
    }

    static void RequestDay3Sea()
    {
        if (System.IO.File.Exists(Day3ScenePath) && EditorPrefs.GetInt(Day3VersionKey, 0) >= Day3SeaVersion) return;
        SceneAutoBuild.Request(Day3AutoBuildKey, () => Build(askToSave: true, day: 3));
    }

    [MenuItem("Tideworks/Rebuild Day 3 Sea Scene")]
    static void RebuildDay3Menu()
    {
        if (System.IO.File.Exists(Day3ScenePath) && !EditorUtility.DisplayDialog("Rebuild Day 3 Sea",
                "Day3Sea sahnesi bastan kurulacak. Sahnede elle yaptiginiz degisiklikler silinir. Devam edilsin mi?",
                "Rebuild", "Cancel"))
            return;
        Build(askToSave: true, day: 3);
    }

    static Button TapTarget(Image img)
    {
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        img.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.12f;
        return b;
    }

    static Transform FullLayer(Transform parent, string name)
    {
        var rt = new GameObject(name, typeof(RectTransform)).transform as RectTransform;
        rt.SetParent(parent, false);
        Place(rt, new Rect(0, 0, Art.x, Art.y));
        return rt;
    }

    static Image Ring(Transform parent, string name, Rect item)
    {
        var ring = Img(parent, name, Day3SeaDir, "TrashRing", new Rect(item.center.x - item.width * 0.6f - 10, item.yMax - 40, item.width * 1.2f + 20, 56));
        return ring;
    }

    /// <summary>Teknedeki Mert (burun solda oldugu icin -x yonunde).</summary>
    static void BuildCoveBoat(CoveUi u, RectTransform items, Image boat)
    {
        var mert = Img(boat.transform, "Mert", Day3SeaDir, "MertBadge", new Rect(0, 0, 70, 70));
        mert.rectTransform.anchorMin = mert.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        mert.rectTransform.anchoredPosition = new Vector2(-112f, 0f);
        u.mertOnBoat = mert.rectTransform;
    }

    /// <summary>Yakin plan ekranlar (tekne ve denizin ustunde): kayalik havuzlar ve gizli koy.</summary>
    static void BuildCovePages(CoveUi u, Transform sea, FishingController c)
    {
        // Kayalik havuzlar
        var pools = FullLayer(sea, "PoolsPage");
        u.poolsPage = pools.gameObject;
        Img(pools, "Background", Day3SeaDir, "PoolsBackground", new Rect(0, 0, Art.x, Art.y));
        u.poolCreatures = new Image[4];
        for (int p = 0; p < 4; p++)
        {
            var cr = Img(pools, PoolCreatures[p], Day3SeaDir, PoolCreatures[p], new Rect(PoolCenters[p].x - 60, PoolCenters[p].y - 60, 120, 120));
            UnityEventTools.AddIntPersistentListener(TapTarget(cr).onClick, c.ClickCreature, p);
            u.poolCreatures[p] = cr;
        }
        u.poolTrash = new Image[8];
        u.poolRings = new Image[8];
        for (int i = 0; i < 8; i++)
        {
            var (sprite, r) = PoolTrashArt[i];
            u.poolRings[i] = Ring(pools, "Ring" + i, r);
            var t = Img(pools, "PoolTrash" + i, ArtDir, sprite, r);
            t.preserveAspect = true;
            UnityEventTools.AddIntPersistentListener(TapTarget(t).onClick, c.ClickPoolTrash, i);
            u.poolTrash[i] = t;
        }
        u.scoop = Img(pools, "Scoop", Day3SeaDir, "Scoop", new Rect(0, 0, 80, 146)).rectTransform;

        // Gizli koy
        var cove = FullLayer(sea, "CovePage");
        u.covePage = cove.gameObject;
        Img(cove, "Background", Day3SeaDir, "CoveBackground", new Rect(0, 0, Art.x, Art.y));
        u.coveGlow = Img(cove, "CoveGlow", UiDir, "SunGlow", new Rect(275, -245, 2000, 2000));
        u.coveGlow.color = new Color(0.55f, 1f, 0.95f, 0f);
        u.hiddenItems = new Image[4];
        u.hiddenRings = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var (sprite, r) = HiddenArt[i];
            u.hiddenRings[i] = Ring(cove, "Ring" + i, r);
            var t = Img(cove, "Hidden" + i, ArtDir, sprite, r);
            t.preserveAspect = true;
            UnityEventTools.AddIntPersistentListener(TapTarget(t).onClick, c.ClickHidden, i);
            u.hiddenItems[i] = t;
        }
        u.coveFishTemplate = Img(cove, "FishTemplate", ArtDir, "FishAnchovy", new Rect(0, 0, 150, 60));
        u.bubblesTemplate = Img(cove, "BubblesTemplate", Day3SeaDir, "Bubbles", new Rect(0, 0, 120, 120));
        var dolphin = Img(cove, "Dolphin", Day3SeaDir, "Dolphin", new Rect(0, 0, 460, 270));
        dolphin.preserveAspect = true;
        u.dolphin = dolphin.rectTransform;
    }

    /// <summary>Mert'in konusma kutusu ve sag ustte "Grandpa's compass" (K: 0 derece, D: -90 derece + rozet).</summary>
    static void BuildCoveHud(CoveUi u, Transform sea, Image tip)
    {
        var box = Img(sea, "DialogBox", Day3SeaDir, "DialogBox", new Rect(44, 1180, 1520, 224));
        var portrait = Img(box.transform, "Mert", "Assets/Sprites/Day3Beach/", "MertPortrait", new Rect(36, 26, 160, 160));
        u.mertPortrait = portrait.rectTransform;
        Label(box.transform, "Name", "Mert", fontHeavy, 34, new Color32(30, 120, 124, 255), new Rect(232, 28, 400, 44));
        u.dialogText = Label(box.transform, "Text", "", fontBody, 32, new Color32(48, 40, 34, 255), new Rect(232, 76, 1240, 116), TextAnchor.UpperLeft);
        u.dialogText.horizontalOverflow = HorizontalWrapMode.Wrap;
        u.dialogText.resizeTextForBestFit = true;
        u.dialogText.resizeTextMaxSize = 32;
        u.dialogText.resizeTextMinSize = 22;

        var hud = new GameObject("CompassHud", typeof(RectTransform)).transform as RectTransform;
        hud.SetParent(sea, false);
        Place(hud, new Rect(2276, 192, 224, 245));
        u.compassHud = hud;
        Image Layer(string name) => Img(hud, name, Day3Dir, name, new Rect(0, 0, 224, 245));
        Layer("CompassCase");
        Layer("CompassLoopNew");
        u.compassNeedle = Img(hud, "Needle", Day3Dir, "CompassNeedle", new Rect(112 - 11, 140 - 60, 22, 120)).rectTransform;
        Layer("CompassLens");
        var label = Label(sea, "CompassLabel", "GRANDPA'S COMPASS", fontHeavy, 24, Color.white, new Rect(2238, 448, 300, 70), TextAnchor.UpperCenter);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.2f, 0.24f, 0.8f);
        var badge = Img(sea, "CompassBadge", Day3SeaDir, "CompassBadge", new Rect(2420, 300, 128, 76));
        Label(badge.transform, "D", "D", fontTitle, 40, Color.white, new Rect(16, -2, 50, 76), TextAnchor.MiddleCenter);
        u.compassBadge = badge.gameObject;
    }

    /// <summary>Gun sonu karti: "Grandpa's cove is alive again!", 4 kutu, bilgi notu, Ayristir / Tekrar oyna.</summary>
    static void BuildCoveCard(CoveUi u, Transform sea, FishingController c)
    {
        var dim = MakeImage(sea, "CoveCard", null);
        dim.color = new Color(0.02f, 0.08f, 0.1f, 0.55f);
        dim.raycastTarget = true;
        Place(dim.rectTransform, new Rect(-400, -400, Art.x + 800, Art.y + 800));
        var card = Img(dim.transform, "Card", BeachDir, "DayCard", new Rect(400 + 680, 400 + 170, 1200, 1070));
        var ct = card.transform;
        Label(ct, "Kicker", "DAY 3 · THE COVE THE COMPASS SHOWED", fontHeavy, 26, new Color32(30, 120, 124, 255),
            new Rect(0, 46, 1200, 44), TextAnchor.MiddleCenter);
        var title = Label(ct, "Title", "Grandpa's cove is alive again!", fontTitle, 76, Ink, new Rect(70, 92, 1060, 190), TextAnchor.MiddleCenter);
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.resizeTextForBestFit = true;
        title.resizeTextMaxSize = 76;
        title.resizeTextMinSize = 44;
        Text Stat(string name, string sprite, float x, string label, Color labelInk, Color valueInk)
        {
            var box = Img(ct, name, BeachDir, sprite, new Rect(x, 300, 256, 142));
            var l = Label(box.transform, "Label", label, fontHeavy, 20, labelInk, new Rect(8, 16, 240, 36), TextAnchor.MiddleCenter);
            l.resizeTextForBestFit = true;
            l.resizeTextMaxSize = 20;
            l.resizeTextMinSize = 14;
            l.horizontalOverflow = HorizontalWrapMode.Wrap;
            return Label(box.transform, "Value", "", fontTitle, 46, valueInk, new Rect(0, 56, 256, 72), TextAnchor.MiddleCenter);
        }
        u.cardPools = Stat("RockPools", "StatBox", 67, "ROCK POOLS", Brown, Ink);
        u.cardHidden = Stat("HiddenTrash", "StatBox", 335, "HIDDEN TRASH", Brown, Ink);
        u.cardBabies = Stat("BabyFish", "StatBox", 603, "BABY FISH SAVED", Brown, Ink);
        u.cardFish = Stat("Fish", "StatBoxDark", 871, "FISH IN THE SEA", SoftTeal, Gold);
        var note = Img(ct, "Note", BeachDir, "RadioBox", new Rect(67, 468, 1068, 243));
        Img(note.transform, "Mert", Day3SeaDir, "MertBadge", new Rect(36, 64, 120, 120));
        var noteText = Label(note.transform, "Text",
            "Rock pools are home to tiny creatures. A single straw left there can ruin a crab's home.",
            fontBody, 34, new Color32(48, 43, 40, 255), new Rect(196, 20, 846, 203));
        noteText.horizontalOverflow = HorizontalWrapMode.Wrap;
        noteText.resizeTextForBestFit = true;
        noteText.resizeTextMaxSize = 34;
        noteText.resizeTextMinSize = 22;
        var sort = Img(ct, "SortCatch", BeachDir, "DayButton", new Rect(196, 884, 460, 122));
        var sortLabel = Label(sort.transform, "Text", "Sort the catch", fontTitle, 44, Color.white, new Rect(0, -6, 404, 122), TextAnchor.MiddleCenter);
        sortLabel.resizeTextForBestFit = true;
        sortLabel.resizeTextMaxSize = 44;
        sortLabel.resizeTextMinSize = 28;
        Img(sort.transform, "Arrow", CoinPath, "ArrowWhite", new Rect(390, 40, 48, 30));
        Clickable(sort, c.CoveSortCatch);
        var again = Img(ct, "PlayAgain", BeachDir, "MapButton", new Rect(676, 884, 320, 122));
        Label(again.transform, "Text", "Play again", fontHeavy, 36, Ink, new Rect(0, -2, 320, 122), TextAnchor.MiddleCenter);
        Clickable(again, c.Restart);
        u.coveCard = dim.gameObject;
        u.coveCardRect = card.rectTransform;
        u.coveCard.SetActive(false);
    }

    /// <summary>Ust bar 31-32.png'deki gibi: menu, Restart, altin, saglik, gorev (+ "10 fish"); diger alt / ust parcalar gizlenir.</summary>
    static void LayoutCoveHud(Transform sea, out Text taskTitle)
    {
        void At(string name, Rect r) => Place((RectTransform)sea.Find(name), r);
        At("MenuButton", new Rect(44, 36, 110, 110));
        At("RestartButton", new Rect(176, 40, 260, 100));
        At("Coins", new Rect(462, 44, 230, 100));
        At("Health", new Rect(780, 32, 480, 112));
        Place((RectTransform)sea.Find("Health/Percent"), new Rect(350, 6, 104, 46));
        At("Task", new Rect(1290, 32, 680, 114));
        var task = sea.Find("Task");
        taskTitle = task.Find("Title").GetComponent<Text>();
        taskTitle.text = "";                                   // kod yazar (asamaya gore)
        Place(taskTitle.rectTransform, new Rect(26, 10, 470, 36));
        Place((RectTransform)task.Find("Progress"), new Rect(26, 50, 470, 52));
        Place((RectTransform)task.Find("FishPill"), new Rect(496, 26, 170, 62));
        var fish = task.Find("FishPill/Count").GetComponent<Text>();
        Place(fish.rectTransform, new Rect(70, 2, 96, 62));
        fish.text = "";
        fish.resizeTextForBestFit = true;
        fish.resizeTextMaxSize = 30;
        fish.resizeTextMinSize = 18;
        foreach (var hide in new[] { "HelpButton", "ShopButton", "Tooltip", "Equipped", "Bag", "SortButton" })
            sea.Find(hide).gameObject.SetActive(false);
    }

    static void ApplyCove(CoveUi u, FishingController c, Transform sea)
    {
        LayoutCoveHud(sea, out var taskTitle);
        SetRefs(c, ("poolsPage", u.poolsPage), ("covePage", u.covePage), ("scoop", u.scoop), ("mertPortrait", u.mertPortrait),
            ("dialogText", u.dialogText), ("taskTitle", taskTitle), ("compassHud", u.compassHud), ("compassNeedle", u.compassNeedle),
            ("compassBadge", u.compassBadge), ("mertOnBoat", u.mertOnBoat), ("bubblesTemplate", u.bubblesTemplate),
            ("coveFishTemplate", u.coveFishTemplate), ("coveGlow", u.coveGlow), ("dolphin", u.dolphin),
            ("coveCard", u.coveCard), ("coveCardRect", u.coveCardRect),
            ("cardPools", u.cardPools), ("cardHidden", u.cardHidden), ("cardBabies", u.cardBabies), ("cardFish", u.cardFish));
        SetArray(c, "poolTrash", u.poolTrash);
        SetArray(c, "poolRings", u.poolRings);
        SetArray(c, "poolCreatures", u.poolCreatures);
        SetArray(c, "hiddenItems", u.hiddenItems);
        SetArray(c, "hiddenRings", u.hiddenRings);
        var so = new SerializedObject(c);
        so.FindProperty("coveMode").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
