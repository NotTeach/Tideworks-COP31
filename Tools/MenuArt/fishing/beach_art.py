"""Gorev 3 - Carettalara yardim: aksam kumsali ve nesneleri. Kullanim: python beach_art.py <Assets/Sprites/Beach>
Artboard 2560x1440. Konumlar BeachController / FishingSceneBuilder ile ayni."""
import sys, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

out = sys.argv[1]
W, H, K = 2560, 1440, 2
INK = (40, 46, 52, 255)
HORIZON, SURF, PROM = 242, 600, 1205


def canvas(w, h):
    im = Image.new("RGBA", (w * 4, h * 4), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (x * 4, y * 4))


def save(im, name, w, h):
    im.resize((w, h), Image.LANCZOS).save(f"{out}/{name}.png")


# ---------------- Zemin ----------------
im = Image.new("RGB", (W * K, H * K)); d = ImageDraw.Draw(im)
P = lambda x, y: (x * K, y * K)
sky = [(96, 92, 150), (170, 110, 140), (236, 140, 120), (250, 196, 140)]
for y in range(0, HORIZON):
    t = y / HORIZON * (len(sky) - 1)
    i = min(int(t), len(sky) - 2); f = t - i
    c = tuple(int(sky[i][j] + (sky[i + 1][j] - sky[i][j]) * f) for j in range(3))
    d.rectangle([P(0, y), P(W, y + 1)], fill=c)
d.ellipse([P(1278 - 92, HORIZON - 92), P(1278 + 92, HORIZON + 92)], fill=(252, 216, 112))
for y in range(HORIZON, SURF):
    t = (y - HORIZON) / (SURF - HORIZON)
    d.rectangle([P(0, y), P(W, y + 1)], fill=(int(36 + 14 * t), int(120 + 26 * t), int(128 + 22 * t)))
for i, (w, y) in enumerate(((170, 268), (120, 296), (90, 324), (60, 352), (34, 380))):  # gunes yansimasi
    d.rounded_rectangle([P(1278 - w, y), P(1278 + w, y + 7)], radius=4 * K, fill=(238, 196, 120))
# kopuk ve islak kum
surf = [P(x, SURF + 10 * math.sin(x / 140)) for x in range(0, W + 20, 20)]
d.polygon(surf + [P(W, SURF + 60), P(0, SURF + 60)], fill=(214, 198, 160))
d.line(surf, fill=(250, 250, 246), width=14 * K, joint="curve")
d.rectangle([P(0, SURF + 40), P(W, PROM)], fill=(236, 214, 168))
for y in range(SURF + 20, SURF + 60):
    t = (y - SURF - 20) / 40
    d.line([P(0, y + 8 * math.sin(0)), P(W, y)], fill=(int(214 + 22 * t), int(198 + 16 * t), int(160 + 8 * t)))
rng = np.random.default_rng(9)
for _ in range(70):
    x, y = rng.uniform(20, W - 20), rng.uniform(SURF + 90, PROM - 30)
    r = rng.uniform(5, 14)
    d.ellipse([P(x - r, y - r * 0.45), P(x + r, y + r * 0.45)], fill=(222, 198, 150))
# kordon (tas doseme)
d.rectangle([P(0, PROM - 26), P(W, PROM)], fill=(170, 150, 120))
d.rectangle([P(0, PROM), P(W, H)], fill=(190, 172, 146))
for row, y in enumerate(range(PROM, H, 78)):
    d.line([P(0, y), P(W, y)], fill=(156, 138, 112), width=4 * K)
    off = 0 if row % 2 == 0 else 150
    for x in range(-off, W, 300):
        d.line([P(x, y), P(x, y + 78)], fill=(156, 138, 112), width=4 * K)
bg = im.resize((W, H), Image.LANCZOS)
bg.save(f"{out}/BeachBackground.png", optimize=True)
bg.resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{out}/BeachBackdrop.png")

# ---------------- Sokak lambasi ----------------
im, d, P = canvas(220, 360)
d.rounded_rectangle([P(76, 330), P(124, 350)], radius=8 * 4, fill=INK)          # ayak
d.rounded_rectangle([P(92, 60), P(108, 336)], radius=6 * 4, fill=(52, 60, 68, 255))
d.arc([P(100, 30), P(170, 100)], 180, 270, fill=(52, 60, 68, 255), width=16 * 4)  # kol
d.rounded_rectangle([P(150, 36), P(206, 70)], radius=12 * 4, fill=(52, 60, 68, 255))
save(im, "Lamp", 220, 360)
im, d, P = canvas(220, 360)
d.ellipse([P(160, 58), P(196, 82)], fill=(255, 236, 160, 255))
save(im, "LampBulb", 220, 360)


def glow(name, w, h, color, power=1.5):
    yy, xx = np.mgrid[0:h, 0:w]
    r = np.sqrt(((xx - w / 2 + 0.5) / (w / 2)) ** 2 + ((yy - h / 2 + 0.5) / (h / 2)) ** 2)
    a = np.clip(1 - r, 0, 1) ** power
    Image.fromarray(np.dstack([np.full((h, w), c) for c in color] + [a * 255]).astype(np.uint8), "RGBA").save(f"{out}/{name}.png")


glow("LampGlow", 256, 256, (255, 226, 140), 1.6)
glow("LightPool", 512, 200, (255, 236, 170), 1.2)

# ---------------- Sezlong ----------------
im, d, P = canvas(240, 170)
d.line([P(40, 160), P(70, 100)], fill=INK, width=8 * 4); d.line([P(160, 160), P(150, 104)], fill=INK, width=8 * 4)
d.line([P(196, 160), P(210, 70)], fill=INK, width=8 * 4)
d.polygon([P(20, 96), P(170, 96), P(178, 110), P(28, 112)], fill=INK)
d.polygon([P(26, 99), P(168, 99), P(172, 107), P(32, 108)], fill=(94, 186, 176, 255))
d.polygon([P(166, 100), P(212, 22), P(226, 30), P(182, 108)], fill=INK)
d.polygon([P(172, 100), P(213, 30), P(220, 34), P(180, 104)], fill=(94, 186, 176, 255))
for x in (60, 100, 140):
    d.line([P(x, 100), P(x + 4, 108)], fill=(240, 240, 232, 255), width=4 * 4)
save(im, "Chair", 240, 170)

# ---------------- Semsiye ----------------
im, d, P = canvas(240, 240)
d.line([P(124, 70), P(128, 226)], fill=INK, width=8 * 4)
d.ellipse([P(100, 216), P(156, 232)], fill=(200, 186, 160, 255), outline=INK, width=3 * 4)
canopy = [P(14, 92)] + [P(120 + 106 * math.cos(math.pi * (1 - t)), 92 - 66 * math.sin(math.pi * t)) for t in np.linspace(0, 1, 30)] + [P(226, 92)]
d.polygon(canopy, fill=INK)
inner = [P(20, 88)] + [P(120 + 100 * math.cos(math.pi * (1 - t)), 88 - 60 * math.sin(math.pi * t)) for t in np.linspace(0, 1, 30)] + [P(220, 88)]
d.polygon(inner, fill=(226, 110, 70, 255))
for t in (0.25, 0.5, 0.75):
    x = 120 + 100 * math.cos(math.pi * (1 - t))
    d.line([P(120, 26), P(x, 88)], fill=(250, 244, 230, 255), width=6 * 4)
for i in range(6):  # alt kenar dalgasi
    x0 = 20 + i * 33.3
    d.arc([P(x0, 76), P(x0 + 33, 100)], 0, 180, fill=INK, width=4 * 4)
save(im, "Umbrella", 240, 240)

# ---------------- Cukur (kurek + kumdan kale) ve doldurulmus hali ----------------
im, d, P = canvas(260, 150)
d.ellipse([P(10, 50), P(190, 120)], fill=(120, 92, 62, 255), outline=INK, width=5 * 4)
d.ellipse([P(26, 62), P(174, 108)], fill=(92, 70, 48, 255))
d.ellipse([P(40, 70), P(160, 104)], fill=(70, 54, 40, 255))
d.line([P(60, 128), P(110, 104)], fill=INK, width=10 * 4)                         # kurek sapi
d.line([P(62, 127), P(108, 105)], fill=(240, 196, 60, 255), width=6 * 4)
d.polygon([P(196, 110), P(204, 50), P(232, 50), P(240, 110)], fill=INK)            # kale
d.polygon([P(200, 106), P(207, 54), P(229, 54), P(236, 106)], fill=(228, 200, 150, 255))
d.line([P(218, 50), P(218, 18)], fill=INK, width=3 * 4)
d.polygon([P(218, 18), P(240, 26), P(218, 32)], fill=(220, 90, 70, 255))
save(im, "Hole", 260, 150)
im, d, P = canvas(260, 150)
d.ellipse([P(20, 64), P(180, 112)], fill=(226, 204, 158, 255))
d.ellipse([P(40, 72), P(160, 102)], fill=(232, 210, 164, 255))
save(im, "HoleFilled", 260, 150)

# ---------------- Yuva ve koruma kafesi ----------------
im, d, P = canvas(220, 110)
d.ellipse([P(8, 40), P(212, 104)], fill=(214, 190, 146, 255))
d.ellipse([P(20, 46), P(200, 96)], fill=(226, 204, 160, 255))
for i, (x, y) in enumerate(((84, 62), (104, 56), (124, 62), (94, 72), (116, 72))):
    d.ellipse([P(x - 10, y - 9), P(x + 10, y + 9)], fill=(250, 248, 240, 255), outline=(150, 136, 120, 255), width=2 * 4)
save(im, "Nest", 220, 110)
im, d, P = canvas(240, 210)
for i in range(7):  # tel kafes
    x = 30 + i * 30
    d.line([P(x, 186), P(120 + (x - 120) * 0.55, 40)], fill=(70, 76, 84, 255), width=4 * 4)
for y in (80, 120, 160):
    w = 60 + (y - 40) * 0.6
    d.line([P(120 - w, y), P(120 + w, y)], fill=(70, 76, 84, 255), width=4 * 4)
d.arc([P(60, 26), P(180, 110)], 180, 360, fill=(70, 76, 84, 255), width=5 * 4)
d.line([P(198, 186), P(206, 60)], fill=INK, width=6 * 4)                          # tabela direk
d.rounded_rectangle([P(176, 34), P(236, 84)], radius=8 * 4, fill=INK)
d.rounded_rectangle([P(180, 38), P(232, 80)], radius=6 * 4, fill=(244, 196, 64, 255))
d.ellipse([P(196, 48), P(216, 68)], fill=(110, 90, 50, 255))
save(im, "Cage", 240, 210)

# ---------------- Yavru kaplumbaga (ustten, kafasi yukari) ----------------
im, d, P = canvas(64, 72)
for fx, fy, a in ((14, 26, -30), (50, 26, 30), (18, 54, 30), (46, 54, -30)):
    d.ellipse([P(fx - 10, fy - 6), P(fx + 10, fy + 6)], fill=(70, 60, 50, 255))
d.ellipse([P(24, 2), P(40, 20)], fill=(84, 72, 58, 255), outline=INK, width=3 * 4)
d.ellipse([P(14, 14), P(50, 62)], fill=INK)
d.ellipse([P(17, 17), P(47, 59)], fill=(120, 96, 66, 255))
for x, y in ((32, 28), (24, 40), (40, 40), (32, 50)):
    d.ellipse([P(x - 5, y - 5), P(x + 5, y + 5)], fill=(96, 76, 52, 255))
save(im, "Hatchling", 64, 72)


# ---------------- Arayuz ----------------
def rr(name, w, h, r, fill, outline=None, ow=0):
    im, d, P = canvas(w, h)
    if outline:
        d.rounded_rectangle([0, 0, w * 4 - 1, h * 4 - 1], radius=r * 4, fill=outline)
        d.rounded_rectangle([ow * 4, ow * 4, (w - ow) * 4 - 1, (h - ow) * 4 - 1], radius=(r - ow) * 4, fill=fill)
    else:
        d.rounded_rectangle([0, 0, w * 4 - 1, h * 4 - 1], radius=r * 4, fill=fill)
    save(im, name, w, h)


rr("TaskFrame", 650, 580, 44, (232, 204, 150, 255), outline=(110, 66, 36, 255), ow=18)
rr("BeachTooltip", 1070, 128, 26, (251, 246, 234, 255), outline=(16, 62, 72, 255), ow=5)
rr("CheckOff", 52, 52, 12, (252, 248, 240, 255), outline=(52, 38, 30, 255), ow=4)
im, d, P = canvas(52, 52)
d.rounded_rectangle([0, 0, 52 * 4 - 1, 52 * 4 - 1], radius=12 * 4, fill=(52, 38, 30, 255))
d.rounded_rectangle([4 * 4, 4 * 4, 48 * 4 - 1, 48 * 4 - 1], radius=8 * 4, fill=(30, 120, 124, 255))
d.line([P(13, 27), P(22, 36), P(39, 16)], fill=(255, 255, 255, 255), width=6 * 4, joint="curve")
save(im, "CheckOn", 52, 52)
print("ok")


# ---------------- Gun sonu karti ----------------
def lipbox(name, w, h, r, fill, outline=None, ow=0, lip=0, lip_color=None):
    im, d, P = canvas(w, h)
    box = lambda t, b=0: [t * 4, t * 4, (w - t) * 4 - 1, (h - t - b) * 4 - 1]
    if lip:
        d.rounded_rectangle(box(0), radius=r * 4, fill=lip_color)
        d.rounded_rectangle(box(0, lip), radius=r * 4, fill=outline or fill)
        if outline: d.rounded_rectangle(box(ow, lip), radius=(r - ow) * 4, fill=fill)
    elif outline:
        d.rounded_rectangle(box(0), radius=r * 4, fill=outline)
        d.rounded_rectangle(box(ow), radius=(r - ow) * 4, fill=fill)
    else:
        d.rounded_rectangle(box(0), radius=r * 4, fill=fill)
    save(im, name, w, h)


lipbox("DayCard", 1200, 1070, 46, (236, 208, 160, 255), outline=(60, 38, 26, 255), ow=8, lip=18, lip_color=(84, 52, 34, 255))
lipbox("StatBox", 344, 142, 24, (252, 248, 238, 255))
lipbox("StatBoxDark", 344, 142, 24, (16, 62, 72, 255))
lipbox("RadioBox", 1068, 243, 26, (252, 250, 245, 255))
lipbox("DayButton", 400, 122, 32, (182, 74, 42, 255), lip=14, lip_color=(116, 44, 24, 255))
lipbox("MapButton", 280, 122, 32, (252, 246, 232, 255), outline=(52, 38, 30, 255), ow=5)
print("ok3")
