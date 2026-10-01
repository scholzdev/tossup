#!/usr/bin/env python3
"""Generate the sound effects as 16-bit mono WAV files with plain Python (no dependencies).

Output: assets/sfx/<name>.wav  and  assets/music/theme.wav (a 32 s chiptune loop)
  click, flip, land_heads, land_tails, score, penalty, combo, discard, buy, shop, levelup, win, lose
Run from the repo root:  python3 tools/gen_sounds.py
To change a sound: edit its function below and run again.
"""

import math
import random
import struct
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets" / "sfx"
RATE = 22050
random.seed(7)


def silence(seconds):
    return [0.0] * int(RATE * seconds)


def tone(freq, seconds, volume=0.6, shape="sine", decay=6.0, attack=0.004, end_freq=None, vibrato=0.0):
    """One note with an exponential decay. end_freq makes it a sweep; shape: sine | square | saw | tri."""
    n = int(RATE * seconds)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = freq if end_freq is None else freq + (end_freq - freq) * (i / n)
        f *= 1 + vibrato * math.sin(2 * math.pi * 6 * t)
        phase += 2 * math.pi * f / RATE
        if shape == "square":
            s = 1.0 if math.sin(phase) >= 0 else -1.0
            s *= 0.5
        elif shape == "saw":
            s = ((phase / (2 * math.pi)) % 1) * 2 - 1
            s *= 0.6
        elif shape == "tri":
            s = 2 * abs(((phase / (2 * math.pi)) % 1) * 2 - 1) - 1
        else:
            s = math.sin(phase)
        env = min(1.0, t / attack) * math.exp(-decay * t / seconds)
        out.append(s * env * volume)
    return out


def noise(seconds, volume=0.5, decay=8.0, lowpass=0.5):
    out, last = [], 0.0
    for i in range(int(RATE * seconds)):
        last += lowpass * (random.uniform(-1, 1) - last)  # one-pole low-pass: smaller = darker
        out.append(last * volume * math.exp(-decay * i / (RATE * seconds)))
    return out


def mix(*tracks):
    n = max(len(t) for t in tracks)
    out = [0.0] * n
    for track in tracks:
        for i, v in enumerate(track):
            out[i] += v
    return out


def seq(*parts):
    out = []
    for part in parts:
        out += part
    return out


def offset(track, seconds):
    return silence(seconds) + track


NOTE = {"C5": 523.25, "D5": 587.33, "E5": 659.25, "G5": 783.99, "A5": 880.0, "C6": 1046.5, "E6": 1318.5, "G6": 1568.0, "C7": 2093.0,
        "A4": 440.0, "E4": 329.63, "C4": 261.63, "G4": 392.0, "F4": 349.23, "D4": 293.66, "B4": 493.88}


def click():
    return mix(tone(1900, 0.03, 0.4, "square", decay=9), noise(0.015, 0.15, 12, 0.9))


def flip():
    # a rising whoosh with a few metallic ticks, like a coin leaving the thumb
    whoosh = tone(380, 0.55, 0.22, "tri", decay=2.0, end_freq=1500, vibrato=0.02)
    ticks = [offset(tone(2600 + 300 * i, 0.05, 0.25, "sine", decay=8), 0.08 + 0.1 * i) for i in range(4)]
    return mix(whoosh, noise(0.5, 0.08, 3, 0.35), *ticks)


def land_heads():
    return mix(tone(1318.5, 0.5, 0.5, decay=5), tone(1976.0, 0.4, 0.3, decay=6), tone(2637.0, 0.25, 0.15, decay=8),
               noise(0.02, 0.2, 10, 0.8))


def land_tails():
    return mix(tone(196, 0.3, 0.7, "sine", decay=7, end_freq=120), noise(0.12, 0.35, 12, 0.25), tone(520, 0.12, 0.2, "tri", decay=9))


def score():
    return seq(tone(NOTE["E5"], 0.09, 0.45, "square", decay=4), tone(NOTE["A5"], 0.2, 0.45, "square", decay=5))


def penalty():
    return mix(tone(150, 0.4, 0.5, "saw", decay=4, end_freq=70), noise(0.15, 0.2, 10, 0.3))


def combo():
    return mix(tone(NOTE["C6"], 0.12, 0.35, "tri", decay=5), tone(NOTE["G6"], 0.18, 0.25, "sine", decay=6))


def discard():
    return mix(noise(0.18, 0.4, 6, 0.6), tone(900, 0.18, 0.15, "tri", decay=6, end_freq=250))


def buy():
    return seq(tone(NOTE["E6"], 0.07, 0.4, "square", decay=3), mix(tone(NOTE["C7"], 0.35, 0.4, "square", decay=5),
                                                                  tone(NOTE["G6"], 0.35, 0.2, "sine", decay=5)))


def shop():
    notes = ["G5", "C6", "E6", "G6"]
    return seq(*[tone(NOTE[n], 0.1 if i < 3 else 0.35, 0.4, "tri", decay=4) for i, n in enumerate(notes)])


def levelup():
    notes = ["C5", "E5", "G5", "C6"]
    parts = [mix(tone(NOTE[n], 0.14 if i < 3 else 0.5, 0.4, "square", decay=3), tone(NOTE[n] / 2, 0.14 if i < 3 else 0.5, 0.25, "sine", decay=3))
             for i, n in enumerate(notes)]
    return seq(*parts)


def win():
    notes = [("C5", .12), ("E5", .12), ("G5", .12), ("C6", .12), ("G5", .1), ("C6", .1), ("E6", .6)]
    parts = [mix(tone(NOTE[n], d, 0.4, "square", decay=2.5), tone(NOTE[n] * 2, d, 0.12, "sine", decay=3)) for n, d in notes]
    return seq(*parts)


def lose():
    notes = [("G4", .22), ("E4", .22), ("C4", .22), ("A4", .12), ("F4", .7)]
    parts = [tone(NOTE[n], d, 0.5, "tri", decay=2.2, vibrato=0.01 if d > .5 else 0) for n, d in notes]
    return seq(*parts)


# ---------------------------------------------------------------- music
BPM = 120
BAR = 60 / BPM * 4  # seconds per 4/4 bar
CHORDS = [  # (bass, arpeggio notes, melody notes) in A minor: Am F C G
    (110.0, [220.0, 261.63, 329.63], [659.25, 523.25, 440.0]),
    (87.31, [174.61, 220.0, 261.63], [523.25, 440.0, 349.23]),
    (130.81, [261.63, 329.63, 392.0], [783.99, 659.25, 523.25]),
    (98.0, [196.0, 246.94, 293.66], [587.33, 493.88, 392.0]),
]
BARS = 16


def place(buffer, track, start):
    i = int(start * RATE)
    for k, v in enumerate(track):
        if i + k < len(buffer):
            buffer[i + k] += v


def music():
    buffer = [0.0] * int(RATE * BAR * BARS)
    beat = BAR / 4
    for bar in range(BARS):
        bass, arp, melody = CHORDS[bar % 4]
        t0 = bar * BAR
        for b in range(4):  # steady square bass on the beat, an octave jump on the off-beats
            place(buffer, tone(bass, beat * .9, 0.22, "square", decay=3), t0 + b * beat)
        for step in range(16):  # 16th-note arpeggio, softer in the first half of the piece
            note = arp[step % 3] * (2 if step % 8 >= 4 else 1)
            place(buffer, tone(note, beat / 4 * .9, 0.09 if bar < 8 else 0.12, "tri", decay=4), t0 + step * beat / 4)
        if bar >= 8:  # the melody enters for the second half
            for k, hit in enumerate((0, 1.5, 2.5)):
                place(buffer, tone(melody[k], beat * 1.4, 0.16, "square", decay=2.5), t0 + hit * beat)
        for b in range(8):  # quiet hi-hat
            place(buffer, noise(0.03, 0.05, 10, 0.95), t0 + b * beat / 2)
    return buffer


SOUNDS = [click, flip, land_heads, land_tails, score, penalty, combo, discard, buy, shop, levelup, win, lose]


def write(name, samples, folder=None):
    peak = max(1e-6, max(abs(s) for s in samples))
    gain = min(1.0, 0.9 / peak)  # normalise loud sounds down, keep quiet ones as authored
    with wave.open(str((folder or OUT) / f"{name}.wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * gain)) * 32767)) for s in samples))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for fn in SOUNDS:
        write(fn.__name__, fn())
    music_dir = ROOT / "assets" / "music"
    music_dir.mkdir(parents=True, exist_ok=True)
    write("theme", music(), music_dir)
    print(f"generated {len(SOUNDS)} sounds in {OUT} and the music loop")


if __name__ == "__main__":
    main()
