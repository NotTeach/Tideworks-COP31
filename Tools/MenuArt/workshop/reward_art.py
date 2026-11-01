"""Tamir sonrasi: odul karti, "Let's Save the Seas!" paneli, altin, nota ve kalp.
Kullanim: python reward_art.py <Assets/Sprites/Workshop>  (2x artboard pikseli)"""
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

out = sys.argv[1]
S = 4
INK = (52, 38, 30, 255)
CREAM = (251, 248, 241, 255)
TEAL = (26, 124, 128, 255)


def card(name, w, h, r, header=0, shadow=18):
    pad = shadow * 2
    im = Image.new("RGBA", ((w + pad * 2) * S, (h + pad * 2) * S), (0, 0, 0, 0))
    sh = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle([pad * S, (pad + shadow) * S, (pad + w) * S, (pad + h + shadow) * S], radius=r * S, fill=(0, 0, 0, 120))
    im = Image.alpha_composite(im, sh.filter(ImageFilter.GaussianBlur(shadow * S * 0.6)))
    d = ImageDraw.Draw(im)
    box = lambda i: [(pad + i) * S, (pad + i) * S, (pad + w - i) * S - 1, (pad + h - i) * S - 1]
    d.rounded_rectangle(box(0), radius=r * S, fill=INK)
    d.rounded_rectangle(box(7), radius=(r - 7) * S, fill=CREAM)
    if header:  # ust serit (deniz rengi) + dalga cizgisi
        top = Image.new("RGBA", im.size, (0, 0, 0, 0)); td = ImageDraw.Draw(top)
        td.rounded_rectangle(box(7), radius=(r - 7) * S, fill=(214, 236, 234, 255))
        mask = Image.new("L", im.size, 0); md = ImageDraw.Draw(mask)
        xs = np.linspace(pad + 7, pad + w - 7, 200)
        pts = [((pad + 7) * S, (pad + 7) * S)] + [(x * S, (pad + header + 10 * np.sin(x / 38)) * S) for x in xs] + [((pad + w - 7) * S, (pad + 7) * S)]
        md.polygon(pts, fill=255)
        im.paste(top, (0, 0), Image.composite(mask, Image.new("L", im.size, 0), top.split()[3]))
        d = ImageDraw.Draw(im)
        d.line([(x * S, (pad + header + 10 * np.sin(x / 38)) * S) for x in xs], fill=TEAL, width=5 * S)
    im.resize((im.width // S, im.height // S), Image.LANCZOS).save(f"{out}/{name}.png")


card("RewardCard", 1000, 560, 44, header=150)
card("MissionCard", 1400, 980, 48, header=170)

# Altin
s = 96 * S
im = Image.new("RGBA", (s, s), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([0, 0, s - 1, s - 1], fill=INK)
d.ellipse([5 * S, 5 * S, s - 1 - 5 * S, s - 1 - 5 * S], fill=(232, 168, 48, 255))
d.ellipse([16 * S, 16 * S, s - 1 - 16 * S, s - 1 - 16 * S], fill=(247, 205, 86, 255))
d.arc([22 * S, 22 * S, s - 1 - 22 * S, s - 1 - 22 * S], 200, 290, fill=(255, 240, 180, 255), width=5 * S)
im.resize((96, 96), Image.LANCZOS).save(f"{out}/Coin.png")

# Nota (sekizlik) ve kalp
s = 80 * S
im = Image.new("RGBA", (s, s), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
col = (244, 192, 74, 255)
d.ellipse([10 * S, 50 * S, 36 * S, 72 * S], fill=INK); d.ellipse([13 * S, 53 * S, 33 * S, 69 * S], fill=col)
d.rectangle([29 * S, 10 * S, 36 * S, 62 * S], fill=INK)
d.polygon([(32 * S, 8 * S), (64 * S, 20 * S), (64 * S, 34 * S), (36 * S, 24 * S)], fill=INK)
im.resize((80, 80), Image.LANCZOS).save(f"{out}/Note.png")

s = 80 * S
im = Image.new("RGBA", (s, s), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
yy, xx = np.mgrid[0:s, 0:s] / S
x, y = (xx - 40) / 30, (52 - yy) / 30
heart = (x ** 2 + y ** 2 - 1) ** 3 - x ** 2 * y ** 3 <= 0
a = np.asarray(Image.fromarray((heart * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(4 * S + 1)))
rgba = np.zeros((s, s, 4), np.uint8)
rgba[a > 0] = INK
rgba[heart] = (232, 96, 110, 255)
Image.fromarray(rgba, "RGBA").resize((80, 80), Image.LANCZOS).save(f"{out}/Heart.png")
print("ok")
