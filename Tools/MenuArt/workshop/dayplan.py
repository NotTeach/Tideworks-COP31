"""Gun plani (Day 1 gorev panosu) gorselleri. Kullanim: python dayplan.py <7.jpg> <cikis>
Tasarim 2752x1536, cikti ayni olcek."""
import sys; sys.path.insert(0, sys.path[0])
from common import *
src, out = sys.argv[1], sys.argv[2]
A = load(src); H, W = A.shape[:2]
O = Out(out, 1)
C = A.copy()

# 1. satir: isaretli kutu -> 2. satirin bos kutusu (satir arasi 133 px)
patch(C, (840, 633, 946, 733), (840, 500))
# 1. satir yazilari (ustu cizili) -> ayni satirin bos kagidi
patch(C, (1330, 512, 1696, 606), (944, 512))
# "Done" rozeti -> soldaki bos kagit
patch(C, (1600, 503, 1736, 582), (1768, 503))

C = C.astype(np.int32)
diff = np.abs(A - C).max(-1)

check = np.zeros((H, W), bool); check[496:600, 836:948] = True
check &= nd.binary_fill_holes(nd.binary_dilation(diff > 28, iterations=1))
lab, n = nd.label(check); sizes = nd.sum(check, lab, range(1, n + 1))
check = lab == np.argmax(sizes) + 1
O.cut("CheckOn", A, soft(nd.binary_fill_holes(check), 0.6), bg=C)

badge = np.zeros((H, W), bool); badge[503:582, 1768:1904] = True
O.cut("BadgeDone", A, np.clip(diff / 30.0, 0, 1) * badge, bg=C)

bb = np.zeros((H, W), bool); bb[1300:1410, 822:1968] = True
red = bb & (A[..., 0] - A[..., 2] > 60) & (A[..., 0] > 90)
red = nd.binary_fill_holes(nd.binary_dilation(red, iterations=2))
lab, n = nd.label(red); sizes = nd.sum(red, lab, range(1, n + 1))
O.cut("StartDayButton", A, soft(lab == np.argmax(sizes) + 1, 0.7))

O.full("DayPlanBackground", C, size=(W, H))
O.save_rects("day_rects.json")
print(json.dumps(O.rects))
