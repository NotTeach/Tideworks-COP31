"""3. gun kumsali, final: "Buyuk Gece".
Kullanim: python day3_beach_art.py <Assets/Sprites> <25.png (Elif, onarilmis Pamuk)> <26.png (Mert, onarilmis pusula)>
Uretir (Sprites/Day3Beach): ElifPortrait, MertPortrait (240, Hatice'ninkiyle ayni stil), CarettaUmut (kabugunda 2. gundeki ag izi).
Ayrica Day2Beach/Caretta.png'ye ayni iz eklenir: Umut 2. gunde de bu isaretle gorunur."""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SP, elif_src, mert_src = sys.argv[1:4]
OUT = f"{SP}/Day3Beach"
os.makedirs(OUT, exist_ok=True)
S = 4
INK = (60, 44, 40, 255)
CREAM = (250, 244, 228, 255)


def canvas(w, h):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (x * S, y * S))


def save(im, name, w, h):
    im.resize((w, h), Image.LANCZOS).save(f"{OUT}/{name}.png")


# ---------------- Portreler (1280x720 tasarimdan yuz, daire icinde) ----------------
def portrait(src, cx, cy, r, name):
    big = Image.open(src).convert("RGB").resize((2560, 1440), Image.LANCZOS)
    face = big.crop((int((cx - r) * 2), int((cy - r) * 2), int((cx + r) * 2), int((cy + r) * 2)))
    face = face.resize((220, 220), Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=2, percent=90, threshold=2))
    out = Image.new("RGBA", (240, 240), (0, 0, 0, 0))
    d = ImageDraw.Draw(out)
    d.ellipse([0, 0, 239, 239], fill=CREAM)
    m = Image.new("L", (880, 880), 0)
    ImageDraw.Draw(m).ellipse([0, 0, 879, 879], fill=255)
    face = face.convert("RGBA")
    face.putalpha(m.resize((220, 220), Image.LANCZOS))
    out.alpha_composite(face, (10, 10))
    ImageDraw.Draw(out).ellipse([8, 8, 231, 231], outline=INK, width=6)
    out.save(f"{OUT}/{name}.png")


portrait(elif_src, 723, 336, 44, "ElifPortrait")
portrait(mert_src, 730, 338, 44, "MertPortrait")

# ---------------- Umut: kabugunda ag izi olan caretta ----------------
def with_mark(src):
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    big = im.resize((w * S, h * S), Image.LANCZOS)
    d = ImageDraw.Draw(big)
    sx, sy = w / 680, h / 640
    P = lambda x, y: (x * sx * S, y * sy * S)
    # Agin biraktigi iz: kabugun sag arka kenarinda acik renkli centik ve uc cizik
    d.polygon([P(486, 438), P(512, 452), P(494, 470)], fill=(246, 226, 196, 255), outline=INK)
    for k in range(3):
        d.line([P(420 + k * 16, 400 + k * 10), P(462 + k * 16, 424 + k * 10)], fill=(246, 214, 170, 255), width=max(2, int(4 * sx)) * S)
    return big.resize((w, h), Image.LANCZOS)


marked = with_mark(f"{SP}/Day2Beach/Caretta.png")
marked.save(f"{OUT}/CarettaUmut.png")
marked.save(f"{SP}/Day2Beach/Caretta.png")
print("ok")
