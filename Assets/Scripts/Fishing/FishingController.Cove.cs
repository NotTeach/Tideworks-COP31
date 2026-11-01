using System.Collections;
using System.Collections.Generic;
using Tideworks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3. gun denizi (Day3Sea sahnesi, coveMode acik): "Pusulanin Gosterdigi Koy". Mert konusma kutusundan konusur.
/// 1 Kayalik havuzlar (yakin plan, tekne yok): 4 havuzda 2'ser cop; dokununca Mert kepceyle alir (+5). Havuz bosalinca canlisi
///   gorunur ve Mert tanitir; canliya dokunulursa uyarir (puan kaybi yok). Saglik %40 -> %64.
/// 2 Pusula (acik deniz, tekne): ibre D'yi gosterir, dogu kenarina kurek cekilir. Yolda 2 cop (istege bagli, +5),
///   bir yavru balik ani (Sal +5 / Al -3); denizde 10 balik yuzer.
/// 3 Gizli koy (yakin plan, tekne yok): dipte 4 cop; ilk dokunusta kabarcik cikar, ikincide Mert ceker (+8).
/// 4 Final: koy parlar, baliklar gelir (10 -> 14), yunus koyun ustunden sicrar, saglik %100; gun sonu karti.
/// </summary>
public partial class FishingController
{
    [Header("Koy (3. gun)")]
    [SerializeField] bool coveMode;
    [SerializeField] GameObject poolsPage;
    [SerializeField] GameObject covePage;
    [SerializeField] Image[] poolTrash;          // havuz i: 2i ve 2i+1
    [SerializeField] Image[] poolRings;
    [SerializeField] Image[] poolCreatures;
    [SerializeField] RectTransform scoop;
    [SerializeField] RectTransform mertPortrait;
    [SerializeField] Text dialogText;
    [SerializeField] Text taskTitle;
    [SerializeField] RectTransform compassHud;
    [SerializeField] RectTransform compassNeedle;
    [SerializeField] GameObject compassBadge;
    [SerializeField] RectTransform mertOnBoat;
    [SerializeField] Image[] hiddenItems;
    [SerializeField] Image[] hiddenRings;
    [SerializeField] Image bubblesTemplate;
    [SerializeField] Image coveFishTemplate;
    [SerializeField] Image coveGlow;
    [SerializeField] RectTransform dolphin;
    [SerializeField] GameObject coveCard;
    [SerializeField] RectTransform coveCardRect;
    [SerializeField] Text cardPools, cardHidden, cardBabies, cardFish;

    enum CovePhase { Pools, Sea, Cove, Done }

    const int PoolReward = 5, HiddenReward = 8, BabyKeepLoss = 3;
    const int CoveFishBefore = 10, CoveFishAfter = 14;
    // Havuz copleri (TrashCatalog.Trash): sise, kutu / altili halka, terlik / sut kutusu, gazete / ip, ton kutusu
    static readonly int[] PoolTrashKinds = { 5, 0, 7, 11, 18, 4, 10, 1 };
    // Gizli copler: eski lastik, ag parcasi, boya kutusu, cam sise
    static readonly int[] HiddenKinds = { 16, 8, 17, 2 };
    // Yol uzerindeki copler: poset, tahta parcasi
    static readonly int[] PathKinds = { 6, 12 };
    static readonly Vector2[] PathSpots = { new Vector2(1180, 520), new Vector2(1760, 880) };
    static readonly Vector2 SeaStart = new Vector2(320, 700);
    const float EastEdge = 2330f, SeaBottom = 1170f;
    // Koyda yuzen baliklar: acik renkli kum elipsinin icinde
    static readonly Vector2 CoveCenter = new Vector2(1275, 755), CoveSwim = new Vector2(820, 330);

    static readonly string[] CreatureLines =
    {
        "A little crab! See how it walks sideways?",
        "Look, a starfish! If it loses an arm, it can grow a new one!",
        "A tiny blenny! It hides in the cracks when the tide goes out.",
        "A sea anemone! It looks like a flower, but it's an animal.",
    };

    bool cove, coveBusy, scooping, babyMomentDone, coveFinale;
    CovePhase phase;
    int scooped, poolsCleaned, pathCollected, hiddenCollected, coveFish = CoveFishBefore;
    readonly bool[] poolTaken = new bool[8];
    readonly int[] hiddenState = new int[4];     // 0 dipte, 1 kabarcik cikti, 2 cekildi
    float compassTarget, compassShown;

    class Swimmer { public RectTransform rt; public Vector2 home; public float phase, speed, dir; }
    readonly List<Swimmer> coveFishes = new List<Swimmer>();

    Vector4[] Rocks => islands;

    /// <summary>Havuz ve koy ekranlari yakin plan: tekne surulmez.</summary>
    bool OnFootPage => cove && phase != CovePhase.Sea;

    /// <summary>Saglik: havuz ve yol copu +3, gizli cop +7.5 (%40 -> %64 -> %70 -> %100).</summary>
    float CoveHealth => coveFinale ? 100f : 40f + 3f * (scooped + pathCollected) + 7.5f * hiddenCollected;

    void SetupCove()
    {
        cove = coveMode;
        if (poolsPage) poolsPage.SetActive(cove);
        if (covePage) covePage.SetActive(false);
        if (coveCard) coveCard.SetActive(false);
        if (!cove) return;

        phase = CovePhase.Pools;
        compassTarget = compassShown = 0f;
        compassBadge.SetActive(false);
        scoop.gameObject.SetActive(false);
        bubblesTemplate.gameObject.SetActive(false);
        coveFishTemplate.gameObject.SetActive(false);
        foreach (var cr in poolCreatures) cr.gameObject.SetActive(false);
        tooltip.gameObject.SetActive(false);
        mobileControls.SetActive(false);
        boat.gameObject.SetActive(false);
        murkAlpha = 0f;                                                      // 3. gun deniz turkuaz
        Say("Grandpa says there's a hidden cove behind these rocks. But first, look at these rock pools! Tap the trash and I'll scoop it out with my net.");
        for (int i = 0; i < PathKinds.Length; i++) AddTrashAt(PathKinds[i], PathSpots[i]);
    }

    void Say(string key)
    {
        if (dialogText) dialogText.text = Loc.T(key);
    }

    Catch AddTrashAt(int kind, Vector2 pos)
    {
        var img = Instantiate(itemTemplate, itemsParent);
        img.gameObject.SetActive(true);
        img.sprite = trashSprites[kind];
        img.SetNativeSize();
        var c = new Catch { rt = img.rectTransform, kind = kind, home = pos, phase = Random.value * 6.28f };
        catches.Add(c);
        img.GetComponent<Button>().onClick.AddListener(() => OnItemClicked(c));
        SetPos(c.rt, pos);
        return c;
    }

    void RefreshCoveTask()
    {
        switch (phase)
        {
            case CovePhase.Pools:
                taskTitle.text = Loc.T("DAY 3 · ROCK POOLS");
                taskText.text = Loc.F("{0} / {1} pieces scooped", scooped, poolTrash.Length);
                break;
            case CovePhase.Sea:
                taskTitle.text = Loc.T("DAY 3 · FOLLOW THE COMPASS");
                taskText.text = Loc.T("Heading east...");
                break;
            default:
                taskTitle.text = Loc.T("DAY 3 · THE HIDDEN COVE");
                taskText.text = Loc.F("{0} / {1} hidden pieces", hiddenCollected, hiddenItems.Length);
                break;
        }
    }

    string CoveTask()
    {
        RefreshCoveTask();
        return taskText.text;
    }

    // ---------------- Her kare ----------------

    void UpdateCove()
    {
        if (!cove || phase != CovePhase.Sea || coveBusy) return;
        if (!babyMomentDone && boatPos.x > 1300f)
        {
            babyMomentDone = true;
            StartCoroutine(BabyMoment());
            return;
        }
        if (boatPos.x > EastEdge && !PromptOpen) StartCoroutine(EnterCove());
    }

    /// <summary>LateUpdate'ten: pusula ibresi, koyun baliklari, teknedeki Mert dik kalir.</summary>
    void KeepMertUpright()
    {
        if (mertOnBoat) mertOnBoat.rotation = Quaternion.identity;
        float t = Time.time;
        compassShown = Mathf.LerpAngle(compassShown, compassTarget, 1f - Mathf.Exp(-4f * Time.deltaTime));
        compassNeedle.localRotation = Quaternion.Euler(0, 0, compassShown + Mathf.Sin(t * 3f) * 2f);
        foreach (var f in coveFishes)
        {
            f.home.x += f.dir * f.speed * Time.deltaTime;
            float dx = (f.home.x - CoveCenter.x) / CoveSwim.x;
            if (Mathf.Abs(dx) > 1f) f.dir = -Mathf.Sign(dx);
            SetPos(f.rt, f.home + new Vector2(0, Mathf.Sin(t * 1.4f + f.phase) * 10f));
            f.rt.localScale = new Vector3(f.dir < 0 ? -1f : 1f, 1f, 1f);
        }
    }

    /// <summary>Acik deniz: kiyi ve ada yok; ust bar ve alttaki konusma kutusu sinir.</summary>
    bool OpenSeaBlocked(Vector2 p, float r, bool hud)
    {
        if (!hud) return false;
        return p.y - r < hudTop || p.y + r > SeaBottom || p.x - r < 0f || p.x + r > playArea.x;
    }

    // ---------------- Kayalik havuzlar ----------------

    Vector2 ArtPos(RectTransform rt) => new Vector2(rt.anchoredPosition.x, -rt.anchoredPosition.y);

    /// <summary>Herhangi bir UI ogesinin artboard konumu (konusma kutusunun icindeki portre gibi).</summary>
    Vector2 ToArt(RectTransform rt)
    {
        Vector2 p = itemsParent.InverseTransformPoint(rt.TransformPoint(rt.rect.center));
        return new Vector2(p.x + itemsParent.rect.width / 2f, itemsParent.rect.height / 2f - p.y);
    }

    /// <summary>Havuzdaki bir cope dokunuldu: Mert kepceyle cikarir.</summary>
    public void ClickPoolTrash(int i)
    {
        if (!cove || phase != CovePhase.Pools || scooping || coveBusy || poolTaken[i]) return;
        StartCoroutine(Scoop(i));
    }

    IEnumerator Scoop(int i)
    {
        scooping = true;
        poolTaken[i] = true;
        var trash = poolTrash[i].rectTransform;
        Vector2 target = ArtPos(trash), mert = ToArt(mertPortrait);
        scoop.gameObject.SetActive(true);
        scoop.SetAsLastSibling();
        TwSes.Efekt(Ses.AgAt, 0.6f);
        yield return Tween(0.35f, t =>
        {
            SetPos(scoop, Vector2.Lerp(mert, target, t));
            scoop.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(40f, -15f, t));
        });
        TwSes.Efekt(Ses.GecisKabarcik, 0.6f);
        poolRings[i].gameObject.SetActive(false);
        yield return Tween(0.45f, t =>
        {
            var p = Vector2.Lerp(target, mert, t * t);
            SetPos(scoop, p);
            SetPos(trash, p + new Vector2(0, -30f));
            trash.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, t);
        });
        scoop.gameObject.SetActive(false);
        trash.gameObject.SetActive(false);

        scooped++;
        PlayerWallet.Add(PoolReward);
        bag.Add(PoolTrashKinds[i]);
        TwSes.EfektCesitli(Ses.AtikTopla, 0.8f);
        ShowFloating("+" + PoolReward, target, new Color32(255, 214, 92, 255));

        int pool = i / 2;
        if (poolTaken[pool * 2] && poolTaken[pool * 2 + 1])
        {
            poolsCleaned++;
            var cr = poolCreatures[pool];
            cr.gameObject.SetActive(true);
            TwSes.Efekt(Ses.Pop);
            StartCoroutine(Tween(0.4f, t => cr.rectTransform.localScale = Vector3.one * EaseOutBack(t)));
            Say(CreatureLines[pool]);
        }
        RefreshHud();
        scooping = false;
        if (scooped == poolTrash.Length) StartCoroutine(CompassPoints());
    }

    /// <summary>Havuz canlisina dokunuldu: Mert uyarir, puan kaybi yok.</summary>
    public void ClickCreature(int i)
    {
        if (!cove || coveBusy) return;
        TwSes.Efekt(Ses.Hata, 0.4f);
        Say("Don't touch! Let's leave them in their home.");
        var cr = poolCreatures[i].rectTransform;
        StartCoroutine(Tween(0.3f, t => cr.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 4f) * 10f * (1f - t))));
    }

    // ---------------- Pusula ve acik deniz ----------------

    IEnumerator CompassPoints()
    {
        coveBusy = true;
        yield return new WaitForSeconds(2.2f);
        // Pusula titrer, ibre D'yi (dogu) gosterir
        TwSes.Efekt(Ses.GecisParilti, 0.8f);
        yield return Tween(0.7f, t => compassHud.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 40f) * 6f * (1f - t)));
        compassTarget = -90f;
        compassBadge.SetActive(true);
        Say("The compass is pointing somewhere! Let's go!");
        yield return new WaitForSeconds(2f);

        TwSes.Efekt(Ses.GecisDalga, 0.8f);
        yield return Fade(screenFader, 0f, 1f, true);
        poolsPage.SetActive(false);
        boat.gameObject.SetActive(true);
        boatPos = SeaStart;
        boatAngle = 180f;                                                   // burun doguya (heading 0)
        forwardSpeed = 0f;
        velocity = Vector2.zero;
        PlaceBoat();
        SpawnCoveFish();
        mobileControls.SetActive(IsMobile);
        phase = CovePhase.Sea;
        RefreshHud();
        yield return Fade(screenFader, 1f, 0f);
        Say("Follow the compass east! Grab anything floating on the way.");
        coveBusy = false;
    }

    /// <summary>Acik denizde 10 balik yuzer (2. gunden kalanlar).</summary>
    void SpawnCoveFish()
    {
        int have = 0;
        foreach (var c in catches) if (c.fish && !c.taken) have++;
        var rng = new System.Random(33);
        for (int i = have; i < CoveFishBefore; i++)
        {
            var p = new Vector2(Mathf.Lerp(500f, 2300f, (float)rng.NextDouble()), Mathf.Lerp(260f, 1080f, (float)rng.NextDouble()));
            AddFish(TrashCatalog.FishReturnOrder[i % TrashCatalog.FishReturnOrder.Length], FishSpotNear(p), false);
        }
    }

    IEnumerator BabyMoment()
    {
        yield return BabyChoice("A baby fish is caught on a floating line! What will you do?", PoolReward, BabyKeepLoss,
            "You protected the ecosystem!", "It was too small!");
    }

    /// <summary>Landed'dan: yol uzerindeki copler sayilir.</summary>
    IEnumerator CoveLanded(List<Catch> got)
    {
        pathCollected += got.Count;
        RefreshHud();
        yield break;
    }

    // ---------------- Gizli koy ----------------

    IEnumerator EnterCove()
    {
        coveBusy = true;
        phase = CovePhase.Cove;
        forwardSpeed = 0f;
        velocity = Vector2.zero;
        TwSes.Efekt(Ses.GecisDalga, 0.8f);
        yield return Fade(screenFader, 0f, 1f, true);
        foreach (var c in catches)
            if (!c.taken && !c.fish) { c.taken = true; c.rt.gameObject.SetActive(false); }   // istege bagli yol copleri kalir
        mobileControls.SetActive(false);
        covePage.SetActive(true);
        SetAlpha(coveGlow, 0f);
        dolphin.gameObject.SetActive(false);
        for (int i = 0; i < hiddenItems.Length; i++) SetAlpha(hiddenItems[i], 0.45f);   // dipte, soluk
        for (int i = 0; i < CoveFishBefore; i++) AddCoveFish(i, false);
        RefreshHud();
        yield return Fade(screenFader, 1f, 0f);
        Say("This is the place Grandpa told me about! But there's something down there...");
        coveBusy = false;
    }

    void AddCoveFish(int i, bool fromEntrance)
    {
        var img = Instantiate(coveFishTemplate, coveFishTemplate.transform.parent);
        img.gameObject.SetActive(true);
        img.sprite = fishSprites[TrashCatalog.FishReturnOrder[i % TrashCatalog.FishReturnOrder.Length]];
        img.SetNativeSize();
        img.rectTransform.sizeDelta *= 0.5f;
        var rng = new System.Random(100 + i);
        var home = CoveCenter + new Vector2(((float)rng.NextDouble() * 2f - 1f) * CoveSwim.x, ((float)rng.NextDouble() * 2f - 1f) * CoveSwim.y);
        if (fromEntrance) home.x = CoveCenter.x - CoveSwim.x;
        coveFishes.Add(new Swimmer { rt = img.rectTransform, home = home, phase = i * 1.3f,
                                     speed = Mathf.Lerp(40f, 80f, (float)rng.NextDouble()), dir = rng.NextDouble() < 0.5 ? -1f : 1f });
        if (fromEntrance)
        {
            coveFishes[coveFishes.Count - 1].dir = 1f;
            StartCoroutine(Tween(1f, t => SetAlpha(img, t)));
        }
    }

    /// <summary>Dipteki bir cope dokunuldu: once kabarcik cikar ve belirginlesir, ikinci dokunusta Mert ceker.</summary>
    public void ClickHidden(int i)
    {
        if (!cove || phase != CovePhase.Cove || coveBusy || scooping || hiddenState[i] == 2) return;
        if (hiddenState[i] == 0)
        {
            hiddenState[i] = 1;
            TwSes.Efekt(Ses.GecisKabarcik, 0.9f);
            StartCoroutine(Bubbles(ArtPos(hiddenItems[i].rectTransform)));
            var img = hiddenItems[i];
            StartCoroutine(Tween(0.6f, t => SetAlpha(img, 0.45f + 0.55f * t)));
            Say("Bubbles! Something is coming up... tap again to pull it out.");
            return;
        }
        StartCoroutine(PullHidden(i));
    }

    IEnumerator Bubbles(Vector2 at)
    {
        for (int k = 0; k < 3; k++)
        {
            StartCoroutine(OneBubble(at + new Vector2(k * 22f - 22f, 0)));
            yield return new WaitForSeconds(0.25f);
        }
    }

    IEnumerator OneBubble(Vector2 at)
    {
        var b = Instantiate(bubblesTemplate, bubblesTemplate.transform.parent);
        b.gameObject.SetActive(true);
        yield return Tween(1.2f, t =>
        {
            SetPos(b.rectTransform, at + new Vector2(Mathf.Sin(t * 9f) * 8f, -110f * t));
            SetAlpha(b, t < 0.7f ? 1f : (1f - t) / 0.3f);
        });
        Destroy(b.gameObject);
    }

    IEnumerator PullHidden(int i)
    {
        scooping = true;
        hiddenState[i] = 2;
        var item = hiddenItems[i].rectTransform;
        hiddenRings[i].gameObject.SetActive(false);
        Vector2 from = ArtPos(item), mert = ToArt(mertPortrait);
        TwSes.Efekt(Ses.AgAt, 0.8f);
        yield return Tween(0.6f, t =>
        {
            SetPos(item, Vector2.Lerp(from, mert, t * t) + new Vector2(0, -120f * Mathf.Sin(t * Mathf.PI)));
            item.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, t);
        });
        item.gameObject.SetActive(false);
        hiddenCollected++;
        PlayerWallet.Add(HiddenReward);
        bag.Add(HiddenKinds[i]);
        TwSes.EfektCesitli(Ses.AtikTopla, 0.8f);
        ShowFloating("+" + HiddenReward, from, new Color32(255, 214, 92, 255));
        RefreshHud();
        scooping = false;
        if (hiddenCollected == hiddenItems.Length) StartCoroutine(Finale());
    }

    IEnumerator Finale()
    {
        coveBusy = true;
        phase = CovePhase.Done;
        coveFinale = true;
        TwSes.Efekt(Ses.OnarimTamam);
        // Koyun suyu parlar, baliklar koya akin eder: 10 -> 14
        StartCoroutine(Tween(2.4f, t => SetAlpha(coveGlow, 0.55f * Mathf.Sin(t * Mathf.PI) + 0.2f * t)));
        for (int i = 0; i < CoveFishAfter - CoveFishBefore; i++)
        {
            AddCoveFish(CoveFishBefore + i, true);
            coveFish++;
            RefreshHud();
            TwSes.Efekt(Ses.GecisKabarcik, 0.5f);
            yield return new WaitForSeconds(0.45f);
        }
        yield return DolphinLeap();
        Say("Grandpa was right! This place is alive!");
        RefreshHud();
        yield return new WaitForSeconds(2.8f);
        ShowCoveCard();
        coveBusy = false;
    }

    /// <summary>Koyun yunusu: koyun ustunden tek bir sicrayis.</summary>
    IEnumerator DolphinLeap()
    {
        dolphin.gameObject.SetActive(true);
        var img = dolphin.GetComponent<Image>();
        Vector2 a = new Vector2(620f, 900f), b = new Vector2(1940f, 820f);
        TwSes.Efekt(Ses.GecisDalga, 0.7f);
        StartCoroutine(Bubbles(a));
        yield return Tween(1.6f, t =>
        {
            var p = Vector2.Lerp(a, b, t) + new Vector2(0, -420f * Mathf.Sin(t * Mathf.PI));
            SetPos(dolphin, p);
            dolphin.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(30f, -30f, t));
            SetAlpha(img, Mathf.Min(1f, Mathf.Min(t, 1f - t) * 8f));
        });
        StartCoroutine(Bubbles(b));
        dolphin.gameObject.SetActive(false);
    }

    // ---------------- Gun sonu ----------------

    void ShowCoveCard()
    {
        sorting = true;                                                      // kart acik
        cardPools.text = poolsCleaned + " / " + poolCreatures.Length;
        cardHidden.text = hiddenCollected + " × " + HiddenReward;
        cardBabies.text = babiesReleased + " / 1";
        cardFish.text = CoveFishBefore + " » " + coveFish;
        coveCard.SetActive(true);
        StartCoroutine(Tween(0.35f, t => coveCardRect.localScale = Vector3.one * EaseOutBack(t)));
    }

    /// <summary>Gun sonu kartindaki "Sort the catch": toplananlar ayristirma sahnesine gider.</summary>
    public void CoveSortCatch()
    {
        if (leaving) return;
        leaving = true;
        TwSes.TekneMotoru(false);
        StartCoroutine(GoSort());
    }

    static void SetAlpha(Graphic g, float a)
    {
        if (!g) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
