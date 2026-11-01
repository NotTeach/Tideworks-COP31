"""3. gun kumsali "Buyuk Gece", tasarim 34-41.png (1280x720; artboard 2560x1440 = 2 kat).
Kullanim: python day3_night_art.py <Assets/Sprites> <34.png>
Uretir (Sprites/Day3Beach):
- Day3BeachBackground/Backdrop (gun batimi: gokyuzu, gunes ve yansimalari, deniz, kopuk, kum), NightBackground (ayni sahne gece)
- StreetLamp, NestFlag (bayrakli yuva), Hatice, Elif (Pamuk'la), Mert (pusula ve kepceyle; 34.png'den kesilir), CharShadow
- TaskPanel, CheckBox, CheckBoxOn, DialogBox, DialogButton, DialogButtonWide, JobsPanel, JobChip, JobChipSelected, TagPill,
  HelperPill, NamePill, PickRing, NotebookCard, SeasonCard"""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as nd

SP, SRC = sys.argv[1:3]
OUT = f"{SP}/Day3Beach"
os.makedirs(OUT, exist_ok=True)
S = 4
W, H = 2560, 1440
INK = (52, 40, 32, 255)
DARK = (14, 62, 72, 255)
CREAM = (251, 248, 241, 255)
design = Image.open(SRC).convert("RGB")
src = np.asarray(design).astype(np.float32)


def canvas(w, h):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (x * S, y * S))


def save(im, name, w, h):
    im.resize((w, h), Image.LANCZOS).save(f"{OUT}/{name}.png")


# ================= Zemin (gun batimi ve gece) =================
# Kopuk cizgisinin ust kenari (tasarimda olculdu, her 64 pikselde bir)
FOAM = [327, 323, 321, 321, 323, 324, 326, 328, 329, 328, 326, 323, 320, 319, 320, 322, 324, 326, 327, 325, 327]


def foam_y(x):                       # 1280 olceginde x -> kopugun ust kenari
    f = x / 64.0
    i = min(int(f), len(FOAM) - 2)
    return FOAM[i] + (FOAM[i + 1] - FOAM[i]) * (f - i)


def scene(sky_rows, sea, foam, band, sand, sun, lines, wave, pebble, stars):
    im = Image.new("RGB", (W * 2, H * 2))                  # 4 kat cizilir, 2560'a kucultulur
    d = ImageDraw.Draw(im)
    k = 4                                                   # 1280 -> 5120
    for y in range(150 * k):
        c = sky_rows(y / k)
        d.line([(0, y), (W * 2, y)], fill=tuple(int(v) for v in c))
    for sx, sy, r in stars:
        d.ellipse([sx * k - r, sy * k - r, sx * k + r, sy * k + r], fill=(250, 246, 230))
    d.ellipse([(640 - 50) * k, (160 - 50) * k, (640 + 50) * k, (160 + 50) * k], fill=sun)
    d.rectangle([0, 150 * k, W * 2, 360 * k], fill=sea)
    for y, x0, x1 in ((167, 588, 692), (189, 602, 678), (211, 614, 666)):
        d.rounded_rectangle([x0 * k, (y - 2) * k, x1 * k, (y + 2) * k], radius=2 * k, fill=lines)
    for x, y in ((130, 218), (430, 268), (910, 230), (1130, 280)):
        pts = [((x + t * 38 / 20) * k, (y + 3.2 * math.sin(t / 20 * 2 * math.pi)) * k) for t in range(21)]
        d.line(pts, fill=wave, width=3 * k, joint="curve")
    xs = np.arange(0, 1281, 2)
    top = [(x * k, foam_y(x) * k) for x in xs]
    d.polygon(top + [(1280 * k, 360 * k), (0, 360 * k)], fill=foam)
    d.polygon([(x, y + 6 * k) for x, y in top] + [(1280 * k, 720 * k), (0, 720 * k)], fill=band)
    d.rectangle([0, 356 * k, W * 2, 720 * k], fill=sand)
    for x, y, w, h in ((124, 413, 32, 12), (804, 553, 32, 12), (470, 640, 26, 10), (1120, 700, 30, 11)):
        d.ellipse([x * k, y * k, (x + w) * k, (y + h) * k], fill=pebble)
    return im.resize((W, H), Image.LANCZOS)


day_sky = src[:150, 500]


def day_rows(y):
    i = min(int(y), 148)
    return day_sky[i] + (day_sky[i + 1] - day_sky[i]) * (y - i)


NIGHT_KEYS = [(0, (44, 58, 92)), (40, (60, 70, 98)), (75, (86, 81, 94)), (105, (103, 88, 91)), (130, (112, 97, 88)), (150, (118, 106, 84))]


def night_rows(y):
    for (y0, c0), (y1, c1) in zip(NIGHT_KEYS, NIGHT_KEYS[1:]):
        if y <= y1:
            t = (y - y0) / (y1 - y0)
            return [a + (b - a) * t for a, b in zip(c0, c1)]
    return NIGHT_KEYS[-1][1]


day = scene(day_rows, (46, 154, 160), (251, 248, 241), (214, 192, 150), (233, 211, 168), (248, 217, 120),
            (157, 188, 138), (120, 200, 200), (217, 190, 142), [])
day.save(f"{OUT}/Day3BeachBackground.png", optimize=True)
day.resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{OUT}/Day3BeachBackdrop.png")
rng = np.random.default_rng(7)
stars = [(rng.uniform(20, 1260), rng.uniform(6, 100), rng.choice([3, 4, 5])) for _ in range(26)]
night = scene(night_rows, (27, 86, 100), (176, 186, 182), (103, 104, 95), (111, 112, 104), (150, 128, 86),
              (150, 168, 172), (66, 128, 140), (100, 100, 92), stars)
night.save(f"{OUT}/NightBackground.png", optimize=True)

# ================= Sokak lambasi ve bayrakli yuva =================
LAMP = (51, 69, 76, 255)
OUTL = (40, 34, 32, 255)
im, d, P = canvas(64, 194)                                  # tasarimda x 770..834, y 224..418
ox, oy = 770, 224
Q = lambda x, y: P(x - ox, y - oy)
d.rounded_rectangle([Q(782, 248), Q(798, 406)], radius=2 * S, fill=OUTL)
d.rectangle([Q(784, 250), Q(796, 404)], fill=LAMP)
d.arc([Q(786, 230), Q(826, 270)], 180, 270, fill=OUTL, width=7 * S)
d.arc([Q(787, 231), Q(825, 269)], 180, 270, fill=LAMP, width=3 * S)
d.polygon([Q(803, 228), Q(831, 228), Q(826, 247), Q(808, 247)], fill=OUTL)
d.polygon([Q(806, 230), Q(828, 230), Q(824, 245), Q(810, 245)], fill=LAMP)
d.rounded_rectangle([Q(772, 402), Q(808, 416)], radius=4 * S, fill=OUTL)
d.rounded_rectangle([Q(774, 404), Q(806, 414)], radius=3 * S, fill=LAMP)
save(im, "StreetLamp", 128, 388)

im, d, P = canvas(144, 72)                                  # tasarimda x 188..332, y 434..506 (1. yuva)
ox, oy = 188, 434
Q = lambda x, y: P(x - ox, y - oy)
d.ellipse([Q(196, 460), Q(324, 500)], fill=(217, 190, 142, 255))
d.chord([Q(198, 448), Q(318, 512)], 180, 360, fill=(227, 203, 152, 255))
d.arc([Q(198, 448), Q(318, 512)], 192, 348, fill=(190, 160, 115, 255), width=2 * S)
d.line([Q(198, 480), Q(318, 480)], fill=(196, 168, 124, 255), width=2 * S)
d.line([Q(314, 452), Q(314, 476)], fill=OUTL, width=2 * S)
d.rounded_rectangle([Q(303, 439), Q(327, 454)], radius=2 * S, fill=OUTL)
d.rounded_rectangle([Q(305, 441), Q(325, 452)], radius=1 * S, fill=(243, 192, 80, 255))
save(im, "NestFlag", 288, 144)

# ================= Karakterler (34.png'den; kum ve golgeler silinir) =================
SAND = np.array([233, 211, 168], np.float32)


def cut(box, name):
    x0, y0, x1, y1 = box
    a = src[y0:y1, x0:x1]
    n = a / (np.linalg.norm(a, axis=2, keepdims=True) + 1e-6)
    ns = SAND / np.linalg.norm(SAND)
    hue_ok = (n * ns).sum(axis=2) > 0.9985                  # kumla ayni renk tonu (golge daha koyu kum)
    bright = a.sum(axis=2) / SAND.sum()
    sandish = hue_ok & (bright > 0.78) & (bright < 1.04)
    greyish = (np.abs(a - np.array([220, 219, 217])).sum(axis=2) < 26)    # Hatice'nin altindaki acik golge
    bgc = sandish | greyish
    lab, _ = nd.label(bgc)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(edge))
    # Kol ile kepce sapi, bacaklar arasi gibi kapali kum ceplerini de sil (tam kum rengi, kirintidan buyuk)
    pure = np.abs(a - SAND).sum(axis=2) < 14
    plab, pn = nd.label(pure)
    if pn:
        sizes = nd.sum(pure, plab, range(1, pn + 1))
        bg |= np.isin(plab, [i + 1 for i, s in enumerate(sizes) if s >= 12])
    feet = np.zeros_like(bg)
    feet[-22:] = np.abs(a[-22:] - np.array([220, 219, 217])).sum(axis=2) < 48       # ayak altindaki acik golge
    bg |= feet
    alpha = np.where(bg, 0, 255).astype(np.uint8)
    keep, n_ = nd.label(alpha > 0)
    if n_ > 1:                                              # kirintilari at
        sizes = nd.sum(alpha > 0, keep, range(1, n_ + 1))
        alpha[np.isin(keep, [i + 1 for i, s in enumerate(sizes) if s < 40])] = 0
    alpha = np.asarray(Image.fromarray(alpha).filter(ImageFilter.GaussianBlur(0.5)))
    img = Image.fromarray(np.dstack([a.astype(np.uint8), alpha]), "RGBA")
    bb = img.getbbox()
    img = img.crop(bb)
    img = img.resize((img.width * 2, img.height * 2), Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=2, percent=80, threshold=2))
    img.save(f"{OUT}/{name}.png")
    print(name, "1280 kutusu:", (x0 + bb[0], y0 + bb[1], bb[2] - bb[0], bb[3] - bb[1]))


cut((872, 355, 1004, 618), "Hatice")
cut((1004, 395, 1110, 618), "Elif")
cut((1112, 395, 1212, 618), "Mert")

im, d, P = canvas(220, 40)
d.ellipse([P(0, 0), P(220, 40)], fill=(40, 30, 20, 70))
save(im, "CharShadow", 220, 40)

# ================= Arayuz =================
im, d, P = canvas(660, 346)                                 # gorev paneli (sag ust)
d.rounded_rectangle([P(0, 0), P(660, 346)], radius=32 * S, fill=DARK)
save(im, "TaskPanel", 660, 346)
for name, on in (("CheckBox", False), ("CheckBoxOn", True)):
    im, d, P = canvas(52, 52)
    d.rounded_rectangle([P(2, 2), P(50, 50)], radius=10 * S, fill=(60, 170, 168, 255) if on else None,
                        outline=(96, 196, 190, 255), width=4 * S)
    if on:
        d.line([P(13, 27), P(22, 37), P(40, 16)], fill=(255, 255, 255, 255), width=6 * S, joint="curve")
    save(im, name, 52, 52)

im, d, P = canvas(1520, 276)                                # konusma kutusu (sol alt)
d.rounded_rectangle([P(2, 14), P(1520, 276)], radius=40 * S, fill=(62, 42, 30, 255))
d.rounded_rectangle([P(0, 0), P(1516, 264)], radius=40 * S, fill=(62, 42, 30, 255))
d.rounded_rectangle([P(8, 8), P(1508, 256)], radius=34 * S, fill=CREAM)
save(im, "DialogBox", 1520, 276)
for name, w in (("DialogButton", 224), ("DialogButtonWide", 540)):
    im, d, P = canvas(w, 108)
    d.rounded_rectangle([P(0, 8), P(w, 108)], radius=24 * S, fill=(6, 36, 42, 255))
    d.rounded_rectangle([P(0, 0), P(w, 100)], radius=24 * S, fill=DARK)
    save(im, name, w, 108)

im, d, P = canvas(920, 502)                                 # "Who does what?" paneli
d.rounded_rectangle([P(2, 14), P(920, 502)], radius=34 * S, fill=(62, 42, 30, 255))
d.rounded_rectangle([P(0, 0), P(916, 490)], radius=34 * S, fill=(62, 42, 30, 255))
d.rounded_rectangle([P(8, 8), P(908, 482)], radius=28 * S, fill=(231, 201, 148, 255))
save(im, "JobsPanel", 920, 502)
for name, edge, wd in (("JobChip", (62, 42, 30, 255), 3), ("JobChipSelected", (23, 119, 122, 255), 7)):
    im, d, P = canvas(830, 90)
    d.rounded_rectangle([P(0, 0), P(830, 90)], radius=22 * S, fill=edge)
    d.rounded_rectangle([P(wd, wd), P(830 - wd, 90 - wd)], radius=(22 - wd // 2) * S, fill=CREAM)
    save(im, name, 830, 90)
im, d, P = canvas(100, 40)
d.rounded_rectangle([P(0, 0), P(100, 40)], radius=20 * S, fill=(180, 72, 40, 255))
save(im, "TagPill", 100, 40)
im, d, P = canvas(240, 50)
d.rounded_rectangle([P(0, 0), P(240, 50)], radius=25 * S, fill=(23, 119, 122, 255))
d.rounded_rectangle([P(0, 0), P(240, 50)], radius=25 * S, outline=(10, 70, 76, 255), width=3 * S)
save(im, "HelperPill", 240, 50)
im, d, P = canvas(150, 54)
d.rounded_rectangle([P(0, 0), P(150, 54)], radius=27 * S, fill=INK)
d.rounded_rectangle([P(3, 3), P(147, 51)], radius=24 * S, fill=CREAM)
save(im, "NamePill", 150, 54)
im, d, P = canvas(200, 200)
d.ellipse([P(6, 6), P(194, 194)], fill=(250, 214, 90, 40), outline=(232, 190, 80, 220), width=5 * S)
save(im, "PickRing", 200, 200)

# Dedenin defteri: kahve cerceve, cizgili sayfa, ustte bant
im, d, P = canvas(1214, 752)
d.rounded_rectangle([P(4, 18), P(1214, 752)], radius=40 * S, fill=(70, 40, 24, 255))
d.rounded_rectangle([P(0, 0), P(1208, 738)], radius=40 * S, fill=(70, 40, 24, 255))
d.rounded_rectangle([P(8, 8), P(1200, 730)], radius=34 * S, fill=(156, 90, 46, 255))
d.rounded_rectangle([P(34, 34), P(1174, 704)], radius=18 * S, fill=(70, 40, 24, 255))
d.rounded_rectangle([P(38, 38), P(1170, 700)], radius=16 * S, fill=(242, 222, 178, 255))
for y in range(110, 700, 58):
    d.line([P(60, y), P(1150, y)], fill=(222, 196, 150, 255), width=2 * S)
d.polygon([P(500, 0), P(712, 0), P(706, 52), P(506, 52)], fill=(246, 236, 210, 235))
save(im, "NotebookCard", 1214, 752)

im, d, P = canvas(1270, 822)                                # sezon sonu karti
d.rounded_rectangle([P(4, 18), P(1270, 822)], radius=44 * S, fill=(62, 42, 30, 255))
d.rounded_rectangle([P(0, 0), P(1264, 808)], radius=44 * S, fill=(62, 42, 30, 255))
d.rounded_rectangle([P(9, 9), P(1255, 799)], radius=36 * S, fill=(231, 201, 148, 255))
save(im, "SeasonCard", 1270, 822)
print("ok")
