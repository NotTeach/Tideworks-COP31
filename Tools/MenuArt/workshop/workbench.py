"""Tamir tezgahi gorselleri. Kullanim: python workbench.py <10.png> <cikis>
Koordinatlar 1280x720 tasarimda; cikti 2x (2560x1440)."""
import sys; sys.path.insert(0, sys.path[0])
from common import *
from PIL import ImageDraw
src, out = sys.argv[1], sys.argv[2]
A = load(src); H, W = A.shape[:2]
K = 2
O = Out(out, K)
C = A.copy()
yy, xx = np.mgrid[0:H, 0:W]

PANEL = [251, 248, 241]
C[203:532, 34:292] = PANEL                                  # tamir adimlari (koddan)
C[506:540, 356:1236] = A[530, 700]                          # mesaj yazisi
C[26:47, 1064:1264] = A[60, 1100]                           # ilerleme cubugu + "2 / 6"

TOOLS = [(116, 218), (230, 332), (344, 446), (458, 560)]
SHELF = [(670, 774), (786, 890), (902, 1006), (1018, 1122), (1134, 1238)]
SLOT_Y = (588, 688)
for x0, x1 in SHELF:                                        # "x1" sayaclari (koddan)
    C[657:682, x0 + 8:x0 + 36] = A[662, x0 + 70]

# Kadran: catlaklar silinir, kadran yuzu kalir (cam ayri sprite)
DC = (893.75, 330.0)
r = np.sqrt((xx - DC[0]) ** 2 + (yy - DC[1]) ** 2)
face = np.array([150, 172, 178])
white = (A.min(-1) > 215)
cracks = white & (r < 44.5) | (white & (r > 51) & (r < 75) & (yy > 360)) | ((r < 6) & (A.sum(-1) < 200))
cracks = nd.binary_dilation(cracks, iterations=2)
C[(cracks | (r < 44)) & (r < 52) & ~((r > 45) & (r < 51.5) & white)] = face
outside = cracks & (r >= 52)
C = inpaint(C, outside, 3)

# Gevsek kablo -> devre yesili
wb = np.zeros((H, W), bool); wb[398:438, 652:726] = True
green = np.array([72, 110, 87])
wire = wb & (np.abs(A - green).sum(-1) > 40) & (np.sqrt(((A - [30, 42, 46]) ** 2).sum(-1)) > 40)
wire = nd.binary_dilation(wire, iterations=1) & wb & ((xx - 744) ** 2 + (yy - 400) ** 2 > 31 ** 2)
C = row_fill(C, wire)

# Ayar dugmesi (sag): yuzu duz boya, cizgisi sprite'ta
KC = (933.75, 436.25)
C[(xx - KC[0]) ** 2 + (yy - KC[1]) ** 2 < 14.2 ** 2] = A[442, 922]

# ---- Kesimler ----
O.cut("LooseWire", A, soft(wire, 0.5), bg=C)
TRAY = A[700, 600]
flat = np.broadcast_to(np.array(TRAY), A.shape)
names = ["ToolScrewdriver", "ToolCloth", "ToolPliers", "ToolSolder",
         "ShelfGlass", "ShelfCopper", "ShelfWood", "ShelfRope", "ShelfFilament"]
for name, (x0, x1) in zip(names, TOOLS + SHELF):
    m = np.zeros((H, W), bool)
    ref = C[640, x0 - 7]
    m[SLOT_Y[0] - 4:SLOT_Y[1] + 4, x0 - 4:x1 + 4] = np.abs(C[SLOT_Y[0] - 4:SLOT_Y[1] + 4, x0 - 4:x1 + 4] - ref).max(-1) > 40
    m = nd.binary_fill_holes(nd.binary_closing(m, iterations=2))
    lab, n = nd.label(m); sizes = nd.sum(m, lab, range(1, n + 1))
    O.cut(name, C, soft(lab == np.argmax(sizes) + 1, 0.5))

O.full("WorkbenchBackground", C)

# ---- Prosedurel parcalar (2x cikti, 4x cizim) ----
S = 4; F = K * S


def canvas(box):
    x0, y0, x1, y1 = box
    im = Image.new("RGBA", (int((x1 - x0) * F), int((y1 - y0) * F)), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (int((x - x0) * F), int((y - y0) * F)))


def save(name, im, box):
    x0, y0, x1, y1 = box
    im.resize((int((x1 - x0) * K), int((y1 - y0) * K)), Image.LANCZOS).save(f"{out}/{name}.png")
    O.rects[name] = [int(x0 * K), int(y0 * K), int((x1 - x0) * K), int((y1 - y0) * K)]


OUTL = (52, 38, 30, 255)
# Kapak: eski (koyu ceviz) ve yeni (zimparalanmis acik tahta)
for name, wood, grain in (("LidOld", (138, 84, 50, 255), (112, 66, 38, 255)),
                          ("LidNew", (222, 178, 124, 255), (196, 150, 98, 255))):
    box = (601, 261, 811, 469)
    im, d, P = canvas(box)
    d.rounded_rectangle([P(602, 262), P(810, 468)], radius=12 * F, fill=OUTL)
    d.rounded_rectangle([P(605.5, 265.5), P(806.5, 464.5)], radius=9 * F, fill=wood)
    for gy in range(280, 456, 22):
        d.line([P(612, gy), P(800, gy + 3)], fill=grain, width=int(1.2 * F))
    for gy in range(318, 410, 14):                                # hoparlor izgarasi
        d.rounded_rectangle([P(648, gy), P(764, gy + 6)], radius=3 * F, fill=OUTL)
    for sx, sy in ((619, 280), (793, 280), (619, 451), (793, 451)):
        d.ellipse([P(sx - 8, sy - 8), P(sx + 8, sy + 8)], fill=grain)
    save(name, im, box)

# Vida
box = (0, 0, 18, 18)
im, d, P = canvas(box)
d.ellipse([P(1, 1), P(17, 17)], fill=OUTL)
d.ellipse([P(2.6, 2.6), P(15.4, 15.4)], fill=(200, 202, 198, 255))
d.arc([P(4, 4), P(14, 14)], 200, 290, fill=(245, 245, 240, 255), width=int(1.3 * F))
d.line([P(5, 13), P(13, 5)], fill=OUTL, width=int(2 * F))
save("Screw", im, box)
for i, (sx, sy) in enumerate(((619, 280), (793, 280), (619, 451), (793, 451))):
    O.rects[f"Screw{i}"] = [int((sx - 9) * K), int((sy - 9) * K), 18 * K, 18 * K]
del O.rects["Screw"]

# Toz lekeleri
rng = np.random.default_rng(7)
DUST = [(652, 318), (712, 352), (770, 300), (702, 432), (874, 300), (918, 362), (640, 405), (960, 300)]
for i, (cx, cy) in enumerate(DUST):
    s = 64
    a = np.zeros((s, s))
    for _ in range(14):
        bx, by, br = rng.uniform(16, 48), rng.uniform(16, 48), rng.uniform(5, 13)
        a = np.maximum(a, np.clip(1 - np.sqrt((np.mgrid[0:s, 0:s][1] - bx) ** 2 + (np.mgrid[0:s, 0:s][0] - by) ** 2) / br, 0, 1))
    a = nd.gaussian_filter(a, 1.5) * 0.85 + rng.uniform(0, 0.12, (s, s)) * (a > 0.1)
    rgba = np.dstack([np.full((s, s), 168), np.full((s, s), 158), np.full((s, s), 146), np.clip(a, 0, 0.9) * 255]).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").resize((s * 2, s * 2), Image.LANCZOS).save(f"{out}/Dust{i}.png")
    O.rects[f"Dust{i}"] = [int((cx - 32) * K), int((cy - 32) * K), 64 * K, 64 * K]

# Kadran camlari (catlak / temiz) ve ibre
R = 53.5
box = (DC[0] - 60, DC[1] - 60, DC[0] + 60, DC[1] + 70)
for name in ("GlassCracked", "GlassClear"):
    im, d, P = canvas(box)
    d.ellipse([P(DC[0] - R, DC[1] - R), P(DC[0] + R, DC[1] + R)], fill=(235, 245, 248, 46))
    d.arc([P(DC[0] - R + 7, DC[1] - R + 7), P(DC[0] + R - 7, DC[1] + R - 7)], 200, 255, fill=(255, 255, 255, 170), width=int(3 * F))
    if name == "GlassCracked":
        for (x1, y1) in ((862, 293), (933, 316), (880, 362), (912, 388)):
            d.line([P(DC[0] + 2, DC[1] + 4), P(x1, y1)], fill=(255, 255, 255, 240), width=int(2.6 * F))
        d.line([P(905, 345), P(870, 371)], fill=(255, 255, 255, 220), width=int(2 * F))
    save(name, im, box)

box = (DC[0] - 46, DC[1] - 46, DC[0] + 46, DC[1] + 46)       # ibre (merkezde doner)
im, d, P = canvas(box)
d.line([P(DC[0], DC[1] + 8), P(DC[0], DC[1] - 40)], fill=(178, 60, 36, 255), width=int(2.6 * F))
d.ellipse([P(DC[0] - 5.5, DC[1] - 5.5), P(DC[0] + 5.5, DC[1] + 5.5)], fill=OUTL)
save("Needle", im, box)

# Istasyon isareti (kadran yuzunde, ibrenin 50 derecesi)
a = np.radians(50)
mx, my = DC[0] + 41 * np.sin(a), DC[1] - 41 * np.cos(a)
box = (mx - 7, my - 7, mx + 7, my + 7)
im, d, P = canvas(box)
d.ellipse([P(mx - 5.5, my - 5.5), P(mx + 5.5, my + 5.5)], fill=(244, 192, 74, 255), outline=OUTL, width=int(1.4 * F))
save("StationMark", im, box)

# Ayar dugmesi (cizgili, doner)
box = (KC[0] - 17, KC[1] - 17, KC[0] + 17, KC[1] + 17)
im, d, P = canvas(box)
d.line([P(KC[0], KC[1]), P(KC[0], KC[1] - 12)], fill=OUTL, width=int(3 * F))
save("TuningKnob", im, box)

# Bakir tel ve lehim
box = (652, 392, 728, 440)
im, d, P = canvas(box)
pts = [P(660 + t * 56, 416 + 16 * np.sin(np.pi * t) - 12 * t) for t in np.linspace(0, 1, 30)]
d.line(pts, fill=OUTL, width=int(5.5 * F), joint="curve")
d.line(pts, fill=(214, 124, 62, 255), width=int(3 * F), joint="curve")
save("CopperWire", im, box)
box = (706, 394, 726, 414)
im, d, P = canvas(box)
d.ellipse([P(708, 396), P(724, 412)], fill=OUTL)
d.ellipse([P(709.6, 397.6), P(722.4, 410.4)], fill=(206, 210, 214, 255))
d.ellipse([P(712, 399.5), P(716, 403.5)], fill=(250, 250, 250, 255))
save("Solder", im, box)

# Secili yuva cercevesi ve satir arka planlari / adim daireleri
def rr(name, w, h, radius, layers):
    im = Image.new("RGBA", (w * F, h * F), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for inset, color, width in layers:
        d.rounded_rectangle([inset * F, inset * F, (w - inset) * F - 1, (h - inset) * F - 1], radius=(radius - inset) * F,
                            fill=None if width else color, outline=color if width else None, width=int(width * F))
    im.resize((w * K, h * K), Image.LANCZOS).save(f"{out}/{name}.png")


rr("SlotSelected", 110, 106, 16, [(1, (244, 192, 74, 255), 5)])
rr("RowDone", 244, 50, 12, [(0, (231, 243, 231, 255), 0)])
rr("RowCurrent", 244, 50, 12, [(0, (240, 196, 92, 255), 0), (1.6, (255, 241, 200, 255), 0)])


def circle(name, fill, check=False):
    s = 28
    im = Image.new("RGBA", (s * F, s * F), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.ellipse([0, 0, s * F - 1, s * F - 1], fill=fill)
    if check:
        d.line([(8 * F, 14.5 * F), (12.5 * F, 19 * F), (20 * F, 9.5 * F)], fill=(255, 255, 255, 255), width=int(3 * F), joint="curve")
    im.resize((s * K, s * K), Image.LANCZOS).save(f"{out}/{name}.png")


circle("StepDone", (46, 135, 85, 255), True)
circle("StepCurrent", (242, 193, 78, 255))
circle("StepPending", (238, 226, 204, 255))

O.save_rects("wb_rects.json")
print(json.dumps(O.rects))
