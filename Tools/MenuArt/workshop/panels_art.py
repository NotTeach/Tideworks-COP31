"""Tamir sonrasi odul karti ve "Let's Save the Seas!" paneli gorselleri (2x artboard pikseli).
Kullanim: python panels_art.py <odul_tasarimi.jpg> <Assets/Sprites/Workshop>
Altin yigini tasarimdan kesilir; kart, butonlar, plaj resmi ve simgeler yeniden cizilir."""
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as nd

src, out = sys.argv[1], sys.argv[2]
S = 4
INK = (60, 38, 26, 255)
WHITE = (255, 255, 255, 255)


def rr(name, w, h, r, fill, outline=None, ow=0, lip=0, lip_color=None, shadow=0):
    """Yuvarlak kose kutu: istege bagli kenar cizgisi, alt dudak (3B buton) ve golge."""
    pad = shadow * 2
    im = Image.new("RGBA", ((w + 2 * pad) * S, (h + 2 * pad) * S), (0, 0, 0, 0))
    if shadow:
        sh = Image.new("RGBA", im.size, (0, 0, 0, 0))
        ImageDraw.Draw(sh).rounded_rectangle([pad * S, (pad + shadow) * S, (pad + w) * S, (pad + h + shadow) * S],
                                             radius=r * S, fill=(0, 0, 0, 120))
        im = Image.alpha_composite(im, sh.filter(ImageFilter.GaussianBlur(shadow * S * 0.6)))
    d = ImageDraw.Draw(im)
    box = lambda t, b=0: [(pad + t) * S, (pad + t) * S, (pad + w - t) * S - 1, (pad + h - t - b) * S - 1]
    if lip:
        d.rounded_rectangle(box(0), radius=r * S, fill=lip_color)
        d.rounded_rectangle(box(0, lip), radius=r * S, fill=outline or fill)
        if outline: d.rounded_rectangle(box(ow, lip), radius=(r - ow) * S, fill=fill)
    elif outline:
        d.rounded_rectangle(box(0), radius=r * S, fill=outline)
        d.rounded_rectangle(box(ow), radius=(r - ow) * S, fill=fill)
    else:
        d.rounded_rectangle(box(0), radius=r * S, fill=fill)
    im.resize((im.width // S, im.height // S), Image.LANCZOS).save(f"{out}/{name}.png")


# ---------- Odul karti ----------
rr("RewardCard", 980, 1080, 56, (236, 208, 164, 255), outline=INK, ow=8, lip=18, lip_color=(84, 52, 34, 255), shadow=20)
rr("RewardPillCoins", 320, 104, 52, (18, 62, 72, 255))
rr("RewardPillHelper", 600, 86, 43, (110, 148, 88, 255))
rr("ButtonOrange", 360, 120, 34, (180, 72, 42, 255), lip=14, lip_color=(116, 44, 24, 255))
rr("CounterPill", 420, 110, 55, (26, 124, 128, 255), outline=(78, 186, 176, 255), ow=6)

# Altin yigini: tasarimdan kes (kart rengi ile fark = alfa)
A = np.asarray(Image.open(src).convert("RGB")).astype(np.float32)
x0, y0, x1, y1 = 1228, 300, 1618, 645
crop = A[y0:y1, x0:x1]
bg = nd.median_filter(crop, size=(1, 1, 1))
card = np.array([232, 203, 159], np.float32)
diff = np.abs(crop - card).max(-1)
alpha = np.clip((diff - 18) / 40, 0, 1)
alpha = nd.gaussian_filter(alpha, 0.6)
rgb = np.clip((crop - (1 - np.maximum(alpha, 0.05))[..., None] * card) / np.maximum(alpha, 0.05)[..., None], 0, 255)
pile = Image.fromarray(np.dstack([rgb, alpha * 255]).astype(np.uint8), "RGBA")
k = 2560 / 2730
pile.resize((round(pile.width * k), round(pile.height * k)), Image.LANCZOS).save(f"{out}/CoinPile.png")

# ---------- "Let's Save the Seas!" paneli ----------
rr("MissionFrame", 2160, 1180, 64, (28, 114, 118, 255), outline=(14, 52, 58, 255), ow=8, shadow=22)
rr("MissionCardRight", 1050, 1120, 40, (250, 244, 232, 255), outline=(214, 186, 140, 255), ow=6)
rr("MissionRowTeal", 930, 120, 34, (16, 72, 82, 255))
rr("MissionRowGreen", 930, 120, 34, (52, 116, 72, 255))
rr("MissionRowOrange", 930, 120, 34, (182, 80, 44, 255))
rr("ButtonOutline", 330, 120, 34, (250, 244, 232, 255), outline=(40, 30, 24, 255), ow=5)
rr("ButtonOrangeWide", 560, 120, 34, (180, 72, 42, 255), lip=14, lip_color=(116, 44, 24, 255))

# Plaj resmi (sol taraf)
W, H, R = 1000, 1100, 40
im = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
P = lambda x, y: (x * S, y * S)
base = Image.new("RGBA", im.size, (0, 0, 0, 0)); bd = ImageDraw.Draw(base)
for y in range(0, 340, 4):  # gokyuzu
    t = y / 340
    bd.rectangle([P(0, y), P(W, y + 4)], fill=(int(150 + 40 * t), int(208 + 20 * t), int(226 + 8 * t), 255))
bd.ellipse([P(720, 90), P(870, 240)], fill=(244, 210, 110, 255))
for bx, by, s in ((560, 120, 1.0), (620, 160, 0.8)):
    bd.arc([P(bx - 22 * s, by - 10 * s), P(bx, by + 10 * s)], 200, 340, fill=(40, 50, 60, 255), width=int(4 * s * S))
    bd.arc([P(bx, by - 10 * s), P(bx + 22 * s, by + 10 * s)], 200, 340, fill=(40, 50, 60, 255), width=int(4 * s * S))
for y in range(340, 760, 4):  # deniz
    t = (y - 340) / 420
    bd.rectangle([P(0, y), P(W, y + 4)], fill=(int(50 - 14 * t), int(160 - 30 * t), int(170 - 30 * t), 255))
bd.rectangle([P(0, 336), P(W, 352)], fill=(110, 196, 200, 255))
rng = np.random.default_rng(5)
for wx, wy in ((90, 470), (300, 430), (620, 600), (180, 600), (420, 660), (760, 470), (860, 640)):
    pts = [P(wx + i * 4, wy + 7 * np.sin(i / 3.2)) for i in range(22)]
    bd.line(pts, fill=(170, 228, 228, 255), width=6 * S, joint="curve")
# tekne
bd.polygon([P(600, 470), P(780, 470), P(760, 505), P(620, 505)], fill=INK)
bd.polygon([P(608, 474), P(772, 474), P(755, 501), P(625, 501)], fill=WHITE)
bd.rectangle([P(688, 330), P(694, 472)], fill=INK)
bd.polygon([P(694, 340), P(694, 462), P(760, 462)], fill=(90, 160, 140, 255), outline=INK)
# kiyi kopugu ve kum
shore = [P(x, 760 - 30 * np.sin(x / 260 + 0.6) - x * 0.03) for x in range(0, W + 10, 10)]
bd.polygon(shore + [P(W, H), P(0, H)], fill=(240, 202, 120, 255))
bd.line(shore, fill=WHITE, width=12 * S, joint="curve")
for y in range(900, H, 6):
    t = (y - 820) / (H - 820)
    bd.line([P(0, y), P(W, y)], fill=(int(240 - 8 * t), int(202 - 16 * t), int(120 - 14 * t), 255), width=6 * S)
# yuvalar, yavrular ve denize giden noktali yollar
nests = [(170, 860), (350, 865), (500, 860), (580, 990)]
egg = (870, 1050)
for (nx, ny) in nests:
    for i in range(1, 12):
        t = i / 12
        px, py = nx + (egg[0] - nx) * t, ny + (egg[1] - ny) * t + 40 * np.sin(t * np.pi)
        bd.ellipse([P(px - 4, py - 3), P(px + 4, py + 3)], fill=(196, 150, 90, 255))
for (nx, ny) in nests:
    bd.ellipse([P(nx - 34, ny - 26), P(nx + 34, ny + 26)], fill=INK)
    bd.ellipse([P(nx - 30, ny - 22), P(nx + 30, ny + 20)], fill=(168, 104, 62, 255))
    bd.ellipse([P(nx - 14, ny - 34), P(nx + 14, ny - 12)], fill=INK)
    bd.ellipse([P(nx - 11, ny - 31), P(nx + 11, ny - 15)], fill=(168, 104, 62, 255))
bd.ellipse([P(egg[0] - 70, egg[1] - 22), P(egg[0] + 70, egg[1] + 26)], fill=(226, 186, 110, 255))
for ex, ey in ((-24, 0), (0, -4), (22, 2)):
    bd.ellipse([P(egg[0] + ex - 13, egg[1] + ey - 11), P(egg[0] + ex + 13, egg[1] + ey + 11)], fill=WHITE, outline=(150, 130, 110, 255))
mask = Image.new("L", im.size, 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, W * S - 1, H * S - 1], radius=R * S, fill=255)
im.paste(base, (0, 0), mask)
d = ImageDraw.Draw(im)
d.rounded_rectangle([0, 0, W * S - 1, H * S - 1], radius=R * S, outline=(14, 52, 58, 255), width=6 * S)
im.resize((W, H), Image.LANCZOS).save(f"{out}/BeachArt.png")


# Beyaz simgeler (satirlar ve "Head out" butonu)
def icon(name, draw, s=72):
    im = Image.new("RGBA", (s * S, s * S), (0, 0, 0, 0))
    draw(ImageDraw.Draw(im), lambda x, y: (x * S * s / 72, y * S * s / 72), S * s / 72)
    im.resize((s, s), Image.LANCZOS).save(f"{out}/{name}.png")


def boat(d, P, k):
    d.polygon([P(10, 50), P(62, 50), P(54, 62), P(18, 62)], fill=WHITE)
    d.rectangle([P(34, 8), P(38, 50)], fill=WHITE)
    d.polygon([P(39, 10), P(39, 44), P(60, 44)], fill=WHITE)
    d.polygon([P(33, 16), P(33, 44), P(16, 44)], fill=WHITE)


def recycle(d, P, k):
    import math
    for a in (90, 210, 330):
        r1, r2 = 26, 12
        ang = math.radians(a)
        cx, cy = 36 + r1 * math.cos(ang) * 0.55, 38 - r1 * math.sin(ang) * 0.55
        d.arc([P(10, 12), P(62, 64)], -a - 50, -a + 40, fill=WHITE, width=int(7 * k))
        tip = math.radians(a - 40)
        tx, ty = 36 + 26 * math.cos(tip), 38 - 26 * math.sin(tip)
        d.regular_polygon((P(tx, ty)[0], P(tx, ty)[1], 9 * k), 3, rotation=-(a - 40) + 90, fill=WHITE)


def turtle(d, P, k):
    d.ellipse([P(16, 20), P(56, 56)], fill=WHITE)
    d.ellipse([P(28, 6), P(44, 22)], fill=WHITE)
    for fx, fy in ((12, 22), (60, 22), (12, 52), (60, 52)):
        d.ellipse([P(fx - 7, fy - 5), P(fx + 7, fy + 5)], fill=WHITE)
    d.ellipse([P(26, 30), P(46, 46)], fill=(0, 0, 0, 0))


icon("IconBoatWhite", boat)
icon("IconRecycleWhite", recycle)
icon("IconTurtleWhite", turtle)
print("ok")

# Sayac hapindaki ok (fontlarda yok)
im = Image.new("RGBA", (64 * S, 40 * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.line([(4 * S, 20 * S), (52 * S, 20 * S)], fill=WHITE, width=5 * S)
d.polygon([(40 * S, 8 * S), (60 * S, 20 * S), (40 * S, 32 * S)], fill=WHITE)
im.resize((64, 40), Image.LANCZOS).save(f"{out}/ArrowWhite.png")
