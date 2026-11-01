"""Turkce gorunum onizlemesi: TR sprite + ArtLabels etiketleri (bestFit benzetimi)."""
import sys, json, re
from PIL import Image, ImageDraw, ImageFont
root, sprite, out = sys.argv[1:4]
STR = r'"(?:[^"\\]|\\.)*"'


def cs(x):
    o = ""
    for p in re.findall(r'"((?:[^"\\]|\\.)*)"', x):
        o += re.sub(r"\\u([0-9a-fA-F]{4})", lambda m: chr(int(m.group(1), 16)), p).replace("\\n", "\n")
    return o


src = open(f"{root}/Assets/Scripts/Localization/LocTable.cs", encoding="utf-8").read()
entries = re.findall(r"\{\s*((?:" + STR + r"\s*\+?\s*)+),\s*((?:" + STR + r"\s*\+?\s*)+)\}", src[src.index("new Dictionary"):])
TR = {cs(a): cs(b) for a, b in entries}
F = {"N": "tr-Nunito-ExtraBold.ttf", "n": "tr-Nunito-Bold.ttf", "C": "tr-Caveat-Bold.ttf", "B": "tr-Baloo2-ExtraBold.ttf"}
fonts = sys.argv[4]
data = json.load(open(f"{root}/Assets/Localization/ArtLabels.json", encoding="utf-8"))
sp = next(s for s in data["sprites"] if s["sprite"] == sprite)
folder, base = sprite.split("/")
im = Image.open(f"{root}/Assets/Sprites/{folder}/TR/{base}.png").convert("RGBA")
d = ImageDraw.Draw(im)
for l in sp["labels"]:
    text = TR.get(l["en"], l["en"])
    size = l["size"]
    while size > l["size"] * 0.55:
        f = ImageFont.truetype(f"{fonts}/{F[l['font']]}", size)
        w = d.textlength(text, font=f)
        if w <= l["w"]: break
        size -= 1
    x = l["x"] if l["align"] == "l" else l["x"] + l["w"] / 2 if l["align"] == "c" else l["x"] + l["w"]
    anchor = {"l": "lm", "c": "mm", "r": "rm"}[l["align"]]
    d.text((x, l["y"] + l["h"] / 2), text, font=f, fill=(l["r"], l["g"], l["b"]), anchor=anchor)
im.save(out)
