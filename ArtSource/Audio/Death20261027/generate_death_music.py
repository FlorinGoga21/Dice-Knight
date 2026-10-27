"""Gentle main-menu-style death cue. Requires NumPy; never edits Unity metadata.

Uses the original menu's D-minor motif and flute, pluck, pad and bass synthesis,
arranged as a slower four-bar farewell with a resolving ending. Run --replace
only to deliberately regenerate this one clip. No external samples or imports
from the ignored local Tools folder are required.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[3]
DESTINATION = ROOT / "Assets/Audio/Music/MUS_Death_AshenOath.wav"
RATE = 44100
DURATION = 16.0
BPM = 88


def timeline(duration):
    return np.arange(max(2, round(duration * RATE)), dtype=np.float64) / RATE


def soften(signal, attack=0.003, release=0.015):
    result = np.array(signal, dtype=np.float64, copy=True)
    a = min(len(result) // 2, max(1, int(attack * RATE)))
    r = min(len(result) // 2, max(1, int(release * RATE)))
    attack_envelope = np.linspace(0, 1, a) ** 2
    release_envelope = np.linspace(1, 0, r) ** 2
    if result.ndim == 2:
        attack_envelope = attack_envelope[:, None]
        release_envelope = release_envelope[:, None]
    result[:a] *= attack_envelope
    result[-r:] *= release_envelope
    return result


def instrument(note, duration, kind):
    # Same timbres as KnightsFortune's original menu generator.
    t = timeline(duration)
    frequency = 440 * 2 ** ((note - 69) / 12)
    phase = 2 * np.pi * frequency * t
    if kind == "flute":
        vibrato = phase + 0.035 * np.sin(2 * np.pi * 4.6 * t)
        tone = np.sin(vibrato) + 0.16 * np.sin(2 * vibrato) + 0.045 * np.sin(3 * vibrato)
        tone *= np.exp(-1.7 * t / duration)
    elif kind == "pad":
        tone = sum(np.sin(phase * (1 + detune)) for detune in [-0.0018, 0, 0.0018]) / 3
        tone += 0.16 * np.sin(phase * 2)
        tone *= np.sin(np.pi * np.minimum(t / duration, 1)) ** 0.6
    elif kind == "bass":
        tone = np.sin(phase) + 0.2 * np.sin(phase * 2) + 0.08 * np.sin(phase * 3)
        tone *= np.exp(-3.2 * t / duration)
    elif kind == "pluck":
        tone = sum(np.sin(phase * h) * (0.48 ** (h - 1)) * np.exp(-t * (3 + 3 * h)) for h in range(1, 7))
    else:
        raise ValueError(kind)
    return soften(tone, 0.012 if kind in ("flute", "pad") else 0.002,
                  min(0.1, duration / 5) if kind == "pad" else 0.015)


def add(mix, signal, start, gain, pan=0):
    offset = round(start * RATE)
    signal = signal[:len(mix) - offset]
    angle = (pan + 1) * np.pi / 4
    mix[offset:offset + len(signal), 0] += signal * gain * np.cos(angle)
    mix[offset:offset + len(signal), 1] += signal * gain * np.sin(angle)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--replace", action="store_true")
    args = parser.parse_args()
    if DESTINATION.exists() and not args.replace:
        raise FileExistsError(f"Refusing to overwrite existing music: {DESTINATION}")

    beat = 60 / BPM
    root = 38
    scale = [0, 2, 3, 5, 7, 8, 10]
    motif = [0, 2, 4, 7, 5, 4, 2, 1]
    mix = np.zeros((round(DURATION * RATE), 2), dtype=np.float64)

    def degree(value):
        return root + 24 + scale[value % len(scale)] + 12 * (value // len(scale))

    # Menu's Dm, Bb and F harmonies, then return home for a gentle one-shot cadence.
    progression = [(0, "minor"), (8, "major"), (3, "major"), (0, "minor")]
    for bar, (offset, quality) in enumerate(progression):
        beginning = bar * 4 * beat
        chord_root = root + offset
        chord = [0, 3 if quality == "minor" else 4, 7]
        duration = beat * (7.0 if bar == 3 else 4.4)
        for i, interval in enumerate(chord):
            add(mix, instrument(chord_root + 12 + interval, duration, "pad"), beginning,
                0.07, -0.3 + i * 0.3)
        # Familiar plucked motion grows quieter and sparser toward the end.
        steps = 8 if bar < 2 else 4
        spacing = beat / 2 if bar < 2 else beat
        for step in range(steps):
            if bar == 3 and step > 2:
                continue
            interval = chord[[0, 1, 2, 1, 0, 2, 1, 2][step]]
            add(mix, instrument(chord_root + 12 + interval, beat * 0.95, "pluck"),
                beginning + step * spacing, 0.14 if bar < 2 else 0.11,
                -0.45 if step % 2 else 0.45)
        for step in range(1 if bar == 3 else 2):
            note = chord_root if step == 0 else chord_root + 7
            add(mix, instrument(note, beat * 2.0, "bass"), beginning + step * 2 * beat, 0.10)

        if bar < 2:
            phrase = [(step / 2, motif[(step + bar * 2) % len(motif)], 0.82) for step in range(8)]
        elif bar == 2:
            phrase = [(0, 4, 1.0), (1, 3, 1.0), (2, 2, 1.0), (3, 1, 1.05)]
        else:
            phrase = [(0, 2, 0.95), (1, 1, 0.95), (2, 0, 3.4)]
        for position, value, duration_beats in phrase:
            tone = instrument(degree(value), beat * duration_beats, "flute")
            when = beginning + position * beat
            add(mix, tone, when, 0.20 if bar < 2 else 0.17, -0.08)
            add(mix, tone, when + beat * 0.75, 0.032, 0.48)

    # A soft final D/A pluck supports the melody's last D, without a death toll.
    for delay, note in [(0.0, 62), (0.055, 69), (0.11, 74)]:
        add(mix, instrument(note, 3.0, "pluck"), 14 * beat + delay, 0.09, 0.05)
    dry = mix.copy()
    for delay, gain in [(beat * 0.75, 0.10), (beat * 1.5, 0.045)]:
        offset = round(delay * RATE)
        mix[offset:] += dry[:-offset, ::-1] * gain
    mix -= mix.mean(axis=0)
    mix = soften(mix, 0.035, 3.0)
    mix *= min(0.085 / np.sqrt(np.mean(mix ** 2)), 0.68 / np.max(np.abs(mix)))
    pcm = np.rint(mix * 32767).astype("<i2")
    assert np.isfinite(mix).all() and np.max(np.abs(mix)) < 0.69
    assert np.max(np.abs(pcm[0])) == 0 and np.max(np.abs(pcm[-1])) == 0
    DESTINATION.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(DESTINATION), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(pcm.tobytes())
    statistics = {
        "path": DESTINATION.relative_to(ROOT).as_posix(),
        "category": "Music", "loop": False, "duration": DURATION,
        "bpm": BPM, "style": "main-menu flute and plucks; gentle farewell",
        "channels": 2, "sampleRate": RATE,
        "peak": round(float(np.max(np.abs(mix))), 5),
        "rms": round(float(np.sqrt(np.mean(mix ** 2))), 5),
        "sha256": hashlib.sha256(DESTINATION.read_bytes()).hexdigest(),
    }
    Path(__file__).with_name("generation.json").write_text(json.dumps(statistics, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(statistics, indent=2))


if __name__ == "__main__":
    main()
