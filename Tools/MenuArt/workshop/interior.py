"""Atolye ici (ilk musteri) gorselleri. Kullanim: python interior.py <8.png> <9.png> <cikis>
Koordinatlar 1280x720 tasarimda; cikti 2x (2560x1440)."""
import sys; sys.path.insert(0, sys.path[0])
from common import *
from PIL import ImageDraw
a_src, b_src, out = sys.argv[1], sys.argv[2], sys.argv[3]
A = load(a_src); B = load(b_src); H, W = A.shape[:2]
K = 2
O = Out(out, K)
C = A.copy()
P = B.copy()

DIALOG = np.array([251, 248, 241])
C[551:614, 48:832] = DIALOG                      # diyalog yazisi (koddan yazilir)
P[176:196, 908:1040] = B[200, 1000]              # "1 of 5 done" (koddan yazilir)
C[28:45, 921:952] = A[36, 915]                   # coin sayaci (koddan yazilir)


def solid(img, ref, box, thr=25, largest=True):
    x0, y0, x1, y1 = box
    m = np.zeros((H, W), bool)
    m[y0:y1, x0:x1] = np.abs(img[y0:y1, x0:x1] - ref[y0:y1, x0:x1]).max(-1) > thr
    m = nd.binary_fill_holes(nd.binary_closing(m, iterations=2))
    if largest:
        lab, n = nd.label(m); sizes = nd.sum(m, lab, range(1, n + 1))
        m = lab == np.argmax(sizes) + 1
    return m


flat = lambda color: np.broadcast_to(np.array(color), A.shape)
O.cut("TellMoreButton", A, soft(solid(A, flat(DIALOG), (476, 620, 637, 690)), 0.5))
O.cut("LetsFixButton", A, soft(solid(A, flat(DIALOG), (638, 620, 830, 690)), 0.5))
BAR = A[40, 600]
O.cut("MenuButton", A, soft(solid(A, flat(BAR), (16, 6, 80, 68)), 0.5))
O.cut("HeadOutButton", A, soft(solid(A, flat(BAR), (970, 6, 1131, 66)), 0.5))
tb = solid(A, flat(BAR), (1132, 6, 1242, 66)) | solid(A, flat(BAR), (1132, 30, 1263, 66))
O.cut("TasksButton", A, soft(tb, 0.5))
tba = solid(B, flat(BAR), (1132, 6, 1242, 66)) | solid(B, flat(BAR), (1132, 30, 1263, 66))
O.cut("TasksButtonActive", B, soft(tba, 0.5))

# Gorev paneli: 9.png'nin 8.png'den farkli oldugu yer (ust bar haric)
panel = np.zeros((H, W), bool)
panel[80:720, 860:1272] = np.abs(B[80:720, 860:1272] - A[80:720, 860:1272]).max(-1) > 14
panel = nd.binary_fill_holes(nd.binary_closing(panel, iterations=3))
lab, n = nd.label(panel); sizes = nd.sum(panel, lab, range(1, n + 1))
panel = lab == np.argmax(sizes) + 1
O.cut("TasksPanel", P, soft(panel, 0.6), pad=0)

# Paneldeki isaretli kutu (Meet Hatice Teyze kutusunun ustune gelecek)
chk = np.zeros((H, W), bool); chk[219:256, 921:960] = True
chk &= (B.sum(-1) < 300) | (B[..., 2] > B[..., 0] + 30) | (B.min(-1) > 225)
chk = nd.binary_fill_holes(chk)
lab, n = nd.label(chk); sizes = nd.sum(chk, lab, range(1, n + 1))
O.cut("PanelCheck", B, soft(lab == np.argmax(sizes) + 1, 0.5))
O.rects["PanelMeetBox"] = [925 * K, 297 * K, 31 * K, 31 * K]

O.full("WorkshopBackground", C)

# Gorev sayisi rozeti (rakam koddan)
S = 8
im = Image.new("RGBA", (26 * K * S, 26 * K * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.ellipse([0, 0, im.width - 1, im.height - 1], fill=(251, 248, 241, 255))
inset = int(1.7 * K * S)
d.ellipse([inset, inset, im.width - 1 - inset, im.height - 1 - inset], fill=(180, 76, 40, 255))
im.resize((26 * K, 26 * K), Image.LANCZOS).save(f"{out}/TaskBadge.png")
O.rects["TaskBadge"] = [int((1254.2 - 13) * K), int((17.1 - 13) * K), 26 * K, 26 * K]

O.save_rects("int_rects.json")
print(json.dumps(O.rects))
