import wave
from pathlib import Path

import numpy as np

REPO = Path(__file__).resolve().parents[1]
OUT = REPO / "Assets" / "_Project" / "Audio"
SR = 44100
rng = np.random.default_rng(7)


def t(seconds):
    return np.arange(int(SR * seconds)) / SR


def env(n, attack=0.005, decay=0.2, curve=4.0):
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-5), 0, 1)
    d = np.exp(-curve * np.clip(x - attack, 0, None) / max(decay, 1e-5))
    return a * d


def sine(freq, seconds, phase=0.0):
    return np.sin(2 * np.pi * freq * t(seconds) + phase)


def sweep(f0, f1, seconds, shape=1.0):
    x = t(seconds)
    k = (x / seconds) ** shape
    freq = f0 + (f1 - f0) * k
    return np.sin(2 * np.pi * np.cumsum(freq) / SR)


def noise(seconds):
    return rng.uniform(-1, 1, int(SR * seconds))


def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc = (1 - a) * x[i] + a * acc
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def mix_at(dst, src, at_seconds, gain=1.0):
    i = int(at_seconds * SR)
    n = min(len(src), len(dst) - i)
    if n > 0:
        dst[i:i + n] += src[:n] * gain
    return dst


def fade_out(x, seconds=0.01):
    n = min(len(x), int(SR * seconds))
    x[-n:] *= np.linspace(1, 0, n)
    return x


def save(path, x, peak=0.85):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x))
    if m > 0:
        x = x / m * peak
    x = fade_out(x)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())


def pluck(freq, seconds=0.5, bright=0.5):
    n = int(SR * seconds)
    body = sine(freq, seconds) * env(n, 0.003, seconds * 0.45, 5)
    click = sine(freq * 4, seconds) * env(n, 0.001, 0.04, 6) * bright
    return body + click * 0.5


def note(semitones_from_a4):
    return 440.0 * 2 ** (semitones_from_a4 / 12)


def sfx_pop():
    n = int(SR * 0.14)
    tone = sweep(520, 880, 0.14, 0.5) * env(n, 0.002, 0.07, 5)
    click = highpass(noise(0.14), 3000) * env(n, 0.001, 0.012, 6) * 0.35
    return tone + click


def sfx_land():
    n = int(SR * 0.09)
    return sweep(190, 90, 0.09) * env(n, 0.002, 0.04, 5) + lowpass(noise(0.09), 900) * env(n, 0.001, 0.02, 5) * 0.4


def sfx_invalid():
    n = int(SR * 0.16)
    return (sweep(240, 150, 0.16) + 0.4 * sweep(360, 220, 0.16)) * env(n, 0.004, 0.08, 4)


def sfx_click():
    n = int(SR * 0.07)
    return sine(900, 0.07) * env(n, 0.001, 0.025, 6) + highpass(noise(0.07), 4000) * env(n, 0.001, 0.008, 6) * 0.3


def sfx_rocket():
    d = 0.45
    n = int(SR * d)
    whoosh = lowpass(highpass(noise(d), 500), 5000) * np.sin(np.pi * np.linspace(0, 1, n)) ** 0.6
    whistle = sweep(500, 1900, d, 0.7) * env(n, 0.02, 0.3, 2.5) * 0.3
    return whoosh * 0.8 + whistle


def sfx_bomb():
    d = 0.7
    n = int(SR * d)
    boom = sweep(150, 38, d, 0.5) * env(n, 0.002, 0.35, 3.5)
    crack = lowpass(noise(d), 2500) * env(n, 0.001, 0.12, 4) * 0.7
    return boom * 1.2 + crack


def sfx_disco():
    d = 0.9
    out = np.zeros(int(SR * d))
    for i, semi in enumerate([3, 7, 10, 15, 19, 22]):
        mix_at(out, pluck(note(semi), 0.45, 0.9), i * 0.07, 0.8)
    shimmer = highpass(noise(d), 6000) * env(len(out), 0.05, 0.5, 3) * 0.08
    return out + shimmer


def sfx_booster_create():
    d = 0.4
    out = np.zeros(int(SR * d))
    for i, semi in enumerate([7, 12, 19]):
        mix_at(out, pluck(note(semi), 0.3, 1.0), i * 0.05, 0.8)
    return out + sweep(700, 2200, d, 0.6) * env(len(out), 0.01, 0.2, 3) * 0.12


def sfx_box_hit():
    n = int(SR * 0.12)
    return lowpass(noise(0.12), 1800) * env(n, 0.001, 0.035, 5) + sweep(260, 150, 0.12) * env(n, 0.001, 0.05, 5) * 0.6


def sfx_box_break():
    d = 0.3
    n = int(SR * d)
    crack = lowpass(noise(d), 3200) * env(n, 0.001, 0.09, 4)
    out = crack + sweep(220, 90, d) * env(n, 0.001, 0.1, 4) * 0.7
    for i in range(4):
        mix_at(out, highpass(noise(0.03), 2500) * env(int(SR * 0.03), 0.001, 0.01, 5), 0.04 + i * 0.045 + rng.uniform(0, 0.01), 0.35)
    return out


def sfx_shuffle():
    d = 0.5
    n = int(SR * d)
    x = np.linspace(0, 1, n)
    return lowpass(highpass(noise(d), 700), 3500) * (np.sin(np.pi * x) ** 1.5) * (0.6 + 0.4 * np.sin(2 * np.pi * 7 * x))


def sfx_star():
    d = 0.6
    out = np.zeros(int(SR * d))
    mix_at(out, pluck(note(15), 0.55, 1.0), 0.0, 1.0)
    mix_at(out, pluck(note(22), 0.5, 1.0), 0.05, 0.7)
    mix_at(out, pluck(note(27), 0.45, 1.0), 0.09, 0.4)
    return out


def sfx_goal():
    d = 0.4
    out = np.zeros(int(SR * d))
    mix_at(out, pluck(note(10), 0.3, 0.8), 0.0, 0.9)
    mix_at(out, pluck(note(15), 0.3, 0.8), 0.09, 0.9)
    return out


def sfx_win():
    d = 1.7
    out = np.zeros(int(SR * d))
    melody = [(0.00, 3), (0.13, 7), (0.26, 10), (0.39, 15), (0.60, 12), (0.73, 15), (0.90, 19)]
    for at, semi in melody:
        mix_at(out, pluck(note(semi), 0.7, 1.0), at, 0.9)
        mix_at(out, pluck(note(semi - 12), 0.7, 0.3), at, 0.4)
    for semi in (3, 7, 10, 15):
        mix_at(out, pluck(note(semi), 0.75, 0.5), 0.92, 0.45)
    return out


def sfx_lose():
    d = 1.1
    out = np.zeros(int(SR * d))
    for at, semi in [(0.0, 5), (0.2, 3), (0.4, 0), (0.62, -4)]:
        mix_at(out, pluck(note(semi), 0.6, 0.3), at, 0.9)
        mix_at(out, pluck(note(semi - 12), 0.6, 0.1), at, 0.5)
    return out


def music_loop():
    bpm = 104
    beat = 60 / bpm
    bars = 16
    total = bars * 4 * beat
    out = np.zeros(int(SR * total) + SR)

    chords = {"C": [3, 7, 10], "Am": [0, 3, 7], "F": [-4, 0, 3], "G": [-2, 2, 5]}
    progression = ["C", "Am", "F", "G"] * 4
    patterns = [
        [0, 1, 2, 1, 0, 2, 1, 2],
        [0, 2, 1, 2, 0, 1, 2, 1],
    ]
    lead_scale = [3, 5, 7, 10, 12, 15, 17, 19]
    lead_rng = np.random.default_rng(3)

    for bar, name in enumerate(progression):
        chord = chords[name]
        start = bar * 4 * beat
        for b in (0, 2):
            f = note(chord[0] - 24)
            n = int(SR * beat * 1.8)
            bass = (sine(f, beat * 1.8) + 0.3 * sine(f * 2, beat * 1.8)) * env(n, 0.01, beat * 1.2, 3)
            mix_at(out, bass, start + b * beat, 0.5)
        pattern = patterns[(bar // 4) % 2]
        for i, idx in enumerate(pattern):
            octave = 12 if (i % 4 == 3) else 0
            mix_at(out, pluck(note(chord[idx] + octave), 0.5, 0.5), start + i * beat / 2, 0.32)
        if bar >= 8:
            for i in range(4):
                if lead_rng.random() < 0.6:
                    semi = lead_scale[lead_rng.integers(2, len(lead_scale))] + 12
                    mix_at(out, pluck(note(semi), 0.7, 0.9), start + i * beat + (beat / 2 if lead_rng.random() < 0.3 else 0), 0.2)
        for i in range(8):
            if i % 2 == 1:
                n = int(SR * 0.05)
                mix_at(out, highpass(noise(0.05), 6000) * env(n, 0.002, 0.02, 5), start + i * beat / 2, 0.05)

    loop_len = int(SR * total)
    out[:SR] += out[loop_len:loop_len + SR]
    return out[:loop_len]


def main():
    sfx = {
        "Pop": sfx_pop, "Land": sfx_land, "Invalid": sfx_invalid, "Click": sfx_click,
        "Rocket": sfx_rocket, "Bomb": sfx_bomb, "Disco": sfx_disco, "BoosterCreate": sfx_booster_create,
        "BoxHit": sfx_box_hit, "BoxBreak": sfx_box_break, "Shuffle": sfx_shuffle,
        "Star": sfx_star, "Goal": sfx_goal, "Win": sfx_win, "Lose": sfx_lose,
    }
    for name, fn in sfx.items():
        save(OUT / "SFX" / f"{name}.wav", fn())

    music = music_loop()
    m = np.max(np.abs(music))
    music = music / m * 0.7
    path = OUT / "Music" / "MainLoop.wav"
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((music * 32767).astype(np.int16).tobytes())
    print("done ->", OUT)


if __name__ == "__main__":
    main()
