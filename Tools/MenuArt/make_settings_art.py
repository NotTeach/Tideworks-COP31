"""Settings sayfasi gorsellerini tasarim gorselinden (2752x1536) uretir.
Kullanim: python make_settings_art.py <tasarim.jpg> <cikis_klasoru>
Etiketler ve basliklar arka planda kalir; kaydirici, secim kutusu, anahtar ve yuzde yazilari
Unity'de cizilir. Bu yuzden onlarin altindaki tasarim pikselleri temizlenir."""
import sys, json
from PIL import Image, ImageDraw
import numpy as np
from scipy import ndimage as nd

src, out = sys.argv[1], sys.argv[2]
img = Image.open(src).convert("RGB")
A = np.asarray(img).astype(np.float64)
H, W = A.shape[:2]
rng = np.random.default_rng(3)

def erase(x0, y0, x1, y1, pad=3):
    """Kutuyu her satirda soldaki/sagdaki kagittan dogrusal doldurur (+ hafif doku)."""
    for y in range(y0, y1):
        left = A[y, x0 - pad - 4:x0 - pad].mean(0)
        right = A[y, x1 + pad:x1 + pad + 4].mean(0)
        t = np.linspace(0, 1, x1 - x0)[:, None]
        A[y, x0:x1] = left * (1 - t) + right * t + rng.normal(0, 2.0, (x1 - x0, 3))
    # kenar gecislerini yumusat
    sub = A[y0 - 2:y1 + 2, x0 - 2:x1 + 2]
    A[y0 - 2:y1 + 2, x0 - 2:x1 + 2] = nd.uniform_filter(sub, size=(3, 3, 1))

# Kaydirici tutamaclari (ust/alt tasan kisimlar) ve yuzde yazilari
for cy in (473, 603, 732):
    erase(640, cy - 30, 1150, cy + 30)          # tum kaydirici satiri (yeni kaydirici ustune cizilir)
    erase(1196, cy - 26, 1300, cy + 26)         # "70%"
bg = Image.fromarray(np.clip(A, 0, 255).astype(np.uint8))
bg.save(f"{out}/SettingsBackground.png", optimize=True)

# --- Kesilmis butonlar (tasarimdaki yerlerine oturur, uzerine gelince buyur) ---
O = np.asarray(img).astype(np.int32)
rects = {}
def cutout(name, box, keep):
    x0, y0, x1, y1 = box
    sub = O[y0:y1, x0:x1]
    m = keep(sub)
    lab, n = nd.label(m); sizes = nd.sum(m, lab, range(1, n + 1))
    m = nd.binary_fill_holes(lab == (np.argmax(sizes) + 1))
    m = nd.binary_dilation(m, iterations=2)
    alpha = np.clip(nd.gaussian_filter(m.astype(float), 1.0) * 1.5 - 0.25, 0, 1) * 255
    ys, xs = np.where(alpha > 0)
    cx0, cy0, cx1, cy1 = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    Image.fromarray(np.dstack([sub, alpha])[cy0:cy1, cx0:cx1].astype(np.uint8), "RGBA").save(f"{out}/{name}.png")
    rects[name] = [int(x0 + cx0), int(y0 + cy0), int(cx1 - cx0), int(cy1 - cy0)]

BGC = np.array([18, 58, 68])
cutout("BtnBack", (40, 28, 215, 205), lambda s: np.sqrt(((s - BGC) ** 2).sum(-1)) > 30)
cutout("BtnSaveBack", (2085, 1375, 2560, 1532), lambda s: (s[..., 0] > s[..., 1] + 45) & (s[..., 0] > 90))
# Reset: kesikli cerceve kagidin ustunde; dikdortgen kesit yeterli
x0, y0, x1, y1 = 292, 1120, 708, 1236
Image.fromarray(O[y0:y1, x0:x1].astype(np.uint8)).save(f"{out}/BtnReset.png")
rects["BtnReset"] = [x0, y0, x1 - x0, y1 - y0]

# --- Kontrol parcalari (keskin olsun diye cizilir; 4x cizip kucultme) ---
INK = (76, 42, 25, 255)
def draw(name, w, h, shapes, border=None):
    S = 4
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for kind, box, r, fill in shapes:
        bx = [v * S for v in box]
        if kind == "rr": d.rounded_rectangle([bx[0], bx[1], bx[2] - 1, bx[3] - 1], radius=r * S, fill=fill)
        else: d.ellipse([bx[0], bx[1], bx[2] - 1, bx[3] - 1], fill=fill)
    im.resize((w, h), Image.LANCZOS).save(f"{out}/{name}.png")
    if border: rects[name + "_border"] = border

# Secim kutusu cercevesi (9-slice) ve secili hap
draw("SegFrame", 120, 120, [("rr", (0, 0, 120, 120), 30, INK), ("rr", (6, 6, 114, 114), 25, (243, 228, 199, 255))], [40, 40, 40, 40])
draw("SegPill", 90, 90, [("rr", (0, 0, 90, 90), 20, (18, 61, 68, 255))], [26, 26, 26, 26])
# Kaydirici: bos ray, dolu ray, tutamac
draw("SliderTrack", 60, 26, [("rr", (0, 0, 60, 26), 13, INK), ("rr", (4, 4, 56, 22), 9, (242, 226, 204, 255))], [16, 0, 16, 0])
draw("SliderFill", 60, 26, [("rr", (0, 0, 60, 26), 13, INK), ("rr", (4, 4, 56, 22), 9, (178, 95, 55, 255)),
                             ("rr", (6, 6, 54, 12), 4, (196, 118, 74, 255))], [16, 0, 16, 0])
draw("SliderKnob", 36, 54, [("rr", (0, 0, 36, 54), 12, (68, 35, 20, 255)), ("rr", (4, 4, 32, 50), 9, (150, 98, 66, 255)),
                             ("rr", (8, 8, 28, 22), 5, (176, 126, 88, 255))])
# Anahtar: acik/kapali ray ve beyaz top
draw("SwitchOn", 140, 82, [("rr", (0, 0, 140, 82), 41, (82, 52, 36, 255)), ("rr", (6, 6, 134, 76), 35, (74, 185, 179, 255))])
draw("SwitchOff", 140, 82, [("rr", (0, 0, 140, 82), 41, (82, 52, 36, 255)), ("rr", (6, 6, 134, 76), 35, (214, 192, 155, 255))])
draw("SwitchKnob", 62, 62, [("el", (0, 0, 62, 62), 0, (68, 40, 26, 255)), ("el", (5, 5, 57, 57), 0, (250, 247, 238, 255))])

json.dump(rects, open(f"{out}/rects.json", "w"), indent=1)
print(json.dumps(rects))
