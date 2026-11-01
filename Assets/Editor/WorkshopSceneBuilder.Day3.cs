using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static MainMenuSceneBuilder;

/// <summary>
/// 3. gun atolye sahnesi (Assets/Scenes/Day3Workshop.unity): Mert ve dedesinin kirik pusulasi.
/// Ortak kurulum: WorkshopSceneBuilder.Day2.cs (BuildWorkshopDay). Bu dosya 3. gunun verilerini ve pusulayi kurar.
/// Gorseller: Assets/Sprites/Day3 (Tools/MenuArt/workshop/day3_art.py, day3_bench_art.py).
/// Tekrar kurmak icin: Unity menusu > Tideworks > Rebuild Day 3 Workshop Scene
/// </summary>
public static partial class WorkshopSceneBuilder
{
    const string Day3ScenePath = "Assets/Scenes/Day3Workshop.unity";
    const string Day3Dir = "Assets/Sprites/Day3/";

    static readonly string[] MertLines =
    {
        "Hi, I'm Mert! I was hunting for crabs in the rock pools and... I slipped. " +
        "Grandpa's compass hit the rocks. The glass shattered and the loop snapped.",
        "The needle won't stop spinning. Grandpa always says, ‘A sailor without a compass gets lost at sea.’ " +
        "Can you fix it before he notices?",
    };

    // Pusula onarilinca (her biri ayri diyalog kutusu, "Next" ile; sonuncusundan sonra "You're welcome!")
    static readonly string[] MertThanks =
    {
        "It points north again! K for kuzey... see? It never lies!",
        "Wait... this lens is sea glass! And the loop is made of ghost-net rope?",
        "Grandpa always says, ‘The sea gives us everything. Let's not give it our trash.’ Now I finally get it.",
        "Here, take this! And from now on I'll help you too. I know every rock pool on this beach!",
    };

    static WorkshopDay Day3 => new WorkshopDay
    {
        day = 3, scenePath = Day3ScenePath, artDir = Day3Dir, version = 2, autoBuildKey = "Tideworks.Day3AutoBuild.v3",
        customer = "Mert", ownerSub = "His grandpa's compass",
        lines = MertLines, thanks = MertThanks,
        tasks = new[]
        {
            ("Repair Pamuk the teddy bear", "Elif and Pamuk are best friends again."),
            ("Meet Mert", "A boy with a fishing net and a broken compass."),
            ("Repair Grandpa's compass", "Needs: sea glass, a magnet, copper wire, rope."),
            ("Clean the rock pools", "Tiny crabs and fish live there."),
            ("Final hatching night", "All the nests are ready."),
        },
        progressTask = 3, progressLabel = "0 / 8",
        rewardLine = "Mert joins as a helper · +50 coins", helperPill = "Mert is now your helper",
        quote = "“The sea gives us everything. Let's not give it our trash.”",
        missionTitle = "Clean the rock pools!",
        missionBody = "The storm left trash in the rock pools by the cliffs. Tiny crabs and fish live there, " +
                      "and Mert knows every pool. Let's clean them up!",
        missionRows = new[] { ("Teal", "IconBoatWhite", "Sail to the rocks"), ("Green", "IconRecycleWhite", "Sort the trash"),
                              ("Orange", "IconTurtleWhite", "Final hatching night") },
        seaScene = "Day3Sea", requiredItem = "", helpFirstLine = "Can you fix Grandpa's compass first? Please?",
        customerHead = new Vector2(1494, 640),
        steps = new[] { "Pick out the broken glass", "Fit a new lens", "Wake up the needle", "Seal the crack", "Clamp the dented rim", "Tie a new loop" },
        stepSubs = new[] { "Tweezers · 4 shards", "Sea-glass lens", "Magnet · 3 strokes", "Pliers · copper staples", "Screwdriver · tin clamp", "Net rope" },
        slotSprites = new[] { "ToolTweezers", "ToolMagnet", "ToolPliers", "ToolScrewdriver", "ShelfLens", "ShelfStaples", "ShelfClamp", "ShelfLoop" },
        toolLabels = new[] { "Tweezers", "Magnet", "Pliers", "Screwdriver" },
        shelf = new[]
        {
            ("Sea-glass lens", "from a clear bottle", "Glass bin"), ("Copper staples", "from an old cable", "Metal bin"),
            ("Tin clamp", "from a tuna tin", "Metal bin"), ("Net rope loop", "from a ghost net", "Nets bin"),
        },
        addBench = go => go.AddComponent<CompassRepairController>(),
        buildSubject = (b, bench) => BuildCompass((CompassRepairController)b, bench),
    };

    static void RequestDay3() => RequestWorkshopDay(Day3);

    [MenuItem("Tideworks/Rebuild Day 3 Workshop Scene")]
    static void RebuildDay3Menu() => RebuildWorkshopDayMenu(Day3);

    /// <summary>Pusula: ayni 640x700 tuvalde katmanlar (merkez 320,400), ibre ve parcalar ayri; tiklanan noktalar.</summary>
    static void BuildCompass(CompassRepairController ctl, Transform bench)
    {
        var compass = new GameObject("Compass", typeof(RectTransform)).transform as RectTransform;
        compass.SetParent(bench, false);
        Place(compass, new Rect(1290, 268, 640, 700));
        Image Layer(string name) => Img2(compass, name, Day3Dir, name, new Rect(0, 0, 640, 700));
        Layer("CompassCase");
        var crack = Layer("CompassCrack");
        Vector2[] staplePos = { new Vector2(62, 345), new Vector2(76, 420) };
        var staples = new Image[2];
        for (int i = 0; i < 2; i++)
            staples[i] = Img2(compass, "Staple" + i, Day3Dir, "CopperStaple", new Rect(staplePos[i].x, staplePos[i].y, 70, 36));
        var dent = Layer("CompassDent");
        var clamp = Layer("CompassClamp");
        var screw = Img2(compass, "ClampScrew", Day3Dir, "ClampScrew", new Rect(521, 368, 48, 48));
        var loopBroken = Layer("CompassLoopBroken");
        var loopNew = Layer("CompassLoopNew");
        var needle = Img2(compass, "Needle", Day3Dir, "CompassNeedle", new Rect(288, 230, 64, 340));
        var lens = Layer("CompassLens");
        var glass = Layer("CompassGlassBroken");
        var bent = Layer("CompassNeedleBent");

        // Tiklanan noktalar (parcalar ustte kalsin diye once)
        var face = Spot(compass, "FaceSpot", new Rect(130, 210, 380, 380), ctl.ClickFace);
        var crackSpot = Spot(compass, "CrackSpot", new Rect(30, 300, 120, 180), ctl.ClickCrack);
        var rimSpot = Spot(compass, "RimSpot", new Rect(500, 290, 110, 210), ctl.ClickRim);
        var loopSpot = Spot(compass, "LoopSpot", new Rect(240, 0, 160, 170), ctl.ClickLoop);

        Vector2[] shardPos = { new Vector2(250, 320), new Vector2(420, 300), new Vector2(300, 520), new Vector2(380, 510) };
        var shards = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            shards[i] = Img2(compass, "Shard" + i, Day3Dir, "Shard" + i, new Rect(shardPos[i].x - 40, shardPos[i].y - 40, 80, 80));
            shards[i].raycastTarget = true;
            var b = shards[i].gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            shards[i].gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.15f;
            UnityEventTools.AddIntPersistentListener(b.onClick, ctl.ClickShard, i);
        }
        var magnet = Img2(compass, "Magnet", Day3Dir, "MagnetFx", new Rect(250, 330, 140, 140));

        SetRefs(ctl, ("compass", compass), ("glassBroken", glass), ("needleBent", bent), ("lens", lens), ("needle", needle),
            ("magnet", magnet), ("crack", crack), ("dent", dent), ("clamp", clamp), ("screw", screw),
            ("loopBroken", loopBroken), ("loopNew", loopNew),
            ("faceSpot", (RectTransform)face.transform), ("crackSpot", (RectTransform)crackSpot.transform),
            ("rimSpot", (RectTransform)rimSpot.transform), ("loopSpot", (RectTransform)loopSpot.transform));
        SetArray(ctl, "shards", shards);
        SetArray(ctl, "staples", staples);
    }
}
