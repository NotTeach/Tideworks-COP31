using System.Collections;
using Tideworks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3. gun tezgahi: Mert'in dedesinin pusulasi 6 adimda onarilir.
/// 1 kirik camlari topla (cimbiz, 4 parca; son parcayla egri ibre de cikar) · 2 yeni lens (deniz cami)
/// 3 ibreyi uyandir (miknatis, 3 surtme; ibre doner ve K'de durur) · 4 catlagi kapat (bakir zimbalar, sonra pense ile 2 kez bas)
/// 5 ezik kenar (teneke kelepce, sonra tornavidayla vidala) · 6 yeni halka (hayalet ag ipi).
/// Secim, adim listesi ve yardim: RepairBench.
/// </summary>
public class CompassRepairController : RepairBench
{
    public enum Item { None = -1, Tweezers, Magnet, Pliers, Screwdriver, Lens, Staples, Clamp, Loop }

    [Header("Pusula")]
    [SerializeField] RectTransform compass;
    [SerializeField] Image[] shards;
    [SerializeField] Image glassBroken;
    [SerializeField] Image needleBent;
    [SerializeField] Image lens;
    [SerializeField] Image needle;
    [SerializeField] Image magnet;
    [SerializeField] Image crack;
    [SerializeField] Image[] staples;
    [SerializeField] Image dent;
    [SerializeField] Image clamp;
    [SerializeField] Image screw;
    [SerializeField] Image loopBroken;
    [SerializeField] Image loopNew;
    [SerializeField] RectTransform faceSpot;
    [SerializeField] RectTransform crackSpot;
    [SerializeField] RectTransform rimSpot;
    [SerializeField] RectTransform loopSpot;
    [Tooltip("Ibre bu kadar surtmeden sonra uyanir")]
    [SerializeField] int strokes = 3;

    static readonly string[] ItemNames = { "tweezers", "magnet", "pliers", "screwdriver", "sea-glass lens", "copper staples", "tin clamp", "net rope loop" };
    static readonly string[] ItemNamesTr = { "cımbız", "mıknatıs", "pense", "tornavida", "deniz camı lens", "bakır zımbalar", "teneke kelepçe", "ağ ipi halka" };
    static readonly string[] ItemNamesTrAcc = { "cımbızı", "mıknatısı", "penseyi", "tornavidayı", "deniz camı lensi", "bakır zımbaları", "teneke kelepçeyi", "ağ ipi halkayı" };

    static readonly string[] StepTitlesEn =
        { "Pick out the broken glass", "Fit a new lens", "Wake up the needle", "Seal the crack", "Clamp the dented rim", "Tie a new loop" };

    Vector2[] stapleHome;
    Vector2 magnetHome;
    float needleAngle;

    void Awake()
    {
        stapleHome = new Vector2[staples.Length];
        for (int i = 0; i < staples.Length; i++) stapleHome[i] = staples[i].rectTransform.anchoredPosition;
        magnetHome = magnet.rectTransform.anchoredPosition;
    }

    protected override string HelperLine => "Elif: Let me show you!";

    protected override void ResetSubject()
    {
        foreach (var s in shards) Show(s, true);
        Show(glassBroken, true);
        Show(needleBent, true);
        needleBent.rectTransform.anchoredPosition = Vector2.zero;
        Show(lens, false);
        Show(needle, false);
        Show(magnet, false);
        Show(crack, true);
        for (int i = 0; i < staples.Length; i++)
        {
            Show(staples[i], false);
            staples[i].rectTransform.anchoredPosition = stapleHome[i];
        }
        Show(dent, true);
        Show(clamp, false);
        Show(screw, false);
        Show(loopBroken, true);
        Show(loopNew, false);
        compass.localRotation = Quaternion.identity;
        Say(L("Careful! Glass shards everywhere. Take the tweezers and pick out all 4 pieces."));
    }

    // ---------------- Pusuladaki noktalar ----------------

    public void ClickShard(int i)
    {
        if (Locked) return;
        if (step != 0) { Wrong(); return; }
        if (!Need((int)Item.Tweezers) || !shards[i].gameObject.activeSelf) return;
        StartCoroutine(PickShard(i));
    }

    public void ClickFace()
    {
        if (Locked) return;
        if (step == 0) { Need((int)Item.Tweezers); return; }
        if (step == 1) { if (Need((int)Item.Lens)) StartCoroutine(FitLens()); return; }
        if (step == 2) { if (Need((int)Item.Magnet)) StartCoroutine(Stroke()); return; }
        Wrong();
    }

    public void ClickCrack()
    {
        if (Locked) return;
        if (step != 3) { Wrong(); return; }
        if (phase == 0) { if (Need((int)Item.Staples)) StartCoroutine(PlaceStaples()); }
        else if (Need((int)Item.Pliers)) StartCoroutine(PressStaple());
    }

    public void ClickRim()
    {
        if (Locked) return;
        if (step != 4) { Wrong(); return; }
        if (phase == 0) { if (Need((int)Item.Clamp)) StartCoroutine(PlaceClamp()); }
        else if (Need((int)Item.Screwdriver)) StartCoroutine(Screw());
    }

    public void ClickLoop()
    {
        if (Locked) return;
        if (step != 5) { Wrong(); return; }
        if (!Need((int)Item.Loop)) return;
        StartCoroutine(TieLoop());
    }

    // ---------------- Adimlar ----------------

    IEnumerator PickShard(int i)
    {
        busy = true;
        TwSes.EfektCesitli(Ses.Tik);
        var rt = shards[i].rectTransform;
        var home = rt.anchoredPosition;
        yield return Tween(0.35f, t => { rt.anchoredPosition = home + new Vector2(80f * t, 120f * t); SetAlpha(shards[i], 1f - t); });
        Show(shards[i], false);
        rt.anchoredPosition = home;
        busy = false;
        Progress();
        if (++count < shards.Length) yield break;

        // Son parcayla catlak cam ve egri ibre de cikar
        busy = true;
        TwSes.Efekt(Ses.ParcaTak);
        var bent = needleBent.rectTransform;
        yield return Tween(0.6f, t =>
        {
            SetAlpha(glassBroken, 1f - t);
            bent.anchoredPosition = new Vector2(120f * t, 160f * t * t);
            SetAlpha(needleBent, 1f - t);
        });
        Show(glassBroken, false);
        Show(needleBent, false);
        busy = false;
        Progress();
        NextStep("All the glass is out, and the bent needle came out too. Fit the new sea-glass lens.");
    }

    IEnumerator FitLens()
    {
        busy = true;
        Deselect();
        TwSes.Efekt(Ses.Pop);
        Show(lens, true);
        yield return Tween(0.35f, t => { lens.rectTransform.localScale = Vector3.one * EaseOutBack(t); SetAlpha(lens, t); });
        // Yeni ibre yerine oturur ama hic kipirdamiyor: miknatisla uyandirilacak
        Show(needle, true);
        needleAngle = 128f;
        needle.rectTransform.localRotation = Quaternion.Euler(0, 0, needleAngle);
        SetAlpha(needle, 0f);
        yield return Tween(0.3f, t => SetAlpha(needle, t));
        busy = false;
        Progress();
        NextStep("Clear as the sea! But the needle is asleep. Stroke it 3 times with the magnet.");
    }

    IEnumerator Stroke()
    {
        busy = true;
        TwSes.EfektCesitli(Ses.Zimpara, 0.6f);
        var m = magnet.rectTransform;
        Show(magnet, true);
        Vector2 from = magnetHome + new Vector2(-170f, -150f), to = magnetHome + new Vector2(170f, 150f);
        float start = needleAngle;
        count++;
        // Her surtmede ibre biraz daha canlanir; ucuncude donup K'de (kuzey, 0 derece) durur
        float spin = count < strokes ? 40f * count : 720f + Mathf.DeltaAngle(start, 0f);
        yield return Tween(0.6f, t =>
        {
            m.anchoredPosition = Vector2.Lerp(from, to, t);
            m.localRotation = Quaternion.Euler(0, 0, -30f);
            SetAlpha(magnet, Mathf.Min(1f, Mathf.Min(t, 1f - t) * 6f));
            needleAngle = start + spin * Mathf.SmoothStep(0f, 1f, t) * (count < strokes ? Mathf.Sin(t * Mathf.PI) : 1f);
            needle.rectTransform.localRotation = Quaternion.Euler(0, 0, needleAngle);
        });
        Show(magnet, false);
        m.anchoredPosition = magnetHome;
        if (count < strokes)
        {
            needleAngle = start;
            needle.rectTransform.localRotation = Quaternion.Euler(0, 0, start);
            busy = false;
            Progress();
            Say(Loc.F("The needle twitched! {0} more.", strokes - count));
            yield break;
        }
        // Ibre bir sure doner, sallanarak K'de durur
        TwSes.Efekt(Ses.GecisParilti, 0.7f);
        yield return Tween(1.2f, t =>
        {
            needleAngle = Mathf.Sin(t * Mathf.PI * 5f) * 18f * (1f - t);
            needle.rectTransform.localRotation = Quaternion.Euler(0, 0, needleAngle);
        });
        needleAngle = 0f;
        needle.rectTransform.localRotation = Quaternion.identity;
        busy = false;
        Progress();
        NextStep("It spins... and stops at K. North! Now seal the crack: pick the copper staples and click the crack.");
    }

    IEnumerator PlaceStaples()
    {
        busy = true;
        Deselect();
        TwSes.Efekt(Ses.ParcaTak);
        for (int i = 0; i < staples.Length; i++)
        {
            var s = staples[i];
            Show(s, true);
            // Zimbalar once havada, egri durur; pense ile basilinca yerine oturur
            s.rectTransform.anchoredPosition = stapleHome[i] + new Vector2(-14f, 10f);
            s.rectTransform.localRotation = Quaternion.Euler(0, 0, 24f);
            yield return Tween(0.2f, t => s.rectTransform.localScale = Vector3.one * EaseOutBack(t));
        }
        phase = 1;
        busy = false;
        Progress();
        Say(L("The staples are in place. Now press them in with the pliers: click the crack twice."));
    }

    IEnumerator PressStaple()
    {
        busy = true;
        TwSes.EfektCesitli(Ses.Cekic, 0.7f);
        var s = staples[count].rectTransform;
        Vector2 from = s.anchoredPosition;
        int i = count;
        yield return Tween(0.25f, t =>
        {
            s.anchoredPosition = Vector2.Lerp(from, stapleHome[i], t);
            s.localRotation = Quaternion.Euler(0, 0, 24f * (1f - t));
        });
        busy = false;
        Progress();
        if (++count < staples.Length) { Say(L("One staple pressed. One more!")); yield break; }
        StartCoroutine(Tween(0.4f, t => SetAlpha(crack, 1f - 0.65f * t)));
        NextStep("The crack is sealed. Now the dented rim: pick the tin clamp and click the dent.");
    }

    IEnumerator PlaceClamp()
    {
        busy = true;
        Deselect();
        TwSes.Efekt(Ses.ParcaTak);
        Show(clamp, true);
        yield return Tween(0.35f, t => { clamp.rectTransform.localScale = Vector3.one * EaseOutBack(t); SetAlpha(clamp, t); });
        Show(screw, true);
        phase = 1;
        busy = false;
        Progress();
        Say(L("The clamp is on the dent. Screw it tight with the screwdriver."));
    }

    IEnumerator Screw()
    {
        busy = true;
        TwSes.EfektCesitli(Ses.Vida, 0.8f);
        var sc = screw.rectTransform;
        yield return Tween(0.8f, t => sc.localRotation = Quaternion.Euler(0, 0, -540f * t));
        yield return Tween(0.3f, t => SetAlpha(dent, 1f - t));
        Show(dent, false);
        busy = false;
        Progress();
        NextStep("Tight and round again! Last step: tie the new net rope loop at the top.");
    }

    IEnumerator TieLoop()
    {
        busy = true;
        Deselect();
        TwSes.Efekt(Ses.ParcaTak);
        yield return Tween(0.25f, t => SetAlpha(loopBroken, 1f - t));
        Show(loopBroken, false);
        Show(loopNew, true);
        yield return Tween(0.35f, t => loopNew.rectTransform.localScale = Vector3.one * EaseOutBack(t));
        MarkAllDone();
        Say(L("Grandpa's compass is as good as new! Let's give it back to Mert."));
        TwSes.Efekt(Ses.OnarimTamam);
        yield return Tween(1.2f, t => compass.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 4f) * 5f * (1f - t)));
        yield return new WaitForSecondsRealtime(1.4f);
        Complete();
    }

    // ---------------- Yardim icin adim bilgisi ----------------

    protected override int Required() => (int)(step switch
    {
        0 => Item.Tweezers,
        1 => Item.Lens,
        2 => Item.Magnet,
        3 => phase == 0 ? Item.Staples : Item.Pliers,
        4 => phase == 0 ? Item.Clamp : Item.Screwdriver,
        5 => Item.Loop,
        _ => Item.None,
    });

    protected override RectTransform TargetRect()
    {
        switch (step)
        {
            case 0: foreach (var s in shards) if (s.gameObject.activeSelf) return s.rectTransform; return null;
            case 1:
            case 2: return faceSpot;
            case 3: return crackSpot;
            case 4: return rimSpot;
            case 5: return loopSpot;
        }
        return null;
    }

    protected override string StepTools() => step switch
    {
        0 => "Then click each glass shard.",
        1 => "Then click the compass face.",
        2 => "Then click the compass face 3 times.",
        3 => phase == 0 ? "Then click the crack on the left." : "Then click the crack to press each staple.",
        4 => phase == 0 ? "Then click the dent on the right." : "Then click the clamp.",
        5 => "Then click the top of the compass.",
        _ => "",
    };

    protected override string Hint() => step switch
    {
        0 => "Step 1: pick out the 4 glass shards with the tweezers.",
        1 => "Step 2: fit the sea-glass lens on the compass face.",
        2 => "Step 3: stroke the needle 3 times with the magnet.",
        3 => phase == 0 ? "Step 4: put the copper staples on the crack." : "Press the staples in with the pliers.",
        4 => phase == 0 ? "Step 5: put the tin clamp on the dented rim." : "Screw the clamp tight with the screwdriver.",
        5 => "Step 6: tie the net rope loop at the top.",
        _ => "",
    };

    protected override string StepTitle(int i) => StepTitlesEn[i];

    protected override string ItemName(int item, bool accusative) =>
        !Loc.Turkish ? ItemNames[item] : accusative ? ItemNamesTrAcc[item] : ItemNamesTr[item];

    protected override IEnumerator AutoMove()
    {
        switch (step)
        {
            case 0:
                for (int i = 0; i < shards.Length; i++)
                    if (shards[i].gameObject.activeSelf) { yield return PickShard(i); break; }
                break;
            case 1: yield return FitLens(); break;
            case 2: yield return Stroke(); break;
            case 3: yield return phase == 0 ? PlaceStaples() : PressStaple(); break;
            case 4: yield return phase == 0 ? PlaceClamp() : Screw(); break;
            case 5: yield return TieLoop(); break;
        }
    }
}
