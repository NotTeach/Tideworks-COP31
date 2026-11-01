"""Deniz (balik + cop toplama) ve ayristirma ekranlarinin arka plan ve arayuz gorselleri.
Kullanim: python fishing_ui.py <Assets/Sprites/Fishing>     (artboard 2560x1440)"""
import sys, json, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

out = sys.argv[1]
W, H, S = 2560, 1440, 2          # buyuk zeminler 2x cizilip kucultulur
INK = (40, 52, 56, 255)
DARK = (16, 62, 72, 255)
CREAM = (251, 246, 234, 255)


def shore_x(y):
    """Sol kumsalin deniz kenari (carpisma icin FishingController ayni formulu kullanir)."""
    return 560 - 170 * (y / H) + 46 * math.sin(y / 260)


ISLANDS = [  # merkez x, y, yaricap x, y (carpisma elipsleri de bunlar)
    (1381, 505, 74, 42), (1975, 545, 190, 86), (2080, 1262, 350, 165),
]

# ---------------- Deniz zemini ----------------
im = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 255))
d = ImageDraw.Draw(im)
for y in range(0, H, 4):
    t = y / H
    d.rectangle([0, y * S, W * S, (y + 4) * S], fill=(int(48 - 10 * t), int(160 - 26 * t), int(166 - 22 * t), 255))
rng = np.random.default_rng(11)
for _ in range(46):  # dalga kivrimlari
    x, y = rng.uniform(600, W - 40), rng.uniform(40, H - 40)
    pts = [((x + i * 5) * S, (y + 6 * math.sin(i / 3)) * S) for i in range(14)]
    d.line(pts, fill=(120, 205, 205, 255), width=5 * S, joint="curve")
# kumsal
sand = [(shore_x(y) * S, y * S) for y in range(0, H + 10, 10)]
d.polygon([(0, 0)] + sand + [(0, H * S)], fill=(244, 222, 180, 255))
d.line([((shore_x(y) + 10) * S, y * S) for y in range(0, H + 10, 10)], fill=(250, 250, 244, 255), width=14 * S, joint="curve")
d.line([((shore_x(y) - 16) * S, y * S) for y in range(0, H + 10, 10)], fill=(232, 206, 160, 255), width=10 * S, joint="curve")
for _ in range(40):
    x, y = rng.uniform(30, 420), rng.uniform(30, H - 30)
    if x < shore_x(y) - 40:
        r = rng.uniform(4, 9)
        d.ellipse([(x - r) * S, (y - r * 0.6) * S, (x + r) * S, (y + r * 0.6) * S], fill=(222, 196, 150, 255))


def palm(d, x, y, s=1.0):
    pts = [((x + 30 * s * math.sin(t * 1.4)) * S, (y - 200 * s * t) * S) for t in np.linspace(0, 1, 20)]
    d.line(pts, fill=(130, 92, 60, 255), width=int(14 * s * S), joint="curve")
    tx, ty = pts[-1][0] / S, pts[-1][1] / S
    for a in (-160, -120, -60, -20, 200):
        r = math.radians(a)
        leaf = [((tx + 110 * s * t * math.cos(r)) * S, (ty + 110 * s * t * math.sin(r) + 40 * s * t * t) * S) for t in np.linspace(0, 1, 12)]
        d.line(leaf, fill=(74, 140, 90, 255), width=int(20 * s * S), joint="curve")


palm(d, 220, 330, 1.0)
palm(d, 150, 1160, 0.8)
# adalar: kopuklu kenar + kum + kayalar
for i, (cx, cy, rx, ry) in enumerate(ISLANDS):
    d.ellipse([(cx - rx - 22) * S, (cy - ry - 18) * S, (cx + rx + 22) * S, (cy + ry + 18) * S], fill=(90, 190, 190, 255))
    d.ellipse([(cx - rx - 8) * S, (cy - ry - 6) * S, (cx + rx + 8) * S, (cy + ry + 6) * S], fill=(250, 250, 244, 255))
    blob = [((cx + rx * math.cos(t) * (1 + 0.06 * math.sin(5 * t + i))) * S, (cy + ry * math.sin(t) * (1 + 0.08 * math.sin(4 * t))) * S)
            for t in np.linspace(0, 2 * math.pi, 80)]
    d.polygon(blob, fill=(244, 222, 180, 255))
    if i == 1: palm(d, cx + 10, cy + 20, 0.75)
    if i == 2:
        for (ox, oy, r) in ((-180, 40, 26), (-140, 60, 18), (150, -60, 22)):
            d.ellipse([(cx + ox - r) * S, (cy + oy - r * 0.7) * S, (cx + ox + r) * S, (cy + oy + r * 0.7) * S], fill=(160, 150, 140, 255), outline=INK, width=3 * S)
bg = im.resize((W, H), Image.LANCZOS).convert("RGB")
bg.save(f"{out}/SeaBackground.png", optimize=True)
bg.resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{out}/SeaBackdrop.png")

# Kirli deniz katmani (deniz sagliklandikca saydamlasir)
A = np.asarray(bg).astype(np.int32)
sea = (A[..., 2] > A[..., 0] + 40) & (A[..., 1] > A[..., 0] + 40)
murk = np.zeros((H, W, 4), np.uint8)
murk[..., 0], murk[..., 1], murk[..., 2] = 96, 104, 76
murk[..., 3] = (sea * 165).astype(np.uint8)
m = Image.fromarray(murk, "RGBA").resize((W // 4, H // 4), Image.LANCZOS)  # yumusak ve hafif
m.save(f"{out}/SeaMurk.png")

json.dump({"islands": ISLANDS}, open(f"{out}/../../../Tools/MenuArt/fishing/sea_layout.json", "w"), indent=1)


# ---------------- Arayuz kutulari ----------------
def rr(name, w, h, r, fill, outline=None, ow=0, lip=0, lip_color=None):
    k = 4
    im = Image.new("RGBA", (w * k, h * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    def box(t, b=0): return [t * k, t * k, (w - t) * k - 1, (h - t - b) * k - 1]
    if lip:
        d.rounded_rectangle(box(0), radius=r * k, fill=lip_color)
        d.rounded_rectangle(box(0, lip), radius=r * k, fill=outline or fill)
        if outline: d.rounded_rectangle(box(ow, lip), radius=(r - ow) * k, fill=fill)
    elif outline:
        d.rounded_rectangle(box(0), radius=r * k, fill=outline)
        d.rounded_rectangle(box(ow), radius=max(1, r - ow) * k, fill=fill)
    else:
        d.rounded_rectangle(box(0), radius=r * k, fill=fill)
    im.resize((w, h), Image.LANCZOS).save(f"{out}/{name}.png")


rr("HudTeal", 110, 110, 26, (26, 124, 128, 255), outline=DARK, ow=5)
rr("HudCream", 260, 100, 30, CREAM, outline=DARK, ow=5)
rr("HudDark220", 230, 100, 30, DARK)
rr("HudDark420", 430, 112, 26, DARK)
rr("HudDark520", 560, 120, 26, DARK)
rr("HudPill", 150, 62, 31, (214, 236, 234, 255))
rr("BarTrack", 340, 18, 9, (40, 110, 114, 255))
rr("BarFill", 340, 18, 9, (110, 214, 204, 255))
rr("Tooltip", 780, 140, 26, CREAM, outline=DARK, ow=5)
rr("EquippedCard", 380, 130, 26, CREAM, outline=DARK, ow=5)
rr("BagStrip", 980, 124, 30, DARK)
rr("BagSlot", 84, 84, 16, (38, 120, 124, 255))
rr("SortButton", 400, 116, 30, (198, 170, 128, 255), outline=(90, 66, 44, 255), ow=5, lip=10, lip_color=(90, 66, 44, 255))
rr("RestartButton", 260, 100, 30, CREAM, outline=DARK, ow=5)
# ayristirma ekrani
rr("TopBar", 2560, 150, 1, (16, 62, 72, 255))
rr("TaskPill", 520, 96, 48, (30, 120, 124, 255))
rr("BinsPanel", 680, 1170, 40, (252, 246, 232, 255), outline=(232, 206, 150, 255), ow=6)
rr("BinRow", 600, 186, 30, (252, 238, 204, 255), outline=(228, 200, 140, 255), ow=5)
rr("BinRowRight", 600, 186, 30, (214, 240, 214, 255), outline=(80, 160, 90, 255), ow=7)
rr("BinRowWrong", 600, 186, 30, (250, 214, 204, 255), outline=(200, 80, 60, 255), ow=7)
rr("ScoreCard", 610, 300, 34, DARK)
rr("ScoreRight", 270, 150, 22, (30, 120, 124, 255))
rr("ScoreWrong", 270, 150, 22, (128, 50, 28, 255))
rr("BagPanel", 610, 790, 34, (252, 246, 232, 255), outline=(232, 206, 150, 255), ow=6)
rr("BagTile", 160, 110, 20, (242, 226, 192, 255))
rr("FinishCard", 1100, 760, 50, (252, 246, 232, 255), outline=(60, 38, 26, 255), ow=8)
rr("FinishButton", 520, 124, 34, (180, 72, 42, 255), lip=14, lip_color=(116, 44, 24, 255))

# Ayristirma zemini: kareli krem kagit
pap = Image.new("RGB", (W, H), (246, 232, 200)); pd = ImageDraw.Draw(pap)
for x in range(0, W, 48): pd.line([(x, 0), (x, H)], fill=(238, 222, 186), width=2)
for y in range(0, H, 48): pd.line([(0, y), (W, y)], fill=(238, 222, 186), width=2)
pap.save(f"{out}/SortBackground.png", optimize=True)
pap.resize((640, 360)).save(f"{out}/SortBackdrop.png")

# Esya tabagi: kesikli cizgili daire + golge
k = 4; s = 600
im = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([10 * k, 10 * k, (s - 10) * k, (s - 10) * k], fill=(252, 246, 232, 255))
for a in range(0, 360, 8):
    d.arc([10 * k, 10 * k, (s - 10) * k, (s - 10) * k], a, a + 4, fill=(220, 190, 130, 255), width=5 * k)
d.ellipse([90 * k, 400 * k, 510 * k, 470 * k], fill=(230, 210, 170, 255))
im.resize((s, s), Image.LANCZOS).save(f"{out}/ItemPlate.png")


# Cop kutulari (renkli, cizgili)
def bin_icon(name, col):
    k = 4; w, h = 110, 130
    im = Image.new("RGBA", (w * k, h * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.polygon([(14 * k, 34 * k), (96 * k, 34 * k), (88 * k, 124 * k), (22 * k, 124 * k)], fill=INK)
    d.polygon([(19 * k, 39 * k), (91 * k, 39 * k), (84 * k, 119 * k), (26 * k, 119 * k)], fill=col)
    for x in (40, 55, 70):
        d.line([(x * k, 52 * k), (x * k, 106 * k)], fill=INK, width=4 * k)
    d.rounded_rectangle([6 * k, 18 * k, 104 * k, 36 * k], radius=6 * k, fill=INK)
    d.rounded_rectangle([10 * k, 22 * k, 100 * k, 32 * k], radius=4 * k, fill=col)
    d.rounded_rectangle([40 * k, 6 * k, 70 * k, 20 * k], radius=5 * k, fill=INK)
    im.resize((w, h), Image.LANCZOS).save(f"{out}/{name}.png")


BINS = {"BinMetal": (150, 156, 160, 255), "BinGlass": (70, 150, 96, 255), "BinPaper": (60, 104, 160, 255),
        "BinPlastic": (240, 192, 70, 255), "BinHousehold": (120, 84, 60, 255)}
for n, c in BINS.items():
    bin_icon(n, c)
    k = 4; s = 64
    im = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.ellipse([0, 0, s * k - 1, s * k - 1], fill=INK)
    d.ellipse([4 * k, 4 * k, (s - 4) * k, (s - 4) * k], fill=c)
    im.resize((s, s), Image.LANCZOS).save(f"{out}/{n}Count.png")

# Joystick ve olta butonu (telefon)
k = 4; s = 320
im = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([0, 0, s * k - 1, s * k - 1], fill=(16, 62, 72, 110))
d.ellipse([6 * k, 6 * k, (s - 6) * k, (s - 6) * k], outline=(255, 255, 255, 170), width=6 * k)
im.resize((s, s), Image.LANCZOS).save(f"{out}/JoystickBase.png")
s = 140
im = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([0, 0, s * k - 1, s * k - 1], fill=DARK)
d.ellipse([6 * k, 6 * k, (s - 6) * k, (s - 6) * k], fill=(240, 246, 244, 255))
im.resize((s, s), Image.LANCZOS).save(f"{out}/JoystickKnob.png")
s = 230
im = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([0, 0, s * k - 1, s * k - 1], fill=DARK)
d.ellipse([8 * k, 8 * k, (s - 8) * k, (s - 8) * k], fill=(244, 192, 74, 255))
im.resize((s, s), Image.LANCZOS).save(f"{out}/RodButton.png")

# Olta ucu (samandira) ve suya dusme halkasi
s = 48
im = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([4 * k, 4 * k, (s - 4) * k, (s - 4) * k], fill=INK)
d.pieslice([8 * k, 8 * k, (s - 8) * k, (s - 8) * k], 180, 360, fill=(220, 70, 56, 255))
d.pieslice([8 * k, 8 * k, (s - 8) * k, (s - 8) * k], 0, 180, fill=(250, 248, 240, 255))
im.resize((s, s), Image.LANCZOS).save(f"{out}/Bobber.png")
s = 200
im = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([10 * k, 10 * k, (s - 10) * k, (s - 10) * k], outline=(240, 252, 252, 230), width=8 * k)
im.resize((s, s), Image.LANCZOS).save(f"{out}/Splash.png")

# Menu (dalga) simgesi
s = 64
im = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
for i, y in enumerate((20, 34, 48)):
    d.line([((6 + j * 4) * k, (y + 4 * math.sin(j / 2)) * k) for j in range(14)], fill=(255, 255, 255, 255), width=5 * k, joint="curve")
im.resize((s, s), Image.LANCZOS).save(f"{out}/IconWaves.png")
print("ok")


# ---------------- Dalgalar, iz ve "How to play" karti ----------------
k = 4
# hareketli dalga cizgisi (beyaz, ucu inceltilmis)
w, h = 150, 34
im = Image.new("RGBA", (w * k, h * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
pts = [((8 + i * 6.7) * k, (17 + 8 * math.sin(i / 3.0)) * k) for i in range(21)]
d.line(pts, fill=(235, 252, 250, 255), width=6 * k, joint="curve")
im = im.resize((w, h), Image.LANCZOS)
a = np.asarray(im).astype(np.float32)
fade = np.clip(np.minimum(np.arange(w), w - 1 - np.arange(w)) / 30.0, 0, 1)
a[..., 3] *= fade[None, :]
Image.fromarray(a.astype(np.uint8), "RGBA").save(f"{out}/WaveLine.png")

# tekne izi kopugu
s = 120
im = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([10 * k, 30 * k, (s - 10) * k, (s - 30) * k], outline=(245, 252, 250, 220), width=7 * k)
im.resize((s, s), Image.LANCZOS).save(f"{out}/Wake.png")

rr("HowToCard", 1400, 1000, 50, CREAM, outline=(60, 38, 26, 255), ow=8)
rr("HowToRow", 1240, 128, 28, (246, 234, 206, 255))
rr("GoButton", 460, 124, 34, (180, 72, 42, 255), lip=14, lip_color=(116, 44, 24, 255))
rr("KeyCap", 84, 84, 16, (250, 248, 242, 255), outline=(52, 42, 40, 255), ow=4, lip=8, lip_color=(52, 42, 40, 255))
rr("KeyCapWide", 230, 84, 16, (250, 248, 242, 255), outline=(52, 42, 40, 255), ow=4, lip=8, lip_color=(52, 42, 40, 255))

# ok tuslari: kapak sabit (alt dudak hep altta), sadece ok doner
for name, ang in (("KeyUp", 0), ("KeyRight", -90), ("KeyDown", 180), ("KeyLeft", 90)):
    arrow = Image.new("RGBA", (84 * k, 84 * k), (0, 0, 0, 0)); ad = ImageDraw.Draw(arrow)
    ad.polygon([(42 * k, 14 * k), (62 * k, 38 * k), (22 * k, 38 * k)], fill=(52, 42, 40, 255))
    ad.rectangle([(37 * k, 36 * k), (47 * k, 60 * k)], fill=(52, 42, 40, 255))
    arrow = arrow.rotate(ang, resample=Image.BICUBIC, center=(42 * k, 37 * k))
    cap = Image.open(f"{out}/KeyCap.png").convert("RGBA").resize((84 * k, 84 * k), Image.LANCZOS)
    cap.alpha_composite(arrow, (0, -2 * k))
    cap.resize((84, 84), Image.LANCZOS).save(f"{out}/{name}.png")
print("ok2")
