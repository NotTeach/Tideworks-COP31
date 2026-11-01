import sys, json
from PIL import Image, ImageFilter, ImageDraw
import numpy as np
from scipy import ndimage as nd

src, out = sys.argv[1], sys.argv[2]
img = Image.open(src).convert("RGB")
A = np.asarray(img).astype(np.int32)
H, W = A.shape[:2]
PAPER = np.array([224, 195, 143])
rects = {}

def dist(a, c): return np.sqrt(((a - c) ** 2).sum(-1))

def save_rgba(name, rgb, alpha, box):
    x0, y0, x1, y1 = box
    rgba = np.dstack([rgb[y0:y1, x0:x1], alpha[y0:y1, x0:x1]]).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(f"{out}/{name}.png")
    rects[name] = [x0, y0, x1 - x0, y1 - y0]

def cutout(name, core_box, pad=16, thr=38, clip_top=None):
    """Butonu kagittan ayirir: cekirdekle bagli 'kagit olmayan' pikseller + golge."""
    x0, y0, x1, y1 = core_box
    bx = (x0 - pad, y0 - pad, x1 + pad, y1 + pad)
    m = np.zeros((H, W), bool)
    sub = dist(A[bx[1]:bx[3], bx[0]:bx[2]], PAPER) > thr
    m[bx[1]:bx[3], bx[0]:bx[2]] = sub
    core = np.zeros((H, W), bool); core[y0 + 10:y1 - 10, x0 + 10:x1 - 10] = True
    m |= core
    lab, _ = nd.label(m)
    ids = np.unique(lab[core]); ids = ids[ids > 0]
    m = np.isin(lab, ids)
    if clip_top is not None: m[:clip_top] = False
    m = nd.binary_fill_holes(m)
    m = nd.binary_opening(m, iterations=2)
    m = nd.binary_dilation(m, iterations=2)
    alpha = nd.gaussian_filter(m.astype(float), 1.2)
    alpha = (np.clip(alpha * 1.6 - 0.3, 0, 1) * 255)
    ys, xs = np.where(alpha > 0)
    box = (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)
    save_rgba(name, A, alpha, box)
    return m

# --- Arka plan (oldugu gibi) ---
img.save(f"{out}/MenuBackground.png", optimize=True)
rects["MenuBackground"] = [0, 0, W, H]

# --- Butonlar ---
cutout("BtnStart", (256, 747, 958, 882))
cutout("BtnHowToPlay", (266, 1041, 599, 1131))
cutout("BtnSettings", (632, 1029, 965, 1119))

# Continue: rozetteki "Day 2 of 3" yazisini sil
B = A.copy()
r, g, b = A[..., 0], A[..., 1], A[..., 2]
yel = np.zeros((H, W), bool); yel[915:985, 740:935] = True
yel &= (r > 200) & (g > 150) & (b < 120)
lab, n = nd.label(yel); sizes = nd.sum(yel, lab, range(1, n + 1)); yel = lab == (np.argmax(sizes) + 1)
badge = nd.binary_erosion(nd.binary_fill_holes(yel), iterations=3)
for y in np.unique(np.where(badge)[0]):
    row = yel[y] & badge[y]
    col = np.median(A[y][row], axis=0) if row.any() else np.median(A[badge], axis=0)
    B[y][badge[y]] = col
B = nd.uniform_filter(B.astype(float), size=(3, 3, 1)) * badge[..., None] + B * (~badge[..., None])
Ab = A; A = B.astype(np.int32)
cutout("BtnContinue", (260, 895, 962, 1013), clip_top=888)
A = Ab
ys, xs = np.where(badge); rects["ContinueBadge"] = [int(xs.min()), int(ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)]

# Bos krem buton (9-slice icin): Settings butonundaki yaziyi sil
C = A.copy()
cream = np.zeros((H, W), bool); cream[1025:1125, 628:970] = True
cream &= (r > 215) & (g > 195) & (b > 160)
lab, n = nd.label(cream); sizes = nd.sum(cream, lab, range(1, n + 1)); cream = lab == (np.argmax(sizes) + 1)
inner = nd.binary_erosion(nd.binary_fill_holes(cream), iterations=3)
for y in np.unique(np.where(inner)[0]):
    row = cream[y] & inner[y]
    C[y][inner[y]] = np.median(A[y][row], axis=0)
Ac = A; A = C
cutout("BtnCreamBlank", (632, 1029, 965, 1119))
A = Ac

# --- Ikonlar (Fix / Collect / Save) ---
def icon(name, box, thr=45):
    x0, y0, x1, y1 = box
    sub = A[y0:y1, x0:x1]
    m = dist(sub, PAPER) > thr
    lab, n = nd.label(m); sizes = nd.sum(m, lab, range(1, n + 1))
    keep = [i + 1 for i, s in enumerate(sizes) if s > 400]
    m = nd.binary_fill_holes(np.isin(lab, keep))
    m = nd.binary_dilation(m, iterations=1)
    full = np.zeros((H, W)); full[y0:y1, x0:x1] = np.clip(nd.gaussian_filter(m.astype(float), 0.8) * 1.5 - 0.2, 0, 1) * 255
    ys, xs = np.where(full > 0)
    save_rgba(name, A, full, (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
icon("IconFix", (300, 560, 410, 665))
icon("IconCollect", (545, 555, 645, 645))
icon("IconSave", (785, 555, 905, 645))

# --- Bos defter sayfasi ---
NB = (60, 55, 1100, 1451)            # defterin dikdortgen kesiti (arka plan dahil)
pm = dist(A, PAPER) < 32
pm[:NB[1]] = False; pm[NB[3]:] = False; pm[:, :NB[0]] = False; pm[:, NB[2]:] = False
lab, n = nd.label(pm); sizes = nd.sum(pm, lab, range(1, n + 1)); pm = lab == (np.argmax(sizes) + 1)
ys, xs = np.where(pm)
from scipy.spatial import ConvexHull
pts = np.c_[xs, ys][::50]
hull = ConvexHull(pts); poly = [tuple(pts[i]) for i in hull.vertices]
hm = Image.new("L", (W, H), 0); ImageDraw.Draw(hm).polygon(poly, fill=255)
hull_m = np.asarray(hm) > 0
page = nd.binary_erosion(hull_m, iterations=14)
P = A.copy().astype(float)
rowcol = {}
for y in np.unique(np.where(page)[0]):
    sel = page[y] & pm[y]
    rowcol[y] = np.median(A[y][sel], axis=0) if sel.sum() > 20 else None
last = PAPER
for y in sorted(rowcol):
    if rowcol[y] is None: rowcol[y] = last
    last = rowcol[y]
fill = np.zeros_like(P)
for y, c in rowcol.items(): fill[y][page[y]] = c
# kenarlara dogru hafif eskime (kenar golgesi)
d = nd.distance_transform_edt(page)
shade = np.clip(1 - np.exp(-d / 40.0), 0, 1) * 0.06 + 0.94
fill *= shade[..., None]
rng = np.random.default_rng(7)
fill += rng.normal(0, 1.6, fill.shape) * page[..., None]
soft = nd.gaussian_filter(page.astype(float), 2.0)[..., None]
P = P * (1 - soft) + fill * soft
# kenar bandinda kalan cikartma artiklarini (beyaz kontur) kagit rengine boya
band = hull_m & ~page
bright = band & (A.sum(-1) > 690)
bright = nd.binary_dilation(bright, iterations=3) & band
for y in np.unique(np.where(bright)[0]):
    P[y][bright[y]] = (rowcol.get(y) if rowcol.get(y) is not None else PAPER) * 0.97
# silik cizgiler
P2 = Image.fromarray(np.clip(P, 0, 255).astype(np.uint8))
dr = ImageDraw.Draw(P2, "RGBA")
ys_p, xs_p = np.where(page)
top, bot = ys_p.min(), ys_p.max()
for y in range(top + 150, bot - 40, 88):
    row = np.where(page[y])[0]
    if len(row): dr.line([(row.min() + 30, y), (row.max() - 30, y)], fill=(150, 120, 80, 40), width=2)
Pn = np.asarray(P2)
Image.fromarray(Pn[NB[1]:NB[3], NB[0]:NB[2]]).save(f"{out}/NotebookPage.png", optimize=True)
rects["NotebookPage"] = [NB[0], NB[1], NB[2] - NB[0], NB[3] - NB[1]]
rects["PageArea"] = [int(xs_p.min()), int(ys_p.min()), int(xs_p.max() - xs_p.min()), int(ys_p.max() - ys_p.min())]

# --- Ay isiltisi + yildiz parlamasi ---
s = 512; yy, xx = np.mgrid[0:s, 0:s]; rr = np.hypot(xx - s / 2, yy - s / 2) / (s / 2)
ga = np.clip(1 - rr, 0, 1) ** 2.2 * 255
Image.fromarray(np.dstack([np.full((s, s), 255), np.full((s, s), 248), np.full((s, s), 214), ga]).astype(np.uint8), "RGBA").save(f"{out}/Glow.png")
s = 128; yy, xx = np.mgrid[0:s, 0:s] - s / 2 + 0.5
sp = np.clip(1 - (np.abs(xx) * np.abs(yy)) ** 0.5 / 7, 0, 1) * np.clip(1 - np.hypot(xx, yy) / (s / 2), 0, 1)
sp = np.maximum(sp, np.clip(1 - np.hypot(xx, yy) / 10, 0, 1))
Image.fromarray(np.dstack([np.full((s, s), 255)] * 3 + [sp * 255]).astype(np.uint8), "RGBA").save(f"{out}/Sparkle.png")

json.dump({k: [int(v) for v in vals] for k, vals in rects.items()}, open(f"{out}/rects.json", "w"), indent=1)
print(json.dumps({k: [int(v) for v in vals] for k, vals in rects.items()}))

# --- Kenar dolgusu: ekran orani farkliyken menunun arkasinda gorunen bulanik, karartilmis kopya ---
from PIL import ImageEnhance
bd = img.resize((688, 384), Image.LANCZOS).filter(ImageFilter.GaussianBlur(28)).filter(ImageFilter.GaussianBlur(20))
ImageEnhance.Brightness(bd).enhance(0.55).save(f"{out}/MenuBackdrop.png")
