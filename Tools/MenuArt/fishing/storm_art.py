"""2. gun firtina denizi icin yumusak katmanlar: bulut golgesi ve gunes parlamasi.
Kullanim: python storm_art.py <Assets/Sprites/Fishing>
Ikisi de beyaz/sicak tonlu; renk ve saydamligi oyun icinde Image.color verir."""
import sys
import os
import random
from PIL import Image, ImageDraw, ImageFilter

out = sys.argv[1]


def cloud():
    w, h = 900, 460
    img = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(img)
    rng = random.Random(7)
    for _ in range(14):
        cx = rng.uniform(180, w - 180)
        cy = rng.uniform(170, h - 170)
        r = rng.uniform(90, 160)
        d.ellipse((cx - r * 1.3, cy - r, cx + r * 1.3, cy + r), fill=255)
    img = img.filter(ImageFilter.GaussianBlur(48))
    rgba = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    rgba.putalpha(img)
    rgba.save(os.path.join(out, "CloudShadow.png"))


def sun():
    s = 700
    img = Image.new("L", (s, s), 0)
    d = ImageDraw.Draw(img)
    for i in range(60, 0, -1):
        r = s / 2 * i / 60
        a = int(255 * (1 - i / 60) ** 1.6)
        d.ellipse((s / 2 - r, s / 2 - r, s / 2 + r, s / 2 + r), fill=a)
    img = img.filter(ImageFilter.GaussianBlur(10))
    rgba = Image.new("RGBA", (s, s), (255, 236, 170, 0))
    rgba.putalpha(img)
    rgba.save(os.path.join(out, "SunGlow.png"))


cloud()
sun()
print("ok")
