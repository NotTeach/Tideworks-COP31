import json
import numpy as np, cv2
from PIL import Image
from scipy import ndimage as nd

def load(p): return np.asarray(Image.open(p).convert("RGB")).astype(np.int32)

def row_fill(img, mask):
    """Maskeli pikselleri ayni satirdaki maskesiz komsulardan doldur (yatay bantli zeminler: deniz)."""
    out = img.copy()
    xs = np.arange(img.shape[1])
    for y in np.where(mask.any(1))[0]:
        m = mask[y]
        if m.all(): continue
        for c in range(3): out[y, m, c] = np.interp(xs[m], xs[~m], img[y, ~m, c])
    return out

def col_fill(img, mask):
    return row_fill(img.transpose(1, 0, 2), mask.T).transpose(1, 0, 2)

def inpaint(img, mask, r=5):
    return cv2.inpaint(img.astype(np.uint8), mask.astype(np.uint8) * 255, r, cv2.INPAINT_TELEA).astype(np.int32)

def patch(img, src_box, dst_xy):
    x0, y0, x1, y1 = src_box; dx, dy = dst_xy
    img[dy:dy + y1 - y0, dx:dx + x1 - x0] = img[y0:y1, x0:x1].copy()

def soft(mask, sigma=0.7):
    return np.clip(nd.gaussian_filter(mask.astype(float), sigma) * 1.5 - 0.25, 0, 1)

class Out:
    def __init__(s, folder, k): s.folder, s.k, s.rects = folder, k, {}
    def full(s, name, img, size=None):
        im = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))
        im = im.resize(size or (im.width * s.k, im.height * s.k), Image.LANCZOS)
        im.save(f"{s.folder}/{name}.png", optimize=True)
    def cut(s, name, img, alpha, pad=2, bg=None):
        """alpha (0..1) olan bolgeyi kesip kaydet; rect = buyutulmus olcekte [x,y,w,h]."""
        ys, xs = np.where(alpha > 0.01)
        x0, y0 = max(xs.min() - pad, 0), max(ys.min() - pad, 0)
        x1, y1 = min(xs.max() + 1 + pad, alpha.shape[1]), min(ys.max() + 1 + pad, alpha.shape[0])
        rgb = img[y0:y1, x0:x1].astype(float)
        a = alpha[y0:y1, x0:x1]
        if bg is not None:  # yari saydam kenarlari arka plandan ayir (unmix)
            b = bg[y0:y1, x0:x1].astype(float); aa = np.maximum(a, 0.05)[..., None]
            rgb = np.where(a[..., None] < 0.999, (rgb - (1 - aa) * b) / aa, rgb)
        rgba = np.dstack([np.clip(rgb, 0, 255), a * 255]).astype(np.uint8)
        im = Image.fromarray(rgba, "RGBA")
        # Buyutmede koyu sacak olmasin: once premultiply
        pm = np.asarray(im).astype(float); pm[..., :3] *= pm[..., 3:] / 255
        pm = Image.fromarray(pm.astype(np.uint8), "RGBA").resize((im.width * s.k, im.height * s.k), Image.LANCZOS)
        p = np.asarray(pm).astype(float); al = p[..., 3:]
        p[..., :3] = np.where(al > 0, p[..., :3] * 255 / np.maximum(al, 1), 0)
        Image.fromarray(np.clip(p, 0, 255).astype(np.uint8), "RGBA").save(f"{s.folder}/{name}.png", optimize=True)
        s.rects[name] = [int(x0 * s.k), int(y0 * s.k), int((x1 - x0) * s.k), int((y1 - y0) * s.k)]
    def save_rects(s, name):
        json.dump(s.rects, open(f"{s.folder}/{name}", "w"), indent=1)
