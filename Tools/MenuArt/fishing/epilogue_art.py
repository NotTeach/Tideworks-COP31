"""Sezon sonu kapanis sayfasi ("The story isn't over..."): tasarimdaki yazilar silinir, Unity'de cevrilebilir metin olarak yazilir.
Kullanim: python epilogue_art.py <Assets/Sprites> <29.png (1280x720 tasarim)>
Cikti: Day3Beach/EpilogueBackground.png (2560x1440) ve EpilogueBackdrop.png. Defter, harita, simgeler ve buton sekilleri kalir.
ERASE kutulari 1280x720 tasarim koordinatidir; FishingSceneBuilder.Day3Beach.cs ayni yerlere (x2) metin koyar."""
import sys
import numpy as np
from PIL import Image, ImageFilter

SP, design = sys.argv[1:3]
img = Image.open(design).convert("RGB").resize((2560, 1440), Image.LANCZOS)
a = np.asarray(img).astype(np.float32)

ERASE = [
    (172, 100, 300, 119),     # KAS IS SAVED
    (172, 122, 490, 214),     # baslik
    (172, 230, 584, 312),     # metin
    (176, 336, 380, 372),     # You can do it too:
    (244, 386, 560, 434),     # liste 1
    (244, 460, 520, 508),     # liste 2
    (244, 534, 560, 582),     # liste 3
    (192, 602, 316, 634),     # Main menu
    (356, 602, 552, 632),     # Play the season again
    (678, 98, 972, 128),      # Grandpa's notebook · the last page
    (684, 458, 1110, 566),    # alinti
    (734, 582, 850, 622),     # NEW COAST UNLOCKING
    (916, 572, 1040, 634),    # The map grows
]


def fill(x0, y0, x1, y1):
    """Kutuyu kenarlarindaki renklerin medyaniyla doldur (duz zeminler: kagit, kart, buton)."""
    X0, Y0, X1, Y1 = x0 * 2, y0 * 2, x1 * 2, y1 * 2
    ring = np.concatenate([a[Y0 - 3:Y0, X0:X1].reshape(-1, 3), a[Y1:Y1 + 3, X0:X1].reshape(-1, 3),
                           a[Y0:Y1, X0 - 3:X0].reshape(-1, 3), a[Y0:Y1, X1:X1 + 3].reshape(-1, 3)])
    a[Y0:Y1, X0:X1] = np.median(ring, axis=0)


for box in ERASE:
    fill(*box)
out = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
out.save(f"{SP}/Day3Beach/EpilogueBackground.png", optimize=True)
out.resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{SP}/Day3Beach/EpilogueBackdrop.png")
print("ok")
