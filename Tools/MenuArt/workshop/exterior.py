"""Atolye disi (anahtar sahnesi) gorselleri. Kullanim: python exterior.py <6.png> <cikis> <fonts>
Koordinatlar 1280x720 tasarimda; cikti 2x (2560x1440)."""
import sys; sys.path.insert(0, sys.path[0])
from common import *
from PIL import ImageDraw, ImageFont
src, out, fonts = sys.argv[1], sys.argv[2], sys.argv[3]
A = load(src); H, W = A.shape[:2]
K = 2
O = Out(out, K)
C = A.copy()

# ---- Dalgalar: denizdeki acik renkli kivrimlar (deniz yatay bantli, satir doldurma yeter) ----
sea = np.zeros((H, W), bool); sea[425:, :720] = True
light = sea & (A[..., 0] > 110) & (A[..., 1] > 185) & (A[..., 2] > 185) & (A[..., 0] < 235)
lab, n = nd.label(nd.binary_dilation(light, iterations=4))
waves = []
for i, s in enumerate(nd.find_objects(lab)):
    h, w = s[0].stop - s[0].start, s[1].stop - s[1].start
    inboat = s[0].start > 560 and s[1].start > 255 and s[1].stop < 512 and s[0].stop < 690
    if w >= 45 and h <= 26 and not inboat: waves.append((i + 1, s))
wave_mask = np.zeros((H, W), bool)
for i, s in waves: wave_mask |= (lab == i) & nd.binary_dilation(light, iterations=1)
C = row_fill(C, nd.binary_dilation(wave_mask, iterations=1))

# ---- Tekne ----
bx0, by0, bx1, by1 = 262, 532, 506, 690
ref = C[:, 240:258].mean(1)
boat = np.zeros((H, W), bool)
dist = np.sqrt(((C[by0:by1, bx0:bx1] - ref[by0:by1, None]) ** 2).sum(-1))
boat[by0:by1, bx0:bx1] = dist > 30
lab2, n2 = nd.label(nd.binary_dilation(boat, iterations=2))
sizes = nd.sum(boat, lab2, range(1, n2 + 1))
boat = nd.binary_fill_holes(lab2 == np.argmax(sizes) + 1)
C = row_fill(C, nd.binary_dilation(boat, iterations=1))

# ---- Bulutlar: kutunun ust/alt satirlarindan gokyuzu, fark = bulut ----
cloud_boxes = [(500, 18, 622, 74), (848, 62, 954, 114)]
cloud_alpha = []
for (x0, y0, x1, y1) in cloud_boxes:
    m = np.zeros((H, W), bool); m[y0:y1, x0:x1] = True
    before = C.copy()
    C = col_fill(C, m)
    d = (before - C).mean(-1)
    a = np.zeros((H, W)); a[m] = np.clip(d[m] / np.maximum(1, (251 - C[m].mean(-1))), 0, 1)
    a = np.clip((a - 0.06) / 0.94, 0, 1)
    cloud_alpha.append(a)

# ---- Gorev karti: yazilari sil ----
C[38:132, 40:452] = [251, 248, 241]

# ---- Anahtar ve etiket kesimleri (orijinalden) ----
keyarea = np.zeros((H, W), bool); keyarea[462:546, 938:992] = True
gold = keyarea & (A[..., 0] > 150) & (A[..., 2] < 125) & (A[..., 0] - A[..., 2] > 90)
outline = keyarea & (A.max(-1) < 175) & (A[..., 0] - A[..., 2] > 45)
key = gold | (outline & nd.binary_dilation(gold, iterations=3))
lab4, n4 = nd.label(key); sizes = nd.sum(key, lab4, range(1, n4 + 1))
key = nd.binary_fill_holes(lab4 == np.argmax(sizes) + 1)
pz = np.zeros((H, W), bool); pz[468:528, 1000:1180] = True
pill_col = pz & (A[..., 0] > 220) & (A[..., 1] > 170) & (A[..., 2] < 120)
pill = nd.binary_fill_holes(nd.binary_dilation(pill_col, iterations=4)) & pz

# ---- 2x arka plan + prosedurel kapi / tabela ----
BG = Image.fromarray(C.astype(np.uint8)).resize((W * K, H * K), Image.LANCZOS).convert("RGBA")
S = 4                      # 2x ciktinin 4 kati cozunurlukte ciz, sonra kucult
F = K * S                  # 1x koordinat -> cizim pikseli
OUTLINE = (62, 42, 30, 255); BAND = (90, 50, 28, 255); WOOD = (156, 98, 54, 255)
GROOVE = (112, 66, 38, 255); GLASS = (169, 200, 204, 255); STRAP = (46, 42, 40, 255); PLATE = (233, 211, 168, 255)


def layer(box):
    x0, y0, x1, y1 = box
    im = Image.new("RGBA", ((x1 - x0) * F, (y1 - y0) * F), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (int((x - x0) * F), int((y - y0) * F)))


def arch_poly(x0, x1, y_spring, y_top, y_bot, P, n=48):
    cx, rx, ry = (x0 + x1) / 2, (x1 - x0) / 2, y_spring - y_top
    pts = [P(x0, y_bot)]
    for i in range(n + 1):
        t = np.pi * (1 - i / n)
        pts.append(P(cx + rx * np.cos(t), y_spring - ry * np.sin(t)))
    pts.append(P(x1, y_bot))
    return pts


def finish(im, box):
    x0, y0, x1, y1 = box
    return im.resize(((x1 - x0) * K, (y1 - y0) * K), Image.LANCZOS)


# Kasa + ic (kapi acilinca gorunen sicak isikli ic mekan) -> arka plana gom
DOOR = (888, 448, 1034, 664)
im, d, P = layer(DOOR)
d.polygon(arch_poly(893, 1029, 474, 452, 662, P), fill=OUTLINE)
d.polygon(arch_poly(897, 1025, 475, 456, 662, P), fill=BAND)
d.polygon(arch_poly(903, 1019, 477, 463, 658, P), fill=OUTLINE)
inner = arch_poly(905, 1017, 478, 465, 657, P)
d.polygon(inner, fill=(74, 46, 28, 255))
gy, gx = np.mgrid[0:im.height, 0:im.width] / F
gx, gy = gx + DOOR[0], gy + DOOR[1]
g = np.clip(1 - np.sqrt(((gx - 961) / 62) ** 2 + ((gy - 575) / 105) ** 2), 0, 1) ** 1.3
base, warm = np.array([74, 46, 28]), np.array([250, 190, 110])
col = base + (warm - base) * g[..., None]
glow = Image.fromarray(np.dstack([col, np.full(g.shape, 255)]).astype(np.uint8), "RGBA")
mask = Image.new("L", im.size, 0); ImageDraw.Draw(mask).polygon(inner, fill=255)
im.paste(glow, (0, 0), mask)
d = ImageDraw.Draw(im)
d.rectangle([P(905, 520), P(1017, 524)], fill=(96, 60, 34, 255))          # raf
d.rectangle([P(905, 640), P(1017, 657)], fill=(120, 78, 44, 255))         # zemin
d.rectangle([P(905, 638), P(1017, 641)], fill=(90, 56, 32, 255))
BG.alpha_composite(finish(im, DOOR), (DOOR[0] * K, DOOR[1] * K))
# Kapinin onundeki saksi tekrar one
pb = np.zeros((H, W), bool); pb[598:664, 868:926] = True
pot = pb & ((np.abs(A - [201, 96, 63]).sum(-1) < 40) | (np.abs(A - [160, 156, 104]).sum(-1) < 60))
pot = nd.binary_fill_holes(nd.binary_dilation(pot, iterations=2)) & pb
pa = Image.fromarray((soft(pot, 0.5) * 255).astype(np.uint8)).resize((W * K, H * K), Image.LANCZOS)
BG.paste(Image.fromarray(A.astype(np.uint8)).resize((W * K, H * K), Image.LANCZOS).convert("RGBA"), (0, 0), pa)


def leaf(name, side):
    """Kapi kanadi (ayri sprite; mentese dis kenarda)."""
    x0, x1 = (905, 962) if side < 0 else (960, 1017)
    box = (x0, 463, x1, 658)
    im, d, P = layer(box)
    full = Image.new("RGBA", ((1017 - 905) * F, (658 - 463) * F), (0, 0, 0, 0))
    fd = ImageDraw.Draw(full)
    Pf = lambda x, y: (int((x - 905) * F), int((y - 463) * F))
    fd.polygon(arch_poly(905, 1017, 478, 465, 657, Pf), fill=OUTLINE)
    fd.polygon(arch_poly(907, 1015, 479, 467, 655, Pf), fill=WOOD)
    for gx in (920, 934, 948, 974, 988, 1002):                                # tahta oluklari
        fd.rectangle([Pf(gx - 0.8, 470), Pf(gx + 0.8, 655)], fill=GROOVE)
    for wx0, wx1 in ((915, 954), (968, 1007)):                                # pencere
        fd.rounded_rectangle([Pf(wx0, 479), Pf(wx1, 531)], radius=3 * F, fill=OUTLINE)
        fd.rounded_rectangle([Pf(wx0 + 2.2, 481.2), Pf(wx1 - 2.2, 528.8)], radius=2 * F, fill=GLASS)
        mx = (wx0 + wx1) / 2
        fd.rectangle([Pf(mx - 1.4, 480), Pf(mx + 1.4, 530)], fill=OUTLINE)
        fd.rectangle([Pf(wx0, 503.6), Pf(wx1, 506.4)], fill=OUTLINE)
        fd.polygon([Pf(wx0 + 4, 484), Pf(wx0 + 9, 484), Pf(wx0 + 4, 492)], fill=(214, 233, 235, 255))
    for sx0, sx1 in ((907, 931), (991, 1015)):                                # mentese kayislari
        fd.rectangle([Pf(sx0, 554), Pf(sx1, 560)], fill=STRAP)
        fd.rectangle([Pf(sx0, 626), Pf(sx1, 632)], fill=STRAP)
    fd.rounded_rectangle([Pf(919, 564), Pf(1003, 592)], radius=4 * F, fill=OUTLINE)   # isim plakasi
    fd.rounded_rectangle([Pf(921.5, 566.5), Pf(1000.5, 589.5)], radius=3 * F, fill=PLATE)
    fd.rectangle([Pf(960.2, 466), Pf(961.8, 657)], fill=OUTLINE)               # orta derz
    fd.ellipse([Pf(953, 600), Pf(958, 605)], fill=STRAP)                      # kulplar
    fd.ellipse([Pf(964, 600), Pf(969, 605)], fill=STRAP)
    fd.rounded_rectangle([Pf(957.5, 607), Pf(964.5, 620)], radius=2 * F, fill=STRAP)  # kilit
    fd.ellipse([Pf(959.6, 609.5), Pf(962.4, 612.3)], fill=(150, 120, 60, 255))
    crop = full.crop((int((x0 - 905) * F), 0, int((x1 - 905) * F), full.height))
    im.paste(crop, (0, 0))
    finish(im, box).save(f"{out}/{name}.png", optimize=True)
    O.rects[name] = [x0 * K, 463 * K, (x1 - x0) * K, (658 - 463) * K]


leaf("DoorLeafLeft", -1)
leaf("DoorLeafRight", 1)

# Etiketin altinda kalacak "OPEN" tabelasi (etiket kalkinca gorunur)
SIGN = (1004, 466, 1178, 530)
im, d, P = layer(SIGN)
d.rounded_rectangle([P(1012, 475), P(1170, 520)], radius=10 * F, fill=OUTLINE)
d.rounded_rectangle([P(1014.5, 477.5), P(1167.5, 517.5)], radius=8 * F, fill=(238, 222, 180, 255))
d.ellipse([P(1027, 494), P(1034, 501)], fill=(60, 140, 90, 255))
d.ellipse([P(1148, 494), P(1155, 501)], fill=(60, 140, 90, 255))
font = ImageFont.truetype(f"{fonts}/Baloo2-ExtraBold.ttf", 30 * F)
tw = d.textlength("OPEN", font=font)
d.text((P(1091, 0)[0] - tw / 2, P(0, 476)[1]), "OPEN", font=font, fill=(60, 140, 90, 255))
BG.alpha_composite(finish(im, SIGN), (SIGN[0] * K, SIGN[1] * K))

BG.convert("RGB").save(f"{out}/ExteriorBackground.png", optimize=True)

# ---- Kesimler ----
for k, (i, s) in enumerate(waves):
    m = (lab == i) & nd.binary_dilation(wave_mask, iterations=1)
    O.cut(f"Wave{k}", A, soft(m, 0.5), bg=C)
O.cut("Boat", A, soft(boat, 0.5), bg=C)
for k, a in enumerate(cloud_alpha):
    O.cut(f"Cloud{k}", A, a, bg=C)
KEYBOX = (942, 462, 980, 546)
im, d, P = layer(KEYBOX)
KO, KG, KH = (110, 70, 25, 255), (240, 180, 60, 255), (255, 224, 130, 255)
d.ellipse([P(946, 466), P(974, 494)], fill=KO)
d.ellipse([P(948.5, 468.5), P(971.5, 491.5)], fill=KG)
d.ellipse([P(954, 474), P(966, 486)], fill=KO)
d.ellipse([P(956.2, 476.2), P(963.8, 483.8)], fill=(250, 244, 228, 255))
d.rounded_rectangle([P(955.5, 491), P(964.5, 542)], radius=2 * F, fill=KO)
d.rounded_rectangle([P(957.8, 489), P(962.2, 539.8)], radius=1 * F, fill=KG)
for ty0, ty1, tx1 in ((516, 523, 975), (529, 536, 972)):
    d.rounded_rectangle([P(962, ty0), P(tx1, ty1)], radius=1.5 * F, fill=KO)
    d.rectangle([P(961, ty0 + 2.2), P(tx1 - 2.2, ty1 - 2.2)], fill=KG)
d.arc([P(950, 470), P(970, 490)], 200, 280, fill=KH, width=int(2 * F))
finish(im, KEYBOX).save(f"{out}/Key.png", optimize=True)
O.rects["Key"] = [KEYBOX[0] * K, KEYBOX[1] * K, (KEYBOX[2] - KEYBOX[0]) * K, (KEYBOX[3] - KEYBOX[1]) * K]
O.cut("TakeKeyButton", A, soft(pill, 0.5))


def radial(name, size, color, power):
    yy2, xx2 = np.mgrid[0:size, 0:size]
    r = np.sqrt((xx2 - size / 2 + 0.5) ** 2 + (yy2 - size / 2 + 0.5) ** 2) / (size / 2)
    a = np.clip(1 - r, 0, 1) ** power
    Image.fromarray(np.dstack([np.full((size, size), c) for c in color] + [a * 255]).astype(np.uint8), "RGBA").save(f"{out}/{name}.png")


radial("Glow", 256, (255, 226, 120), 1.4)
s = 128; yy2, xx2 = np.mgrid[0:s, 0:s] - s / 2 + 0.5
star = np.clip(1 - (np.abs(xx2) ** 0.5 + np.abs(yy2) ** 0.5) / (s / 2) ** 0.5, 0, 1) ** 0.6
Image.fromarray(np.dstack([np.full((s, s), 255), np.full((s, s), 252), np.full((s, s), 235), star * 255]).astype(np.uint8), "RGBA").save(f"{out}/Sparkle.png")

O.save_rects("ext_rects.json")
print(json.dumps(O.rects))
