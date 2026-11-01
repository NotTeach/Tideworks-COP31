using System.Collections;
using System.Collections.Generic;
using Tideworks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dukkan aletleri ve 2. gunun firtina denizi (Day2Sea sahnesi, stormMode acik).
/// Aletler: buyuk ag (atisin cevresindeki butun copler), saglam olta (2 kat hizli), gunes motoru (daha hizli tekne).
/// Firtina: 6 hayalet ag (+8, agir), ruzgar copleri kiyiya surukler (kiyiya varan cop -2 saglik),
/// 3 agdan yavru balik cikar (Sal / Al secimi), son agda tekne altindan balik surusu gecer.
/// </summary>
public partial class FishingController
{
    [Header("Dukkan aletleri")]
    [SerializeField] GameObject shopButton;
    [SerializeField] Image equippedIcon;
    [SerializeField] Text equippedName;
    [SerializeField] Sprite rodIcon, sturdyRodIcon, netIcon;
    [SerializeField] RectTransform netCircle;
    [Tooltip("Buyuk agin yaricapi (artboard pikseli)")]
    [SerializeField] float netRadius = 190f;

    [Header("Firtina (2. gun)")]
    [SerializeField] bool stormMode;
    [SerializeField] Image stormTint;
    [SerializeField] Image[] cloudShadows;
    [SerializeField] Image sunGlow;
    [SerializeField] float windSpeed = 7f;
    [SerializeField] GameObject babyPrompt;
    [SerializeField] RectTransform babyPromptCard;
    [SerializeField] Image babyPromptFish;
    [SerializeField] Text babyPromptText;
    [SerializeField] GameObject babyButtons;

    const int GhostNets = 6, NormalTrashInStorm = 7, GhostReward = 8, TrashReward = 5, BabyCoins = 5, ShoreDamage = 2;
    const int GhostKind = 8;              // TrashCatalog.Trash: "TrashGhostNet"
    const int YesterdaysFish = 5;
    const int SurvivorsAfterStorm = 2;
    const int SchoolSize = 6;

    static readonly string[] ReleaseLines =
    {
        "Bye little one! Grow big and strong in the clean water.",
        "It's swimming back to its family. Well done!",
        "The sea needs every little fish. Great choice!",
    };

    static readonly string[] KeepLines =
    {
        "Oh no... Baby fish can't grow up in a bucket.",
        "The sea just lost a little fish. Next time, let it go.",
        "Elif looks sad. Little fish belong in the sea.",
    };

    bool storm;
    int ghostCollected, lostToShore, babiesReleased, babiesKept, schoolFish, fishAtDayStart;
    bool shopHintShown;
    int babyChoice;                       // 0 bekliyor, 1 sal, -1 al
    float stormTarget = 1f, stormShown = 1f;
    Vector2[] cloudHomes;

    bool PromptOpen => babyPrompt && babyPrompt.activeSelf;
    bool NetEquipped => EquipmentState.CurrentTool == EquipmentState.Net;
    float ReelFactor => ShopManager.Owns("sturdy_rod") && !NetEquipped ? 0.5f : 1f;
    float MotorBoost => ShopManager.Owns("solar_motor") ? 1.35f : 1f;

    /// <summary>Denizde kalan (toplanmamis, kiyiya vurmamis) cop sayisi.</summary>
    int Remaining
    {
        get
        {
            int n = 0;
            foreach (var c in catches) if (!c.fish && !c.taken) n++;
            return n;
        }
    }

    /// <summary>Firtinada: yesterday'in 2 hayatta kalani + salinan yavrular + balik surusu.</summary>
    int FishCount => cove ? coveFish : SurvivorsAfterStorm + babiesReleased + schoolFish;

    float CurrentHealth
    {
        get
        {
            if (cove) return CoveHealth;
            float h = Mathf.Lerp(healthStart, healthTarget, bag.Count / (float)Mathf.Max(1, TrashTotal));
            return Mathf.Clamp(h - lostToShore * ShoreDamage, 0f, 100f);
        }
    }

    /// <summary>Gunun copleri: (tur, hayalet ag mi, icinde yavru var mi).</summary>
    IEnumerable<(int, bool, bool)> TodaysCatch()
    {
        if (cove) yield break;                 // 3. gunun copleri sabit yerlerde: SetupCove
        if (!storm)
        {
            for (int i = 0; i < TrashTotal; i++) yield return (i, false, false);
            yield break;
        }
        for (int i = 0; i < GhostNets; i++) yield return (GhostKind, true, i % 2 == 1);
        int added = 0;
        for (int i = 0; added < NormalTrashInStorm && i < trashSprites.Length; i++)
        {
            if (i == GhostKind) continue;
            yield return (i, false, false);
            added++;
        }
    }

    // ---------------- Aletler ----------------

    void SetupGear()
    {
        if (shopButton) shopButton.SetActive(ShopManager.IsOpen);
        if (netCircle)
        {
            netCircle.sizeDelta = Vector2.one * netRadius * 2f;
            netCircle.gameObject.SetActive(false);
        }
        EquipmentState.OnChanged += OnGearChanged;
        ShopManager.OnPurchased += OnPurchased;
        PlayerWallet.OnChanged += OnCoinsChanged;
        RefreshGear();
    }

    void TeardownGear()
    {
        EquipmentState.OnChanged -= OnGearChanged;
        ShopManager.OnPurchased -= OnPurchased;
        PlayerWallet.OnChanged -= OnCoinsChanged;
    }

    void OnGearChanged(string tool) => RefreshGear();

    void OnPurchased(ShopItem item) => RefreshGear();

    void OnCoinsChanged(int coins) => coinText.text = coins.ToString();

    void RefreshGear()
    {
        if (!equippedIcon) return;
        bool net = NetEquipped, sturdy = ShopManager.Owns("sturdy_rod");
        equippedIcon.sprite = net ? netIcon : sturdy ? sturdyRodIcon : rodIcon;
        equippedName.text = Loc.T(net ? "Big net" : sturdy ? "Sturdy rod" : "Grandpa's rod");
    }

    /// <summary>Ust bardaki "Shop" butonu.</summary>
    public void OpenShop()
    {
        if (sorting || leaving || ShopPanel.IsOpen) return;
        if (!ShopManager.IsOpen)
        {
            ShowFloating(Loc.T("The shop opens soon! Keep saving your coins."), boatPos, Color.white);
            return;
        }
        ShopPanel.Open(RefreshGear);
    }

    /// <summary>Sol alttaki "Equipped" karti: ag varsa olta ve ag arasinda gecer, yoksa dukkani acar.</summary>
    public void EquippedClicked()
    {
        if (sorting || leaving || Paused) return;
        if (!ShopManager.Owns("big_net")) { OpenShop(); return; }
        EquipmentState.Equip(NetEquipped ? EquipmentState.Rod : EquipmentState.Net);
        TwSes.Efekt(Ses.Tik);
        ShowFloating(Loc.F("{0} equipped.", equippedName.text), boatPos, new Color32(255, 236, 160, 255));
    }

    /// <summary>Ag kusaniliysa: nisan noktasinda kesik cizgili daire (agin kapsadigi alan).</summary>
    void UpdateNetAim()
    {
        if (!netCircle) return;
        bool show = NetEquipped && !casting;
        netCircle.gameObject.SetActive(show);
        if (!show) return;
        SetPos(netCircle, NetAim());
        netCircle.localRotation = Quaternion.Euler(0, 0, Time.time * 12f);
    }

    /// <summary>Ag nereye atilir: menzildeki en yakin cop, yoksa burnun onu.</summary>
    Vector2 NetAim()
    {
        Catch best = null;
        float bestD = castRange;
        foreach (var c in catches)
        {
            if (c.taken || c.fish) continue;
            float d = Vector2.Distance(c.home, boatPos);
            if (d < bestD) { bestD = d; best = c; }
        }
        if (best != null) return best.home;
        float a = (boatAngle - 180f) * Mathf.Deg2Rad;
        return boatPos + new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * 240f;
    }

    void CastNet() => CastNet(NetAim());

    void CastNet(Vector2 at) => StartCoroutine(NetRoutine(at));

    IEnumerator NetRoutine(Vector2 at)
    {
        casting = true;
        TwSes.Efekt(Ses.AgAt, 0.9f);
        Vector2 start = boatPos;
        yield return Tween(0.3f, t => DrawLine(start, Vector2.Lerp(start, at, t)));
        Splash(at);

        var hits = new List<Catch>();
        foreach (var c in catches)
            if (!c.taken && !c.fish && Vector2.Distance(c.home, at) <= netRadius) { c.taken = true; hits.Add(c); }
        if (hits.Count == 0)
        {
            ShowFloating(Loc.T("The net came up empty. Aim closer to the trash!"), at, Color.white);
            yield return new WaitForSeconds(0.35f);
            line.gameObject.SetActive(false);
            bobber.gameObject.SetActive(false);
            casting = false;
            yield break;
        }

        var from = new List<Vector2>();
        foreach (var c in hits) from.Add(c.home);
        bool heavy = hits.Exists(c => c.ghost);
        yield return Tween(heavy ? 0.9f : 0.55f, t =>
        {
            for (int i = 0; i < hits.Count; i++)
            {
                var p = Vector2.Lerp(from[i], boatPos, t * t);
                SetPos(hits[i].rt, p);
                hits[i].rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, t);
            }
            DrawLine(boatPos, Vector2.Lerp(at, boatPos, t * t));
        });
        line.gameObject.SetActive(false);
        bobber.gameObject.SetActive(false);
        foreach (var c in hits) c.rt.gameObject.SetActive(false);

        yield return Landed(hits);
        casting = false;
    }

    /// <summary>Tekneye cekilen copler: odul, canta, balik donusu; firtinada yavru ve suru anlari.</summary>
    IEnumerator Landed(List<Catch> got)
    {
        int reward = 0, ghostsBefore = ghostCollected;
        foreach (var c in got)
        {
            reward += c.ghost ? GhostReward : TrashReward;
            bag.Add(c.kind);
            if (c.ghost) ghostCollected++;
            CheckFishReturn(c.home);
        }
        PlayerWallet.Add(reward);
        TwSes.EfektCesitli(Ses.AtikTopla, 0.8f);
        ShowFloating("+" + reward, boatPos, new Color32(255, 214, 92, 255));
        RefreshHud();

        if (storm)
        {
            foreach (var c in got)
                if (c.baby) yield return BabyChoice();
            if (!shopHintShown && ghostsBefore < 3 && ghostCollected >= 3 && !ShopManager.Owns("big_net"))
            {
                shopHintShown = true;
                Radio("Elif (radio): So many nets! Grandpa's shop had a big net, maybe it'll make this easier?");
                if (shopButton) StartCoroutine(Pulse(shopButton.transform));
            }
            if (ghostsBefore < GhostNets && ghostCollected >= GhostNets) yield return FishSchool();
        }
        if (cove) yield return CoveLanded(got);

        if (!cove && Remaining == 0) StartCoroutine(AllCollected());
    }

    // ---------------- Firtina ----------------

    void SetupStorm()
    {
        storm = stormMode;
        foreach (var img in cloudShadows ?? new Image[0]) if (img) img.gameObject.SetActive(storm);
        if (stormTint) stormTint.gameObject.SetActive(storm);
        if (sunGlow) sunGlow.gameObject.SetActive(storm);
        if (babyPrompt) babyPrompt.SetActive(false);
        if (!storm) return;

        fishAtDayStart = YesterdaysFish;
        tooltipLine.text = Loc.T("Elif (radio): The storm dragged all the nets into the bay! Just like the ones that caught Pamuk...");
        cloudHomes = new Vector2[cloudShadows.Length];
        for (int i = 0; i < cloudShadows.Length; i++)
        {
            var p = cloudShadows[i].rectTransform.anchoredPosition;
            cloudHomes[i] = new Vector2(p.x, -p.y);
        }
        stormShown = stormTarget = 1f;
        ApplyStormLook();
    }

    /// <summary>Temizlendikce bulutlar dagilir, gunes cikar.</summary>
    void RefreshStorm()
    {
        if (!storm) return;
        stormTarget = Remaining / (float)Mathf.Max(1, TrashTotal);
    }

    void LateUpdate()
    {
        if (cove) KeepMertUpright();
        if (!storm) return;
        stormShown = Mathf.MoveTowards(stormShown, stormTarget, Time.deltaTime * 0.25f);
        ApplyStormLook();
    }

    void ApplyStormLook()
    {
        float s = stormShown, t = Time.time;
        if (stormTint) { var c = stormTint.color; c.a = 0.42f * s; stormTint.color = c; }
        for (int i = 0; i < cloudShadows.Length; i++)
        {
            var img = cloudShadows[i];
            var c = img.color; c.a = 0.35f * s; img.color = c;
            // Bulutlar saga dogru kayar; dagildikca ekrandan cikar
            float x = Mathf.Repeat(cloudHomes[i].x + t * (14f + i * 3f) + (1f - s) * 900f, Art.x + 900f) - 450f;
            SetPos(img.rectTransform, new Vector2(x, cloudHomes[i].y));
        }
        if (sunGlow)
        {
            var c = sunGlow.color; c.a = Mathf.SmoothStep(0f, 0.85f, 1f - s); sunGlow.color = c;
            sunGlow.rectTransform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 0.8f));
        }
    }

    static readonly Vector2 Art = new Vector2(2560, 1440);

    /// <summary>Ruzgar copleri yavasca kiyiya iter; kiyiya varan cop sahili kirletir (-2 saglik).</summary>
    void Drift(Catch c, float dt)
    {
        if (c.taken || Paused || sorting || leaving) return;
        c.home += new Vector2(-windSpeed, Mathf.Sin(Time.time * 0.3f + c.phase) * 2f) * dt;
        if (c.home.x - 40f > ShoreX(c.home.y) + 12f) return;
        c.taken = true;
        lostToShore++;
        TwSes.Efekt(Ses.Hata, 0.6f);
        ShowFloating(Loc.F("Trash washed onto the beach! -{0} ocean health", ShoreDamage), c.home + new Vector2(260f, 0),
            new Color32(255, 190, 170, 255));
        StartCoroutine(Sink(c));
        RefreshHud();
        if (Remaining == 0 && !casting) StartCoroutine(AllCollected());
    }

    IEnumerator Sink(Catch c)
    {
        var img = c.rt.GetComponent<Image>();
        yield return Tween(0.8f, t => { var col = img.color; col.a = 1f - t; img.color = col; });
        c.rt.gameObject.SetActive(false);
    }

    void Radio(string key)
    {
        tooltipLine.text = Loc.T(key);
        hintTimer = 0f;
        TwSes.Efekt(Ses.Onay, 0.5f);
    }

    IEnumerator Pulse(Transform t)
    {
        for (int i = 0; i < 4; i++)
            yield return Tween(0.5f, k => t.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(k * Mathf.PI)));
    }

    /// <summary>
    /// Yavru balik karti: Sal (+altin, olumlu soz) ya da Al (-altin, olumsuz soz).
    /// 2. gun aga takilan yavrular; 3. gun yolda (question, altinlar ve sozler verilir).
    /// </summary>
    IEnumerator BabyChoice(string question = "A baby fish is tangled in the net! What will you do?",
        int releaseCoins = BabyCoins, int keepCoins = BabyCoins, string releaseLine = null, string keepLine = null)
    {
        if (!babyPrompt) yield break;
        int species = TrashCatalog.FishReturnOrder[(babiesReleased + babiesKept) % TrashCatalog.FishReturnOrder.Length];
        babyPromptFish.sprite = fishSprites[species];
        babyPromptText.text = Loc.T(question);
        babyChoice = 0;
        if (babyButtons) babyButtons.SetActive(true);
        babyPrompt.SetActive(true);
        TwSes.Efekt(Ses.GecisKabarcik, 0.8f);
        yield return Tween(0.3f, t => babyPromptCard.localScale = Vector3.one * EaseOutBack(t));
        while (babyChoice == 0) yield return null;

        // Secim hemen gorunsun: butonlar kalkar, sonuc cumlesi ve altin karta yazilir
        if (babyButtons) babyButtons.SetActive(false);
        int n = babiesReleased + babiesKept;
        if (babyChoice > 0)
        {
            babiesReleased++;
            PlayerWallet.Add(releaseCoins);
            TwSes.Efekt(Ses.Onay);
            babyPromptText.text = Loc.T(releaseLine ?? ReleaseLines[n % ReleaseLines.Length]) + "\n<color=#C88A10>+" + releaseCoins + "</color>";
            ShowFloating("+" + releaseCoins, boatPos, new Color32(255, 214, 92, 255));
        }
        else
        {
            babiesKept++;
            int lose = Mathf.Min(keepCoins, PlayerWallet.Coins);
            PlayerWallet.Add(-lose);
            TwSes.Efekt(Ses.Hata, 0.7f);
            babyPromptText.text = Loc.T(keepLine ?? KeepLines[n % KeepLines.Length]) + (lose > 0 ? "\n<color=#C0442E>-" + lose + "</color>" : "");
            if (lose > 0) ShowFloating("-" + lose, boatPos, new Color32(255, 150, 130, 255));
        }
        RefreshHud();
        yield return Tween(0.18f, t => babyPromptCard.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * Mathf.PI)));
        yield return new WaitForSeconds(1.6f);
        yield return Tween(0.2f, t => babyPromptCard.localScale = Vector3.one * (1f - t));
        babyPrompt.SetActive(false);

        if (babyChoice > 0)
        {
            AddFish(species, FishSpotNear(boatPos + new Vector2(0, 170f)), true);
            catches[catches.Count - 1].rt.sizeDelta *= 0.55f;
        }
    }

    /// <summary>Yavru balik karti: "Release" butonu.</summary>
    public void ReleaseBaby() { if (babyChoice == 0) babyChoice = 1; }

    /// <summary>Yavru balik karti: "Keep" butonu.</summary>
    public void KeepBaby() { if (babyChoice == 0) babyChoice = -1; }

    /// <summary>Son hayalet ag da cikinca teknenin altindan kucuk bir balik surusu gecer.</summary>
    IEnumerator FishSchool()
    {
        Radio("That's what the notebook said... They're dancing!");
        ShowFloating(Loc.T("That's what the notebook said... They're dancing!"), boatPos + new Vector2(0, -150f),
            new Color32(190, 255, 236, 255));
        TwSes.Efekt(Ses.GecisParilti, 0.8f);
        var school = new List<Image>();
        for (int i = 0; i < SchoolSize; i++)
        {
            var img = Instantiate(itemTemplate, itemsParent);
            img.gameObject.SetActive(true);
            img.raycastTarget = false;
            img.sprite = fishSprites[TrashCatalog.FishReturnOrder[0]];
            img.SetNativeSize();
            img.rectTransform.sizeDelta *= 0.38f;
            img.rectTransform.SetSiblingIndex(boat.GetSiblingIndex());
            school.Add(img);
        }
        var from = boatPos + new Vector2(520f, 60f);
        var to = boatPos + new Vector2(-520f, -40f);
        yield return Tween(3.2f, t =>
        {
            for (int i = 0; i < school.Count; i++)
            {
                float k = Mathf.Clamp01(t * 1.15f - i * 0.03f);
                var p = Vector2.Lerp(from, to, k) + new Vector2(i % 3 * 46f, (i / 3) * 52f - 26f)
                        + new Vector2(0, Mathf.Sin(k * 14f + i) * 18f);
                SetPos(school[i].rectTransform, p);
                school[i].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 14f + i) * 12f);
                var c = school[i].color; c.a = Mathf.Min(1f, Mathf.Min(k, 1f - k) * 6f); school[i].color = c;
            }
        });
        foreach (var img in school) Destroy(img.gameObject);
        schoolFish = SchoolSize - 1;      // suruden 5'i koyda kalir
        RefreshHud();
    }
}
