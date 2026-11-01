"""Odul karti ve deniz paneli onizlemesi (builder yerlesimini taklit eder). python preview_panels.py <Workshop sprites> <fonts> <out_prefix>"""
import sys
from PIL import Image, ImageDraw, ImageFont
D, F, out = sys.argv[1:4]
f = lambda n, s: ImageFont.truetype(f"{F}/{n}", s)
BALOO, NB, NX, CAV = "tr-Baloo2-ExtraBold.ttf", "tr-Nunito-Bold.ttf", "tr-Nunito-ExtraBold.ttf", "tr-Caveat-Bold.ttf"


def put(bg, name, x, y, size=None):
    im = Image.open(f"{D}/{name}.png").convert("RGBA")
    if size: im = im.resize(size, Image.LANCZOS)
    bg.alpha_composite(im, (int(x), int(y)))
    return im.size


def text(d, s, font, size, color, rect, anchor="mm", wrap=False, spacing=1.0):
    x, y, w, h = rect
    fo = f(font, size)
    if wrap:
        words, lines, cur = s.split(), [], ""
        for wd in words:
            t = (cur + " " + wd).strip()
            if d.textlength(t, font=fo) <= w: cur = t
            else: lines.append(cur); cur = wd
        lines.append(cur)
        lh = size * 1.25 * spacing
        y0 = y + (h - lh * len(lines)) / 2 if anchor[1] == "m" else y
        for i, ln in enumerate(lines):
            px = x + w / 2 if anchor[0] == "m" else x
            d.text((px, y0 + lh * i + lh / 2), ln, font=fo, fill=color, anchor=anchor[0] + "m")
        return
    px = x + w / 2 if anchor[0] == "m" else x
    d.text((px, y + h / 2), s, font=fo, fill=color, anchor=anchor[0] + "m")


# --- Odul karti ---
bg = Image.new("RGBA", (2560, 1440), (16, 64, 74, 255)); d = ImageDraw.Draw(bg)
put(bg, "CounterPill", 1070, 62)
put(bg, "Coin", 1102, 89, (56, 56))
text(d, "120", BALOO, 56, "white", (1170, 64, 100, 110), "lm")
put(bg, "ArrowWhite", 1276, 97, (56, 35))
text(d, "160", BALOO, 56, "white", (1342, 64, 130, 110), "lm")
cx, cy = 750, 200
put(bg, "RewardCard", cx, cy)
pw, ph = Image.open(f"{D}/CoinPile.png").size
put(bg, "CoinPile", cx + 530 - pw / 2, cy + 66)
text(d, "Repair complete!", BALOO, 96, (18, 62, 72), (cx + 40, cy + 396, 980, 130))
put(bg, "RewardPillCoins", cx + 370, cy + 534)
text(d, "+40 coins", BALOO, 56, (244, 192, 74), (cx + 370, cy + 536, 320, 104))
put(bg, "RewardPillHelper", cx + 230, cy + 664)
text(d, "Hatice Teyze is now your helper", BALOO, 40, "white", (cx + 250, cy + 666, 560, 86))
text(d, "“Fixing an old thing means less waste than buying a new one.”", CAV, 64, (110, 62, 40), (cx + 90, cy + 770, 880, 170), wrap=True, spacing=0.9)
put(bg, "ButtonOrange", cx + 350, cy + 958)
text(d, "Continue", BALOO, 56, "white", (cx + 350, cy + 953, 360, 120))
bg.resize((1280, 720)).save(out + "_reward.png")

# --- Deniz paneli ---
bg = Image.new("RGBA", (2560, 1440), (16, 64, 74, 255)); d = ImageDraw.Draw(bg)
fx, fy = 1280 - 1124, 740 - 634
put(bg, "MissionFrame", fx, fy)
put(bg, "BeachArt", fx + 84, fy + 84)
rx, ry = fx + 1114, fy + 74
put(bg, "MissionCardRight", rx, ry)
text(d, "Let's Save the Seas!", BALOO, 108, (18, 62, 72), (rx + 60, ry + 40, 930, 270), "lt", wrap=True, spacing=0.8)
text(d, "Every piece of trash that washes ashore blocks a caretta hatchling's way to the sea. Clean the sea, tidy up the beach and help the hatchlings reach the water.",
     NB, 40, (48, 43, 40), (rx + 60, ry + 316, 930, 230), "lt", wrap=True, spacing=1.15)
for i, (c, ic, t) in enumerate((("Teal", "IconBoatWhite", "Clean the sea"), ("Green", "IconRecycleWhite", "Recycle"), ("Orange", "IconTurtleWhite", "Protect the carettas"))):
    y = ry + 560 + i * 136
    put(bg, "MissionRow" + c, rx + 60, y)
    put(bg, ic, rx + 104, y + 24)
    text(d, t, BALOO, 52, "white", (rx + 210, y + 2, 760, 120), "lm")
put(bg, "ButtonOrangeWide", rx + 60, ry + 974)
put(bg, "IconBoatWhite", rx + 180, ry + 996, (64, 64))
text(d, "Head out", BALOO, 54, "white", (rx + 96, ry + 969, 560, 120))
put(bg, "ButtonOutline", rx + 660, ry + 974)
text(d, "Later", NX, 44, (48, 43, 40), (rx + 660, ry + 969, 330, 120))
bg.resize((1280, 720)).save(out + "_mission.png")
