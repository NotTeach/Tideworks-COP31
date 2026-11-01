"""2. gun dis cephe: yaliya yeniden yapilan 2 ev (zemine cizilir), magaza binasi ve CLOSED/OPEN tabelasi.
Once: python pollute.py <Workshop> 0.45 ExteriorClean.png ExteriorBackgroundDay2
Sonra: python day2_art.py <Assets/Sprites/Workshop>        (2x artboard pikseli)"""
import sys
from PIL import Image, ImageDraw

D = sys.argv[1]
S = 4
INK = (60, 44, 40, 255)
WALL = (246, 240, 228, 255)
ROOF = (201, 96, 63, 255)
BLUE = (70, 112, 172, 255)


def layer(w, h):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (x * S, y * S))


def window(d, P, x, y, w=34, h=40):
    d.rectangle([P(x - 8, y), P(x, y + h)], fill=BLUE, outline=INK, width=2 * S)            # panjur
    d.rectangle([P(x + w, y), P(x + w + 8, y + h)], fill=BLUE, outline=INK, width=2 * S)
    d.rectangle([P(x, y), P(x + w, y + h)], fill=(170, 210, 226, 255), outline=INK, width=3 * S)
    d.line([P(x + w / 2, y), P(x + w / 2, y + h)], fill=INK, width=2 * S)


# ---- Ev 1: kiremit catili beyaz ev ----
house1, d, P = layer(260, 230)
d.rectangle([P(20, 90), P(240, 226)], fill=WALL, outline=INK, width=4 * S)
d.polygon([P(4, 96), P(130, 22), P(256, 96)], fill=INK)
d.polygon([P(14, 92), P(130, 30), P(246, 92)], fill=ROOF)
for i in range(5):
    d.line([P(40 + i * 40, 92), P(70 + i * 22, 52)], fill=(176, 80, 52, 255), width=2 * S)
window(d, P, 48, 116); window(d, P, 172, 116)
d.rounded_rectangle([P(110, 150), P(150, 226)], radius=16 * S, fill=BLUE, outline=INK, width=3 * S)
d.ellipse([P(140, 186), P(146, 192)], fill=INK)
for x in (52, 62, 182, 192):  # cicek saksisi
    d.ellipse([P(x - 6, 158), P(x + 6, 170)], fill=(220, 90, 110, 255))
house1 = house1.resize((260, 230), Image.LANCZOS)

# ---- Ev 2: duz catili, balkonlu iki katli ev ----
house2, d, P = layer(200, 270)
d.rectangle([P(16, 40), P(184, 266)], fill=(240, 228, 206, 255), outline=INK, width=4 * S)
d.rectangle([P(8, 26), P(192, 44)], fill=INK)
d.rectangle([P(12, 30), P(188, 40)], fill=(226, 212, 186, 255))
window(d, P, 40, 70); window(d, P, 124, 70)
d.rectangle([P(30, 150), P(170, 158)], fill=INK)                                          # balkon
for x in range(36, 168, 14):
    d.line([P(x, 128), P(x, 152)], fill=INK, width=2 * S)
d.line([P(30, 128), P(170, 128)], fill=INK, width=3 * S)
window(d, P, 40, 168, 34, 36)
d.rounded_rectangle([P(118, 178), P(158, 266)], radius=4 * S, fill=(150, 98, 60, 255), outline=INK, width=3 * S)
house2 = house2.resize((200, 270), Image.LANCZOS)

bg = Image.open(f"{D}/ExteriorBackgroundDay2.png").convert("RGBA")
bg.alpha_composite(house1, (210, 430))      # alt kenar ~660: yalinin ustu
bg.alpha_composite(house2, (560, 392))
bg.convert("RGB").save(f"{D}/ExteriorBackgroundDay2.png", optimize=True)

# ---- Magaza (tiklanabilir sprite; tabela yazisi Unity'de) ----
shop, d, P = layer(340, 300)
d.rectangle([P(24, 96), P(316, 296)], fill=(250, 232, 182, 255), outline=INK, width=4 * S)
d.rounded_rectangle([P(60, 14), P(280, 74)], radius=12 * S, fill=INK)                     # tabela
d.rounded_rectangle([P(64, 18), P(276, 70)], radius=10 * S, fill=(252, 246, 232, 255))
d.line([P(100, 74), P(100, 96)], fill=INK, width=4 * S); d.line([P(240, 74), P(240, 96)], fill=INK, width=4 * S)
for i in range(7):                                                                         # cizgili tente
    x0 = 14 + i * 44.6
    d.polygon([P(x0, 100), P(x0 + 44.6, 100), P(x0 + 44.6, 140), P(x0, 140)],
              fill=(60, 150, 100, 255) if i % 2 == 0 else (250, 248, 240, 255))
d.rectangle([P(14, 100), P(326, 140)], outline=INK, width=4 * S)
for i in range(7):
    x0 = 14 + i * 44.6
    d.arc([P(x0, 128), P(x0 + 44.6, 152)], 0, 180, fill=INK, width=3 * S)
d.rectangle([P(48, 168), P(180, 262)], fill=(170, 210, 226, 255), outline=INK, width=4 * S)  # vitrin
d.line([P(114, 168), P(114, 262)], fill=INK, width=3 * S)
for x, c in ((70, (220, 90, 70)), (96, (244, 192, 74)), (136, (90, 160, 120)), (160, (70, 112, 172))):
    d.rounded_rectangle([P(x - 9, 230), P(x + 9, 260)], radius=4 * S, fill=c + (255,), outline=INK, width=2 * S)
d.rounded_rectangle([P(214, 172), P(282, 296)], radius=8 * S, fill=(150, 98, 60, 255), outline=INK, width=4 * S)
d.ellipse([P(268, 232), P(276, 240)], fill=(244, 192, 74, 255))
shop.resize((340, 300), Image.LANCZOS).save(f"{D}/Shop.png")

# ---- Bos tabela (CLOSED / OPEN yazisi Unity'de) ----
sign, d, P = layer(330, 100)
d.rounded_rectangle([P(6, 6), P(324, 96)], radius=18 * S, fill=INK)
d.rounded_rectangle([P(11, 11), P(319, 88)], radius=14 * S, fill=(238, 222, 180, 255))
sign.resize((330, 100), Image.LANCZOS).save(f"{D}/SignBoard.png")
print("ok")
