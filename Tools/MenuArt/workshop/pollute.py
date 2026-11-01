"""Dis cepheyi "kirli sahil" haline getirir (exterior.py'den SONRA calistirin):
- arkadaki kasaba evleri silinir: yerine agacli yesil tepe + kum rengi yali (yar)
- deniz bulaniklasir, yag lekeleri ve kopuk eklenir; ustunde sallanan copler ayri sprite (Trash*.png)
- dalga sprite'lari grilesir. Atolye binasi, kapi ve anahtar degismez.
Kullanim: python pollute.py <Assets/Sprites/Workshop>      (2x artboard: 2560x1440)"""
import sys, json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

D = sys.argv[1]
# Istege bagli: kirlilik seviyesi (1 = 1. gun, 0.45 = 2. gun daha temiz), temiz zemin ve cikti adi.
# Cikti adi verilirse sadece zemin yazilir (tekne, dalga ve cop sprite'lari degismez).
LEVEL = float(sys.argv[2]) if len(sys.argv) > 2 else 1.0
SRC = sys.argv[3] if len(sys.argv) > 3 else f"{D}/ExteriorBackground.png"
OUTNAME = sys.argv[4] if len(sys.argv) > 4 else None
S = 4
img = Image.open(SRC).convert("RGB")
W, H = img.size
A = np.asarray(img).astype(np.float32)
yy, xx = np.mgrid[0:H, 0:W]

# ---------------- 1) Kasaba yerine tepe + yar ----------------
TOP, CLIFF, BOTTOM = 515, 650, 848


# Atolye catisinin kenarlari (cati pikselleri y=420..560 arasindan olculdu, egim 1.829)
def roof_left(y): return 2056 - (y - 420) * 1.829 - 14
def roof_right(y):
    if y < 592: return 2138 + (y - 420) * 1.829 + 14
    if y < 682: return 2492 - (y - 592) * 0.55 + 8   # sacak alti, duvara dogru
    return 2447


paint = np.zeros((H, W), bool)
for y in range(TOP, BOTTOM):
    lx = roof_left(y) if y < 672 else 1626
    paint[y, :int(max(0, lx))] = True
    paint[y, int(min(W, roof_right(y))):] = True

# Evlerin arkasi: ustteki tepe rengini asagi uzat (yeni tepelerin ustunde ev kalmasin)
from scipy import ndimage as nd
behind = nd.median_filter(A[TOP - 8], size=(181, 1))   # kule uclari gibi dikenler gitsin
A[TOP:BOTTOM] = np.where(paint[TOP:BOTTOM, :, None], behind[None], A[TOP:BOTTOM])

land = Image.new("RGBA", (W * S, (BOTTOM - TOP + 40) * S), (0, 0, 0, 0))
d = ImageDraw.Draw(land)
P = lambda x, y: (x * S, (y - TOP) * S)
OUT = (62, 66, 72, 255)
xs = np.arange(0, W + 20, 10)
# arka sira tepeler (koyu yesil) ve on sira (acik yesil), ust kenari dalgali
back = [P(x, 560 + 22 * np.sin(x / 210) + 10 * np.sin(x / 77)) for x in xs]
d.polygon([P(0, BOTTOM)] + back + [P(W, BOTTOM)], fill=(86, 128, 96, 255))
d.line(back, fill=OUT, width=3 * S)
front = [P(x, 610 + 16 * np.sin(x / 160 + 1.3)) for x in xs]
d.polygon([P(0, BOTTOM)] + front + [P(W, BOTTOM)], fill=(108, 160, 104, 255))
d.line(front, fill=(70, 110, 76, 255), width=2 * S)
# agaclar: govde + yuvarlak tepe
rng = np.random.default_rng(3)
for x in range(30, W, 92):
    x = x + rng.integers(-18, 18)
    base = 560 + 22 * np.sin(x / 210) + 10 * np.sin(x / 77) + 8
    r = rng.integers(20, 27)
    d.rectangle([P(x - 2.5, base - 6), P(x + 2.5, base + 14)], fill=(70, 60, 48, 255))
    d.ellipse([P(x - r, base - 2 * r), P(x + r, base)], fill=(52, 102, 74, 255), outline=OUT, width=2 * S)
# yar: kum rengi, dikey cizgilerle bolunmus, koyu akinti izleri
cliff_top = [P(x, CLIFF + 6 * np.sin(x / 300)) for x in xs]
d.polygon([P(0, BOTTOM + 30)] + cliff_top + [P(W, BOTTOM + 30)], fill=(226, 204, 170, 255))
for x in range(0, W, 26):
    if rng.random() < 0.55:
        w = rng.integers(10, 26); top = CLIFF + 18 + rng.integers(0, 30); bot = top + rng.integers(60, 130)
        d.rounded_rectangle([P(x, top), P(x + w, bot)], radius=w * S // 2, fill=(206, 182, 146, 255))
for x in range(rng.integers(80, 200), W, 215):
    d.line([P(x, CLIFF + 6 * np.sin(x / 300)), P(x + rng.integers(-8, 8), BOTTOM - 26)], fill=(150, 128, 104, 255), width=2 * S)
d.line(cliff_top, fill=OUT, width=3 * S)
d.rectangle([P(0, BOTTOM - 24), P(W, BOTTOM + 30)], fill=(196, 172, 138, 255))   # yarin dibi, islak kum
d.line([P(0, BOTTOM - 24), P(W, BOTTOM - 24)], fill=(160, 136, 108, 255), width=2 * S)
land = land.resize((W, BOTTOM - TOP + 40), Image.LANCZOS)
L = np.asarray(land).astype(np.float32)[: BOTTOM - TOP]
m = paint[TOP:BOTTOM, :, None] * (L[..., 3:] / 255)
A[TOP:BOTTOM] = A[TOP:BOTTOM] * (1 - m) + L[..., :3] * m

# ---------------- 2) Kirli deniz ----------------
r, g, b = A[..., 0], A[..., 1], A[..., 2]
sea = (yy >= BOTTOM - 2) & (b > r + 35) & (g > r + 35)
murk = np.array([92, 112, 104], np.float32)
mix = 0.5 * LEVEL
shade = A * (1 - mix) + murk * mix
shade *= 0.92 + 0.08 * np.sin(xx / 47.0 + yy / 23.0)[..., None]          # hafif bulanik dalgalanma
A = np.where(sea[..., None], shade, A)

over = Image.new("RGBA", (W, H), (0, 0, 0, 0))
od = ImageDraw.Draw(over)
# yag lekeleri: gokkusagi halkalari
OIL = ((230, 1120, 150, 46), (760, 920, 120, 34), (330, 1390, 170, 40), (1200, 1180, 90, 28))
for cx, cy, rx, ry in OIL[:max(1, round(len(OIL) * LEVEL))]:
    for k, col in enumerate([(150, 90, 170), (70, 140, 190), (90, 170, 110), (210, 190, 80), (200, 110, 80)]):
        f = 1 - k * 0.16
        od.ellipse([cx - rx * f, cy - ry * f, cx + rx * f, cy + ry * f], outline=col + (int(70 * max(LEVEL, 0.6)),), width=7)
# kopuk / pislik lekeleri
FOAM = ((520, 990, 1.0), (140, 1290, 0.8), (1150, 1000, 0.7), (900, 1390, 0.9), (420, 1240, 0.6))
for cx, cy, s in FOAM[:max(1, round(len(FOAM) * LEVEL))]:
    for _ in range(9):
        ox, oy, rr = rng.normal(0, 26 * s), rng.normal(0, 8 * s), rng.uniform(10, 22) * s
        od.ellipse([cx + ox - rr, cy + oy - rr * 0.55, cx + ox + rr, cy + oy + rr * 0.55], fill=(226, 214, 160, 150))
over = over.filter(ImageFilter.GaussianBlur(1.5))
O = np.asarray(over).astype(np.float32)
oa = O[..., 3:] / 255 * sea[..., None]
A = A * (1 - oa) + O[..., :3] * oa
name = OUTNAME or "ExteriorBackground"
Image.fromarray(np.clip(A, 0, 255).astype(np.uint8)).save(f"{D}/{name}.png", optimize=True)
if OUTNAME:
    print("ok", name)
    sys.exit(0)
Image.fromarray(np.clip(A, 0, 255).astype(np.uint8)).resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{D}/ExteriorBackdrop.png")

# Tekne kenarindaki eski deniz rengi (yari saydam pikseller) de bulaniklassin
bp = f"{D}/Boat.png"
bt = np.asarray(Image.open(bp).convert("RGBA")).astype(np.float32)
edge = (bt[..., 3] < 250) & (bt[..., 2] > bt[..., 0] + 35) & (bt[..., 1] > bt[..., 0] + 35)
bt[edge, :3] = bt[edge, :3] * 0.5 + murk * 0.5
Image.fromarray(bt.astype(np.uint8), "RGBA").save(bp)

# Dalgalar: kirli, grimsi kopuk
for i in range(8):
    p = f"{D}/Wave{i}.png"
    w = np.asarray(Image.open(p).convert("RGBA")).astype(np.float32)
    w[..., :3] = w[..., :3] * 0.55 + np.array([170, 172, 150]) * 0.45
    Image.fromarray(w.astype(np.uint8), "RGBA").save(p)

# ---------------- 3) Yuzen copler (ayri sprite, Bob ile sallanir) ----------------
INK = (40, 44, 48, 255)


def sprite(name, w, h, draw):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    draw(ImageDraw.Draw(im), lambda x, y: (x * S, y * S))
    im = im.resize((w, h), Image.LANCZOS)
    im.save(f"{D}/{name}.png")


def bottle(d, P):
    d.rounded_rectangle([P(6, 14), P(78, 40)], radius=12 * S, fill=INK)
    d.rounded_rectangle([P(9, 17), P(75, 37)], radius=10 * S, fill=(176, 214, 230, 255))
    d.rectangle([P(18, 17), P(50, 37)], fill=(70, 150, 200, 255))
    d.rounded_rectangle([P(76, 20), P(92, 34)], radius=3 * S, fill=INK)
    d.rounded_rectangle([P(78, 22), P(90, 32)], radius=2 * S, fill=(230, 80, 70, 255))
    d.line([P(14, 21), P(70, 21)], fill=(240, 250, 255, 255), width=2 * S)


def cup(d, P):
    d.polygon([P(10, 8), P(54, 8), P(48, 52), P(16, 52)], fill=INK)
    d.polygon([P(13, 11), P(51, 11), P(45.5, 49), P(18.5, 49)], fill=(250, 248, 240, 255))
    d.rectangle([P(16, 24), P(48, 32)], fill=(120, 170, 200, 255))
    d.rounded_rectangle([P(6, 4), P(58, 12)], radius=3 * S, fill=INK)


def bag(d, P):
    d.polygon([P(12, 30), P(30, 8), P(36, 22), P(56, 6), P(62, 30), P(76, 54), P(8, 58)], fill=(80, 90, 96, 200))
    d.polygon([P(15, 32), P(30, 13), P(36, 27), P(55, 11), P(59, 32), P(71, 52), P(13, 55)], fill=(236, 240, 242, 190))
    d.line([P(22, 40), P(40, 46), P(60, 40)], fill=(190, 200, 205, 200), width=2 * S)


def can(d, P):
    d.rounded_rectangle([P(6, 12), P(66, 40)], radius=6 * S, fill=INK)
    d.rounded_rectangle([P(9, 15), P(63, 37)], radius=4 * S, fill=(214, 66, 58, 255))
    d.rectangle([P(24, 15), P(44, 37)], fill=(240, 236, 228, 255))
    d.rectangle([P(56, 15), P(63, 37)], fill=(190, 190, 186, 255))


def plank(d, P):
    d.polygon([P(4, 26), P(150, 6), P(156, 22), P(10, 44)], fill=INK)
    d.polygon([P(8, 27), P(148, 9.5), P(152, 20), P(12, 40)], fill=(150, 98, 58, 255))
    d.line([P(20, 30), P(140, 15)], fill=(118, 74, 42, 255), width=2 * S)
    d.ellipse([P(110, 14), P(116, 20)], fill=INK)


def net(d, P):
    for i in range(7):
        d.line([P(8 + i * 14, 8), P(24 + i * 12, 62)], fill=(60, 80, 70, 230), width=2 * S)
        d.line([P(4, 12 + i * 8), P(108, 4 + i * 9)], fill=(60, 80, 70, 230), width=2 * S)
    d.ellipse([P(40, 26), P(56, 40)], fill=(230, 140, 60, 255), outline=INK, width=2 * S)


TRASH = [  # ad, cizim, boyut (2x px), merkez (2x artboard)
    ("TrashBottle0", bottle, (96, 52), (300, 960)),
    ("TrashBag0", bag, (84, 64), (150, 1180)),
    ("TrashCan0", can, (72, 52), (930, 945)),
    ("TrashPlank0", plank, (160, 50), (560, 900)),
    ("TrashCup0", cup, (64, 60), (470, 1210)),
    ("TrashBottle1", bottle, (96, 52), (1290, 930)),
    ("TrashNet0", net, (112, 66), (1260, 1210)),
    ("TrashBag1", bag, (84, 64), (1010, 1390)),
    ("TrashCan1", can, (72, 52), (90, 1000)),
    ("TrashCup1", cup, (64, 60), (1180, 1060)),
]
rects = {}
for name, fn, (w, h), (cx, cy) in TRASH:
    sprite(name, w, h, fn)
    rects[name] = [cx - w // 2, cy - h // 2, w, h]
json.dump(rects, open(f"{D}/../../../Tools/MenuArt/workshop/trash_rects.json", "w"), indent=1)
print(json.dumps(rects))
