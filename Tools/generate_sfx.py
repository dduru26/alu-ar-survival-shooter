#!/usr/bin/env python3
"""
generate_sfx.py — procedurally synthesises the original sound effects used in
AR Survival Shooter (no third-party audio). Pure Python standard library.

Run from the project root:   python3 Tools/generate_sfx.py
Writes 44.1 kHz mono 16-bit WAV files into Assets/_Project/Audio/Generated/.

Sounds made here:
  enemy_spawn.wav   rising "materialise" sweep           (Enemy spawn)
  melee_hit.wav     low punch/thump with crunch          (Melee enemy attack hits player)
  enemy_hit.wav     short high blip                      (Player bullet hits enemy)
  enemy_death.wav   small burst + falling tone           (Enemy destroyed)
  player_death.wav  long falling tone + noise tail       (Player death)
  victory.wav       rising arpeggio                      (Survived the timer)
  ambient_loop.wav  quiet seamless low drone (8 s loop)  (Environmental ambience)
"""
import math
import os
import random
import struct
import wave

SR = 44100
OUT = os.path.join("Assets", "_Project", "Audio", "Generated")
random.seed(26)


def write(name, samples, peak=0.85):
    m = max(1e-9, max(abs(s) for s in samples))
    k = peak / m
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name), "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * k)) * 32767)) for s in samples))
    print(f"wrote {name}  ({len(samples) / SR:.2f}s)")


def env_ad(t, dur, attack=0.005):
    """Fast attack, linear decay to zero."""
    if t < attack:
        return t / attack
    return max(0.0, 1.0 - (t - attack) / max(1e-6, dur - attack))


def fade(samples, fin=0.005, fout=0.02):
    n_in, n_out = int(fin * SR), int(fout * SR)
    for i in range(min(n_in, len(samples))):
        samples[i] *= i / n_in
    for i in range(min(n_out, len(samples))):
        samples[-1 - i] *= i / n_out
    return samples


class Lowpass:
    def __init__(self, cutoff):
        self.a = 1 - math.exp(-2 * math.pi * cutoff / SR)
        self.y = 0.0

    def __call__(self, x):
        self.y += self.a * (x - self.y)
        return self.y


def sweep(f0, f1, dur, wave_fn=math.sin, curve=1.0):
    phase, out = 0.0, []
    n = int(dur * SR)
    for i in range(n):
        u = (i / n) ** curve
        f = f0 + (f1 - f0) * u
        phase += 2 * math.pi * f / SR
        out.append(wave_fn(phase))
    return out


def saw(p):
    return 2 * ((p / (2 * math.pi)) % 1.0) - 1


def square(p):
    return 1.0 if math.sin(p) >= 0 else -1.0


# ---------------------------------------------------------------- spawn
def enemy_spawn():
    dur = 0.55
    tone = sweep(180, 950, dur, curve=0.7)
    lp = Lowpass(2500)
    out = []
    for i, s in enumerate(tone):
        t = i / SR
        trem = 0.6 + 0.4 * math.sin(2 * math.pi * 22 * t)
        shimmer = lp(random.uniform(-1, 1)) * 0.5
        e = math.sin(math.pi * min(1, t / dur))          # swell in and out
        out.append((s * trem * 0.7 + shimmer) * e)
    return fade(out)


# ---------------------------------------------------------------- melee hit
def melee_hit():
    dur = 0.3
    thump = sweep(110, 40, dur, curve=0.4)
    lp = Lowpass(1800)
    out = []
    for i, s in enumerate(thump):
        t = i / SR
        body = s * math.exp(-t * 14)
        crack = lp(random.uniform(-1, 1)) * math.exp(-t * 60) * 1.6
        crunch = square(2 * math.pi * 130 * t) * math.exp(-t * 35) * 0.25
        out.append(body + crack + crunch)
    return fade(out, fout=0.03)


# ---------------------------------------------------------------- enemy hit
def enemy_hit():
    dur = 0.12
    tone = sweep(1400, 650, dur, curve=0.5)
    return fade([s * env_ad(i / SR, dur, 0.002) for i, s in enumerate(tone)])


# ---------------------------------------------------------------- enemy death
def enemy_death():
    dur = 0.5
    tone = sweep(420, 60, dur, wave_fn=saw, curve=0.6)
    lp = Lowpass(1200)
    out = []
    for i, s in enumerate(tone):
        t = i / SR
        boom = lp(random.uniform(-1, 1)) * math.exp(-t * 9) * 1.4
        out.append(s * 0.35 * math.exp(-t * 5) + boom)
    return fade(out, fout=0.05)


# ---------------------------------------------------------------- player death
def player_death():
    dur = 1.5
    phase, out = 0.0, []
    lp = Lowpass(900)
    for i in range(int(dur * SR)):
        t = i / SR
        f = 440 * (55 / 440) ** (t / dur) * (1 + 0.03 * math.sin(2 * math.pi * 7 * t))
        phase += 2 * math.pi * f / SR
        tone = saw(phase) * 0.5 + math.sin(phase * 0.5) * 0.4
        noise = lp(random.uniform(-1, 1)) * (0.6 if t < 0.25 else 0.25) * math.exp(-t * 2)
        out.append((tone + noise) * min(1, (dur - t) / 0.4))
    return fade(out, fout=0.1)


# ---------------------------------------------------------------- victory
def victory():
    notes = [523.25, 659.25, 783.99, 1046.5]   # C5 E5 G5 C6
    out = []
    for n, f in enumerate(notes):
        dur = 0.16 if n < 3 else 0.55
        for i in range(int(dur * SR)):
            t = i / SR
            s = math.sin(2 * math.pi * f * t) + 0.35 * math.sin(4 * math.pi * f * t)
            out.append(s * env_ad(t, dur, 0.004))
    return fade(out)


# ---------------------------------------------------------------- ambient loop
def ambient_loop():
    dur = 8.0                     # every frequency below completes whole cycles in 8 s → seamless loop
    out = []
    for i in range(int(dur * SR)):
        t = i / SR
        lfo = 0.65 + 0.35 * math.sin(2 * math.pi * 0.25 * t)
        s = (math.sin(2 * math.pi * 55 * t) * 0.6 +
             math.sin(2 * math.pi * 82.5 * t) * 0.3 +
             math.sin(2 * math.pi * 110 * t) * 0.12 * (0.5 + 0.5 * math.sin(2 * math.pi * 0.125 * t)))
        out.append(s * lfo)
    return out


if __name__ == "__main__":
    write("enemy_spawn.wav", enemy_spawn())
    write("melee_hit.wav", melee_hit())
    write("enemy_hit.wav", enemy_hit(), peak=0.6)
    write("enemy_death.wav", enemy_death())
    write("player_death.wav", player_death())
    write("victory.wav", victory(), peak=0.7)
    write("ambient_loop.wav", ambient_loop(), peak=0.35)
