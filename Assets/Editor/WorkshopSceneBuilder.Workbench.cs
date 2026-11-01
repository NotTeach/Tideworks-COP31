using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static MainMenuSceneBuilder;

/// <summary>
/// Workshop sahnesinin 4. sayfasi: tamir tezgahi (RepairController).
/// Gorseller: Assets/Sprites/Workbench (uretici: Tools/MenuArt/workshop/workbench.py)
/// </summary>
public static partial class WorkshopSceneBuilder
{
    const string BenchDir = "Assets/Sprites/Workbench/";

    static readonly Color BenchInk = new Color32(51, 47, 44, 255);
    static readonly Color StepSubInk = new Color32(138, 126, 109, 255);
    static readonly Color StepNumberInk = new Color32(70, 56, 44, 255);
    static readonly Color CountInk = new Color32(96, 84, 72, 255);
    static readonly Color BarTrack = new Color32(23, 119, 122, 255);
    static readonly Color BarFill = new Color32(242, 193, 78, 255);

    static readonly string[] StepTitles =
        { "Open the case", "Clean the dust", "Replace the dial glass", "Rewire the speaker", "Close it up", "Tune in" };
    static readonly string[] StepSubs =
        { "Screwdriver · 4 screws", "Cloth", "Pliers, then clear glass", "Copper wire, then solder", "Wood lid, then screws", "Find the station" };

    // Item sirasiyla (RepairController.Item)
    static readonly string[] SlotNames =
        { "ToolScrewdriver", "ToolCloth", "ToolPliers", "ToolSolder", "ShelfGlass", "ShelfCopper", "ShelfWood", "ShelfRope", "ShelfFilament" };
    static readonly float[] ShelfX = { 670, 786, 902, 1018, 1134 };

    // workbench.py ciktisi (wb_rects.json), 2x artboard pikseli
    static readonly Dictionary<string, Rect> BenchArt = new Dictionary<string, Rect>
    {
        { "LooseWire", new Rect(1316, 792, 140, 82) },
        { "ToolScrewdriver", new Rect(220, 1164, 228, 224) }, { "ToolCloth", new Rect(448, 1164, 228, 224) },
        { "ToolPliers", new Rect(676, 1164, 228, 224) }, { "ToolSolder", new Rect(904, 1164, 228, 224) },
        { "ShelfGlass", new Rect(1328, 1164, 232, 224) }, { "ShelfCopper", new Rect(1560, 1164, 232, 224) },
        { "ShelfWood", new Rect(1792, 1164, 232, 224) }, { "ShelfRope", new Rect(2024, 1164, 232, 224) },
        { "ShelfFilament", new Rect(2256, 1164, 232, 224) },
        { "LidOld", new Rect(1202, 522, 420, 416) },
        { "Screw0", new Rect(1220, 542, 36, 36) }, { "Screw1", new Rect(1568, 542, 36, 36) },
        { "Screw2", new Rect(1220, 884, 36, 36) }, { "Screw3", new Rect(1568, 884, 36, 36) },
        { "Dust0", new Rect(1240, 572, 128, 128) }, { "Dust1", new Rect(1360, 640, 128, 128) },
        { "Dust2", new Rect(1476, 536, 128, 128) }, { "Dust3", new Rect(1340, 800, 128, 128) },
        { "Dust4", new Rect(1684, 536, 128, 128) }, { "Dust5", new Rect(1772, 660, 128, 128) },
        { "Dust6", new Rect(1216, 746, 128, 128) }, { "Dust7", new Rect(1856, 536, 128, 128) },
        { "GlassCracked", new Rect(1667, 540, 240, 260) }, { "GlassClear", new Rect(1667, 540, 240, 260) },
        { "Needle", new Rect(1695, 568, 184, 184) }, { "StationMark", new Rect(1836, 593, 28, 28) },
        { "TuningKnob", new Rect(1833, 838, 68, 68) },
        { "CopperWire", new Rect(1304, 784, 152, 96) }, { "Solder", new Rect(1412, 788, 40, 40) },
    };

    static CanvasGroup BuildWorkbench(Transform canvas, WorkshopOpeningController host, out RepairController r)
    {
        ConfigureSprites(BenchDir);
        var page = MakePage(canvas, "WorkbenchPage", "Workbench", WideArt, out var bench, BenchDir);
        r = page.gameObject.AddComponent<RepairController>();

        // --- Ust bar ---
        Spot(bench, "BackSpot", R2(22, 12, 48, 48), r.Back);
        Spot(bench, "WorkshopTabSpot", R2(338, 13, 122, 47), r.Back);
        var track = MakeImage(bench, "RepairTrack", null);
        track.color = BarTrack;
        Place(track.rectTransform, R2(1071, 31, 139, 11));
        var fill = MakeImage(bench, "RepairFill", null);
        fill.color = BarFill;
        PlaceLeft(fill.rectTransform, R2(1071, 31, 0, 11));
        var progress = Label(bench, "RepairCount", "0 / 6", fontHeavy, 34, Color.white, R2(1221, 24, 60, 24));

        // --- Tamir adimlari ---
        int n = StepTitles.Length;
        var rows = new Image[n]; var circles = new Image[n]; var numbers = new Text[n]; var titles = new Text[n];
        for (int i = 0; i < n; i++)
        {
            float yc = 233 + i * 53.8f;
            rows[i] = MakeImage(bench, "StepRow" + (i + 1), LoadSprite("RowCurrent", BenchDir));
            Place(rows[i].rectTransform, R2(40, yc - 25, 244, 50));
            circles[i] = MakeImage(bench, "StepCircle" + (i + 1), LoadSprite("StepPending", BenchDir));
            Place(circles[i].rectTransform, R2(51, yc - 14, 28, 28));
            numbers[i] = Label(circles[i].transform, "Number", (i + 1).ToString(), fontHeavy, 26, StepNumberInk,
                new Rect(0, 0, 56, 56), TextAnchor.MiddleCenter);
            titles[i] = Label(bench, "StepTitle" + (i + 1), StepTitles[i], fontHeavy, 26, BenchInk, R2(88, yc - 16, 200, 18));
            Label(bench, "StepSub" + (i + 1), StepSubs[i], fontBody, 23, StepSubInk, R2(88, yc + 1, 200, 16));
        }

        var message = Label(bench, "Message", "", fontHeavy, 30, BenchInk, R2(362, 503, 826, 38));
        message.horizontalOverflow = HorizontalWrapMode.Wrap;
        message.lineSpacing = 0.95f;

        // --- Radyo (cizim sirasi = ust uste binme; gorunmez tiklama alanlari en ustte) ---
        BenchCut(bench, "StationMark");
        var needle = BenchCut(bench, "Needle");
        var glassClear = BenchCut(bench, "GlassClear");
        var glassCracked = BenchCut(bench, "GlassCracked");
        var looseWire = BenchCut(bench, "LooseWire");
        var copper = BenchCut(bench, "CopperWire");
        var solder = BenchCut(bench, "Solder");
        var knob = BenchCut(bench, "TuningKnob");

        Spot(bench, "CaseSpot", R2(605, 265, 202, 200), r.ClickCase);
        Spot(bench, "DialSpot", R2(838, 274, 112, 112), r.ClickDial);
        Spot(bench, "WireSpot", R2(648, 392, 82, 48), r.ClickWire);
        var knobButton = knob.gameObject.AddComponent<Button>();
        knob.raycastTarget = true;
        knobButton.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(knobButton.onClick, r.ClickKnob);

        var dust = new Image[8];
        for (int i = 0; i < dust.Length; i++)
        {
            dust[i] = BenchCut(bench, "Dust" + i);
            dust[i].raycastTarget = true;
            var b = dust[i].gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            UnityEventTools.AddIntPersistentListener(b.onClick, r.ClickDust, i);
        }
        var lid = BenchCut(bench, "LidOld", "Lid");
        var screws = new Image[4];
        for (int i = 0; i < screws.Length; i++) screws[i] = BenchCut(bench, "Screw" + i, sprite: "Screw");
        for (int i = 0; i < screws.Length; i++)
        {
            var sr = BenchArt["Screw" + i];
            var spot = Spot(bench, "ScrewSpot" + i, new Rect(sr.center.x - 30, sr.center.y - 30, 60, 60), null);
            UnityEventTools.AddIntPersistentListener(spot.onClick, r.ClickScrew, i);
        }

        // --- Aletler ve raf ---
        var slots = new Button[SlotNames.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            var img = BenchCut(bench, SlotNames[i]);
            img.raycastTarget = true;
            var b = slots[i] = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = colors.disabledColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            img.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.04f;
            UnityEventTools.AddIntPersistentListener(b.onClick, r.Select, i);
        }
        var counts = new Text[ShelfX.Length];
        for (int i = 0; i < counts.Length; i++)
            counts[i] = Label(slots[4 + i].transform, "Count", "×1", fontBody, 22, CountInk,
                Local(R2(ShelfX[i] + 9, 659, 40, 20), BenchArt[SlotNames[4 + i]]));
        var selected = MakeImage(bench, "SlotSelected", LoadSprite("SlotSelected", BenchDir));
        Place(selected.rectTransform, Centered(BenchArt["ToolScrewdriver"].center.x, BenchArt["ToolScrewdriver"].center.y, 220));
        selected.rectTransform.sizeDelta = new Vector2(220, 212);

        // --- Yardim: ipucu halkalari, "?" butonu ve yardim karti ---
        var slotHint = Hint(bench, "SlotHint");
        var targetHint = Hint(bench, "TargetHint");

        var help = MakeImage(bench, "HelpButton", LoadSprite("HelpButton", BenchDir));
        help.raycastTarget = true;
        Place(help.rectTransform, R2(1201, 504, 38, 38));
        Label(help.transform, "Mark", "?", fontHeavy, 50, Color.white, new Rect(0, 2, 76, 76), TextAnchor.MiddleCenter);
        var helpButton = help.gameObject.AddComponent<Button>();
        helpButton.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(helpButton.onClick, r.OpenHelp);

        var overlay = MakeImage(bench, "HelpOverlay", null);
        overlay.color = new Color(0.02f, 0.08f, 0.1f, 0.55f);
        overlay.raycastTarget = true; // arkadaki tezgaha tiklanmasin
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
        HelpCardButton(cardT, "ShowMe", "HelpButtonTeal", 96, "Show me", Color.white, r.ShowMe);
        HelpCardButton(cardT, "DoItForMe", "HelpButtonOrange", 471, "Do it for me", Color.white, r.DoItForMe);
        HelpCardButton(cardT, "GotIt", "HelpButtonOutline", 846, "Got it", BenchInk, r.CloseHelp);
        overlay.gameObject.SetActive(false);

        SetRefs(r, ("host", host), ("progressText", progress), ("progressFill", fill.rectTransform),
            ("message", message), ("slotSelected", selected.rectTransform),
            ("lid", lid), ("lidNew", LoadSprite("LidNew", BenchDir)),
            ("glassCracked", glassCracked), ("glassClear", glassClear), ("looseWire", looseWire),
            ("copperWire", copper), ("solder", solder), ("needle", needle.rectTransform), ("tuningKnob", knob.rectTransform),
            ("rowDone", LoadSprite("RowDone", BenchDir)), ("rowCurrent", LoadSprite("RowCurrent", BenchDir)),
            ("circleDone", LoadSprite("StepDone", BenchDir)), ("circleCurrent", LoadSprite("StepCurrent", BenchDir)),
            ("circlePending", LoadSprite("StepPending", BenchDir)),
            ("helpButton", help.rectTransform), ("helpOverlay", overlay.gameObject), ("helpStep", helpStep),
            ("helpPick", helpPick), ("helpClick", helpClick), ("helpIcon", helpIcon),
            ("slotHint", slotHint.rectTransform), ("targetHint", targetHint.rectTransform));
        SetArray(r, "stepRows", rows);
        SetArray(r, "stepCircles", circles);
        SetArray(r, "stepNumbers", numbers);
        SetArray(r, "stepTitles", titles);
        SetArray(r, "slots", slots);
        SetArray(r, "shelfCounts", counts);
        SetArray(r, "screws", screws);
        SetArray(r, "dust", dust);
        return page;
    }

    static Image BenchCut(Transform parent, string name, string objectName = null, string sprite = null)
    {
        var img = MakeImage(parent, objectName ?? name, LoadSprite(sprite ?? name, BenchDir));
        Place(img.rectTransform, BenchArt[name]);
        return img;
    }

    /// <summary>Gorunmez tiklama alani (tasarimda zaten cizili olan seyler icin).</summary>
    static Button Spot(Transform parent, string name, Rect r, UnityAction onClick)
    {
        var img = MakeImage(parent, name, null);
        img.color = new Color(1, 1, 1, 0);
        img.raycastTarget = true;
        Place(img.rectTransform, r);
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        if (onClick != null) UnityEventTools.AddPersistentListener(b.onClick, onClick);
        return b;
    }

    /// <summary>Dogru aleti / dogru yeri gosteren yanip sonen halka (tiklamayi engellemez).</summary>
    static Image Hint(Transform parent, string name)
    {
        var img = MakeImage(parent, name, LoadSprite("HintRing", BenchDir));
        Place(img.rectTransform, new Rect(0, 0, 200, 200));
        var p = img.gameObject.AddComponent<MenuPulse>();
        p.minAlpha = 0.35f;
        p.maxAlpha = 1f;
        p.speed = 1.1f;
        p.scaleAmount = 0.08f;
        img.gameObject.SetActive(false);
        return img;
    }

    static void HelpCardButton(Transform card, string name, string sprite, float x, string label, Color ink, UnityAction onClick)
    {
        var img = MakeImage(card, name, LoadSprite(sprite, BenchDir));
        img.raycastTarget = true;
        Place(img.rectTransform, new Rect(x, 580, 370, 112));
        var b = img.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.fadeDuration = 0.08f;
        b.colors = colors;
        img.gameObject.AddComponent<MenuButtonFx>().hoverScale = 1.04f;
        var t = Label(img.transform, "Label", label, fontHeavy, 40, ink, new Rect(0, -4, 370, 112), TextAnchor.MiddleCenter);
        t.resizeTextForBestFit = true;
        t.resizeTextMaxSize = 40;
        t.resizeTextMinSize = 24;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        UnityEventTools.AddPersistentListener(b.onClick, onClick);
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
