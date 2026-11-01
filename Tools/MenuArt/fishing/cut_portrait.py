"""Hatice Teyze'nin yuvarlak portresini tamir tezgahi tasarimindan keser.
Kullanim: python cut_portrait.py <tezgah_tasarimi.png> <Assets/Sprites/Beach>"""
import sys
from PIL import Image, ImageDraw

src, out = sys.argv[1], sys.argv[2]
cx, cy, r = 68.5, 142.5, 28.5   # 1280x720 tasarimda portre dairesi
A = Image.open(src).convert("RGB")
crop = A.crop((int(cx - r - 2), int(cy - r - 2), int(cx + r + 3), int(cy + r + 3))).resize((240, 240), Image.LANCZOS)
m = Image.new("L", (960, 960), 0)
ImageDraw.Draw(m).ellipse([8, 8, 952, 952], fill=255)
img = crop.convert("RGBA")
img.putalpha(m.resize((240, 240), Image.LANCZOS))
img.save(f"{out}/HaticePortrait.png")
