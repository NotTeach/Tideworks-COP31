"""3. gun tezgahi: Mert'in pusulasi (katmanlar), alet / raf yuvalari ve Mert portreli tezgah zemini.
Kullanim: python day3_bench_art.py <Assets/Sprites> <27.png (pusula tezgahi tasarimi, 1280x720)>
Cikti: Assets/Sprites/Day3. Pusula katmanlarinin hepsi ayni 640x700 tuvaldedir (merkez 320,400);
ibre, makas gibi hareketli parcalar ayri kucuk sprite'lardir. Tamamlanmis katmanlarin toplami onarilmis pusuladir."""
import sys, os, math, shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SP, design = sys.argv[1:3]
OUT = f"{SP}/Day3"
os.makedirs(OUT, exist_ok=True)
S = 4
INK = (60, 40, 30, 255)
BRASS, BRASS_D, BRASS_L = (214, 166, 64, 255), (164, 118, 40, 255), (246, 214, 120, 255)
FACE = (246, 238, 214, 255)
CW, CH = 640, 700
CX, CY, R = 320, 400, 250
FONT = os.path.join(os.path.dirname(SP), "Fonts", "Baloo2-ExtraBold.ttf")


def canvas(w=CW, h=CH):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (x * S, y * S))


def save(im, name, w=CW, h=CH):
    im.resize((w, h), Image.LANCZOS).save(f"{OUT}/{name}.png")


def circle(d, P, cx, cy, r, **kw):
    d.ellipse([P(cx - r, cy - r), P(cx + r, cy + r)], **kw)


# ---------------- Govde (kadran, harfler, gul) ----------------
im, d, P = canvas()
circle(d, P, CX, 140, 30, fill=INK)                               # tepe topuzu
circle(d, P, CX, 140, 24, fill=BRASS)
circle(d, P, CX, 140, 11, fill=INK)
d.rectangle([P(CX - 26, 150), P(CX + 26, 172)], fill=INK)
d.rectangle([P(CX - 20, 150), P(CX + 20, 168)], fill=BRASS_D)
circle(d, P, CX, CY, R + 6, fill=INK)
circle(d, P, CX, CY, R, fill=BRASS)
circle(d, P, CX, CY, R - 10, fill=BRASS_L)
circle(d, P, CX, CY, R - 22, fill=BRASS)
for i in range(36):                                               # tirtikli kenar
    a = math.radians(i * 10)
    d.line([P(CX + (R - 4) * math.cos(a), CY + (R - 4) * math.sin(a)), P(CX + (R - 18) * math.cos(a), CY + (R - 18) * math.sin(a))],
           fill=BRASS_D, width=3 * S)
circle(d, P, CX, CY, 206, fill=INK)
circle(d, P, CX, CY, 200, fill=FACE)
for i in range(72):                                               # derece cizgileri
    a = math.radians(i * 5)
    r0 = 186 if i % 2 else 176
    d.line([P(CX + r0 * math.cos(a), CY + r0 * math.sin(a)), P(CX + 196 * math.cos(a), CY + 196 * math.sin(a))], fill=(120, 96, 70, 255), width=2 * S)
for k in range(8):                                                # 8 kollu gul
    a = math.radians(k * 45 - 90)
    long = 150 if k % 2 == 0 else 96
    tip = (CX + long * math.cos(a), CY + long * math.sin(a))
    side = math.radians(k * 45 - 90 + 90)
    w = 22 if k % 2 == 0 else 16
    l = (CX + w * math.cos(side), CY + w * math.sin(side))
    r = (CX - w * math.cos(side), CY - w * math.sin(side))
    d.polygon([P(*l), P(*tip), P(CX, CY)], fill=(200, 176, 132, 255), outline=(120, 96, 70, 255))
    d.polygon([P(*r), P(*tip), P(CX, CY)], fill=(232, 214, 176, 255), outline=(120, 96, 70, 255))
font = ImageFont.truetype(FONT, 58 * S)
for letter, (dx, dy), col in (("K", (0, -134), (196, 64, 52, 255)), ("G", (0, 134), INK), ("B", (-134, 0), INK), ("D", (134, 0), INK)):
    x, y = CX + dx, CY + dy
    d.text(P(x, y + 6), letter, font=font, fill=col, anchor="mm")
circle(d, P, CX, CY, 14, fill=INK)
circle(d, P, CX, CY, 9, fill=BRASS)
save(im, "CompassCase")

# ---------------- Kirik cam (catlaklar + yildiz delikler) ----------------
im, d, P = canvas()
circle(d, P, CX, CY, 200, fill=(214, 236, 240, 70))
rng = np.random.default_rng(7)
for hx, hy in ((250, 320), (420, 300), (300, 520), (380, 510), (330, 400)):
    for k in range(6):
        a = rng.uniform(0, 2 * math.pi)
        pts = [(hx, hy)]
        x, y = hx, hy
        for _ in range(4):
            a += rng.uniform(-0.5, 0.5)
            x += 26 * math.cos(a); y += 26 * math.sin(a)
            if (x - CX) ** 2 + (y - CY) ** 2 > 196 ** 2: break
            pts.append((x, y))
        d.line([P(px, py) for px, py in pts], fill=(70, 70, 76, 220), width=2 * S)
mask = Image.new("L", im.size, 0)
ImageDraw.Draw(mask).ellipse([P(CX - 200, CY - 200), P(CX + 200, CY + 200)], fill=255)
im.putalpha(Image.fromarray((np.asarray(im.split()[3]) * (np.asarray(mask) / 255)).astype(np.uint8)))
save(im, "CompassGlassBroken")

# ---------------- Cam parcalari (cimbizla alinir) ----------------
for i, pts in enumerate(([(10, 40), (50, 6), (72, 52), (40, 74)], [(8, 20), (66, 10), (58, 70)],
                         [(20, 8), (70, 30), (50, 72), (6, 56)], [(12, 12), (72, 26), (30, 70)])):
    im, d, P = canvas(80, 80)
    d.polygon([P(x, y) for x, y in pts], fill=(214, 238, 246, 230), outline=INK)
    d.line([P(pts[0][0] + 6, pts[0][1] + 4), P(pts[1][0] - 6, pts[1][1] + 6)], fill=(255, 255, 255, 255), width=3 * S)
    save(im, f"Shard{i}", 80, 80)

# ---------------- Egri ibre (ilk adimda cikar) ----------------
im, d, P = canvas()
d.line([P(CX - 20, CY + 120), P(CX, CY), P(CX + 70, CY - 60), P(CX + 40, CY - 120)], fill=INK, width=14 * S, joint="curve")
d.line([P(CX - 20, CY + 120), P(CX, CY)], fill=(60, 70, 80, 255), width=8 * S)
d.line([P(CX, CY), P(CX + 70, CY - 60), P(CX + 40, CY - 120)], fill=(170, 80, 64, 255), width=8 * S, joint="curve")
save(im, "CompassNeedleBent")

# ---------------- Ibre (ayri sprite, ortadan doner; kuzey ucu yukari) ----------------
NW, NH = 64, 340
im, d, P = canvas(NW, NH)
d.polygon([P(32, 6), P(54, 170), P(10, 170)], fill=INK)
d.polygon([P(32, 16), P(47, 166), P(17, 166)], fill=(214, 68, 56, 255))
d.polygon([P(32, 334), P(54, 170), P(10, 170)], fill=INK)
d.polygon([P(32, 324), P(47, 174), P(17, 174)], fill=(70, 86, 100, 255))
circle(d, P, 32, 170, 14, fill=INK)
circle(d, P, 32, 170, 8, fill=BRASS_L)
save(im, "CompassNeedle", NW, NH)

# ---------------- Yeni lens (deniz cami: hafif yesil-mavi, parlak kavis) ----------------
im, d, P = canvas()
circle(d, P, CX, CY, 202, fill=(150, 214, 206, 46))
d.arc([P(CX - 170, CY - 170), P(CX + 170, CY + 170)], 200, 260, fill=(255, 255, 255, 190), width=10 * S)
d.arc([P(CX - 150, CY - 150), P(CX + 150, CY + 150)], 210, 240, fill=(255, 255, 255, 140), width=6 * S)
save(im, "CompassLens")

# ---------------- Govde catlagi (sol kenar) ve bakir zimbalar ----------------
im, d, P = canvas()
crack = [(70, 330), (92, 352), (78, 376), (100, 400), (84, 428), (104, 450)]
d.line([P(x, y) for x, y in crack], fill=INK, width=6 * S)
d.line([P(x + 2, y) for x, y in crack], fill=(110, 76, 30, 255), width=3 * S)
save(im, "CompassCrack")
im, d, P = canvas(70, 36)
d.rounded_rectangle([P(4, 6), P(66, 30)], radius=6 * S, fill=INK)
d.rounded_rectangle([P(8, 10), P(62, 26)], radius=4 * S, fill=(196, 110, 60, 255))
d.line([P(12, 14), P(58, 14)], fill=(240, 170, 120, 255), width=2 * S)
save(im, "CopperStaple", 70, 36)

# ---------------- Ezik kenar (sag) ve teneke kelepce + vida ----------------
im, d, P = canvas()
a0, a1 = math.radians(-30), math.radians(10)
outer = [(CX + (R + 8) * math.cos(a), CY + (R + 8) * math.sin(a)) for a in np.linspace(a0, a1, 12)]
inner = [(CX + (R - 30 + 10 * math.sin(k)) * math.cos(a), CY + (R - 30 + 10 * math.sin(k)) * math.sin(a))
         for k, a in enumerate(np.linspace(a1, a0, 12))]
d.polygon([P(x, y) for x, y in outer + inner], fill=(120, 86, 34, 255))
for k in range(4):
    a = math.radians(-24 + k * 10)
    d.line([P(CX + (R - 26) * math.cos(a), CY + (R - 26) * math.sin(a)), P(CX + (R + 4) * math.cos(a + 0.05), CY + (R + 4) * math.sin(a + 0.05))],
           fill=INK, width=3 * S)
save(im, "CompassDent")
im, d, P = canvas()
a0, a1 = math.radians(-34), math.radians(14)
outer = [(CX + (R + 8) * math.cos(a), CY + (R + 8) * math.sin(a)) for a in np.linspace(a0, a1, 16)]
inner = [(CX + (R - 34) * math.cos(a), CY + (R - 34) * math.sin(a)) for a in np.linspace(a1, a0, 16)]
d.polygon([P(x, y) for x, y in outer + inner], fill=INK)
outer2 = [(CX + (R + 3) * math.cos(a), CY + (R + 3) * math.sin(a)) for a in np.linspace(a0 + 0.03, a1 - 0.03, 16)]
inner2 = [(CX + (R - 28) * math.cos(a), CY + (R - 28) * math.sin(a)) for a in np.linspace(a1 - 0.03, a0 + 0.03, 16)]
d.polygon([P(x, y) for x, y in outer2 + inner2], fill=(196, 204, 210, 255))
for k in range(3):                                                # ton baligi kutusu cizgileri
    rr = R - 18 + k * 12
    d.arc([P(CX - rr, CY - rr), P(CX + rr, CY + rr)], -30, 10, fill=(150, 160, 170, 255), width=2 * S)
save(im, "CompassClamp")
im, d, P = canvas(48, 48)
circle(d, P, 24, 24, 20, fill=INK)
circle(d, P, 24, 24, 15, fill=(214, 220, 226, 255))
d.line([P(12, 24), P(36, 24)], fill=INK, width=4 * S)
save(im, "ClampScrew", 48, 48)

# ---------------- Halka: kopuk ve yeni (hayalet ag ipi) ----------------
im, d, P = canvas()
d.line([P(CX - 30, 120), P(CX - 54, 70), P(CX - 46, 40)], fill=INK, width=12 * S, joint="curve")
d.line([P(CX - 30, 120), P(CX - 54, 70), P(CX - 46, 40)], fill=(196, 160, 110, 255), width=7 * S, joint="curve")
for k in range(3):
    d.line([P(CX - 46, 40), P(CX - 56 + k * 10, 20)], fill=(196, 160, 110, 255), width=3 * S)
d.line([P(CX + 30, 120), P(CX + 50, 96)], fill=INK, width=12 * S)
d.line([P(CX + 30, 120), P(CX + 50, 96)], fill=(196, 160, 110, 255), width=7 * S)
save(im, "CompassLoopBroken")
im, d, P = canvas()
d.ellipse([P(CX - 52, 14), P(CX + 52, 130)], outline=INK, width=18 * S)
d.ellipse([P(CX - 52, 14), P(CX + 52, 130)], outline=(64, 150, 112, 255), width=10 * S)
for k in range(14):                                               # burgulu ip
    a = math.radians(k * 360 / 14)
    x, y = CX + 52 * math.cos(a), 72 + 58 * math.sin(a)
    d.line([P(x - 5, y - 5), P(x + 5, y + 5)], fill=(150, 214, 180, 255), width=3 * S)
save(im, "CompassLoopNew")

# ---------------- Animasyon icin mıknatıs ----------------
im, d, P = canvas(140, 140)
d.arc([P(20, 10), P(120, 110)], 180, 360, fill=INK, width=34 * S)
d.arc([P(26, 16), P(114, 104)], 180, 360, fill=(214, 72, 60, 255), width=22 * S)
d.rectangle([P(20, 58), P(54, 128)], fill=INK); d.rectangle([P(86, 58), P(120, 128)], fill=INK)
d.rectangle([P(26, 60), P(48, 100)], fill=(214, 72, 60, 255)); d.rectangle([P(92, 60), P(114, 100)], fill=(214, 72, 60, 255))
d.rectangle([P(26, 100), P(48, 122)], fill=(200, 206, 212, 255)); d.rectangle([P(92, 100), P(114, 122)], fill=(200, 206, 212, 255))
save(im, "MagnetFx", 140, 140)


# ================= Alet ve raf yuvalari (2. gunun yuva stili) =================
def slot(w, h):
    im, d, P = canvas(w, h)
    d.rounded_rectangle([P(0, 0), P(w, h)], radius=22 * S, fill=INK)
    d.rounded_rectangle([P(5, 5), P(w - 5, h - 12)], radius=18 * S, fill=(246, 230, 196, 255))
    return im, d, P


def tool(name, draw):
    im, d, P = slot(210, 200)
    draw(d, P)
    save(im, name, 210, 200)


def magnet(d, P):
    d.arc([P(62, 24), P(148, 110)], 180, 360, fill=INK, width=30 * S)
    d.arc([P(67, 29), P(143, 105)], 180, 360, fill=(214, 72, 60, 255), width=20 * S)
    d.rectangle([P(62, 66), P(92, 128)], fill=INK); d.rectangle([P(118, 66), P(148, 128)], fill=INK)
    d.rectangle([P(67, 68), P(87, 104)], fill=(214, 72, 60, 255)); d.rectangle([P(123, 68), P(143, 104)], fill=(214, 72, 60, 255))
    d.rectangle([P(67, 104), P(87, 124)], fill=(200, 206, 212, 255)); d.rectangle([P(123, 104), P(143, 124)], fill=(200, 206, 212, 255))


def pliers(d, P):
    d.line([P(80, 130), P(112, 64)], fill=INK, width=14 * S); d.line([P(130, 130), P(98, 64)], fill=INK, width=14 * S)
    d.line([P(80, 130), P(112, 64)], fill=(232, 120, 70, 255), width=9 * S); d.line([P(130, 130), P(98, 64)], fill=(232, 120, 70, 255), width=9 * S)
    d.polygon([P(98, 64), P(112, 64), P(118, 24), P(108, 22)], fill=(150, 156, 164, 255), outline=INK)
    d.polygon([P(112, 64), P(98, 64), P(92, 24), P(102, 22)], fill=(170, 176, 184, 255), outline=INK)
    circle(d, P, 105, 66, 7, fill=INK)


def screwdriver(d, P):
    d.line([P(70, 128), P(110, 84)], fill=INK, width=24 * S)
    d.line([P(70, 128), P(110, 84)], fill=(232, 120, 70, 255), width=17 * S)
    d.line([P(110, 84), P(146, 40)], fill=INK, width=9 * S)
    d.line([P(110, 84), P(146, 40)], fill=(170, 176, 184, 255), width=5 * S)


for n, f in (("ToolMagnet", magnet), ("ToolPliers", pliers), ("ToolScrewdriver", screwdriver)):
    tool(n, f)
shutil.copy(f"{SP}/Day2/ToolTweezers.png", f"{OUT}/ToolTweezers.png")


def shelf(name, draw):
    im, d, P = slot(266, 200)
    d.rounded_rectangle([P(18, 18), P(70, 70)], radius=10 * S, fill=INK)
    d.rounded_rectangle([P(21, 21), P(67, 67)], radius=8 * S, fill=(252, 248, 240, 255))
    draw(d, P)
    save(im, name, 266, 200)


shelf("ShelfLens", lambda d, P: (circle(d, P, 44, 44, 17, fill=INK), circle(d, P, 44, 44, 13, fill=(170, 222, 214, 255)),
                                 d.arc([P(34, 34), P(54, 54)], 200, 260, fill=(255, 255, 255, 255), width=3 * S)))
shelf("ShelfStaples", lambda d, P: [d.line([P(30, y), P(40, y - 10), P(50, y + 4), P(60, y - 6)], fill=(196, 110, 60, 255), width=4 * S)
                                    for y in (40, 54)])
shelf("ShelfClamp", lambda d, P: (d.rounded_rectangle([P(28, 34), P(60, 54)], radius=4 * S, fill=INK),
                                  d.rounded_rectangle([P(31, 37), P(57, 51)], radius=3 * S, fill=(196, 204, 210, 255)),
                                  circle(d, P, 44, 44, 4, fill=INK)))
shelf("ShelfLoop", lambda d, P: (d.ellipse([P(30, 28), P(58, 62)], outline=INK, width=7 * S),
                                 d.ellipse([P(30, 28), P(58, 62)], outline=(64, 150, 112, 255), width=4 * S)))

# ================= Tezgah zemini: 2. gunun tezgahi + Mert portresi =================
B = Image.open(f"{SP}/Day2/BenchDay2Background.png").convert("RGBA")
src = Image.open(design).convert("RGB").resize((2560, 1440), Image.LANCZOS)
por = src.crop((80, 226, 196, 342)).resize((120, 120), Image.LANCZOS).convert("RGBA")
pm = Image.new("L", (480, 480), 0); ImageDraw.Draw(pm).ellipse([6, 6, 474, 474], fill=255)
por.putalpha(pm.resize((120, 120), Image.LANCZOS))
white = Image.new("RGBA", (130, 130), tuple(np.asarray(B)[300, 300].tolist()))
B.alpha_composite(white, (75, 223))                               # Elif portresini ort
B.alpha_composite(por, (80, 228))
B.convert("RGB").save(f"{OUT}/BenchDay3Background.png", optimize=True)
B.convert("RGB").resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{OUT}/BenchDay3Backdrop.png")
print("ok")
