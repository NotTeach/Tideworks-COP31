using System;
using System.Collections;
using System.Collections.Generic;
using Tideworks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gorev 1 - Denizi temizle: tekneyle dolas, oltayla copleri (+5) topla. Balik avlanmaz:
/// copler azaldikca temizlenen yerlerde balik turleri geri doner (okyanus sagligi gunde ~%35 artar).
/// Bilgisayar: yukari/W ileri, asagi/S geri, sol-sag/A-D dumen, E (veya Space) ile olta at, ya da bir esyaya tikla.
/// Telefon: joystick ile kurek cek, olta butonuyla at. Adalara ve kumsala carpinca tekne geri seker.
/// Butun copler toplaninca ayristirma sahnesi (Recycling), ardindan kumsal (Beach) gelir.
/// Konumlar artboard pikselidir: x saga, y asagi, sol ust kose (0,0).
/// </summary>
public class FishingController : MonoBehaviour
{
    [Header("Sahneler")]
    [SerializeField] string menuSceneName = "MainMenu";
    [SerializeField] string recyclingSceneName = "Recycling";

    [Header("Sayfalar")]
    [SerializeField] CanvasGroup seaPage;
    [SerializeField] CanvasGroup screenFader;

    [Header("Deniz")]
    [SerializeField] RectTransform boat;
    [SerializeField] RectTransform itemsParent;
    [SerializeField] Image itemTemplate;
    [SerializeField] Sprite[] trashSprites;   // TrashCatalog.Trash sirasiyla
    [SerializeField] Sprite[] fishSprites;    // TrashCatalog.FishSprites sirasiyla
    [SerializeField] Image murk;
    [SerializeField] float murkAlpha = 1f;
    [SerializeField] Vector2 boatStart = new Vector2(1250, 760);
    [Header("Tekne (kayik gibi)")]
    [Tooltip("En yuksek hiz (artboard pikseli / saniye)")]
    [SerializeField] float maxSpeed = 250f;
    [Tooltip("Kurek cekince hizlanma")]
    [SerializeField] float acceleration = 120f;
    [Tooltip("Su direnci: buyudukce daha cabuk durur")]
    [SerializeField] float waterDrag = 0.55f;
    [Tooltip("Tam hizda donus (derece / saniye)")]
    [SerializeField] float turnRate = 48f;
    [Tooltip("Dururken donus (derece / saniye)")]
    [SerializeField] float minTurnRate = 20f;
    [SerializeField] float turnAcceleration = 70f;
    [Tooltip("Omurga: yana kaymayi ne kadar cabuk duzeltir")]
    [SerializeField] float keel = 1.8f;
    [SerializeField] float boatRadius = 78f;
    [SerializeField] float castRange = 380f;
    [Tooltip("Adalar: merkez x, y ve yaricap x, y (Tools/MenuArt/fishing/sea_layout.json)")]
    [SerializeField] Vector4[] islands =
    {
        new Vector4(1381, 505, 74, 42), new Vector4(1975, 545, 190, 86), new Vector4(2080, 1262, 350, 165),
    };
    [SerializeField] Vector2 playArea = new Vector2(2560, 1440);
    [SerializeField] float hudTop = 170f, hudBottom = 150f;

    [Header("Olta")]
    [SerializeField] RectTransform line;
    [SerializeField] RectTransform bobber;
    [SerializeField] RectTransform splash;
    [SerializeField] Text floatingText;

    [Header("Ust bar")]
    [SerializeField] Text coinText;
    [SerializeField] RectTransform healthFill;
    [SerializeField] float healthWidth = 340f;
    [SerializeField] Text healthText;
    [SerializeField] Text taskText;
    [SerializeField] Text fishText;
    [SerializeField] CanvasGroup tooltip;
    [SerializeField] Text tooltipLine;

    [Header("Alt bar")]
    [SerializeField] Image[] bagIcons;
    [SerializeField] Text bagCount;
    [SerializeField] Button sortButton;

    [Header("Deniz hareketi")]
    [SerializeField] Image waveTemplate;
    [SerializeField] int waveCount = 26;
    [SerializeField] Image wakeTemplate;
    [Tooltip("Teknenin dalgada yana yatmasi (derece)")]
    [SerializeField] float rollDegrees = 7f;
    [Tooltip("Dalgada inip kalkma (artboard pikseli)")]
    [SerializeField] float bobPixels = 9f;
    [Tooltip("Burnun dalgaya binip inmesi (olcek orani)")]
    [SerializeField] float pitchAmount = 0.045f;

    [Header("Nasil oynanir")]
    [SerializeField] GameObject howTo;
    [SerializeField] RectTransform howToCard;
    [SerializeField] GameObject desktopRows;
    [SerializeField] GameObject mobileRows;

    [Header("Telefon kontrolleri")]
    [SerializeField] GameObject mobileControls;
    [SerializeField] VirtualJoystick joystick;

    const int TrashPerRun = 13;

    class Catch
    {
        public RectTransform rt;
        public int kind;
        public bool fish;
        public Vector2 home;
        public float phase, swim;
        public bool taken;
    }

    readonly List<Catch> catches = new List<Catch>();
    readonly List<int> bag = new List<int>();
    Vector2 boatPos, velocity;
    float boatAngle = 180f, forwardSpeed, turnVelocity, bumpCooldown, hintTimer;
    int healthStart, healthTarget, fishBefore, fishToday, fishShown;
    readonly List<int> fishThresholds = new List<int>();
    bool casting, leaving, sorting;

    class Wave { public RectTransform rt; public Image img; public Vector2 home; public float phase, speed; }
    readonly List<Wave> waves = new List<Wave>();
    float wakeTimer;

    bool Paused => howTo && howTo.activeSelf;

    static bool IsMobile => Application.isMobilePlatform || Touchscreen.current != null;

    void Awake()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (!button.GetComponent<TwButonSesi>())
                button.gameObject.AddComponent<TwButonSesi>();
    }

    void Start()
    {
        Time.timeScale = 1f;
        seaPage.gameObject.SetActive(true);
        mobileControls.SetActive(IsMobile);
        itemTemplate.gameObject.SetActive(false);
        line.gameObject.SetActive(false);
        bobber.gameObject.SetActive(false);
        splash.gameObject.SetActive(false);
        floatingText.gameObject.SetActive(false);
        sortButton.interactable = false;

        tooltipLine.text = Loc.T(IsMobile ? "Use the joystick to row and the rod button to cast."
                                          : "Row with the arrow keys. Press E or tap trash to cast your rod.");
        boatPos = boatStart;
        PlaceBoat();
        SpawnWaves();
        SetupEcology();
        Spawn();
        OpenHowTo();
        RefreshHud();
        TwSes.EkranGecisi(Ses.MuzikAnaTema, Ses.AmbiyansDeniz);
        TwSes.TekneMotoru(true);
        if (screenFader) StartCoroutine(Fade(screenFader, 1f, 0f));
    }

    void OnDestroy() => TwSes.TekneMotoru(false);

    // ---------------- Yerlesim ----------------

    void Spawn()
    {
        var rng = new System.Random(GameProgress.CurrentDay * 7919);
        var taken = new List<Vector2> { boatStart };
        Vector2 Free(float margin)
        {
            for (int tries = 0; tries < 400; tries++)
            {
                var p = new Vector2(Mathf.Lerp(600, playArea.x - 90, (float)rng.NextDouble()),
                                    Mathf.Lerp(hudTop + 70, playArea.y - hudBottom - 60, (float)rng.NextDouble()));
                if (Blocked(p, margin)) continue;
                bool near = false;
                foreach (var q in taken) if ((q - p).sqrMagnitude < 190f * 190f) { near = true; break; }
                if (near) continue;
                taken.Add(p);
                return p;
            }
            return new Vector2(1500, 900);
        }

        for (int i = 0; i < Mathf.Min(TrashPerRun, trashSprites.Length); i++) Add(i, false, Free(70));
        boat.SetAsLastSibling();

        // Onceki gunlerde donen baliklar zaten yuzuyor
        for (int i = 0; i < fishBefore; i++) AddFish(TrashCatalog.FishReturnOrder[i], FishSpotNear(Free(FishRadius)), false);

        void Add(int kind, bool fish, Vector2 pos)
        {
            var img = Instantiate(itemTemplate, itemsParent);
            img.gameObject.SetActive(true);
            img.sprite = trashSprites[kind];
            img.SetNativeSize();
            var c = new Catch { rt = img.rectTransform, kind = kind, fish = false, home = pos,
                                phase = (float)rng.NextDouble() * 6.28f };
            catches.Add(c);
            img.GetComponent<Button>().onClick.AddListener(() => OnItemClicked(c));
            SetPos(c.rt, pos);
        }
    }

    /// <summary>Baligin govdesi (yaklasik 190 px uzunluk) tamamen suda kalsin diye kullanilan yaricap.</summary>
    const float FishRadius = 120f;

    /// <summary>Verilen noktaya en yakin, adaya / kumsala / ust-alt bara tasmayan su noktasi.</summary>
    Vector2 FishSpotNear(Vector2 p)
    {
        if (!Blocked(p, FishRadius)) return p;
        for (float r = 40f; r <= 900f; r += 40f)
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f;
                var q = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (!Blocked(q, FishRadius)) return q;
            }
        return new Vector2(1250f, 900f);
    }

    /// <summary>Temiz suda yuzen balik (avlanmaz). appear: yeni donduyse belirerek gelir.</summary>
    void AddFish(int species, Vector2 pos, bool appear)
    {
        var img = Instantiate(itemTemplate, itemsParent);
        img.gameObject.SetActive(true);
        img.sprite = fishSprites[species];
        img.SetNativeSize();
        img.rectTransform.sizeDelta *= 0.62f;
        img.rectTransform.SetSiblingIndex(boat.GetSiblingIndex());
        var c = new Catch { rt = img.rectTransform, kind = species, fish = true, home = pos,
                            phase = UnityEngine.Random.value * 6.28f, swim = UnityEngine.Random.value < 0.5f ? -1 : 1 };
        catches.Add(c);
        img.GetComponent<Button>().onClick.AddListener(() => OnItemClicked(c));
        SetPos(c.rt, pos);
        if (appear) StartCoroutine(FishAppears(img));
    }

    IEnumerator FishAppears(Image img)
    {
        yield return Tween(1.2f, t =>
        {
            var col = img.color; col.a = t; img.color = col;
        });
    }

    /// <summary>Gunun hedefleri: saglik %0 -> %35 (sonraki gunler ustune), balik turleri esiklerde doner.</summary>
    void SetupEcology()
    {
        int day = GameProgress.CurrentDay;
        healthStart = GameProgress.OceanHealth;
        healthTarget = Mathf.Min(100, healthStart + TrashCatalog.HealthPerDay);
        if (day >= GameProgress.TotalDays) healthTarget = 100;
        fishBefore = GameProgress.FishReturned;
        fishToday = Mathf.Min(TrashCatalog.FishForDay(day), TrashCatalog.FishReturnOrder.Length - fishBefore);
        fishThresholds.Clear();
        for (int k = 0; k < fishToday; k++)
            fishThresholds.Add(Mathf.RoundToInt(TrashTotal * (k + 1f) / (fishToday + 1f)));
    }

    /// <summary>Esige gelindiyse temizlenen yerde bir balik turu belirir.</summary>
    void CheckFishReturn(Vector2 cleanedSpot)
    {
        while (fishShown < fishThresholds.Count && bag.Count >= fishThresholds[fishShown])
        {
            int species = TrashCatalog.FishReturnOrder[fishBefore + fishShown];
            fishShown++;
            var spot = FishSpotNear(cleanedSpot);
            AddFish(species, spot, true);
            TwSes.Efekt(Ses.GecisKabarcik, 0.8f);
            ShowFloating(Loc.F("{0} is back in the clean water!", Loc.T(TrashCatalog.FishNames[species])),
                spot + new Vector2(0, -60f), new Color32(190, 255, 236, 255));
        }
    }

    // ---------------- Her kare ----------------

    void Update()
    {
        float dt = Time.deltaTime;
        bumpCooldown -= dt;
        AnimateCatches(dt);
        AnimateWaves();
        if (sorting || leaving) { PlaceBoat(); return; }
        if (Paused)
        {
            var k = Keyboard.current;
            if (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame))
                CloseHowTo();
            PlaceBoat();
            return;
        }

        // Ipucu kutusu birkac saniye sonra soner
        hintTimer += dt;
        if (tooltip) tooltip.alpha = Mathf.Clamp01(1.2f - (hintTimer - 7f) / 1.2f);

        Row(dt);
        LeaveWake(dt);
        PlaceBoat();

        var kb = Keyboard.current;
        bool cast = kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame);
        if (cast) CastNearest();
        if (kb != null && kb.escapeKey.wasPressedThisFrame) BackToMenu();
    }

    /// <summary>
    /// Kayik fizigi: tekne sadece burnunun baktigi yone (ya da geri) itilir.
    /// Klavye: ileri/geri kurek, sag/sol dumen. Joystick: cubugun gosterdigi yone doner ve kurek ceker.
    /// Donus dururken daha yavas; su direnci yavaslatir; donuste eski hiz bir sure yana kaydirir.
    /// </summary>
    void Row(float dt)
    {
        float heading = boatAngle - 180f;   // art uzayinda burun yonu: (cos, -sin); artinca sola doner
        float rate = Mathf.Lerp(minTurnRate, turnRate, Mathf.Abs(forwardSpeed) / maxSpeed);

        // Klavye: tekne gibi. Ileri / geri kurek, sag / sol dumen.
        float throttle = 0f, steer = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) throttle += 1f;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) throttle -= 1f;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) steer -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) steer += 1f;
        }

        // Joystick / gamepad: cubugun gosterdigi yone don ve oraya kurek cek
        var stick = ReadStick();
        float stickTurn = 0f;
        if (stick.sqrMagnitude > 0.01f)
        {
            float desired = Mathf.Atan2(-stick.y, stick.x) * Mathf.Rad2Deg;
            float diff = Mathf.DeltaAngle(heading, desired);
            stickTurn = Mathf.Clamp(diff * 1.5f, -rate, rate);
            throttle += stick.magnitude * Mathf.Max(0f, Mathf.Cos(diff * Mathf.Deg2Rad));
        }

        float wantTurn = Mathf.Clamp(steer * rate + stickTurn, -rate, rate);
        turnVelocity = Mathf.MoveTowards(turnVelocity, wantTurn, turnAcceleration * dt);
        heading += turnVelocity * dt;

        throttle = Mathf.Clamp(throttle, -1f, 1f);
        // Geri kurek daha zayif
        forwardSpeed += acceleration * (throttle >= 0f ? throttle : throttle * 0.6f) * dt;
        forwardSpeed *= Mathf.Exp(-waterDrag * dt);
        forwardSpeed = Mathf.Clamp(forwardSpeed, -maxSpeed * 0.4f, maxSpeed);
        boatAngle = heading + 180f;

        float h = heading * Mathf.Deg2Rad;
        var target = new Vector2(Mathf.Cos(h), -Mathf.Sin(h)) * forwardSpeed;
        velocity = Vector2.Lerp(velocity, target, 1f - Mathf.Exp(-keel * dt));
        Move(velocity * dt);
    }

    /// <summary>Joystick ve gamepad cubugu (art uzayinda: y asagi).</summary>
    Vector2 ReadStick()
    {
        var v = Vector2.zero;
        var pad = Gamepad.current;
        if (pad != null)
        {
            var s = pad.leftStick.ReadValue();
            v += new Vector2(s.x, -s.y);
            if (pad.buttonSouth.wasPressedThisFrame) CastNearest();
        }
        if (joystick && mobileControls.activeSelf) v += new Vector2(joystick.Value.x, -joystick.Value.y);
        return Vector2.ClampMagnitude(v, 1f);
    }

    void Move(Vector2 delta)
    {
        var next = boatPos + delta;
        if (!Blocked(next, boatRadius)) { boatPos = next; return; }
        // Kenar boyunca kay: tek eksende ilerlemeyi dene
        var onlyX = boatPos + new Vector2(delta.x, 0);
        var onlyY = boatPos + new Vector2(0, delta.y);
        if (!Blocked(onlyX, boatRadius)) { boatPos = onlyX; velocity.y *= -0.3f; }
        else if (!Blocked(onlyY, boatRadius)) { boatPos = onlyY; velocity.x *= -0.3f; }
        else velocity *= -0.45f;
        forwardSpeed *= -0.3f;
        turnVelocity = 0f;
        Bump(next);
    }

    void Bump(Vector2 at)
    {
        if (bumpCooldown > 0f) return;
        bumpCooldown = 0.8f;
        TwSes.Efekt(Ses.Hata, 0.5f);
        bool beach = at.x - boatRadius < ShoreX(at.y) + 12f;
        ShowFloating(Loc.T(beach ? "Too close to the beach!" : "Careful, don't hit the island!"), boatPos, new Color32(255, 236, 200, 255));
        StartCoroutine(Shake());
    }

    bool Blocked(Vector2 p, float r, bool hud = true)
    {
        if (p.x - r < ShoreX(p.y) + 12f) return true;
        if (hud && (p.x + r > playArea.x || p.y - r < hudTop || p.y + r > playArea.y - hudBottom + 40f)) return true;
        foreach (var isl in islands)
        {
            float dx = (p.x - isl.x) / (isl.z + r * 0.85f), dy = (p.y - isl.y) / (isl.w + r * 0.85f);
            if (dx * dx + dy * dy < 1f) return true;
        }
        return false;
    }

    /// <summary>Sol kumsalin deniz kenari (fishing_ui.py'deki shore_x ile ayni).</summary>
    float ShoreX(float y) => 560f - 170f * (y / playArea.y) + 46f * Mathf.Sin(y / 260f);

    void AnimateCatches(float dt)
    {
        float t = Time.time;
        foreach (var c in catches)
        {
            if (c.taken) continue;
            if (c.fish)
            {
                // Balik ileri geri yuzer; bir yere carpacaksa doner
                var next = c.home + new Vector2(c.swim * 60f * dt, 0);
                if (Blocked(next, FishRadius) || UnityEngine.Random.value < 0.002f) c.swim = -c.swim;
                else c.home = next;
                c.rt.localScale = new Vector3(c.swim < 0 ? -1f : 1f, 1f, 1f);
                SetPos(c.rt, c.home + new Vector2(0, Mathf.Sin(t * 1.6f + c.phase) * 6f));
            }
            else
            {
                if (GameSettings.ReduceMotion) continue;
                SetPos(c.rt, c.home + new Vector2(Mathf.Sin(t * 0.7f + c.phase) * 8f, Mathf.Sin(t * 1.3f + c.phase) * 5f));
                c.rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.9f + c.phase) * 6f);
            }
        }
    }

    // ---------------- Olta ----------------

    /// <summary>Telefon olta butonu.</summary>
    public void CastButton() => CastNearest();

    void CastNearest()
    {
        if (casting || sorting || Paused) return;
        Catch best = null;
        float bestD = castRange;
        foreach (var c in catches)
        {
            if (c.taken || c.fish) continue;
            float d = Vector2.Distance(c.home, boatPos);
            if (d < bestD) { bestD = d; best = c; }
        }
        if (best != null) StartCoroutine(Reel(best));
        else StartCoroutine(Miss());
    }

    void OnItemClicked(Catch c)
    {
        if (casting || sorting || c.taken || Paused) return;
        if (c.fish)
        {
            ShowFloating(Loc.T("Let it swim! Fish need clean water."), c.home, new Color32(190, 255, 236, 255));
            return;
        }
        if (Vector2.Distance(c.home, boatPos) > castRange)
        {
            ShowFloating(Loc.T("Too far! Row a little closer."), c.home, Color.white);
            return;
        }
        StartCoroutine(Reel(c));
    }

    IEnumerator Reel(Catch c)
    {
        casting = true;
        c.taken = true;
        TwSes.Efekt(Ses.AgAt, 0.8f);
        Vector2 start = boatPos;
        yield return Tween(0.28f, t => DrawLine(start, Vector2.Lerp(start, c.home, t)));
        Splash(c.home);
        yield return Tween(0.45f, t =>
        {
            var p = Vector2.Lerp(c.home, boatPos, t * t);
            SetPos(c.rt, p);
            c.rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, t) * (c.fish ? Mathf.Sign(c.rt.localScale.x) : 1f);
            DrawLine(boatPos, p);
        });
        line.gameObject.SetActive(false);
        bobber.gameObject.SetActive(false);
        c.rt.gameObject.SetActive(false);

        const int reward = 5;
        GameProgress.Coins += reward;
        TwSes.EfektCesitli(Ses.AtikTopla, 0.8f);
        ShowFloating("+" + reward, boatPos, new Color32(255, 214, 92, 255));
        bag.Add(c.kind);
        CheckFishReturn(c.home);
        RefreshHud();
        casting = false;

        if (bag.Count >= TrashTotal) StartCoroutine(AllCollected());
    }

    IEnumerator Miss()
    {
        casting = true;
        TwSes.Efekt(Ses.AgAt, 0.6f);
        float a = (boatAngle - 180f) * Mathf.Deg2Rad;
        var end = boatPos + new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * 240f;
        yield return Tween(0.25f, t => DrawLine(boatPos, Vector2.Lerp(boatPos, end, t)));
        Splash(end);
        ShowFloating(Loc.T("Nothing in reach. Row closer to the trash!"), end, Color.white);
        yield return new WaitForSeconds(0.35f);
        line.gameObject.SetActive(false);
        bobber.gameObject.SetActive(false);
        casting = false;
    }

    void DrawLine(Vector2 from, Vector2 to)
    {
        line.gameObject.SetActive(true);
        bobber.gameObject.SetActive(true);
        var d = to - from;
        SetPos(line, from);
        line.sizeDelta = new Vector2(d.magnitude, line.sizeDelta.y);
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg);
        SetPos(bobber, to);
    }

    void Splash(Vector2 at)
    {
        StartCoroutine(SplashRoutine(at));
    }

    IEnumerator SplashRoutine(Vector2 at)
    {
        splash.gameObject.SetActive(true);
        SetPos(splash, at);
        var img = splash.GetComponent<Image>();
        yield return Tween(0.5f, t =>
        {
            splash.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, t);
            var col = img.color; col.a = 1f - t; img.color = col;
        });
        splash.gameObject.SetActive(false);
    }

    IEnumerator AllCollected()
    {
        sortButton.interactable = true;
        TwSes.Efekt(Ses.OnarimTamam);
        ShowFloating(Loc.T("The ocean is clean! Time to sort the catch."), boatPos + new Vector2(0, -120), new Color32(255, 236, 160, 255));
        yield return new WaitForSeconds(2.5f);
        if (!sorting) SortTheCatch();
    }

    // ---------------- Arayuz ----------------

    int TrashTotal => Mathf.Min(TrashPerRun, trashSprites.Length);

    void RefreshHud()
    {
        float cleaned = bag.Count / (float)TrashTotal;
        float health = Mathf.Lerp(healthStart, healthTarget, cleaned) / 100f;
        coinText.text = GameProgress.Coins.ToString();
        healthFill.sizeDelta = new Vector2(Mathf.Max(18f, healthWidth * health), healthFill.sizeDelta.y);
        healthFill.gameObject.SetActive(health > 0f);
        healthText.text = Mathf.RoundToInt(health * 100f) + "%";
        taskText.text = Loc.F("{0} / {1} trash fished out", bag.Count, TrashTotal);
        fishText.text = (fishBefore + fishShown) + "/" + fishSprites.Length;
        bagCount.text = Loc.F("{0} items", bag.Count);
        for (int i = 0; i < bagIcons.Length; i++)
        {
            int idx = bag.Count - bagIcons.Length + i;
            bool on = bag.Count > bagIcons.Length ? true : i < bag.Count;
            int item = bag.Count > bagIcons.Length ? bag[idx] : (i < bag.Count ? bag[i] : -1);
            bagIcons[i].gameObject.SetActive(on && item >= 0);
            if (on && item >= 0) bagIcons[i].sprite = trashSprites[item];
        }
        var c = murk.color; c.a = murkAlpha * (1f - health); murk.color = c;
        sortButton.interactable = bag.Count >= TrashTotal;
    }

    void ShowFloating(string text, Vector2 at, Color color)
    {
        var t = Instantiate(floatingText, floatingText.transform.parent);
        t.gameObject.SetActive(true);
        t.text = text;
        t.color = color;
        StartCoroutine(FloatUp(t, at));
    }

    IEnumerator FloatUp(Text t, Vector2 at)
    {
        yield return Tween(1.2f, k =>
        {
            SetPos(t.rectTransform, at + new Vector2(0, -60f - 70f * k));
            var c = t.color; c.a = k < 0.6f ? 1f : (1f - k) / 0.4f; t.color = c;
        });
        Destroy(t.gameObject);
    }

    IEnumerator Shake()
    {
        yield return Tween(0.25f, t => boat.anchoredPosition = new Vector2(boatPos.x, -boatPos.y) + UnityEngine.Random.insideUnitCircle * 8f * (1f - t));
    }

    /// <summary>Tekne dalgada yalpalar (yana yatma) ve hafifce iner kalkar; hizlaninca biraz daha.</summary>
    void PlaceBoat()
    {
        float t = Time.time;
        // Dururken de dalgada sallanir; hizlaninca biraz daha
        float sway = GameSettings.ReduceMotion ? 0f : 1f + 0.5f * Mathf.Abs(forwardSpeed) / maxSpeed;
        float roll = (Mathf.Sin(t * 1.5f) * 0.65f + Mathf.Sin(t * 0.55f + 1.1f) * 0.35f) * rollDegrees * sway;
        float bob = (Mathf.Sin(t * 1.9f + 0.4f) * 0.7f + Mathf.Sin(t * 0.8f) * 0.3f) * bobPixels * sway;
        float drift = Mathf.Sin(t * 0.7f + 2f) * bobPixels * 0.5f * sway;
        SetPos(boat, boatPos + new Vector2(drift, bob));
        // Sprite'ta burun solda (-x): hareket yonu "heading" ise sprite heading + 180 donmeli
        boat.localRotation = Quaternion.Euler(0, 0, boatAngle + roll);
        float pitch = Mathf.Sin(t * 2.2f + 0.9f) * pitchAmount * sway;
        boat.localScale = new Vector3(1f + pitch * 0.5f, 1f - pitch, 1f);
    }

    // ---------------- Dalgalar ve iz ----------------

    void SpawnWaves()
    {
        var rng = new System.Random(4242);
        waveTemplate.gameObject.SetActive(false);
        for (int i = 0; i < waveCount; i++)
        {
            Vector2 p = default;
            for (int tries = 0; tries < 100; tries++)
            {
                p = new Vector2(Mathf.Lerp(620, playArea.x - 80, (float)rng.NextDouble()), Mathf.Lerp(40, playArea.y - 40, (float)rng.NextDouble()));
                if (!Blocked(p, 50f, false)) break;
            }
            var img = Instantiate(waveTemplate, waveTemplate.transform.parent);
            img.gameObject.SetActive(true);
            img.rectTransform.SetAsFirstSibling();
            img.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.25f, (float)rng.NextDouble());
            waves.Add(new Wave { rt = img.rectTransform, img = img, home = p,
                                 phase = (float)rng.NextDouble() * 6.28f, speed = Mathf.Lerp(0.35f, 0.7f, (float)rng.NextDouble()) });
        }
    }

    void AnimateWaves()
    {
        float t = Time.time;
        bool still = GameSettings.ReduceMotion;
        foreach (var w in waves)
        {
            float a = t * w.speed + w.phase;
            SetPos(w.rt, w.home + (still ? Vector2.zero : new Vector2(Mathf.Sin(a) * 26f, Mathf.Sin(a * 0.7f) * 6f)));
            var c = w.img.color;
            c.a = still ? 0.45f : 0.12f + 0.5f * (0.5f + 0.5f * Mathf.Sin(a * 1.3f));
            w.img.color = c;
        }
    }

    /// <summary>Tekne giderken kicindan kopuk halkalari birakir.</summary>
    void LeaveWake(float dt)
    {
        if (Mathf.Abs(forwardSpeed) < 45f || GameSettings.ReduceMotion) return;
        wakeTimer -= dt;
        if (wakeTimer > 0f) return;
        wakeTimer = 0.14f;
        float h = (boatAngle - 180f) * Mathf.Deg2Rad;
        var stern = boatPos - new Vector2(Mathf.Cos(h), -Mathf.Sin(h)) * 150f;
        StartCoroutine(WakeRing(stern));
    }

    IEnumerator WakeRing(Vector2 at)
    {
        var img = Instantiate(wakeTemplate, wakeTemplate.transform.parent);
        img.gameObject.SetActive(true);
        img.rectTransform.SetSiblingIndex(boat.GetSiblingIndex());
        SetPos(img.rectTransform, at);
        img.rectTransform.localRotation = Quaternion.Euler(0, 0, boatAngle - 180f);
        yield return Tween(1.1f, k =>
        {
            img.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.5f, k);
            var c = img.color; c.a = 0.8f * (1f - k); img.color = c;
        });
        Destroy(img.gameObject);
    }

    // ---------------- Nasil oynanir ----------------

    void OpenHowTo()
    {
        if (!howTo) return;
        desktopRows.SetActive(!IsMobile);
        mobileRows.SetActive(IsMobile);
        howTo.SetActive(true);
        StartCoroutine(Tween(0.35f, t => howToCard.localScale = Vector3.one * EaseOutBack(t)));
    }

    /// <summary>Kartin "Let's go!" butonu (Enter / Space / E de kapatir).</summary>
    public void CloseHowTo()
    {
        if (!Paused) return;
        howTo.SetActive(false);
        hintTimer = 0f;
        TwSes.Efekt(Ses.Tik, 0.6f);
    }

    /// <summary>Ust bardaki "?" butonu: karti tekrar acar.</summary>
    public void ShowHowTo()
    {
        if (Paused || sorting) return;
        OpenHowTo();
    }

    static float EaseOutBack(float t)
    {
        const float c = 1.7f;
        float u = t - 1f;
        return 1f + (c + 1f) * u * u * u + c * u * u;
    }

    static void SetPos(RectTransform rt, Vector2 artPos) => rt.anchoredPosition = new Vector2(artPos.x, -artPos.y);

    // ---------------- Gecisler ----------------

    /// <summary>"Sort the catch" (butun copler toplaninca acilir).</summary>
    public void SortTheCatch()
    {
        if (sorting || bag.Count < TrashTotal) return;
        sorting = true;
        TwSes.TekneMotoru(false);
        StartCoroutine(GoSort());
    }

    IEnumerator GoSort()
    {
        // Gunun sonucu kaydedilir: saglik hedefe ulasti, donen baliklar ekosisteme katildi
        GameProgress.OceanHealth = healthTarget;
        GameProgress.FishReturned = fishBefore + fishShown;
        CatchBag.Items = new List<int>(bag);
        GameSettings.Save();
        yield return Load(recyclingSceneName);
    }

    public void Restart()
    {
        if (leaving) return;
        leaving = true;
        StartCoroutine(Load(SceneManager.GetActiveScene().name));
    }

    public void BackToMenu()
    {
        if (leaving) return;
        leaving = true;
        StartCoroutine(Load(menuSceneName));
    }

    IEnumerator Load(string scene)
    {
        GameSettings.Save();
        if (screenFader) yield return Fade(screenFader, 0f, 1f, true);
        SceneManager.LoadScene(scene);
    }

    IEnumerator Fade(CanvasGroup g, float from, float to, bool keep = false)
    {
        g.gameObject.SetActive(true);
        g.blocksRaycasts = true;
        yield return Tween(0.45f, t => g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t)));
        if (to == 0f && !keep) g.gameObject.SetActive(false);
    }

    static IEnumerator Tween(float duration, Action<float> step)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            step(t / duration);
            yield return null;
        }
        step(1f);
    }
}
