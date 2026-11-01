"""Gomulu yazilari siler (Turkce gorunum icin temiz sprite) ve yerlerini/olculerini cikarir.
Kullanim: python erase_labels.py <Assets/Sprites> <ocr.json> <fonts> <ArtLabels.json cikisi>
Temiz sprite'lar: Assets/Sprites/<Klasor>/TR/<ad>.png"""
import sys, os, json
import numpy as np, cv2
from PIL import Image, ImageFont
from scipy import ndimage as nd
sys.path.insert(0, os.path.dirname(__file__))
from labels_spec import SPEC

sprites, ocr_path, fonts, out_json = sys.argv[1:5]
OCR = json.load(open(ocr_path, encoding="utf-8"))
FONT = {"N": "tr-Nunito-ExtraBold.ttf", "n": "tr-Nunito-Bold.ttf", "C": "tr-Caveat-Bold.ttf", "B": "tr-Baloo2-ExtraBold.ttf"}


def find_box(name, key):
    if not isinstance(key, str): return key
    text, k = (key.split("#") + ["0"])[:2]
    hits = [it for it in OCR.get(name, []) if it[4].strip() == text]
    if not hits: raise SystemExit(f"OCR'da yok: {name} / {key}")
    return hits[int(k)][:4]


def text_width(font, size, s):
    b = ImageFont.truetype(f"{fonts}/{FONT[font]}", size).getbbox(s)
    return b[2] - b[0], b[3] - b[1]


result = []
for name, labels in SPEC.items():
    folder, base = name.split("/")
    src = f"{sprites}/{folder}/{base}.png"
    im = Image.open(src).convert("RGBA")
    rgba = np.asarray(im).astype(np.int32)
    rgb, alpha = rgba[..., :3].copy(), rgba[..., 3]
    H, W = alpha.shape
    erase = np.zeros((H, W), bool)
    entries = []
    for key, en, font, align in labels:
        x0, y0, x1, y1 = find_box(name, key)
        P = 4
        bx0, by0, bx1, by1 = max(0, x0 - P), max(0, y0 - P), min(W, x1 + P), min(H, y1 + P)
        ring = np.zeros((H, W), bool)
        ring[max(0, by0 - 5):min(H, by1 + 5), max(0, bx0 - 5):min(W, bx1 + 5)] = True
        ring[by0:by1, bx0:bx1] = False
        ring &= alpha > 200
        bg = np.median(rgb[ring], axis=0) if ring.any() else np.median(rgb[by0:by1, bx0:bx1].reshape(-1, 3), axis=0)
        diff = np.abs(rgb[by0:by1, bx0:bx1] - bg).max(-1)
        m = (diff > 45) & (alpha[by0:by1, bx0:bx1] > 100)
        if not m.any(): raise SystemExit(f"yazi bulunamadi: {name} / {en}")
        ys, xs = np.where(m)
        tx0, ty0, tx1, ty1 = xs.min() + bx0, ys.min() + by0, xs.max() + 1 + bx0, ys.max() + 1 + by0
        strong = diff > diff.max() * 0.7
        ink = rgb[by0:by1, bx0:bx1][strong].mean(0)
        full = np.zeros((H, W), bool); full[by0:by1, bx0:bx1] = m
        erase |= nd.binary_dilation(full, iterations=3)

        tw, th = tx1 - tx0, ty1 - ty0
        w100, h100 = text_width(font, 100, en)
        size = 100 * tw / max(w100, 1)
        if len(en) <= 4: size = 0.5 * size + 0.5 * 100 * th / max(h100, 1)
        size = int(round(size))
        rect_w = max(tw * 1.6, tw + size * 2)
        rect_h = size * 1.45
        cy = (ty0 + ty1) / 2 + size * 0.04
        if align == "c": rx = (tx0 + tx1) / 2 - rect_w / 2
        elif align == "r": rx = tx1 - rect_w
        else: rx = tx0
        if W < 1200:  # buton gibi kucuk sprite: kenarlardan tasmasin
            rx = max(rx, 6); rect_w = min(rect_w, W - 6 - rx)
            if align == "c": rx = max(6, (tx0 + tx1) / 2 - rect_w / 2); rect_w = min(rect_w, W - 6 - rx)
        entries.append(dict(en=en, font=font, align=align, size=size,
                            x=round(float(rx), 1), y=round(float(cy - rect_h / 2), 1), w=round(float(rect_w), 1), h=round(float(rect_h), 1),
                            r=int(ink[0]), g=int(ink[1]), b=int(ink[2])))
    clean = cv2.inpaint(rgb.astype(np.uint8), erase.astype(np.uint8) * 255, 5, cv2.INPAINT_TELEA)
    os.makedirs(f"{sprites}/{folder}/TR", exist_ok=True)
    Image.fromarray(np.dstack([clean, alpha]).astype(np.uint8), "RGBA").save(f"{sprites}/{folder}/TR/{base}.png", optimize=True)
    result.append(dict(sprite=name, width=W, height=H, labels=entries))
    print(name, [(e["en"], e["size"]) for e in entries])

os.makedirs(os.path.dirname(out_json), exist_ok=True)
json.dump(dict(sprites=result), open(out_json, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
