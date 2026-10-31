"""Intro (yukleme) ekrani gorsellerini tasarim gorselinden uretir.
Kullanim: python make_intro_art.py <tasarim.png> <cikis_klasoru>
Tum koordinatlar 1280x720 tasarim gorselindedir; cikti 2x (2560x1440) olcektir."""
import sys, json
from PIL import Image, ImageDraw
import numpy as np
from scipy import ndimage as nd

src, out = sys.argv[1], sys.argv[2]
K = 2
A = np.asarray(Image.open(src).convert("RGB")).astype(np.int32)
H, W = A.shape[:2]
BG = np.array([14, 61, 72])

# --- Defterdeki el yazisini sil (yatay cizgiler korunur: her satirda soldan/sagdan interpolasyon) ---
B = A.copy()
x0, y0, x1, y1 = 178, 130, 566, 442
reg = B[y0:y1, x0:x1]
mask = nd.binary_dilation(reg.sum(-1) < 470, iterations=2)
for y in range(reg.shape[0]):
    m = mask[y]
    if not m.any() or m.all(): continue
    xs = np.arange(len(m))
    for c in range(3):
        reg[y, m, c] = np.interp(xs[m], xs[~m], reg[y, ~m, c])

# --- Sagdaki yazilar, kartlar, butonlar ve ikonlar: duz arka plan rengine boya ---
boxes = [(790, 70, 1135, 140), (735, 148, 1183, 212), (712, 238, 1208, 328),
         (700, 378, 1227, 536), (700, 552, 1214, 668)]
for bx0, by0, bx1, by1 in boxes:
    B[by0:by1, bx0:bx1] = BG

Image.fromarray(B.astype(np.uint8)).resize((W * K, H * K), Image.LANCZOS).save(f"{out}/IntroBackground.png", optimize=True)

# --- Ikonlar (kartlarin / butonlarin ustune tasanlar) ---
rects = {}
def icon(name, box, extra=None):
    bx0, by0, bx1, by1 = box
    sub = A[by0:by1, bx0:bx1]
    r, g, b = sub[..., 0], sub[..., 1], sub[..., 2]
    teal = (g > r + 40) & (b > r + 40)
    m = ~teal
    if extra is not None: m &= ~extra(sub, np.mgrid[by0:by1, bx0:bx1][0])
    lab, n = nd.label(m); sizes = nd.sum(m, lab, range(1, n + 1))
    m = nd.binary_fill_holes(lab == (np.argmax(sizes) + 1))
    alpha = np.clip(nd.gaussian_filter(m.astype(float), 0.6) * 1.4 - 0.2, 0, 1) * 255
    ys, xs = np.where(alpha > 0)
    cx0, cy0, cx1, cy1 = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    rgba = np.dstack([sub, alpha])[cy0:cy1, cx0:cx1].astype(np.uint8)
    im = Image.fromarray(rgba, "RGBA")
    im.resize((im.width * K, im.height * K), Image.LANCZOS).save(f"{out}/{name}.png")
    rects[name] = [int(bx0 + cx0), int(by0 + cy0), int(cx1 - cx0), int(cy1 - cy0)]

ORANGE = np.array([179, 74, 38]); LIP = np.array([122, 50, 24])
near = lambda s, c, t: np.sqrt(((s - c) ** 2).sum(-1)) < t
icon("IconHourglass", (898, 378, 964, 450))
icon("IconNest", (1146, 386, 1229, 437))
icon("IconKey", (726, 560, 792, 602), lambda s, ys: near(s, ORANGE, 28) & (ys > 583) | near(s, LIP, 20))
icon("IconBook", (1066, 551, 1117, 602), lambda s, ys: (s.min(-1) > 215) & (ys > 584))

# --- Kart ve butonlar: keskin olsun diye yeniden ciziliyor (4x cizip kucultme = yumusak kenar) ---
def rounded(name, w, h, radius, layers):
    S = 4
    im = Image.new("RGBA", (w * K * S, h * K * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for (lx0, ly0, lx1, ly1, color) in layers:
        d.rounded_rectangle([lx0 * K * S, ly0 * K * S, (w - lx1) * K * S - 1, (h - ly1) * K * S - 1],
                            radius=radius * K * S, fill=color)
    im.resize((w * K, h * K), Image.LANCZOS).save(f"{out}/{name}.png")

# Kart 241x122: alt golge + kenarlik + dolgu
rounded("Card", 241, 122, 16, [(0, 6, 0, 0, (10, 44, 52, 255)), (0, 0, 0, 6, (46, 154, 154, 255)),
                                (3, 3, 3, 9, (23, 119, 122, 255))])
# Turuncu buton 241x75: alt dudak + govde
rounded("ButtonOrange", 241, 75, 16, [(0, 7, 0, 0, (122, 50, 24, 255)), (0, 0, 0, 7, (179, 74, 38, 255))])
# Cerceveli buton 241x68: beyaz kenarlik + koyu dolgu
rounded("ButtonOutline", 241, 68, 16, [(0, 0, 0, 0, (251, 248, 241, 255)), (3, 3, 3, 3, (14, 61, 72, 255))])

json.dump(rects, open(f"{out}/rects.json", "w"), indent=1)
print(json.dumps(rects))
