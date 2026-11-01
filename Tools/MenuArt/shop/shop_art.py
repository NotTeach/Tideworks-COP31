"""Dedemin dukkani (Grandpa's Shop): panel, kartlar, butonlar ve urun ikonlari.
Kullanim: python shop_art.py <Assets/Sprites/Shop> <Assets/Art/Fishing/Rod.png>      (2x artboard pikseli)"""
import sys, math
import numpy as np
from PIL import Image, ImageDraw

out, rod_png = sys.argv[1], sys.argv[2]
S = 4
INK = (52, 38, 30, 255)


def rr(name, w, h, r, fill, outline=None, ow=0, lip=0, lip_color=None):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    box = lambda t, b=0: [t * S, t * S, (w - t) * S - 1, (h - t - b) * S - 1]
    if lip:
        d.rounded_rectangle(box(0), radius=r * S, fill=lip_color)
        d.rounded_rectangle(box(0, lip), radius=r * S, fill=outline or fill)
        if outline: d.rounded_rectangle(box(ow, lip), radius=(r - ow) * S, fill=fill)
    elif outline:
        d.rounded_rectangle(box(0), radius=r * S, fill=outline)
        if fill: d.rounded_rectangle(box(ow), radius=(r - ow) * S, fill=fill)
        else:
            d.rounded_rectangle(box(ow), radius=(r - ow) * S, fill=(0, 0, 0, 0))
    else:
        d.rounded_rectangle(box(0), radius=r * S, fill=fill)
    im.resize((w, h), Image.LANCZOS).save(f"{out}/{name}.png")


rr("ShopModal", 1800, 1180, 56, (238, 214, 168, 255), outline=(60, 38, 26, 255), ow=9, lip=18, lip_color=(84, 52, 34, 255))
rr("ShopCard", 520, 400, 30, (252, 246, 232, 255), outline=(214, 186, 140, 255), ow=5)
rr("ShopCardSelected", 532, 412, 34, None, outline=(244, 192, 74, 255), ow=9)
rr("ShopBtnYellow", 472, 100, 28, (244, 192, 74, 255), lip=10, lip_color=(176, 124, 34, 255))
rr("ShopBtnGrey", 472, 100, 28, (206, 200, 190, 255), lip=10, lip_color=(160, 152, 140, 255))
rr("ShopBtnTeal", 472, 100, 28, (26, 124, 128, 255), lip=10, lip_color=(12, 72, 78, 255))
rr("ShopNote", 472, 48, 24, (214, 238, 212, 255))
rr("ShopNeeded", 220, 46, 23, (200, 60, 46, 255))
rr("ShopCoinPill", 260, 96, 48, (16, 62, 72, 255))
rr("ShopBack", 300, 100, 30, (252, 246, 232, 255), outline=(52, 38, 30, 255), ow=5)
rr("ShopGreat", 340, 110, 32, (182, 74, 42, 255), lip=12, lip_color=(116, 44, 24, 255))
rr("ShopButtonHud", 210, 92, 28, (244, 192, 74, 255), outline=(52, 38, 30, 255), ow=4)

# Ag dairesi (denizde: icindeki copler tek atista toplanir)
n = 400
im = Image.new("RGBA", (n * S, n * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
for a in range(0, 360, 10):
    d.arc([8 * S, 8 * S, (n - 8) * S, (n - 8) * S], a, a + 6, fill=(255, 250, 220, 235), width=7 * S)
d.ellipse([12 * S, 12 * S, (n - 12) * S, (n - 12) * S], fill=(255, 240, 180, 40))
im.resize((n, n), Image.LANCZOS).save(f"{out}/NetCircle.png")


# ---------------- Urun ikonlari (300x300) ----------------
def icon(name, draw):
    im = Image.new("RGBA", (300 * S, 300 * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    P = lambda x, y: (x * S, y * S)
    draw(d, P)
    im.resize((300, 300), Image.LANCZOS).save(f"{out}/{name}.png")


def sewing(d, P):
    d.rounded_rectangle([P(30, 110), P(270, 260)], radius=26 * S, fill=INK)
    d.rounded_rectangle([P(38, 118), P(262, 252)], radius=20 * S, fill=(214, 120, 96, 255))
    d.rounded_rectangle([P(38, 118), P(262, 150)], radius=14 * S, fill=(190, 96, 74, 255))
    for x, c in ((90, (150, 98, 60)), (150, (96, 160, 140)), (210, (244, 192, 74))):       # makaralar
        d.rectangle([P(x - 26, 70), P(x + 26, 80)], fill=INK)
        d.rectangle([P(x - 22, 80), P(x + 22, 120)], fill=c + (255,), outline=INK, width=3 * S)
    d.line([P(70, 230), P(240, 60)], fill=(180, 186, 194, 255), width=7 * S)                # igne
    d.ellipse([P(232, 50), P(252, 70)], outline=INK, width=4 * S)
    d.ellipse([P(120, 170), P(180, 230)], fill=(250, 244, 230, 255), outline=INK, width=4 * S)  # yama
    for a in range(0, 360, 45):
        x, y = 150 + 22 * math.cos(math.radians(a)), 200 + 22 * math.sin(math.radians(a))
        d.ellipse([P(x - 3, y - 3), P(x + 3, y + 3)], fill=INK)


def bignet(d, P):
    d.ellipse([P(30, 50), P(270, 270)], fill=(84, 150, 128, 255), outline=INK, width=6 * S)
    mesh = Image.new("RGBA", (300 * S, 300 * S), (0, 0, 0, 0)); md = ImageDraw.Draw(mesh)
    for k in range(-8, 9):
        md.line([P(150 + k * 30 - 120, 50), P(150 + k * 30 + 120, 270)], fill=(40, 96, 80, 255), width=4 * S)
        md.line([P(150 + k * 30 + 120, 50), P(150 + k * 30 - 120, 270)], fill=(40, 96, 80, 255), width=4 * S)
    m = Image.new("L", mesh.size, 0)
    ImageDraw.Draw(m).ellipse([P(36, 56), P(264, 264)], fill=255)
    d._image.paste(mesh, (0, 0), Image.composite(mesh.split()[3], Image.new("L", mesh.size, 0), m))
    for x, y in ((50, 80), (250, 80), (40, 170), (260, 170), (150, 40)):                    # samandiralar
        d.ellipse([P(x - 16, y - 16), P(x + 16, y + 16)], fill=(236, 128, 60, 255), outline=INK, width=4 * S)


def lamp(d, P):
    d.arc([P(110, 20), P(190, 90)], 180, 360, fill=INK, width=8 * S)
    d.rectangle([P(96, 60), P(204, 86)], fill=INK)
    d.polygon([P(100, 86), P(200, 86), P(216, 236), P(84, 236)], fill=INK)
    d.polygon([P(110, 96), P(190, 96), P(202, 226), P(98, 226)], fill=(246, 178, 66, 255))
    for r, a in ((70, 60), (52, 110), (34, 170)):
        d.ellipse([P(150 - r * 0.6, 160 - r * 0.6), P(150 + r * 0.6, 160 + r * 0.6)], fill=(255, 224, 140, a))
    d.line([P(150, 96), P(150, 226)], fill=INK, width=4 * S)
    d.rounded_rectangle([P(76, 232), P(224, 262)], radius=8 * S, fill=INK)


def solar(d, P):
    d.polygon([P(40, 60), P(200, 40), P(230, 120), P(70, 140)], fill=INK)                  # gunes paneli
    d.polygon([P(48, 66), P(196, 48), P(222, 114), P(76, 132)], fill=(60, 104, 168, 255))
    for t in (0.33, 0.66):
        d.line([P(48 + 148 * t, 66 - 18 * t), P(76 + 146 * t, 132 - 18 * t)], fill=(170, 200, 230, 255), width=3 * S)
    d.line([P(62, 99), P(209, 81)], fill=(170, 200, 230, 255), width=3 * S)
    d.rounded_rectangle([P(150, 140), P(250, 210)], radius=18 * S, fill=INK)               # motor
    d.rounded_rectangle([P(156, 146), P(244, 204)], radius=14 * S, fill=(236, 236, 228, 255))
    d.rectangle([P(190, 204), P(210, 262)], fill=INK)
    d.polygon([P(170, 262), P(230, 262), P(214, 280), P(186, 280)], fill=INK)
    d.ellipse([P(250, 236), P(282, 290)], fill=(244, 192, 74, 255), outline=INK, width=3 * S)   # pervane
    d.line([P(140, 120), P(176, 146)], fill=INK, width=5 * S)


icon("ItemSewingKit", sewing)
icon("ItemBigNet", bignet)
icon("ItemAmberLamp", lamp)
icon("ItemSolarMotor", solar)
rod = Image.open(rod_png).convert("RGBA")
rod.thumbnail((280, 280), Image.LANCZOS)
canvas = Image.new("RGBA", (300, 300), (0, 0, 0, 0))
canvas.alpha_composite(rod, ((300 - rod.width) // 2, (300 - rod.height) // 2))
# saglam olta: sapa altin halkalar
dd = ImageDraw.Draw(canvas)
for x, y in ((95, 220), (125, 190)):
    dd.ellipse([x - 10, y - 10, x + 10, y + 10], outline=(244, 192, 74, 255), width=5)
canvas.save(f"{out}/ItemSturdyRod.png")
print("ok")
