"""Sakin, Akdeniz havasinda fon muzigi (deniz ve kumsal sahneleri, menu): naylon telli gitar arpejleri,
mandolin tremolosuyla seyrek bir ezgi ve yumusak bas. Karplus-Strong tel sentezi, hafif yanki; dikissiz dongu.
Kullanim: python mediterranean_loop.py <Assets/Tideworks/Resources/TideworksSes/muzik_akdeniz_sakin_loop.wav>"""
import sys
import numpy as np
from scipy.io import wavfile
from scipy.signal import fftconvolve

OUT = sys.argv[1]
SR = 32000
EIGHTH = 0.42                      # ~71 bpm, 6/8 hissi (olcu basina 6 sekizlik)
BAR = 6 * EIGHTH
rng = np.random.default_rng(11)


def midi_hz(m): return 440.0 * 2 ** ((m - 69) / 12)


_cache = {}


def pluck(midi, dur, bright=0.5, decay=0.996):
    """Karplus-Strong tel: periyot uzunlugunda bloklar halinde (vektorel) uretilir."""
    key = (midi, round(dur, 2), bright, decay)
    if key in _cache: return _cache[key]
    n_total = int(dur * SR)
    N = int(SR / midi_hz(midi))
    y = np.zeros(n_total + N + 1)
    burst = rng.uniform(-1, 1, N)
    # Parlaklik: gurultu once yumusatilir (naylon tel daha boguk)
    for _ in range(int((1 - bright) * 4)):
        burst = 0.5 * (burst + np.roll(burst, 1))
    y[:N] = burst
    pos = N
    while pos < n_total + 1:
        end = min(pos + N, n_total + 1)
        prev = y[pos - N:end - N]
        prev2 = y[pos - N - 1:end - N - 1] if pos - N - 1 >= 0 else np.concatenate([[0], y[pos - N:end - N - 1]])
        y[pos:end] = decay * 0.5 * (prev + prev2)
        pos = end
    out = y[:n_total]
    out *= np.minimum(1, np.linspace(0, 40, n_total))           # tik sesini yumusat
    fade = int(0.08 * SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    _cache[key] = out
    return out


def soft_bass(midi, dur):
    t = np.arange(int(dur * SR)) / SR
    f = midi_hz(midi)
    env = np.exp(-t * 2.2) * np.minimum(1, t * 60)
    return (np.sin(2 * np.pi * f * t) + 0.25 * np.sin(4 * np.pi * f * t)) * env


# ---------------- Akorlar (La minor, E7'de sol diyez: Akdeniz rengi) ----------------
A2, D3, E2, G2, C3, F2 = 45, 50, 40, 43, 48, 41
CH = {
    "Am": (A2, [57, 64, 69, 72, 69, 64]),
    "Dm": (D3, [57, 62, 65, 69, 65, 62]),
    "E7": (E2, [56, 62, 64, 68, 64, 62]),
    "G":  (G2, [55, 62, 67, 71, 67, 62]),
    "C":  (C3, [55, 60, 64, 67, 64, 60]),
    "F":  (F2, [57, 60, 65, 69, 65, 60]),
}
PROG = ["Am", "Am", "Dm", "Am", "G", "C", "E7", "E7",
        "Am", "F", "G", "C", "Dm", "Am", "E7", "Am",
        "F", "C", "Dm", "Am", "Dm", "E7", "E7", "Am"]

# Ezgi (sekizlik, midi; None = sus). Uzun notalar mandolin tremolosuyla calinir.
MEL = {
    0: [(0, 76, 3), (3, 74, 1.5), (4.5, 72, 1.5)], 1: [(0, 71, 3), (3, 72, 3)],
    2: [(0, 74, 4.5), (4.5, 72, 1.5)], 3: [(0, 69, 6)],
    4: [(0, 71, 3), (3, 74, 3)], 5: [(0, 72, 4.5), (4.5, 71, 1.5)],
    6: [(0, 68, 3), (3, 71, 3)], 7: [(0, 76, 6)],
    8: [(0, 76, 3), (3, 77, 1.5), (4.5, 76, 1.5)], 9: [(0, 72, 6)],
    10: [(0, 74, 3), (3, 71, 3)], 11: [(0, 72, 6)],
    12: [(0, 77, 3), (3, 76, 1.5), (4.5, 74, 1.5)], 13: [(0, 72, 3), (3, 71, 1.5), (4.5, 72, 1.5)],
    14: [(0, 71, 3), (3, 68, 3)], 15: [(0, 69, 6)],
    20: [(0, 74, 3), (3, 77, 3)], 21: [(0, 76, 3), (3, 74, 1.5), (4.5, 71, 1.5)],
    22: [(0, 68, 6)], 23: [(0, 69, 6)],
}

total = len(PROG) * BAR
L = int(total * SR)
mix = np.zeros(L + 6 * SR)                                       # kuyruk sonra basa sarilir


def add(sig, at, gain):
    i = int(at * SR)
    mix[i:i + len(sig)] += sig * gain


for b, name in enumerate(PROG):
    root, arp = CH[name]
    t0 = b * BAR
    add(soft_bass(root, BAR * 1.1), t0, 0.32)
    add(soft_bass(root + 7 if name != "E7" else root + 7, BAR * 0.6), t0 + 3 * EIGHTH, 0.14)
    for k, note in enumerate(arp):
        swing = rng.normal(0, 0.008)                              # insan eli
        vel = (0.30 if k in (0, 3) else 0.22) * rng.uniform(0.9, 1.05)
        add(pluck(note, 2.6, bright=0.35, decay=0.9975), t0 + k * EIGHTH + swing, vel)

for b, notes in MEL.items():
    t0 = b * BAR
    for start, midi, length in notes:
        dur = length * EIGHTH
        if length >= 3:                                            # mandolin tremolosu
            step = 0.085
            n = int(dur / step)
            for i in range(n):
                g = 0.10 * (0.75 + 0.25 * np.sin(i * 0.9)) * (1 - 0.5 * i / n)
                add(pluck(midi, 0.5, bright=0.75, decay=0.994), t0 + start * EIGHTH + i * step, g)
        else:
            add(pluck(midi, 1.4, bright=0.7, decay=0.996), t0 + start * EIGHTH, 0.16)

# Dikissiz dongu: dongunun sonunu tasan kuyruk basa eklenir
loop = mix[:L].copy()
tail = mix[L:]
loop[:len(tail)] += tail

# Yanki (oda): ustel sonumlu gurultu ile kivrim, iki kanal farkli -> genislik
ir_len = int(1.6 * SR)
t = np.arange(ir_len) / SR
stereo = []
for seed in (1, 2):
    r = np.random.default_rng(seed)
    ir = r.normal(0, 1, ir_len) * np.exp(-t * 3.2)
    ir[0] = 0
    ir /= np.sqrt(np.sum(ir ** 2))
    wet = fftconvolve(np.concatenate([loop, loop]), ir)[len(loop):2 * len(loop)]  # dongu yankisi da sarili
    stereo.append(loop * 0.82 + wet * 0.30)
out = np.stack(stereo, axis=1)
# Yumusak ust kesim (tiz sertligini al) ve seviye
k = np.exp(-np.arange(9) / 3.0); k /= k.sum()
for c in range(2):                                               # dairesel: dongu dikisinde tik olmasin
    padded = np.concatenate([out[-4:, c], out[:, c], out[:4, c]])
    out[:, c] = np.convolve(padded, k, mode="valid")
out /= np.max(np.abs(out)) / 0.7
wavfile.write(OUT, SR, (out * 32767).astype(np.int16))
print(f"ok {total:.1f}s")
