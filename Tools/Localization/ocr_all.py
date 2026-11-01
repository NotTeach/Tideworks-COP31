"""Tum sanat sprite'larinda yazilari bulur -> ocr.json  {sprite: [[x0,y0,x1,y1,text,score],...]}"""
import sys, json, glob, os
import numpy as np
from PIL import Image
from rapidocr_onnxruntime import RapidOCR

root = sys.argv[1]
ocr = RapidOCR()
res = {}
files = []
for d in ["MainMenu", "Settings", "Workshop", "Workbench"]:
    files += sorted(glob.glob(f"{root}/{d}/*.png"))
for f in files:
    name = os.path.basename(os.path.dirname(f)) + "/" + os.path.splitext(os.path.basename(f))[0]
    if any(k in name for k in ("Backdrop", "Wave", "Cloud", "Dust", "Glow", "Sparkle", "Slider", "Switch", "Seg", "Screw", "Needle")):
        continue
    im = Image.open(f).convert("RGBA")
    bg = Image.new("RGBA", im.size, (128, 128, 128, 255)); bg.alpha_composite(im)
    a = np.asarray(bg.convert("RGB"))
    scale = 1
    if max(a.shape[:2]) < 400:  # kucuk sprite: buyut
        scale = 3
        a = np.asarray(bg.convert("RGB").resize((im.width * 3, im.height * 3), Image.LANCZOS))
    out, _ = ocr(a)
    items = []
    for box, text, score in out or []:
        xs = [p[0] / scale for p in box]; ys = [p[1] / scale for p in box]
        items.append([round(min(xs)), round(min(ys)), round(max(xs)), round(max(ys)), text, round(float(score), 2)])
    if items:
        res[name] = items
        print(name, im.size)
        for it in items: print("   ", it)
json.dump(res, open(sys.argv[2], "w", encoding="utf-8"), ensure_ascii=False, indent=1)
