"""Tamir yardim karti gorselleri (2x artboard pikseli). Kullanim: python help_art.py <Assets/Sprites/Workbench>"""
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as nd

out = sys.argv[1]
S = 4
INK = (52, 38, 30, 255)


def rounded(name, w, h, r, layers, shadow=0):
    pad = shadow * 2
    im = Image.new("RGBA", ((w + pad * 2) * S, (h + pad * 2) * S), (0, 0, 0, 0))
    if shadow:
        sh = Image.new("RGBA", im.size, (0, 0, 0, 0))
        ImageDraw.Draw(sh).rounded_rectangle([pad * S, (pad + shadow) * S, (pad + w) * S, (pad + h + shadow) * S], radius=r * S, fill=(0, 0, 0, 110))
        im = Image.alpha_composite(im, sh.filter(ImageFilter.GaussianBlur(shadow * S * 0.6)))
    d = ImageDraw.Draw(im)
    for (x0, y0, x1, y1, color) in layers:
        d.rounded_rectangle([(pad + x0) * S, (pad + y0) * S, (pad + w - x1) * S - 1, (pad + h - y1) * S - 1],
                            radius=max(1, r - x0) * S, fill=color)
    im.resize((im.width // S, im.height // S), Image.LANCZOS).save(f"{out}/{name}.png")


rounded("HelpCard", 1240, 700, 44, [(0, 0, 0, 0, INK), (7, 7, 7, 7, (251, 248, 241, 255))], shadow=18)
rounded("HelpButtonOrange", 400, 112, 30, [(0, 0, 0, 0, INK), (4, 4, 4, 4, (122, 50, 24, 255)), (4, 4, 4, 14, (190, 82, 44, 255))])
rounded("HelpButtonTeal", 400, 112, 30, [(0, 0, 0, 0, INK), (4, 4, 4, 4, (12, 72, 78, 255)), (4, 4, 4, 14, (26, 124, 128, 255))])
rounded("HelpButtonOutline", 400, 112, 30, [(0, 0, 0, 0, INK), (4, 4, 4, 4, (251, 248, 241, 255))])
rounded("HelpIconFrame", 240, 232, 30, [(0, 0, 0, 0, (240, 196, 92, 255)), (5, 5, 5, 5, (255, 243, 210, 255))])

# "?" butonu: teal daire + krem halka
s = 76 * S
im = Image.new("RGBA", (s, s), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([0, 0, s - 1, s - 1], fill=INK)
d.ellipse([4 * S, 4 * S, s - 1 - 4 * S, s - 1 - 4 * S], fill=(26, 124, 128, 255))
im.resize((76, 76), Image.LANCZOS).save(f"{out}/HelpButton.png")

# Ipucu halkasi: yumusak sari halka (hedefin etrafinda yanip soner)
n = 256
yy, xx = np.mgrid[0:n, 0:n] - n / 2 + 0.5
r = np.sqrt(xx ** 2 + yy ** 2) / (n / 2)
ring = np.exp(-((r - 0.82) / 0.07) ** 2) + 0.35 * np.clip(1 - r, 0, 1) ** 2
a = np.clip(ring, 0, 1) * 255
Image.fromarray(np.dstack([np.full((n, n), 255), np.full((n, n), 214), np.full((n, n), 90), a]).astype(np.uint8), "RGBA").save(f"{out}/HintRing.png")
print("ok")
