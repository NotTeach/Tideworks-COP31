"""3. gun denizi: "Pusulanin Gosterdigi Koy" (tasarim: 30.png kayalik havuzlar, 31.png pusula / acik deniz,
32.png gizli koy, 33.png koyun yunusu).
Kullanim: python day3_sea_art.py <Assets> <images klasoru (27, 30, 33.png)>
Uretir:
- Sprites/Day3Sea: PoolsBackground (30.png'den: HUD, pusula, copler ve konusma kutusu silinmis), Day3SeaBackground (kiyisiz acik deniz),
  CoveBackground (kayalarla cevrili koy, ortada acik renkli kum), Dolphin (33.png'deki cizim), TrashRing (copun altindaki sari halka),
  DialogBox, CompassBadge, Crab, Starfish, Blenny, Anemone, Scoop, MertBadge, Bubbles
- Art/Fishing: yeni copler TrashBottleCap, TrashStraw, TrashBrokenToy, TrashTire, TrashPaintCan, TrashMilkCarton"""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as nd

A, IMG = sys.argv[1:3]
OUT = f"{A}/Sprites/Day3Sea"
ART = f"{A}/Art/Fishing"
os.makedirs(OUT, exist_ok=True)
S = 4
INK = (52, 44, 40, 255)
W, H = 2560, 1440
ROCK = (156, 151, 138)
rng = np.random.default_rng(3)


def canvas(w, h):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (x * S, y * S))


def save(im, path, w, h):
    im.resize((w, h), Image.LANCZOS).save(path)


def backdrop(img, name):
    img.convert("RGB").resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{OUT}/{name}Backdrop.png")


# ================= Kayalik havuzlar (30.png) =================
pools = np.asarray(Image.open(f"{IMG}/30.png").convert("RGB")).astype(np.float32)


def fill_rows(x0, y0, x1, y1, sx):
    """Her satiri ayni satirdaki temiz bir pikselle doldur (gokyuzu ve deniz yatay seritler)."""
    pools[y0:y1, x0:x1] = pools[y0:y1, sx:sx + 1]


def fill_ring(x0, y0, x1, y1):
    """Kutuyu kenarlarinin medyan rengiyle doldur (havuz suyu, kaya)."""
    ring = np.concatenate([pools[y0 - 2:y0, x0:x1].reshape(-1, 3), pools[y1:y1 + 2, x0:x1].reshape(-1, 3),
                           pools[y0:y1, x0 - 2:x0].reshape(-1, 3), pools[y0:y1, x1:x1 + 2].reshape(-1, 3)])
    pools[y0:y1, x0:x1] = np.median(ring, axis=0)


fill_rows(0, 0, 1000, 92, 1275)                 # ust bar (gokyuzu)
fill_rows(1126, 90, 1262, 300, 1275)            # pusula ve etiketi (gokyuzu + deniz)
for box in [(184, 438, 278, 472), (284, 468, 358, 502), (518, 390, 604, 430), (626, 396, 674, 444),
            (826, 480, 890, 532), (910, 503, 990, 547), (1050, 593, 1152, 628), (1153, 615, 1217, 645)]:
    fill_ring(*box)                              # havuzlardaki copler ve sari halkalari (Unity'de ayri sprite)
fill_ring(18, 586, 790, 706)                     # konusma kutusu (Unity'de)
pools_img = Image.fromarray(np.clip(pools, 0, 255).astype(np.uint8)).resize((W, H), Image.LANCZOS)
pools_img.save(f"{OUT}/PoolsBackground.png", optimize=True)
backdrop(pools_img, "Pools")

# ================= Acik deniz (31.png: kiyisiz, dalga isaretli) =================
sea = Image.new("RGB", (W, H))
d = ImageDraw.Draw(sea)
for y in range(H):
    t = y / H
    d.line([(0, y), (W, y)], fill=(int(48 - 6 * t), int(156 - 10 * t), int(162 - 8 * t)))
for _ in range(70):
    x, y = rng.uniform(40, W - 80), rng.uniform(200, H - 60)
    pts = [(x + k * 6, y + 6 * math.sin(k / 2.2)) for k in range(10)]
    d.line(pts, fill=(140, 214, 214), width=4)
sea.save(f"{OUT}/Day3SeaBackground.png", optimize=True)
backdrop(sea, "Day3Sea")

# ================= Gizli koy (32.png: kayalar, turkuaz su, ortada acik kum) =================
cove = Image.new("RGBA", (W, H), (62, 182, 180, 255))
d = ImageDraw.Draw(cove)
d.ellipse([370, 330, 2180, 1180], fill=(150, 206, 190, 255))                      # kumlu dip
for _ in range(24):
    x, y = rng.uniform(450, 2100), rng.uniform(380, 1120)
    pts = [(x + k * 7, y + 7 * math.sin(k / 2.0)) for k in range(9)]
    d.line(pts, fill=(196, 236, 226), width=4)


def rock_band(points, light=(156, 151, 138)):
    d.polygon(points, fill=light + (255,))
    d.line(points + [points[0]], fill=INK, width=8)


xs = np.arange(-20, W + 40, 40)
top = [(x, 150 + 40 * math.sin(x / 260) + 18 * math.sin(x / 90)) for x in xs]
rock_band([(-20, -20)] + top + [(W + 40, -20)])
left = [(170 + 70 * math.sin(y / 220) + 30 * math.sin(y / 80), y) for y in np.arange(120, H + 40, 40)]
rock_band([(-20, 120)] + left + [(-20, H + 40)])
right = [(W - 120 - 70 * math.sin(y / 240 + 1) - 25 * math.sin(y / 70), y) for y in np.arange(120, H + 40, 40)]
rock_band([(W + 40, 120)] + right + [(W + 40, H + 40)])
for x, y in ((360, 210), (420, 196), (1150, 230), (1480, 120), (2300, 420), (90, 900)):
    d.ellipse([x - 14, y - 14, x + 14, y + 14], fill=(110, 150, 100, 255))     # kaya ustunde yosun
cove.convert("RGB").save(f"{OUT}/CoveBackground.png", optimize=True)
backdrop(cove, "Cove")

# ================= Yunus (33.png sol panel, krem zemin silinir) =================
dol = Image.open(f"{IMG}/33.png").convert("RGB").crop((95, 262, 660, 590))
arr = np.asarray(dol).astype(np.int32)
bg = np.array([251, 248, 241])
near = np.abs(arr - bg).sum(axis=2) < 24
lab, n = nd.label(near)
edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
outside = np.isin(lab, list(edge))
alpha = np.where(outside, 0, 255).astype(np.uint8)
alpha = np.asarray(Image.fromarray(alpha).filter(ImageFilter.GaussianBlur(0.6)))
rgba = np.dstack([arr.astype(np.uint8), alpha])
dimg = Image.fromarray(rgba, "RGBA")
dimg = dimg.crop(dimg.getbbox()).resize((dimg.getbbox()[2] - dimg.getbbox()[0], dimg.getbbox()[3] - dimg.getbbox()[1]))
dimg.save(f"{OUT}/Dolphin.png")

# ================= Sari halka, konusma kutusu, pusula rozeti =================
im, d, P = canvas(160, 80)
d.ellipse([P(6, 10), P(154, 70)], fill=(250, 214, 90, 70), outline=(240, 196, 60, 230), width=4 * S)
save(im, f"{OUT}/TrashRing.png", 160, 80)
im, d, P = canvas(1520, 224)
d.rounded_rectangle([P(4, 12), P(1516, 222)], radius=40 * S, fill=(70, 52, 40, 255))
d.rounded_rectangle([P(0, 0), P(1512, 212)], radius=40 * S, fill=INK)
d.rounded_rectangle([P(8, 8), P(1504, 204)], radius=34 * S, fill=(250, 246, 236, 255))
save(im, f"{OUT}/DialogBox.png", 1520, 224)
im, d, P = canvas(128, 76)
d.rounded_rectangle([P(0, 0), P(128, 76)], radius=38 * S, fill=(196, 84, 52, 255))
d.line([P(62, 38), P(108, 38)], fill=(255, 255, 255, 255), width=6 * S)
d.polygon([P(100, 26), P(116, 38), P(100, 50)], fill=(255, 255, 255, 255))
save(im, f"{OUT}/CompassBadge.png", 128, 76)


# ================= Canlilar =================
def creature(name, w, h, draw):
    im, d, P = canvas(w, h)
    draw(d, P)
    save(im, f"{OUT}/{name}.png", w, h)


def crab(d, P):
    for s in (-1, 1):
        for k in range(3):
            d.line([P(60 + s * 26, 62 + k * 8), P(60 + s * (50 + k * 4), 52 + k * 16)], fill=INK, width=6 * S)
        d.line([P(60 + s * 28, 50), P(60 + s * 44, 28)], fill=INK, width=7 * S)
        d.ellipse([P(60 + s * 44 - 13, 14), P(60 + s * 44 + 13, 38)], fill=(222, 92, 60, 255), outline=INK, width=3 * S)
    d.ellipse([P(28, 44), P(92, 90)], fill=(222, 92, 60, 255), outline=INK, width=4 * S)
    for s in (-1, 1):
        d.line([P(60 + s * 8, 46), P(60 + s * 10, 34)], fill=INK, width=3 * S)
        d.ellipse([P(60 + s * 10 - 5, 28), P(60 + s * 10 + 5, 38)], fill=(255, 255, 255, 255), outline=INK, width=2 * S)


def starfish(d, P):
    pts = []
    for k in range(10):
        a = math.radians(-90 + k * 36)
        r = 52 if k % 2 == 0 else 22
        pts.append(P(60 + r * math.cos(a), 60 + r * math.sin(a)))
    d.polygon(pts, fill=(244, 140, 70, 255), outline=INK)
    for k in range(5):
        a = math.radians(-90 + k * 72)
        for j in range(1, 4):
            d.ellipse([P(60 + j * 11 * math.cos(a) - 3, 60 + j * 11 * math.sin(a) - 3),
                       P(60 + j * 11 * math.cos(a) + 3, 60 + j * 11 * math.sin(a) + 3)], fill=(255, 210, 160, 255))


def blenny(d, P):
    d.polygon([P(14, 60), P(0, 44), P(0, 76)], fill=(150, 120, 80, 255), outline=INK)
    d.ellipse([P(10, 42), P(108, 78)], fill=(176, 146, 96, 255), outline=INK, width=3 * S)
    for x in (36, 54, 72):
        d.ellipse([P(x - 6, 52), P(x + 6, 66)], fill=(120, 92, 60, 255))
    d.ellipse([P(88, 50), P(98, 60)], fill=INK)
    d.line([P(30, 42), P(90, 40)], fill=(200, 90, 60, 255), width=4 * S)


def anemone(d, P):
    for k in range(14):
        a = math.radians(-160 + k * 140 / 13)
        d.line([P(60, 74), P(60 + 46 * math.cos(a), 74 + 46 * math.sin(a))], fill=(232, 96, 150, 255), width=8 * S)
        d.ellipse([P(60 + 46 * math.cos(a) - 6, 74 + 46 * math.sin(a) - 6), P(60 + 46 * math.cos(a) + 6, 74 + 46 * math.sin(a) + 6)],
                  fill=(255, 190, 220, 255))
    d.ellipse([P(36, 64), P(84, 104)], fill=(190, 60, 110, 255), outline=INK, width=3 * S)


for n, f in (("Crab", crab), ("Starfish", starfish), ("Blenny", blenny), ("Anemone", anemone)):
    creature(n, 120, 120, f)

# ================= Kepce, rozet, kabarcik =================
im, d, P = canvas(110, 200)
d.line([P(55, 196), P(55, 96)], fill=(150, 98, 60, 255), width=8 * S)
d.ellipse([P(10, 8), P(100, 98)], outline=INK, width=8 * S)
d.ellipse([P(10, 8), P(100, 98)], outline=(70, 170, 160, 255), width=5 * S)
for k in range(-4, 5):
    d.line([P(55 + k * 10, 14), P(55 + k * 10, 92)], fill=(120, 200, 190, 200), width=2 * S)
    d.line([P(16, 53 + k * 10), P(94, 53 + k * 10)], fill=(120, 200, 190, 200), width=2 * S)
save(im, f"{OUT}/Scoop.png", 110, 200)

src = Image.open(f"{IMG}/27.png").convert("RGB").resize((2560, 1440), Image.LANCZOS)
por = src.crop((80, 226, 196, 342)).resize((96, 96), Image.LANCZOS).convert("RGBA")
m = Image.new("L", (384, 384), 0)
ImageDraw.Draw(m).ellipse([8, 8, 376, 376], fill=255)
por.putalpha(m.resize((96, 96), Image.LANCZOS))
badge = Image.new("RGBA", (104, 104), (0, 0, 0, 0))
ImageDraw.Draw(badge).ellipse([0, 0, 103, 103], fill=(255, 255, 255, 255), outline=INK, width=3)
badge.alpha_composite(por, (4, 4))
badge.save(f"{OUT}/MertBadge.png")

im, d, P = canvas(120, 120)
for x, y, r in ((40, 90, 14), (70, 70, 10), (52, 46, 8), (82, 28, 6), (30, 30, 5)):
    d.ellipse([P(x - r, y - r), P(x + r, y + r)], outline=(240, 252, 252, 255), width=3 * S, fill=(200, 240, 240, 90))
save(im, f"{OUT}/Bubbles.png", 120, 120)


# ================= Yeni copler (Art/Fishing; ayristirmada da kullanilir) =================
def trash(name, w, h, draw):
    im, d, P = canvas(w, h)
    draw(d, P)
    save(im, f"{ART}/{name}.png", w, h)


trash("TrashBottleCap", 56, 56, lambda d, P: (d.ellipse([P(4, 4), P(52, 52)], fill=INK),
                                               d.ellipse([P(8, 8), P(48, 48)], fill=(60, 120, 210, 255)),
                                               [d.line([P(28 + 18 * math.cos(a), 28 + 18 * math.sin(a)), P(28 + 22 * math.cos(a), 28 + 22 * math.sin(a))],
                                                       fill=(30, 80, 160, 255), width=3 * S) for a in np.linspace(0, 6.28, 16)]))
trash("TrashStraw", 130, 40, lambda d, P: (d.line([P(8, 30), P(90, 18), P(122, 6)], fill=INK, width=14 * S, joint="curve"),
                                            d.line([P(8, 30), P(90, 18), P(122, 6)], fill=(250, 250, 250, 255), width=9 * S, joint="curve"),
                                            [d.line([P(14 + k * 14, 30 - k * 1.6), P(20 + k * 14, 29 - k * 1.6)], fill=(230, 70, 80, 255), width=6 * S) for k in range(6)]))


def toy(d, P):                                                       # kirik plastik ordek
    d.ellipse([P(10, 30), P(90, 80)], fill=(250, 210, 60, 255), outline=INK, width=4 * S)
    d.ellipse([P(54, 6), P(94, 46)], fill=(250, 210, 60, 255), outline=INK, width=4 * S)
    d.polygon([P(90, 24), P(112, 30), P(90, 36)], fill=(240, 130, 50, 255), outline=INK)
    d.ellipse([P(76, 16), P(84, 24)], fill=INK)
    d.line([P(12, 40), P(28, 52), P(16, 62), P(32, 74)], fill=INK, width=4 * S)


trash("TrashBrokenToy", 116, 86, toy)


def tire(d, P):
    d.ellipse([P(4, 4), P(116, 116)], fill=INK)
    d.ellipse([P(10, 10), P(110, 110)], fill=(56, 58, 62, 255))
    for a in np.linspace(0, 6.28, 20, endpoint=False):
        d.line([P(60 + 44 * math.cos(a), 60 + 44 * math.sin(a)), P(60 + 54 * math.cos(a), 60 + 54 * math.sin(a))], fill=(30, 30, 34, 255), width=4 * S)
    d.ellipse([P(34, 34), P(86, 86)], fill=INK)
    d.ellipse([P(38, 38), P(82, 82)], fill=(80, 150, 150, 255))


trash("TrashTire", 120, 120, tire)


def paint(d, P):
    d.rounded_rectangle([P(10, 14), P(90, 100)], radius=6 * S, fill=INK)
    d.rounded_rectangle([P(14, 18), P(86, 96)], radius=4 * S, fill=(196, 202, 208, 255))
    d.rectangle([P(14, 40), P(86, 70)], fill=(230, 90, 70, 255))
    d.polygon([P(30, 40), P(46, 40), P(40, 62)], fill=(70, 130, 210, 255))
    d.arc([P(20, 0), P(80, 40)], 200, 340, fill=INK, width=4 * S)
    d.line([P(60, 96), P(68, 110), P(56, 116)], fill=(230, 90, 70, 255), width=5 * S)


trash("TrashPaintCan", 100, 120, paint)


def carton(d, P):                                                    # sut kutusu (havuz 3)
    d.polygon([P(22, 30), P(46, 8), P(70, 30), P(70, 108), P(22, 108)], fill=INK)
    d.polygon([P(26, 32), P(46, 13), P(66, 32), P(66, 104), P(26, 104)], fill=(250, 250, 248, 255))
    d.rectangle([P(26, 52), P(66, 84)], fill=(60, 110, 200, 255))
    d.ellipse([P(38, 58), P(54, 76)], fill=(250, 250, 248, 255))


trash("TrashMilkCarton", 92, 116, carton)
print("ok")
