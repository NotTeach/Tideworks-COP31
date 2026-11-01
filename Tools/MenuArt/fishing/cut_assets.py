"""Kullanicinin verdigi gorsellerden oyun asset'lerini keser -> Assets/Art/Fishing
Kullanim: python cut_assets.py <olta.jpg> <baliklar.png> <copler.png> <tekne.png> <cikis>"""
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as nd

rod_p, fish_p, trash_p, boat_p, out = sys.argv[1:6]
os.makedirs(out, exist_ok=True)


def save(name, rgb, alpha, box=None, scale=1.0):
    a = np.clip(alpha, 0, 1)
    ys, xs = np.where(a > 0.02)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    im = Image.fromarray(np.dstack([rgb[y0:y1, x0:x1], a[y0:y1, x0:x1] * 255]).astype(np.uint8), "RGBA")
    if scale != 1.0:
        im = im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
    im.save(f"{out}/{name}.png", optimize=True)
    print(name, im.size)


def largest(m):
    lab, n = nd.label(m)
    if n == 0: return m
    sizes = nd.sum(m, lab, range(1, n + 1))
    return lab == np.argmax(sizes) + 1


soft = lambda m, s=0.7: np.clip(nd.gaussian_filter(m.astype(float), s) * 1.4 - 0.2, 0, 1)

# ---- Olta (acik gri zemin) ----
A = np.asarray(Image.open(rod_p).convert("RGB")).astype(np.float32)
bg = np.array([247, 247, 247], np.float32)
diff = np.abs(A - bg).max(-1)
alpha = np.clip((diff - 10) / 35, 0, 1)
save("Rod", A, alpha, scale=0.5)

# ---- Baliklar (saydam zemin; golgeler ayri parca, atilir) ----
F = np.asarray(Image.open(fish_p).convert("RGBA")).astype(np.float32)
boxes = {  # 2400x1040 gorselde
    "FishSeaBream": (20, 60, 660, 450), "FishRedMullet": (820, 90, 1460, 420), "FishAnchovy": (1650, 110, 2290, 380),
    "FishPuffer": (80, 610, 640, 942), "FishMackerel": (810, 630, 1520, 900),
}
for name, (x0, y0, x1, y1) in boxes.items():
    sub = F[y0:y1, x0:x1]
    m = largest(sub[..., 3] > 30)
    m = nd.binary_fill_holes(nd.binary_dilation(m, iterations=2))
    a = sub[..., 3] / 255 * m
    save(name, sub[..., :3], a, scale=0.5)

# ---- Copler (gri zemin + beyaz tabaklar; renkli ya da koyu pikseller = esya) ----
T = np.asarray(Image.open(trash_p).convert("RGB")).astype(np.float32)
mx, mn = T.max(-1), T.min(-1)
item = (mx < 150) | ((mx - mn) > 34)
trash = {  # 1502x408 gorselde
    "TrashGlassBottle": (35, 115, 200, 190), "TrashRope": (255, 95, 445, 185), "TrashGhostNet": (520, 70, 720, 180),
    "TrashPlank": (810, 130, 940, 180), "TrashBuoy": (1060, 115, 1240, 190), "TrashSixPack": (1320, 120, 1460, 185),
    "TrashWaterBottle": (30, 295, 185, 355), "TrashPlasticBag": (295, 260, 430, 350), "TrashWoodBoard": (530, 285, 705, 345),
    "TrashGlassJar": (835, 285, 920, 355), "TrashFlipFlop": (1080, 285, 1150, 360),
}
HOLLOW = {"TrashRope", "TrashSixPack"}
for name, (x0, y0, x1, y1) in trash.items():
    sub = T[y0:y1, x0:x1]
    m = item[y0:y1, x0:x1]
    m = nd.binary_closing(m, iterations=3)
    if name not in HOLLOW:  # halat ve halkalarin ici bos kalmali
        m = nd.binary_fill_holes(m)
    m = nd.binary_opening(m, iterations=1) | (item[y0:y1, x0:x1] & nd.binary_dilation(m, iterations=6))
    save(name, sub, soft(m, 0.6), scale=1.0)

# ---- Tekne (beyaz zemin) ----
B = np.asarray(Image.open(boat_p).convert("RGB")).astype(np.float32)
mx, mn = B.max(-1), B.min(-1)
m = (mx < 200) | ((mx - mn) > 25)
m = nd.binary_fill_holes(nd.binary_closing(m, iterations=4))
m = largest(m)
save("BoatTop", B, soft(m, 0.8), scale=0.42)


# ---- Gorsellerde olmayan ama listede gereken copler (ayni cizgi tarzinda cizilir) ----
from PIL import ImageDraw
S = 4
INK = (74, 58, 66, 255)


def draw(name, w, h, fn):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    fn(ImageDraw.Draw(im), lambda x, y: (x * S, y * S))
    im.resize((w, h), Image.LANCZOS).save(f"{out}/{name}.png")
    print(name, (w, h))


def tin(d, P):  # yatik konserve kutusu
    d.rounded_rectangle([P(4, 8), P(112, 56)], radius=10 * S, fill=INK)
    d.rounded_rectangle([P(8, 12), P(108, 52)], radius=8 * S, fill=(236, 236, 230, 255))
    d.rectangle([P(30, 12), P(86, 52)], fill=(214, 72, 58, 255))
    d.line([P(30, 12), P(30, 52)], fill=INK, width=3 * S); d.line([P(86, 12), P(86, 52)], fill=INK, width=3 * S)
    for x in (16, 98):
        d.line([P(x, 16), P(x, 48)], fill=(190, 190, 186, 255), width=2 * S)
    d.line([P(36, 22), P(80, 22)], fill=(255, 220, 210, 255), width=3 * S)


def tuna(d, P):  # basik ton baligi kutusu
    d.ellipse([P(6, 28), P(118, 64)], fill=INK)
    d.rectangle([P(6, 22), P(118, 46)], fill=INK)
    d.ellipse([P(6, 6), P(118, 42)], fill=INK)
    d.ellipse([P(10, 32), P(114, 60)], fill=(200, 200, 196, 255))
    d.rectangle([P(10, 24), P(114, 46)], fill=(200, 200, 196, 255))
    d.rectangle([P(10, 30), P(114, 40)], fill=(236, 186, 70, 255))
    d.ellipse([P(10, 10), P(114, 38)], fill=(222, 222, 218, 255), outline=INK, width=3 * S)
    d.ellipse([P(28, 16), P(96, 32)], outline=(170, 170, 166, 255), width=2 * S)
    d.ellipse([P(84, 18), P(96, 26)], fill=(170, 170, 166, 255))


def paper(d, P):  # katlanmis gazete
    d.polygon([P(6, 18), P(96, 6), P(112, 46), P(20, 62)], fill=INK)
    d.polygon([P(10, 20), P(94, 10), P(107, 44), P(22, 57)], fill=(236, 234, 224, 255))
    d.polygon([P(52, 15), P(94, 10), P(107, 44), P(64, 50)], fill=(222, 220, 208, 255))
    d.line([P(52, 15), P(64, 50)], fill=INK, width=2 * S)
    for i in range(4):
        d.line([P(20 + i * 2, 26 + i * 7), P(50 + i * 2, 22 + i * 7)], fill=(120, 120, 120, 255), width=2 * S)
    d.rectangle([P(70, 20), P(88, 32)], fill=(150, 150, 150, 255))


draw("TrashTinCan", 116, 64, tin)
draw("TrashTunaCan", 124, 70, tuna)
draw("TrashNewspaper", 116, 66, paper)
