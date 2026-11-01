"""3. gun: etraf daha temiz, yaliya yeni evler eklenmis dis cephe + Mert'li atolye ici.
Once: python pollute.py <Workshop> 0.15 ExteriorClean.png ExteriorBackgroundDay3
Sonra: python day3_art.py <Assets/Sprites> <28.png (Mert, kirik pusula)> <26.png (Mert, onarilmis pusula)>
- Workshop/ExteriorBackgroundDay3 (+TR: ayni evler, TR zemin yazisiz 2. gun zemininden degil kendi zemininden)
- Day3/InteriorDay3Background, InteriorDay3AfterBackground (+TR): Day2 zeminindeki Elif yerine Mert
- ArtLabels.json'a 3. gun zeminleri icin 2. gununkilerin aynisi eklenir (yazi yerleri degismedi).
  erase_labels.py yeniden calistirilirsa bu betik de tekrar calistirilmali."""
import sys, os, json
from PIL import Image, ImageDraw, ImageFilter

SP, mert_src, after_src = sys.argv[1:4]
W_DIR = f"{SP}/Workshop"
OUT = f"{SP}/Day3"
os.makedirs(f"{OUT}/TR", exist_ok=True)
S = 4
INK = (60, 44, 40, 255)
BLUE = (70, 112, 172, 255)


def layer(w, h):
    im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im), (lambda x, y: (x * S, y * S))


def window(d, P, x, y, w=34, h=40, shutter=BLUE):
    d.rectangle([P(x - 8, y), P(x, y + h)], fill=shutter, outline=INK, width=2 * S)
    d.rectangle([P(x + w, y), P(x + w + 8, y + h)], fill=shutter, outline=INK, width=2 * S)
    d.rectangle([P(x, y), P(x + w, y + h)], fill=(170, 210, 226, 255), outline=INK, width=3 * S)
    d.line([P(x + w / 2, y), P(x + w / 2, y + h)], fill=INK, width=2 * S)


def tile_house(w, h, wall, roof, shutter, door):
    """Kiremit catili ev (2. gundeki ev 1 gibi, farkli renkler)."""
    im, d, P = layer(w, h)
    top = h * 0.4
    d.rectangle([P(16, top), P(w - 16, h - 4)], fill=wall, outline=INK, width=4 * S)
    d.polygon([P(2, top + 6), P(w / 2, 18), P(w - 2, top + 6)], fill=INK)
    d.polygon([P(12, top + 2), P(w / 2, 26), P(w - 12, top + 2)], fill=roof)
    window(d, P, 40, top + 24, shutter=shutter)
    window(d, P, w - 82, top + 24, shutter=shutter)
    d.rounded_rectangle([P(w / 2 - 20, h - 76), P(w / 2 + 20, h - 4)], radius=14 * S, fill=door, outline=INK, width=3 * S)
    return im.resize((w, h), Image.LANCZOS)


def flat_house(w, h, wall, shutter):
    """Duz catili, iki katli ev."""
    im, d, P = layer(w, h)
    d.rectangle([P(14, 36), P(w - 14, h - 4)], fill=wall, outline=INK, width=4 * S)
    d.rectangle([P(6, 22), P(w - 6, 40)], fill=INK)
    d.rectangle([P(10, 26), P(w - 10, 36)], fill=(226, 212, 186, 255))
    window(d, P, 34, 62, shutter=shutter)
    window(d, P, w - 76, 62, shutter=shutter)
    window(d, P, 34, 140, shutter=shutter)
    d.rounded_rectangle([P(w - 70, 150), P(w - 34, h - 4)], radius=4 * S, fill=(150, 98, 60, 255), outline=INK, width=3 * S)
    # Balkonda cicekler
    for x in range(30, w - 30, 18):
        d.ellipse([P(x - 6, 120), P(x + 6, 132)], fill=(220, 90, 110, 255))
    d.line([P(22, 132), P(w - 22, 132)], fill=INK, width=3 * S)
    return im.resize((w, h), Image.LANCZOS)


def day2_houses():
    """2. gunde yapilan iki ev (day2_art.py ile ayni cizim)."""
    from importlib import util
    h1 = tile_house(260, 230, (246, 240, 228, 255), (201, 96, 63, 255), BLUE, BLUE)
    h2 = flat_house(200, 270, (240, 228, 206, 255), BLUE)
    return h1, h2


def exterior(path):
    bg = Image.open(path).convert("RGBA")
    h1, h2 = day2_houses()
    bg.alpha_composite(h1, (210, 430))
    bg.alpha_composite(h2, (560, 392))
    # 3. gunun yeni evleri: solda, ortada (magaza ile 2. ev arasi) ve magazanin saginda
    bg.alpha_composite(flat_house(180, 250, (232, 240, 236, 255), (60, 150, 120, 255)), (10, 412))
    bg.alpha_composite(tile_house(230, 210, (250, 236, 210, 255), (176, 84, 60, 255), (214, 120, 60, 255), (60, 150, 120, 255)), (790, 450))
    bg.alpha_composite(tile_house(170, 180, (244, 244, 236, 255), (210, 112, 70, 255), BLUE, (150, 98, 60, 255)), (1436, 482))
    return bg.convert("RGB")


ext = exterior(f"{W_DIR}/ExteriorBackgroundDay3.png")
ext.save(f"{W_DIR}/ExteriorBackgroundDay3.png", optimize=True)
# TR: yazili alanlar (tabela, OPEN) 2. gunun TR zemininden alinir; gerisi 3. gunun zemini
tr2 = Image.open(f"{W_DIR}/TR/ExteriorBackgroundDay2.png").convert("RGB")
tr3 = ext.copy()
tr3.paste(tr2.crop((1620, 380, 2560, 1440)), (1620, 380))   # atolye binasi (tabelalar burada)
tr3.save(f"{W_DIR}/TR/ExteriorBackgroundDay3.png", optimize=True)


# ---------------- Atolye ici: Elif yerine Mert ----------------
BOX = (650, 286, 845, 532)       # 1280x720 tasarim koordinati (raf etiketlerinin alti, diyalog kutusunun ustu)


def with_mert(base_path, src_path, out_path):
    base = Image.open(base_path).convert("RGB")
    src = Image.open(src_path).convert("RGB").resize((2560, 1440), Image.LANCZOS)
    x0, y0, x1, y1 = (v * 2 for v in BOX)
    patch = src.crop((x0, y0, x1, y1)).filter(ImageFilter.UnsharpMask(radius=3, percent=80, threshold=2))
    m = Image.new("L", patch.size, 0)
    ImageDraw.Draw(m).rounded_rectangle([12, 12, patch.size[0] - 12, patch.size[1] + 40], radius=30, fill=255)
    base.paste(patch, (x0, y0), m.filter(ImageFilter.GaussianBlur(8)))
    base.save(out_path, optimize=True)
    return base


for tr in ("", "TR/"):
    a = with_mert(f"{SP}/Day2/{tr}InteriorDay2Background.png", mert_src, f"{OUT}/{tr}InteriorDay3Background.png")
    with_mert(f"{SP}/Day2/{tr}InteriorDay2Background.png", after_src, f"{OUT}/{tr}InteriorDay3AfterBackground.png")
    if not tr:
        a.resize((640, 360), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10)).save(f"{OUT}/InteriorDay3Backdrop.png")

# ---------------- ArtLabels: 2. gunun yazi yerleri 3. gun zeminleri icin de gecerli ----------------
labels_path = os.path.join(os.path.dirname(SP), "Localization", "ArtLabels.json")
data = json.load(open(labels_path, encoding="utf-8"))
by = {s["sprite"]: s for s in data["sprites"]}
for new, old in (("Workshop/ExteriorBackgroundDay3", "Workshop/ExteriorBackgroundDay2"),
                 ("Day3/InteriorDay3Background", "Day2/InteriorDay2Background"),
                 ("Day3/InteriorDay3AfterBackground", "Day2/InteriorDay2AfterBackground")):
    entry = dict(by[old])
    entry["sprite"] = new
    if new in by: data["sprites"][data["sprites"].index(by[new])] = entry
    else: data["sprites"].append(entry)
json.dump(data, open(labels_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("ok")
