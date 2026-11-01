"""2. gun atolye: Elif'li atolye ici, oyuncak ayi tezgahi, Pamuk katmanlari, alet / raf yuvalari.
Kullanim: python day2_workshop_art.py <Assets/Sprites> <22.png (Elif)> <23.png (tezgah)>
Cikti: Assets/Sprites/Day2 (2x artboard 2560x1440)."""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as nd

SP, elif_src, bench_src = sys.argv[1:4]
OUT = f"{SP}/Day2"
os.makedirs(OUT, exist_ok=True)
S = 4
INK = (60, 40, 30, 255)


def canvas(w, h):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (x * S, y * S))


def save(im, name, w, h):
    im.resize((w, h), Image.LANCZOS).save(f"{OUT}/{name}.png")


def map_small(img_path, scale, ox, oy, box1x, size2x):
    """Kucuk tasarimdan 1280 koordinatli bir kutuyu 2x boyuta buyuterek al."""
    x0, y0, x1, y1 = box1x
    im = Image.open(img_path).convert("RGB")
    crop = im.crop((ox + x0 * scale, oy + y0 * scale, ox + x1 * scale, oy + y1 * scale))
    crop = crop.resize(size2x, Image.LANCZOS)
    return crop.filter(ImageFilter.UnsharpMask(radius=3, percent=90, threshold=2))


# ================= Atolye ici (2. gun) =================
A = Image.open(f"{SP}/Workshop/WorkshopBackground.png").convert("RGB")      # dialog ve altin zaten silinmis
# Hatice yerine Elif + Pamuk (22.png: x = 4.48 + 0.576 * X, y = 18.64 + 0.576 * Y)
box = (650, 210, 835, 528)
patch = map_small(elif_src, 0.576, 4.48, 18.64, box, ((box[2] - box[0]) * 2, (box[3] - box[1]) * 2))
m = Image.new("L", patch.size, 0)
ImageDraw.Draw(m).rounded_rectangle([10, 10, patch.size[0] - 10, patch.size[1] + 40], radius=30, fill=255)
m = m.filter(ImageFilter.GaussianBlur(8))
A.paste(patch, (box[0] * 2, box[1] * 2), m)
a = np.array(A)
bar = a[40, 1200].copy()
a[80:118, 160:540] = bar                       # "Grandpa's workshop · Day 1" (koddan yazilir)
a[48:96, 996:1100] = bar                       # "34%"
Image.fromarray(a).save(f"{OUT}/InteriorDay2Background.png", optimize=True)
Image.fromarray(a).resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{OUT}/InteriorDay2Backdrop.png")

# Isim etiketi (sari) ve gorev paneli parcalari
im, d, P = canvas(150, 46)
d.rounded_rectangle([P(0, 0), P(150, 46)], radius=23 * S, fill=INK)
d.rounded_rectangle([P(3, 3), P(147, 43)], radius=20 * S, fill=(242, 190, 72, 255))
save(im, "NamePillYellow", 300, 92)

panel = Image.open(f"{SP}/Workshop/TR/TasksPanel.png").convert("RGBA")       # yazilari silinmis panel
pa = np.array(panel)
paper = pa[150, 400, :3].copy()
pa[200:1110, 70:720, :3] = paper                # satirlar (kutular, cubuklar) temizlenir; satirlar Unity'de
Image.fromarray(pa).save(f"{OUT}/TasksPanelBlank.png")
im, d, P = canvas(660, 136)
d.rounded_rectangle([P(0, 0), P(660, 136)], radius=24 * S, fill=(232, 184, 80, 255))
d.rounded_rectangle([P(5, 5), P(655, 131)], radius=20 * S, fill=(252, 243, 220, 255))
save(im, "TaskHighlight", 660, 136)

# ================= Tezgah (2. gun) =================
B = Image.open(f"{SP}/Workbench/TR/WorkbenchBackground.png").convert("RGB")  # yazilari silinmis
b = np.array(B).astype(np.float32)
H, W = b.shape[:2]


def row_fill(img, x0, x1, y0, y1):
    """Kutuyu ayni satirdaki sol/sag piksellerle doldur (yatay tahta duvar icin)."""
    L, R = img[y0:y1, x0 - 4:x0].mean(1), img[y0:y1, x1:x1 + 4].mean(1)
    t = np.linspace(0, 1, x1 - x0)[None, :, None]
    img[y0:y1, x0:x1] = L[:, None] * (1 - t) + R[:, None] * t


row_fill(b, 1150, 2000, 290, 420)            # radyo sapi (sagda parca tepsisi var)
row_fill(b, 1150, 2340, 420, 1000)           # radyo govdesi, golgesi ve kablo
# Hatice portresi ve adi -> panel beyazi
panel_white = np.median(b[560:700, 120:540].reshape(-1, 3), axis=0)
b[220:350, 70:480] = panel_white
# Alet ve raf yuvalari (yeni yuvalar ustune gelecek) -> tepsi rengi
tray = b[1380, 600].copy()
b[1166:1386, 206:1140] = tray
b[1166:1386, 1316:2500] = tray
B = Image.fromarray(np.clip(b, 0, 255).astype(np.uint8)).convert("RGBA")

# Yesil kesim mati
im, d, P = canvas(720, 744)
d.rounded_rectangle([P(0, 0), P(720, 744)], radius=34 * S, fill=INK)
d.rounded_rectangle([P(8, 8), P(712, 736)], radius=28 * S, fill=(84, 142, 104, 255))
for x in range(48, 712, 56): d.line([P(x, 14), P(x, 730)], fill=(110, 166, 128, 255), width=2 * S)
for y in range(48, 736, 56): d.line([P(14, y), P(706, y)], fill=(110, 166, 128, 255), width=2 * S)
mat = im.resize((720, 744), Image.LANCZOS)
B.alpha_composite(mat, (1250, 246))
# Elif portresi (23.png: x = 4.44 + 0.578 * X, y = 52.54 + 0.578 * Y)
por = map_small(bench_src, 0.578, 4.44, 52.54, (40, 114, 98, 172), (120, 120)).convert("RGBA")
pm = Image.new("L", (480, 480), 0); ImageDraw.Draw(pm).ellipse([6, 6, 474, 474], fill=255)
por.putalpha(pm.resize((120, 120), Image.LANCZOS))
B.alpha_composite(por, (80, 228))
B.convert("RGB").save(f"{OUT}/BenchDay2Background.png", optimize=True)
B.convert("RGB").resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{OUT}/BenchDay2Backdrop.png")

# ================= Pamuk (oyuncak ayi) katmanlari: hepsi ayni 620x700 tuval =================
TW, TH = 620, 700
FUR, FUR2, LIGHT = (176, 114, 64, 255), (150, 94, 52, 255), (234, 200, 152, 255)


def teddy_layer(name, draw):
    im, d, P = canvas(TW, TH)
    draw(d, P)
    save(im, name, TW, TH)


def oval(d, P, cx, cy, rx, ry, fill, ow=5):
    d.ellipse([P(cx - rx - ow, cy - ry - ow), P(cx + rx + ow, cy + ry + ow)], fill=INK)
    d.ellipse([P(cx - rx, cy - ry), P(cx + rx, cy + ry)], fill=fill)


def arm(d, P, cx, cy, flip):
    pts = []
    for i in range(40):
        t = 2 * math.pi * i / 40
        x, y = 48 * math.cos(t), 98 * math.sin(t)
        a = math.radians(-24 if flip else 24)
        pts.append((cx + x * math.cos(a) - y * math.sin(a), cy + x * math.sin(a) + y * math.cos(a)))
    d.polygon([P(x, y) for x, y in pts], fill=INK)
    inner = [(cx + (px - cx) * 0.9, cy + (py - cy) * 0.93) for px, py in pts]
    d.polygon([P(x, y) for x, y in inner], fill=FUR)
    px, py = cx + (-30 if flip else 30) * 0.5, cy + 70
    d.ellipse([P(px - 26, py - 22), P(px + 26, py + 22)], fill=LIGHT)


def body(d, P):
    for ex in (188, 432):                                   # kulaklar
        oval(d, P, ex, 92, 52, 52, FUR); d.ellipse([P(ex - 28, 66), P(ex + 28, 118)], fill=LIGHT)
    arm(d, P, 152, 420, False)                              # sol kol (izleyicinin solu)
    for lx in (222, 398):                                   # bacaklar
        oval(d, P, lx, 612, 78, 62, FUR); d.ellipse([P(lx - 44, 596), P(lx + 44, 652)], fill=LIGHT)
    oval(d, P, 310, 450, 150, 172, FUR)                     # govde
    d.ellipse([P(215, 360), P(405, 590)], fill=LIGHT)       # gobek
    oval(d, P, 310, 196, 136, 124, FUR)                     # bas
    d.ellipse([P(240, 205), P(380, 290)], fill=LIGHT)       # burun bolgesi
    d.ellipse([P(286, 214), P(334, 246)], fill=INK)
    d.arc([P(276, 236), P(312, 272)], 20, 160, fill=INK, width=4 * S)
    d.arc([P(308, 236), P(344, 272)], 20, 160, fill=INK, width=4 * S)
    d.ellipse([P(238, 158), P(272, 192)], fill=INK)         # sol goz (dugme)
    d.ellipse([P(246, 164), P(256, 174)], fill=(255, 255, 255, 255))
    for a in (40, 130):                                     # sag goz yok: sarkan iplik
        d.line([P(366, 176), P(366 + 16 * math.cos(math.radians(a)), 176 + 22 * math.sin(math.radians(a)))], fill=INK, width=3 * S)
    d.ellipse([P(358, 168), P(374, 184)], outline=INK, width=3 * S)
    d.polygon([P(412, 318), P(452, 304), P(470, 346), P(446, 380), P(410, 368), P(420, 342)], fill=(70, 46, 30, 255))  # yirtik omuz
    for x in (418, 440, 460):
        d.line([P(x, 312), P(x + 6, 300)], fill=INK, width=3 * S)


teddy_layer("TeddyBody", body)
teddy_layer("TeddyArmAttached", lambda d, P: (arm(d, P, 468, 420, True), d.arc([P(420, 320), P(500, 400)], 200, 330, fill=INK, width=3 * S)))


def stitches(d, P):
    for i in range(7):
        a = math.radians(200 + i * 20)
        x, y = 460 + 44 * math.cos(a), 362 + 44 * math.sin(a)
        d.line([P(x - 8, y - 6), P(x + 8, y + 6)], fill=(150, 104, 60, 255), width=5 * S)


teddy_layer("TeddyStitches", stitches)


def puff(d, P, col, edge):
    for (x, y, r) in ((428, 330, 22), (452, 318, 20), (446, 350, 22), (424, 356, 16), (468, 340, 14)):
        d.ellipse([P(x - r - 3, y - r - 3), P(x + r + 3, y + r + 3)], fill=edge)
        d.ellipse([P(x - r, y - r), P(x + r, y + r)], fill=col)


teddy_layer("TeddyStuffingOld", lambda d, P: puff(d, P, (150, 152, 140, 255), (90, 92, 84, 255)))
teddy_layer("TeddyStuffingNew", lambda d, P: puff(d, P, (252, 252, 248, 255), (200, 200, 196, 255)))


def eye(d, P):
    d.ellipse([P(348, 158), P(384, 194)], fill=INK)
    d.ellipse([P(351, 161), P(381, 191)], fill=(96, 186, 176, 255))
    for ox, oy in ((-6, -6), (6, -6), (-6, 6), (6, 6)):
        d.ellipse([P(366 + ox - 3, 176 + oy - 3), P(366 + ox + 3, 176 + oy + 3)], fill=INK)
    d.arc([P(354, 164), P(374, 184)], 200, 260, fill=(220, 250, 246, 255), width=3 * S)


teddy_layer("TeddyEye", eye)


def heart(d, P):
    pts = [P(310 + 46 * 16 * math.sin(t) ** 3 / 16, 470 - 46 * (13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)) / 16)
           for t in np.linspace(0, 2 * math.pi, 80)]
    d.polygon(pts, fill=INK)
    inner = [(p[0] * 0.93 + 310 * S * 0.07, p[1] * 0.93 + 466 * S * 0.07) for p in pts]
    d.polygon(inner, fill=(214, 72, 70, 255))
    for i in range(0, 80, 5):
        x, y = inner[i]
        d.ellipse([x - 3 * S, y - 3 * S, x + 3 * S, y + 3 * S], fill=(250, 240, 224, 255))


teddy_layer("TeddyHeart", heart)


def net(d, P):
    for k in range(-6, 8):
        d.line([P(60 + k * 80, 40), P(60 + k * 80 + 560, 660)], fill=(40, 120, 96, 220), width=5 * S)
        d.line([P(560 - k * 80, 40), P(560 - k * 80 - 560, 660)], fill=(40, 120, 96, 220), width=5 * S)


im, d, P = canvas(TW, TH)
net(d, P)
mask = Image.new("L", im.size, 0)
ImageDraw.Draw(mask).ellipse([P(20, 0), P(600, 700)], fill=255)
im.putalpha(Image.fromarray((np.asarray(im.split()[3]) * (np.asarray(mask) / 255)).astype(np.uint8)))
save(im, "TeddyNet", TW, TH)

# Dugumler (makas) ve kir lekeleri (sunger): ayri kucuk sprite'lar
im, d, P = canvas(60, 60)
d.ellipse([P(8, 8), P(52, 52)], fill=INK); d.ellipse([P(13, 13), P(47, 47)], fill=(40, 120, 96, 255))
d.line([P(14, 30), P(46, 30)], fill=(150, 210, 180, 255), width=4 * S); d.line([P(30, 14), P(30, 46)], fill=(150, 210, 180, 255), width=4 * S)
save(im, "NetKnot", 60, 60)
rng = np.random.default_rng(4)
for i in range(4):
    s = 110
    yy, xx = np.mgrid[0:s, 0:s]
    a = np.zeros((s, s))
    for _ in range(9):
        bx, by, br = rng.uniform(30, 80), rng.uniform(30, 80), rng.uniform(12, 26)
        a = np.maximum(a, np.clip(1 - np.sqrt((xx - bx) ** 2 + (yy - by) ** 2) / br, 0, 1))
    a = nd.gaussian_filter(a, 2) * 0.8
    Image.fromarray(np.dstack([np.full((s, s), 96), np.full((s, s), 80), np.full((s, s), 52), np.clip(a, 0, 0.75) * 255]).astype(np.uint8), "RGBA").save(f"{OUT}/Mud{i}.png")

# Kopuk kol (matin yaninda yatiyor)
im, d, P = canvas(240, 140)
pts = []
for i in range(40):
    t = 2 * math.pi * i / 40
    x, y = 98 * math.cos(t), 48 * math.sin(t)
    pts.append((120 + x, 70 + y))
d.polygon([P(x, y) for x, y in pts], fill=INK)
d.polygon([P(120 + (x - 120) * 0.93, 70 + (y - 70) * 0.9) for x, y in pts], fill=FUR)
d.ellipse([P(178, 50), P(222, 90)], fill=LIGHT)
for (x, y, r) in ((30, 60, 14), (24, 82, 12), (40, 46, 10)):
    d.ellipse([P(x - r, y - r), P(x + r, y + r)], fill=(240, 240, 236, 255), outline=INK, width=2 * S)
save(im, "TeddyArmLoose", 240, 140)


# ================= Alet ve raf yuvalari =================
def slot(name, w, h):
    im, d, P = canvas(w, h)
    d.rounded_rectangle([P(0, 0), P(w, h)], radius=22 * S, fill=INK)
    d.rounded_rectangle([P(5, 5), P(w - 5, h - 12)], radius=18 * S, fill=(246, 230, 196, 255))
    return im, d, P


def tool(name, draw):
    im, d, P = slot(name, 210, 200)
    draw(d, P)
    save(im, name, 210, 200)


def scissors(d, P):
    d.line([P(78, 30), P(132, 104)], fill=(150, 156, 164, 255), width=9 * S)
    d.line([P(132, 30), P(78, 104)], fill=(150, 156, 164, 255), width=9 * S)
    for cx in (84, 126):
        d.ellipse([P(cx - 20, 98), P(cx + 20, 138)], outline=(214, 72, 60, 255), width=9 * S)


def sponge(d, P):
    d.rounded_rectangle([P(56, 52), P(156, 118)], radius=12 * S, fill=INK)
    d.rounded_rectangle([P(60, 56), P(152, 98)], radius=10 * S, fill=(246, 206, 74, 255))
    d.rectangle([P(60, 96), P(152, 114)], fill=(80, 140, 96, 255))
    for x, y in ((80, 70), (104, 80), (128, 68), (140, 86), (92, 88)):
        d.ellipse([P(x - 4, y - 4), P(x + 4, y + 4)], fill=(214, 168, 50, 255))


def tweezers(d, P):
    d.polygon([P(100, 26), P(110, 26), P(88, 132), P(80, 130)], fill=(150, 156, 164, 255), outline=INK)
    d.polygon([P(104, 26), P(114, 26), P(134, 130), P(126, 132)], fill=(150, 156, 164, 255), outline=INK)
    d.rectangle([P(98, 22), P(116, 34)], fill=INK)


def needle(d, P):
    d.line([P(70, 130), P(140, 30)], fill=(120, 126, 134, 255), width=7 * S)
    d.ellipse([P(132, 24), P(146, 40)], outline=INK, width=3 * S)
    d.line([P(138, 32), P(160, 60), P(130, 90), P(150, 120)], fill=(214, 72, 60, 255), width=4 * S, joint="curve")


for n, f in (("ToolScissors", scissors), ("ToolSponge", sponge), ("ToolTweezers", tweezers), ("ToolNeedle", needle)):
    tool(n, f)


def shelf(name, draw):
    im, d, P = slot(name, 266, 200)
    d.rounded_rectangle([P(18, 18), P(70, 70)], radius=10 * S, fill=INK)
    d.rounded_rectangle([P(21, 21), P(67, 67)], radius=8 * S, fill=(252, 248, 240, 255))
    draw(d, P)
    save(im, name, 266, 200)


shelf("ShelfFiber", lambda d, P: [d.ellipse([P(x - 11, y - 11), P(x + 11, y + 11)], fill=(250, 250, 248, 255), outline=(170, 170, 166, 255), width=2 * S)
                                 for x, y in ((36, 40), (52, 36), (46, 54))])
shelf("ShelfThread", lambda d, P: (d.rectangle([P(30, 28), P(58, 32)], fill=INK), d.rectangle([P(30, 58), P(58, 62)], fill=INK),
                                   d.rectangle([P(34, 32), P(54, 58)], fill=(150, 98, 60, 255))))
shelf("ShelfButton", lambda d, P: (d.ellipse([P(28, 28), P(60, 60)], fill=INK), d.ellipse([P(31, 31), P(57, 57)], fill=(96, 186, 176, 255))))
shelf("ShelfHeart", lambda d, P: (d.polygon([P(44, 62), P(26, 42), P(30, 30), P(44, 34), P(58, 30), P(62, 42)], fill=(214, 72, 70, 255), outline=INK)))
im, d, P = canvas(220, 40)
d.rounded_rectangle([P(0, 0), P(220, 40)], radius=20 * S, fill=(206, 234, 244, 255))
save(im, "SourcePill", 220, 40)
print("ok")


# ================= Tamirden sonra: Elif onarilmis Pamuk'la (25.png, 1280x720 ayni duzen) =================
if len(sys.argv) > 4:
    after_src = sys.argv[4]
    base = Image.open(f"{OUT}/InteriorDay2Background.png").convert("RGB")
    box = (640, 200, 840, 530)
    src = Image.open(after_src).convert("RGB").crop(box).resize(((box[2] - box[0]) * 2, (box[3] - box[1]) * 2), Image.LANCZOS)
    m = Image.new("L", src.size, 0)
    ImageDraw.Draw(m).rounded_rectangle([12, 12, src.size[0] - 12, src.size[1] + 40], radius=30, fill=255)
    base.paste(src, (box[0] * 2, box[1] * 2), m.filter(ImageFilter.GaussianBlur(8)))
    base.save(f"{OUT}/InteriorDay2AfterBackground.png", optimize=True)
    print("after ok")
