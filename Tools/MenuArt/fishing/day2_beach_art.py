"""2. gun kumsali (gun batimi, kayalik koy) gorselleri.
Kullanim: python day2_beach_art.py <Assets/Sprites/Day2Beach>
Uretir: Day2BeachBackground/Backdrop (2560x1440), Caretta (ustten, basi yukarida), CarettaNet (yesil hayalet ag),
CarettaBaby (yavru: ayni cizimin kucugu), Moon, NameCard butonlari kullanilmaz (mevcut UI kullanilir)."""
import sys
import os
import math
import random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

out = sys.argv[1]
os.makedirs(out, exist_ok=True)
W, H = 2560, 1440
HORIZON, SHORE = 330, 640


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def vgrad(img, y0, y1, stops):
    d = ImageDraw.Draw(img)
    for y in range(y0, y1):
        t = (y - y0) / max(1, y1 - y0 - 1)
        for k in range(len(stops) - 1):
            a, b = stops[k], stops[k + 1]
            if a[0] <= t <= b[0]:
                c = lerp(a[1], b[1], (t - a[0]) / max(1e-6, b[0] - a[0]))
                break
        d.line([(0, y), (W, y)], fill=c)


# ---------------- Arka plan ----------------

def background():
    rng = random.Random(11)
    img = Image.new("RGB", (W, H))
    vgrad(img, 0, HORIZON, [(0, (58, 44, 104)), (0.45, (196, 96, 112)), (0.8, (248, 150, 96)), (1, (255, 196, 124))])
    vgrad(img, HORIZON, SHORE, [(0, (150, 104, 128)), (0.4, (78, 82, 132)), (1, (52, 96, 128))])
    vgrad(img, SHORE, H, [(0, (196, 150, 120)), (0.08, (226, 172, 128)), (0.5, (232, 184, 136)), (1, (206, 150, 110))])

    # Gunes ve parlamasi
    glow = Image.new("L", (W, H), 0)
    gd = ImageDraw.Draw(glow)
    sx, sy = 1720, HORIZON - 10
    for r in range(420, 0, -6):
        gd.ellipse((sx - r, sy - r, sx + r, sy + r), fill=int(150 * (1 - r / 420) ** 2))
    glow = glow.filter(ImageFilter.GaussianBlur(20))
    img = Image.composite(Image.new("RGB", (W, H), (255, 214, 140)), img, glow)
    d = ImageDraw.Draw(img)
    d.ellipse((sx - 95, sy - 95, sx + 95, sy + 95), fill=(255, 226, 150))
    vgrad_sea = Image.new("RGB", (W, H))
    # Gunesin altini deniz kapatir
    sea = img.crop((0, HORIZON, W, SHORE))
    sea_base = Image.new("RGB", (W, SHORE - HORIZON))
    tmp = Image.new("RGB", (W, H))
    vgrad(tmp, HORIZON, SHORE, [(0, (150, 104, 128)), (0.4, (78, 82, 132)), (1, (52, 96, 128))])
    img.paste(tmp.crop((0, HORIZON, W, SHORE)), (0, HORIZON))
    d = ImageDraw.Draw(img)

    # Bulut seritleri
    clouds = Image.new("L", (W, H), 0)
    cd = ImageDraw.Draw(clouds)
    for _ in range(9):
        x = rng.uniform(0, W)
        y = rng.uniform(60, HORIZON - 60)
        w = rng.uniform(260, 620)
        cd.ellipse((x - w, y - 18, x + w, y + 18), fill=150)
    clouds = clouds.filter(ImageFilter.GaussianBlur(14))
    img = Image.composite(Image.new("RGB", (W, H), (255, 170, 140)), img, clouds)
    d = ImageDraw.Draw(img)

    # Denizde gunes yansimasi ve dalga cizgileri
    for i in range(46):
        y = HORIZON + 8 + i * 6.5
        half = 140 - i * 2.2 + rng.uniform(-30, 30)
        if half < 12:
            continue
        d.line([(sx - half, y), (sx + half, y)], fill=(255, 196, 140), width=3)
    for _ in range(160):
        x = rng.uniform(0, W)
        y = rng.uniform(HORIZON + 20, SHORE - 20)
        w = rng.uniform(20, 70)
        c = (120, 130, 170) if rng.random() < 0.6 else (240, 170, 140)
        d.line([(x - w, y), (x + w, y)], fill=c, width=2)

    # Kiyi kopugu ve islak kum
    for k in range(3):
        pts = [(x, SHORE + 6 + k * 16 + 10 * math.sin(x / 120 + k)) for x in range(0, W + 20, 20)]
        d.line(pts, fill=(250, 236, 220) if k == 0 else (214, 166, 132), width=10 if k == 0 else 6)

    # Kum taneleri
    for _ in range(9000):
        x = rng.uniform(0, W)
        y = rng.uniform(SHORE + 40, H)
        c = rng.choice([(214, 160, 116), (244, 198, 150), (200, 146, 110)])
        d.point((x, y), fill=c)

    # Kayalar: sol ve sag koy
    def rock(cx, cy, rx, ry, seed):
        r = random.Random(seed)
        pts = []
        n = 14
        for i in range(n):
            a = 2 * math.pi * i / n
            k = r.uniform(0.78, 1.08)
            pts.append((cx + math.cos(a) * rx * k, cy + math.sin(a) * ry * k))
        d.polygon(pts, fill=(104, 84, 92))
        # Gun batimi kenar isigi (sag ust)
        hl = [(x - 10, y - 8) for x, y in pts]
        d.polygon([(cx + (x - cx) * 0.86 - 8, cy + (y - cy) * 0.86 - 10) for x, y in pts], fill=(132, 104, 108))
        d.line(pts[10:] + pts[:3], fill=(240, 160, 120), width=6)
        d.line(pts + [pts[0]], fill=(62, 48, 60), width=4)
        for _ in range(4):
            x = cx + r.uniform(-rx * 0.5, rx * 0.5)
            y = cy + r.uniform(-ry * 0.4, ry * 0.4)
            d.line([(x, y), (x + r.uniform(-40, 40), y + r.uniform(10, 40))], fill=(80, 62, 72), width=3)

    left = [(120, 520, 260, 200), (60, 820, 300, 260), (330, 700, 200, 150), (180, 1140, 330, 280), (430, 1010, 170, 130),
            (90, 1380, 360, 200)]
    right = [(2440, 520, 260, 200), (2500, 820, 300, 260), (2230, 690, 190, 140), (2400, 1150, 330, 290),
             (2150, 1020, 160, 120), (2470, 1390, 360, 200)]
    for i, (cx, cy, rx, ry) in enumerate(left + right):
        rock(cx, cy, rx, ry, i)

    # Uzak burun kayaliklari (ufukta)
    d.polygon([(0, HORIZON), (0, HORIZON - 80), (160, HORIZON - 110), (330, HORIZON - 60), (460, HORIZON)], fill=(84, 60, 96))
    d.polygon([(W, HORIZON), (W, HORIZON - 90), (2380, HORIZON - 120), (2200, HORIZON - 50), (2100, HORIZON)], fill=(84, 60, 96))

    img.save(os.path.join(out, "Day2BeachBackground.png"))
    img.resize((W // 4, H // 4)).filter(ImageFilter.GaussianBlur(6)).resize((W // 2, H // 2)).save(
        os.path.join(out, "Day2BeachBackdrop.png"))


# ---------------- Caretta ----------------

S = 2  # 2 kat buyuk cizip kucult (kenar yumusatma)


def ell_layer(size, box, fill, outline, width, angle=0, center=None):
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse(box, fill=fill, outline=outline, width=width)
    if angle:
        layer = layer.rotate(angle, resample=Image.BICUBIC, center=center)
    return layer


def caretta(w=680, h=640, baby=False):
    size = (w * S, h * S)
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    skin = (164, 132, 92) if not baby else (88, 76, 66)
    skin_dark = (104, 80, 56) if not baby else (46, 40, 36)
    shell = (182, 94, 50) if not baby else (98, 78, 62)
    shell_dark = (112, 58, 32) if not baby else (56, 44, 38)
    line = (70, 42, 26) if not baby else (30, 26, 24)
    cx = w * S // 2
    ow = 6 * S

    def flipper(x, y, rx, ry, ang):
        box = (x - rx, y - ry, x + rx, y + ry)
        return ell_layer(size, box, skin, line, ow, ang, (x, y))

    # Yuzgecler (on: buyuk, arka: kucuk)
    for sgn in (-1, 1):
        img.alpha_composite(flipper(cx + sgn * 190 * S, 210 * S, 150 * S, 48 * S, -sgn * 35))
        img.alpha_composite(flipper(cx + sgn * 140 * S, 500 * S, 70 * S, 34 * S, sgn * 40))
    # Kuyruk
    d = ImageDraw.Draw(img)
    d.polygon([(cx - 22 * S, 540 * S), (cx + 22 * S, 540 * S), (cx, 600 * S)], fill=skin, outline=line)
    # Bas
    d.ellipse((cx - 64 * S, 30 * S, cx + 64 * S, 190 * S), fill=skin, outline=line, width=ow)
    for sx in (-1, 1):
        d.ellipse((cx + sx * 34 * S - 12 * S, 82 * S, cx + sx * 34 * S + 12 * S, 106 * S), fill=(30, 22, 18))
        d.ellipse((cx + sx * 34 * S - 4 * S, 86 * S, cx + sx * 34 * S + 2 * S, 94 * S), fill=(255, 255, 255))
    # Bas pulları
    for (px, py, r) in [(0, 60, 16), (-26, 126, 14), (26, 126, 14), (0, 150, 12)]:
        d.ellipse((cx + px * S - r * S, py * S - r * S, cx + px * S + r * S, py * S + r * S), outline=skin_dark, width=3 * S)
    # Kabuk
    top, bot = 150 * S, 560 * S
    d.ellipse((cx - 200 * S, top, cx + 200 * S, bot), fill=shell, outline=line, width=ow)
    d.ellipse((cx - 168 * S, top + 28 * S, cx + 168 * S, bot - 26 * S), outline=shell_dark, width=4 * S)
    # Omurga plakalari
    ys = [200, 270, 345, 420, 490]
    for i in range(len(ys) - 1):
        y0, y1 = ys[i] * S, ys[i + 1] * S
        hw = 58 * S
        d.polygon([(cx, y0), (cx + hw, y0 + 16 * S), (cx + hw, y1 - 16 * S), (cx, y1), (cx - hw, y1 - 16 * S), (cx - hw, y0 + 16 * S)],
                  fill=lerp(shell, (230, 150, 80), 0.25) if not baby else lerp(shell, (150, 130, 110), 0.25),
                  outline=shell_dark, width=4 * S)
    # Yan plakalar
    for sgn in (-1, 1):
        for y in (250, 330, 410, 470):
            d.line([(cx + sgn * 58 * S, y * S), (cx + sgn * 182 * S, (y + sgn * 0 - 12) * S)], fill=shell_dark, width=4 * S)
    # Kabuk parlamasi
    hl = Image.new("L", size, 0)
    ImageDraw.Draw(hl).ellipse((cx - 150 * S, top + 30 * S, cx - 30 * S, top + 160 * S), fill=70)
    hl = hl.filter(ImageFilter.GaussianBlur(20 * S))
    white = Image.new("RGBA", size, (255, 240, 210, 0))
    white.putalpha(ImageChops.multiply(hl, img.getchannel("A")))
    img.alpha_composite(white)
    return img.resize((w, h), Image.LANCZOS)


# Agin uc parcasi: 0 on yuzgec, 1 boyun, 2 kabuk (her biri kendi dugumuyle kesilir)
NET_PARTS = [((20, 140, 290, 300), [(40, 280)]),
             ((250, 110, 430, 260), [(260, 250)]),
             ((190, 300, 560, 520), [(540, 470), (400, 510)])]


def net_overlay(part, w=680, h=640):
    """Yesil hayalet agin bir parcasi."""
    size = (w * S, h * S)
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    green = (64, 176, 112, 255)
    dark = (30, 104, 66, 255)
    rng = random.Random(5 + part)
    box, strands = NET_PARTS[part]
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).ellipse(tuple(v * S for v in box), fill=255)
    step = 64 * S
    for col, wd in ((dark, 8), (green, 4)):
        for k in range(-20, 30):
            x0 = k * step
            d.line([(x0, 0), (x0 + h * S, h * S)], fill=col, width=wd * S)
            d.line([(x0 + w * S, 0), (x0 + w * S - h * S, h * S)], fill=col, width=wd * S)
    img.putalpha(ImageChops.multiply(img.getchannel("A"), mask))
    # Sarkan ip uclari
    d = ImageDraw.Draw(img)
    for (x, y) in strands:
        pts = [(x * S, y * S)]
        for i in range(5):
            x += rng.uniform(-18, 18)
            y += 16
            pts.append((x * S, y * S))
        d.line(pts, fill=green, width=6 * S)
    return img.resize((w, h), Image.LANCZOS)


def moon():
    s = 360
    img = Image.new("RGBA", (s * S, s * S), (0, 0, 0, 0))
    glow = Image.new("L", img.size, 0)
    gd = ImageDraw.Draw(glow)
    c = s * S // 2
    for r in range(c, 0, -4):
        gd.ellipse((c - r, c - r, c + r, c + r), fill=int(120 * (1 - r / c) ** 2))
    glow = glow.filter(ImageFilter.GaussianBlur(10 * S))
    g = Image.new("RGBA", img.size, (220, 230, 255, 0))
    g.putalpha(glow)
    img.alpha_composite(g)
    d = ImageDraw.Draw(img)
    r = 80 * S
    d.ellipse((c - r, c - r, c + r, c + r), fill=(246, 244, 226))
    for (x, y, rr) in [(-26, -18, 18), (24, 14, 13), (-6, 34, 10), (30, -30, 8)]:
        d.ellipse((c + x * S - rr * S, c + y * S - rr * S, c + x * S + rr * S, c + y * S + rr * S), fill=(222, 220, 200))
    return img.resize((s, s), Image.LANCZOS)


background()
caretta().save(os.path.join(out, "Caretta.png"))
for i in range(3):
    net_overlay(i).save(os.path.join(out, "CarettaNet%d.png" % i))
caretta(680, 640, baby=True).resize((84, 80), Image.LANCZOS).save(os.path.join(out, "CarettaBaby.png"))
moon().save(os.path.join(out, "Moon.png"))
print("ok")
