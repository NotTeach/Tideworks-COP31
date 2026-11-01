using System.Collections;
using Tideworks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 2. gun tezgahi: Elif'in oyuncak ayisi Pamuk 6 adimda iyilesir.
/// Asagidan bir alet ya da raftan bir malzeme secilir, sonra ayida dogru yere tiklanir.
/// 1 agi kes (makas, 3 dugum) · 2 denizi yika (sunger, 4 leke) · 3 omzu doldur (cimbiz, sonra geri donusturulmus elyaf)
/// 4 kolu dik (ip, sonra igne) · 5 yeni goz (deniz cami dugme) · 6 sevgiyle yamala (yelken bezi kalp).
/// Secim, adim listesi ve yardim: RepairBench.
/// </summary>
public class TeddyRepairController : RepairBench
{
    public enum Item { None = -1, Scissors, Sponge, Tweezers, Needle, Fiber, Thread, Button, Heart }

    [Header("Pamuk")]
    [SerializeField] Image net;
    [SerializeField] Image[] knots;
    [SerializeField] Image[] mud;
    [SerializeField] Image stuffingOld;
    [SerializeField] Image stuffingNew;
    [SerializeField] Image armLoose;
    [SerializeField] Image armAttached;
    [SerializeField] Image stitches;
    [SerializeField] Image eye;
    [SerializeField] Image heart;
    [SerializeField] RectTransform teddy;
    [SerializeField] RectTransform shoulderSpot;
    [SerializeField] RectTransform eyeSpot;
    [SerializeField] RectTransform chestSpot;

    static readonly string[] ItemNames = { "scissors", "sponge", "tweezers", "needle", "recycled fiber", "rope thread", "sea-glass button", "sail-cloth heart" };
    static readonly string[] ItemNamesTr = { "makas", "sünger", "cımbız", "iğne", "geri dönüştürülmüş elyaf", "halat ipliği", "deniz camı düğme", "yelken bezi kalp" };
    static readonly string[] ItemNamesTrAcc = { "makası", "süngeri", "cımbızı", "iğneyi", "geri dönüştürülmüş elyafı", "halat ipliğini", "deniz camı düğmeyi", "yelken bezi kalbi" };

    static readonly string[] StepTitlesEn =
        { "Cut the net away", "Wash off the sea", "Re-stuff the shoulder", "Sew the arm back on", "Give him a new eye", "Patch it with love" };
    Vector2 armLooseHome, netHome, stuffingHome;

    void Awake()
    {
        armLooseHome = armLoose.rectTransform.anchoredPosition;
        netHome = net.rectTransform.anchoredPosition;
        stuffingHome = stuffingOld.rectTransform.anchoredPosition;
    }

    protected override void ResetSubject()
    {
        Show(net, true);
        net.rectTransform.anchoredPosition = netHome;
        stuffingOld.rectTransform.anchoredPosition = stuffingHome;
        foreach (var k in knots) Show(k, true);
        foreach (var m in mud) Show(m, true);
        Show(stuffingOld, true);
        Show(stuffingNew, false);
        Show(armLoose, true);
        armLoose.rectTransform.anchoredPosition = armLooseHome;
        Show(armAttached, false);
        Show(stitches, false);
        Show(eye, false);
        Show(heart, false);
        Say(L("Pamuk is wrapped in a ghost net. Pick the scissors and snip the 3 knots."));
    }

    // ---------------- Ayidaki noktalar ----------------

    public void ClickKnot(int i)
    {
        if (Locked) return;
        if (step != 0) { Wrong(); return; }
        if (!Need((int)Item.Scissors) || !knots[i].gameObject.activeSelf) return;
        StartCoroutine(Snip(i));
    }

    public void ClickMud(int i)
    {
        if (Locked) return;
        if (step != 1) { Wrong(); return; }
        if (!Need((int)Item.Sponge) || !mud[i].gameObject.activeSelf) return;
        StartCoroutine(Wash(i));
    }

    public void ClickShoulder()
    {
        if (Locked) return;
        if (step == 2)
        {
            if (phase == 0) { if (Need((int)Item.Tweezers)) StartCoroutine(PullOld()); }
            else if (Need((int)Item.Fiber)) StartCoroutine(Stuff());
        }
        else if (step == 3 && phase == 1) { if (Need((int)Item.Needle)) StartCoroutine(Sew()); }
        else if (step == 3) Say(L("First bring the arm over: pick the rope thread and click the loose arm."));
        else Wrong();
    }

    public void ClickArm()
    {
        if (Locked) return;
        if (step != 3 || phase != 0) { Wrong(); return; }
        if (!Need((int)Item.Thread)) return;
        StartCoroutine(AttachArm());
    }

    public void ClickEye()
    {
        if (Locked) return;
        if (step != 4) { Wrong(); return; }
        if (!Need((int)Item.Button)) return;
        StartCoroutine(NewEye());
    }

    public void ClickChest()
    {
        if (Locked) return;
        if (step != 5) { Wrong(); return; }
        if (!Need((int)Item.Heart)) return;
        StartCoroutine(Patch());
    }

    // ---------------- Adimlar ----------------

    IEnumerator Snip(int i)
    {
        busy = true;
        TwSes.EfektCesitli(Ses.Tik);
        var rt = knots[i].rectTransform;
        yield return Tween(0.25f, t => { rt.localScale = Vector3.one * (1f + 0.6f * t); SetAlpha(knots[i], 1f - t); });
        Show(knots[i], false);
        busy = false;
        Progress();
        if (++count < knots.Length) yield break;
        busy = true;
        yield return Tween(0.6f, t =>
        {
            net.rectTransform.anchoredPosition = netHome + new Vector2(0, -140f * t * t);
            SetAlpha(net, 1f - t);
        });
        Show(net, false);
        net.rectTransform.anchoredPosition = netHome;
        busy = false;
        Progress();
        NextStep("Free! But he smells like the sea. Pick the sponge and wash the 4 muddy spots.");
    }

    IEnumerator Wash(int i)
    {
        busy = true;
        TwSes.EfektCesitli(Ses.Zimpara, 0.7f);
        float a = mud[i].color.a;
        yield return Tween(0.35f, t => SetAlpha(mud[i], a * (1f - t)));
        Show(mud[i], false);
        busy = false;
        Progress();
        if (++count < mud.Length) yield break;
        NextStep("Clean! Now the shoulder: pull out the soggy stuffing with the tweezers.");
    }

    IEnumerator PullOld()
    {
        busy = true;
        TwSes.Efekt(Ses.ParcaTak);
        var rt = stuffingOld.rectTransform;
        yield return Tween(0.5f, t => { rt.anchoredPosition = stuffingHome + new Vector2(60f * t, 90f * t); SetAlpha(stuffingOld, 1f - t); });
        Show(stuffingOld, false);
        rt.anchoredPosition = stuffingHome;
        phase = 1;
        busy = false;
        Progress();
        Say(L("Now fill it with recycled fiber from the shelf. It was made from 6 plastic bottles!"));
    }

    IEnumerator Stuff()
    {
        busy = true;
        Deselect();
        TwSes.Efekt(Ses.Pop);
        Show(stuffingNew, true);
        yield return Tween(0.35f, t => stuffingNew.rectTransform.localScale = Vector3.one * EaseOutBack(t));
        busy = false;
        Progress();
        NextStep("Soft and fluffy again. Step 4: pick the rope thread and click the loose arm.");
    }

    IEnumerator AttachArm()
    {
        busy = true;
        Deselect();
        TwSes.Efekt(Ses.ParcaTak);
        var rt = armLoose.rectTransform;
        Vector2 from = rt.anchoredPosition, to = from + new Vector2(-420f, 200f);
        yield return Tween(0.55f, t =>
        {
            rt.anchoredPosition = Vector2.Lerp(from, to, t * t);
            SetAlpha(armLoose, 1f - t * t);
        });
        Show(armLoose, false);
        Show(armAttached, true);
        SetAlpha(armAttached, 0f);
        yield return Tween(0.25f, t => SetAlpha(armAttached, t));
        phase = 1;
        busy = false;
        Progress();
        Say(L("The arm is in place. Now sew it on: pick the needle and click the shoulder."));
    }

    IEnumerator Sew()
    {
        busy = true;
        TwSes.EfektCesitli(Ses.Vida, 0.6f);
        Show(stitches, true);
        SetAlpha(stitches, 0f);
        yield return Tween(0.5f, t => SetAlpha(stitches, t));
        busy = false;
        Progress();
        NextStep("Strong stitches! Step 5: give him a new eye with the sea-glass button.");
    }

    IEnumerator NewEye()
    {
        busy = true;
        Deselect();
        TwSes.Efekt(Ses.Pop);
        Show(eye, true);
        yield return Tween(0.35f, t => eye.rectTransform.localScale = Vector3.one * EaseOutBack(t));
        busy = false;
        Progress();
        NextStep("He can see again! Last step: patch his chest with the sail-cloth heart.");
    }

    IEnumerator Patch()
    {
        busy = true;
        Deselect();
        TwSes.Efekt(Ses.Onay);
        Show(heart, true);
        yield return Tween(0.4f, t => heart.rectTransform.localScale = Vector3.one * EaseOutBack(t));
        MarkAllDone();
        Say(L("Pamuk is as good as new! Let's give him back to Elif."));
        TwSes.Efekt(Ses.OnarimTamam);
        yield return Tween(1.2f, t => teddy.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 4f) * 6f * (1f - t)));
        yield return new WaitForSecondsRealtime(1.4f);
        Complete();
    }

    // ---------------- Yardim icin adim bilgisi ----------------

    protected override int Required() => (int)(step switch
    {
        0 => Item.Scissors,
        1 => Item.Sponge,
        2 => phase == 0 ? Item.Tweezers : Item.Fiber,
        3 => phase == 0 ? Item.Thread : Item.Needle,
        4 => Item.Button,
        5 => Item.Heart,
        _ => Item.None,
    });

    protected override RectTransform TargetRect()
    {
        switch (step)
        {
            case 0: foreach (var k in knots) if (k.gameObject.activeSelf) return k.rectTransform; return null;
            case 1: foreach (var m in mud) if (m.gameObject.activeSelf) return m.rectTransform; return null;
            case 2: return shoulderSpot;
            case 3: return phase == 0 ? armLoose.rectTransform : shoulderSpot;
            case 4: return eyeSpot;
            case 5: return chestSpot;
        }
        return null;
    }

    protected override string StepTools() => step switch
    {
        0 => "Then click the 3 knots of the net.",
        1 => "Then click the muddy spots.",
        2 => "Then click the torn shoulder.",
        3 => phase == 0 ? "Then click the loose arm." : "Then click the shoulder.",
        4 => "Then click where his eye is missing.",
        5 => "Then click his chest.",
        _ => "",
    };

    protected override string Hint() => step switch
    {
        0 => "Step 1: snip the 3 knots of the net with the scissors.",
        1 => "Step 2: wash the 4 muddy spots with the sponge.",
        2 => phase == 0 ? "Step 3: pull the soggy stuffing out of the shoulder with the tweezers." : "Fill the shoulder with recycled fiber.",
        3 => phase == 0 ? "Step 4: pick the rope thread and click the loose arm." : "Sew the arm on: pick the needle and click the shoulder.",
        4 => "Step 5: put the sea-glass button where his eye is missing.",
        5 => "Step 6: patch his chest with the sail-cloth heart.",
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
                for (int i = 0; i < knots.Length; i++)
                    if (knots[i].gameObject.activeSelf) { yield return Snip(i); break; }
                break;
            case 1:
                for (int i = 0; i < mud.Length; i++)
                    if (mud[i].gameObject.activeSelf) { yield return Wash(i); break; }
                break;
            case 2: yield return phase == 0 ? PullOld() : Stuff(); break;
            case 3: yield return phase == 0 ? AttachArm() : Sew(); break;
            case 4: yield return NewEye(); break;
            case 5: yield return Patch(); break;
        }
    }
}
