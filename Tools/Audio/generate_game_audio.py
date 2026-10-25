"""Original, deterministic retro-fantasy score and procedural sound design for Dice Knight.

Requires NumPy. Writes WAV assets and an Inspector assignment manifest, never Unity metadata.
Existing output files are preserved unless --replace-generated is explicitly requested.
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
AUDIO = ROOT / "Assets" / "Audio"
RATE = 44100
RNG = np.random.default_rng(20261025)
MANIFEST: dict = {"sampleRate": RATE, "music": [], "shared": [], "characters": [], "assets": []}
REPLACE = False


def midi(note: float) -> float:
    return 440 * 2 ** ((note - 69) / 12)


def timeline(seconds: float) -> np.ndarray:
    return np.arange(max(2, round(seconds * RATE)), dtype=np.float64) / RATE


def soften(signal: np.ndarray, attack: float = .003, release: float = .015) -> np.ndarray:
    result = np.array(signal, dtype=np.float64, copy=True)
    a = min(len(result) // 2, max(1, int(attack * RATE)))
    r = min(len(result) // 2, max(1, int(release * RATE)))
    result[:a] *= np.linspace(0, 1, a) ** 2
    result[-r:] *= np.linspace(1, 0, r) ** 2
    return result


def instrument(note: float, duration: float, kind: str = "pluck") -> np.ndarray:
    t = timeline(duration)
    frequency = midi(note)
    phase = 2 * np.pi * frequency * t
    if kind == "bell":
        tone = sum(gain * np.sin(phase * ratio) * np.exp(-t * decay)
                   for ratio, gain, decay in [(1, 1, 3.3), (2, .28, 6), (3.01, .12, 8), (4.2, .035, 12)])
    elif kind == "flute":
        vibrato = phase + .035 * np.sin(2 * np.pi * 4.6 * t)
        tone = (np.sin(vibrato) + .16 * np.sin(2 * vibrato) + .045 * np.sin(3 * vibrato))
        tone *= np.exp(-1.7 * t / max(duration, .01))
    elif kind == "pad":
        tone = sum(np.sin(phase * (1 + detune)) for detune in [-.0018, 0, .0018]) / 3
        tone += .16 * np.sin(phase * 2)
        tone *= np.sin(np.pi * np.minimum(t / duration, 1)) ** .6
    elif kind == "bass":
        tone = np.sin(phase) + .2 * np.sin(phase * 2) + .08 * np.sin(phase * 3)
        tone *= np.exp(-3.2 * t / duration)
    elif kind == "pulse":
        tone = sum(np.sin(phase * h) * math.sin(math.pi * h * .28) / h
                   for h in range(1, min(13, max(2, int(RATE / frequency / 2)))))
        tone *= np.exp(-4.2 * t / duration)
    elif kind == "mallet":
        tone = np.sin(phase) * np.exp(-9 * t) + .22 * np.sin(phase * 3.98) * np.exp(-24 * t)
    else:
        tone = sum(np.sin(phase * h) * (.48 ** (h - 1)) * np.exp(-t * (3 + 3 * h)) for h in range(1, 7))
    return soften(tone, .012 if kind in ("flute", "pad") else .002,
                  min(.1, duration / 5) if kind == "pad" else .015)


def noise(duration: float, decay: float = 12, smoothing: int = 3) -> np.ndarray:
    t = timeline(duration)
    value = RNG.uniform(-1, 1, len(t))
    if smoothing > 1:
        value = np.convolve(value, np.ones(smoothing) / smoothing, mode="same") * math.sqrt(smoothing)
    return soften(value * np.exp(-decay * t), .001, .008)


def sweep(start: float, end: float, duration: float, grit: float = 0) -> np.ndarray:
    t = timeline(duration)
    frequency = start * (end / start) ** (t / duration)
    phase = np.cumsum(frequency) * (2 * np.pi / RATE)
    tone = np.sin(phase + grit * np.sin(phase * 1.51)) * np.exp(-t * 3 / duration)
    return soften(tone, .002, min(.04, duration / 4))


def add(buffer: np.ndarray, signal: np.ndarray, at: float, gain: float = 1, pan: float = 0,
        wrap: bool = False) -> None:
    start = round(at * RATE)
    positions = np.arange(len(signal)) + start
    if wrap:
        positions %= len(buffer)
    else:
        keep = (positions >= 0) & (positions < len(buffer))
        signal, positions = signal[keep], positions[keep]
    if buffer.ndim == 1:
        np.add.at(buffer, positions, signal * gain)
    else:
        left, right = math.cos((pan + 1) * math.pi / 4), math.sin((pan + 1) * math.pi / 4)
        np.add.at(buffer[:, 0], positions, signal * gain * left)
        np.add.at(buffer[:, 1], positions, signal * gain * right)


def save(relative: str, signal: np.ndarray, category: str, target_rms: float = .13,
         loop: bool = False) -> str:
    path = AUDIO / relative
    if path.exists() and not REPLACE:
        raise FileExistsError(f"Refusing to replace existing audio: {path}")
    signal = signal - signal.mean(axis=0)
    if not loop:
        signal = soften(signal, .002, .018)
    rms = float(np.sqrt(np.mean(signal ** 2)))
    if rms < 1e-8 or not np.isfinite(signal).all():
        raise ValueError(f"Silent or invalid synthesis: {relative}")
    signal *= target_rms / rms
    peak = float(np.abs(signal).max())
    if peak > .82:
        signal *= .82 / peak
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.rint(signal * 32767).astype("<i2")
    with wave.open(str(path), "wb") as output:
        output.setnchannels(1 if signal.ndim == 1 else signal.shape[1])
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(pcm.tobytes())
    asset_path = path.relative_to(ROOT).as_posix()
    MANIFEST["assets"].append({"path": asset_path, "category": category, "loop": loop,
        "duration": round(len(signal) / RATE, 4), "peak": round(float(np.abs(signal).max()), 5),
        "rms": round(float(np.sqrt(np.mean(signal ** 2))), 5), "channels": 1 if signal.ndim == 1 else signal.shape[1]})
    return asset_path


def score(field: str, title: str, bpm: int, root: int, scale: list[int], style: str,
          progression: list[tuple[int, str]], motif: list[int]) -> None:
    beat = 60 / bpm
    bars = 16
    length = bars * 4 * beat
    buffer = np.zeros((round(length * RATE), 2), dtype=np.float64)
    vigorous = style in ("combat", "boss")
    lead = {"menu": "flute", "map": "pluck", "room": "flute", "combat": "pulse",
            "boss": "pulse", "shop": "mallet", "treasure": "bell", "event": "bell"}[style]

    def degree(value: int) -> int:
        return root + 24 + scale[value % len(scale)] + 12 * (value // len(scale))

    for bar in range(bars):
        offset, quality = progression[bar % len(progression)]
        chord_root = root + offset
        chord = [0, 3 if quality == "minor" else 4, 7]
        beginning = bar * 4 * beat
        # Soft sustained harmony; octave placement leaves space for the melody.
        for i, interval in enumerate(chord):
            add(buffer, instrument(chord_root + 12 + interval, beat * 4.4, "pad"), beginning,
                .075 if vigorous else .11, -.3 + i * .3, wrap=True)
        # Plucked accompaniment, alternating hand/pan, with a different inversion each phrase.
        for step in range(8):
            note = chord_root + 12 + chord[[0, 1, 2, 1, 0, 2, 1, 2][step]]
            if bar % 4 == 3 and step >= 4:
                note += 12
            add(buffer, instrument(note, beat * .82, "pluck" if style != "treasure" else "bell"),
                beginning + step * beat / 2, .09 if style == "room" else .13,
                -.45 if step % 2 else .45, wrap=True)
        # Bass pulse: resting exploration versus driving combat ostinato.
        for step in range(4 if vigorous else 2):
            when = step if vigorous else step * 2
            note = chord_root if step % 2 == 0 else chord_root + 7
            add(buffer, instrument(note, beat * (0.8 if vigorous else 1.6), "bass"),
                beginning + when * beat, .23 if vigorous else .15, 0, wrap=True)
        # Original four-phrase tune with a response and a cadence, rather than random beeps.
        for step in range(8):
            if style == "room" and (bar % 4 in (1, 3) or step % 3 == 1):
                continue
            if style == "treasure" and step % 2:
                continue
            index = motif[(step + (bar % 4) * 2) % len(motif)]
            if 4 <= bar % 8 < 6:
                index += 2
            if bar % 8 == 7:
                index = [4, 3, 2, 1, 0, 2, 1, 0][step]
            if bar == 15 and step >= 4:
                index = 0
            note = degree(index)
            duration = beat * (1.4 if style == "treasure" else .46 if vigorous else .74)
            if style == "boss":
                note -= 12 if bar % 4 < 2 else 0
            when = beginning + step * beat / 2 + (.045 * beat if style == "shop" and step % 2 else 0)
            tone = instrument(note, duration, lead)
            add(buffer, tone, when, .18 if vigorous else .20, -.08, wrap=True)
            add(buffer, tone, when + beat * .75, .035, .48, wrap=True)
        if style != "treasure":
            for step in range(4):
                when = beginning + step * beat
                if step % 2 == 0:
                    add(buffer, sweep(135, 43, .18), when, .23 if vigorous else .105, 0, wrap=True)
                elif style not in ("room", "event"):
                    snare = noise(.13, 27, 2) + .22 * sweep(230, 100, .13)
                    add(buffer, snare, when, .12 if vigorous else .045, .15, wrap=True)
            if style not in ("room", "event"):
                for step in range(8):
                    add(buffer, noise(.045, 85, 1), beginning + step * beat / 2,
                        .025 if not vigorous else .045, -.35, wrap=True)
            if vigorous and bar % 4 == 3:
                for step in range(4):
                    add(buffer, sweep(170 - step * 20, 65, .15), beginning + (3 + step / 4) * beat,
                        .14, (step - 1.5) / 4, wrap=True)
    # Circular ambience: all tails and echoes wrap into the start for uninterrupted looping.
    buffer += .10 * np.roll(buffer[:, ::-1], round(beat * .75 * RATE), axis=0)
    path = save(f"Music/MUS_{title}.wav", buffer, "Music", .105 if not vigorous else .12, True)
    MANIFEST["music"].append({"field": field, "path": path, "bpm": bpm, "bars": bars})


def chime(notes: list[int], spacing: float = .09, tone: str = "bell", decay: float = .35) -> np.ndarray:
    output = np.zeros(round((spacing * (len(notes) - 1) + decay + .04) * RATE))
    for i, note in enumerate(notes):
        add(output, instrument(note, decay, tone), i * spacing, .75 if i < len(notes) - 1 else 1)
    return output


def metal(duration: float = .4, pitch: float = 300) -> np.ndarray:
    t = timeline(duration)
    tone = sum(np.sin(2 * np.pi * pitch * ratio * t) * gain * np.exp(-decay * t)
               for ratio, gain, decay in [(1, .7, 13), (1.49, .45, 18), (2.1, .25, 23), (2.73, .15, 32)])
    return soften(tone + .3 * noise(duration, 40, 1))


def rattle(count: int = 11, duration: float = .65) -> np.ndarray:
    output = np.zeros(round(duration * RATE))
    for i in range(count):
        when = .008 + (duration - .12) * (i / max(1, count - 1)) ** .82
        hit = noise(.075, 70, 3) + .4 * instrument(60 + int(RNG.integers(0, 14)), .075, "mallet")
        add(output, hit, when, .5 + .35 * RNG.random())
    return output


def growl(duration: float, pitch: float, fall: float, slime: bool = False) -> np.ndarray:
    t = timeline(duration)
    frequency = pitch * (fall / pitch) ** (t / duration) * (1 + .05 * np.sin(2 * np.pi * 21 * t))
    phase = 2 * np.pi * np.cumsum(frequency) / RATE
    sound = np.sin(phase + (2.2 if slime else 1.3) * np.sin(phase * 2.03))
    sound *= (.65 + .35 * np.sin(2 * np.pi * (18 if slime else 37) * t) ** 2)
    sound *= np.exp(-t * 3 / duration)
    return soften(sound + noise(duration, 5 / duration, 14) * (.13 if slime else .32), .012, .07)


def burst(duration: float, bright: bool = False) -> np.ndarray:
    t = timeline(duration)
    air = noise(duration, 5 / duration, 2 if bright else 12)
    phase = 2 * np.pi * np.cumsum(130 * (1 + 3 * t / duration)) / RATE
    return soften(air * (np.sin(np.pi * t / duration) ** .7) + .2 * np.sin(phase) * np.exp(-t * 9), .01, .04)


def shared_sounds() -> None:
    definitions = [
        ("UI", "uiFocus", "Focus", lambda: chime([84], decay=.045), .065),
        ("UI", "uiConfirm", "Confirm", lambda: chime([74, 81], .045, decay=.16), .10),
        ("UI", "uiBack", "Back", lambda: chime([79, 72], .055, decay=.13), .085),
        ("UI", "settingChanged", "SettingTick", lambda: instrument(79, .035, "mallet"), .055),
        ("UI", "pause", "Pause", lambda: chime([74, 69, 65], .045, decay=.2), .09),
        ("UI", "resume", "Resume", lambda: chime([65, 69, 74], .045, decay=.2), .09),
        ("UI", "saveSuccess", "SaveSuccess", lambda: chime([74, 77, 81, 86], .07, decay=.4), .11),
        ("UI", "loadSuccess", "LoadSuccess", lambda: chime([69, 74, 81, 86], .08, "pluck", .45), .11),
        ("UI", "deleteSave", "DeleteSave", lambda: chime([65, 60, 53], .075, "mallet", .23), .10),
        ("Combat", "encounterStarted", "EncounterStart", lambda: chime([50, 57, 62], .075, "pulse", .5), .16),
        ("Combat", "encounterWon", "EncounterVictory", lambda: chime([62, 65, 69, 74, 77], .105, "bell", .65), .14),
        ("Combat", "turnStarted", "YourTurn", lambda: chime([74, 81], .055, "mallet", .19), .095),
        ("Combat", "diceRoll", "DiceWoodRattle", lambda: rattle(), .14),
        ("Combat", "diceSettled", "DiceSettle", lambda: rattle(3, .24), .15),
        ("Combat", "rollWin", "RollWin", lambda: chime([74, 77, 81], .055, decay=.25), .12),
        ("Combat", "rollLose", "RollLose", lambda: chime([65, 62, 57], .06, "mallet", .24), .12),
        ("Combat", "rollTie", "RollTie", lambda: chime([69, 69], .09, "pluck", .18), .10),
        ("Combat", "blockRaised", "ShieldRaise", lambda: metal(.23, 210), .14),
        ("Combat", "blockImpact", "ShieldImpact", lambda: metal(.48, 290), .19),
        ("Combat", "healingPotion", "PotionBubbles", potion, .13),
        ("World", "coinPickup", "CoinPickup", lambda: chime([88, 93], .045, decay=.18), .09),
        ("World", "mapTravel", "MapTravel", lambda: chime([62, 69, 74], .09, "pluck", .25), .10),
        ("World", "roomEntered", "RoomEntered", lambda: chime([50, 57, 62], .12, "pluck", .42), .11),
        ("World", "roomCompleted", "RoomComplete", lambda: chime([62, 69, 74, 81], .095, decay=.55), .13),
    ]
    for folder, field, name, create, loudness in definitions:
        path = save(f"SFX/{folder}/SFX_{name}.wav", create(), folder, loudness)
        MANIFEST["shared"].append({"field": field, "path": path})


def potion() -> np.ndarray:
    output = np.zeros(round(.8 * RATE))
    for i in range(9):
        add(output, sweep(300 + i * 55, 650 + i * 90, .09, 1.6), i * .06, .35)
    add(output, chime([81, 86, 89], .08, decay=.25), .30, .4)
    return output


def footstep(actor: str, variation: int) -> np.ndarray:
    duration = .17 if actor != "Zombie" else .24
    output = np.zeros(round(duration * RATE))
    if actor == "Knight":
        add(output, noise(.11, 40, 7), 0, .65)
        add(output, metal(.13, 470 + variation * 35), .018, .24)
        add(output, sweep(110, 52, .09), 0, .45)
    elif actor == "Skeleton":
        for i in range(3):
            add(output, instrument(75 + variation + i * 5, .065, "mallet"), i * .023, .65 - i * .1)
        add(output, noise(.09, 65, 3), 0, .25)
    elif actor == "Zombie":
        add(output, noise(.21, 17, 13), .006, .7)
        add(output, sweep(90 + variation * 6, 37, .15), 0, .55)
    else:
        add(output, growl(.14, 145 + variation * 14, 55, True), 0, .75)
        add(output, noise(.12, 28, 14), .016, .25)
    return output


def character_sound(actor: str, field: str) -> np.ndarray:
    if actor == "Knight":
        if field == "jump": return sweep(175, 560, .22)
        if field == "land": return footstep(actor, 1) + .4 * sweep(120, 44, .17)
        if field == "attack":
            out = np.zeros(round(.32 * RATE)); add(out, burst(.25, True), 0, .65); add(out, metal(.2, 620), .06, .3); return out
        if field == "hurt": return growl(.27, 145, 77)
        if field == "death": return growl(.7, 145, 38)
    if actor == "Skeleton":
        if field == "attack":
            out = np.zeros(round(.29 * RATE)); add(out, burst(.23, True), 0, .65); add(out, metal(.18, 740), .04, .35); return out
        if field == "hurt": return rattle(4, .23)
        if field == "death": return rattle(14, .8)
    if actor == "Zombie":
        if field == "attack": return growl(.45, 96, 66) + burst(.45) * .5
        if field == "hurt": return growl(.42, 112, 49)
        if field == "death": return growl(1.05, 94, 27)
    if actor == "Slime":
        if field == "attack": return growl(.30, 170, 63, True)
        if field == "hurt": return growl(.24, 280, 95, True)
        if field == "death":
            out = growl(.7, 155, 35, True); add(out, noise(.48, 9, 22), .10, .4); return out
    if actor == "DiceDemon":
        if field == "attack": return growl(.42, 83, 120)
        if field == "hurt": return growl(.37, 155, 67)
        if field == "death":
            out = growl(1.25, 93, 24); add(out, burst(.75), .25, .5); return out
        if field == "projectileLaunch":
            out = burst(.48, True); add(out, sweep(150, 690, .38, 1.8), .02, .5); return out
        if field == "projectileImpact":
            out = burst(.65); add(out, sweep(170, 32, .45, .8), 0, .8); return out
    raise ValueError((actor, field))


def characters() -> None:
    for actor in ["Knight", "Skeleton", "Zombie", "Slime", "DiceDemon"]:
        fields = {}
        if actor != "DiceDemon":
            fields["footsteps"] = [save(f"SFX/Characters/{actor}/SFX_{actor}_Footstep_{i+1:02d}.wav",
                footstep(actor, i), actor, .105) for i in range(4)]
        active = ["attack", "hurt", "death"]
        if actor == "Knight": active = ["jump", "land"] + active
        if actor == "DiceDemon": active += ["projectileLaunch", "projectileImpact"]
        for field in active:
            name = field[0].upper() + field[1:]
            fields[field] = save(f"SFX/Characters/{actor}/SFX_{actor}_{name}.wav",
                character_sound(actor, field), actor, .15 if field != "death" else .17)
        prefab = "Assets/Art/Player/Knight.prefab" if actor == "Knight" else f"Assets/Prefabs/Enemies/{actor}.prefab"
        MANIFEST["characters"].append({"prefab": prefab, "fields": fields})


def main() -> None:
    global REPLACE
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--replace-generated", action="store_true")
    args = parser.parse_args()
    REPLACE = args.replace_generated
    # Preflight before writing anything: all generated files use these reserved prefixes.
    existing = list(AUDIO.glob("**/MUS_*.wav")) + list(AUDIO.glob("**/SFX_*.wav"))
    if existing and not REPLACE:
        raise FileExistsError("Generated output already exists; inspect it before explicitly regenerating.")
    minor = [0, 2, 3, 5, 7, 8, 10]
    major = [0, 2, 4, 5, 7, 9, 11]
    score("mainMenuMusic", "MainMenu_KnightsFortune", 96, 38, minor, "menu",
          [(0, "minor"), (8, "major"), (3, "major"), (10, "major")], [0, 2, 4, 7, 5, 4, 2, 1])
    score("mapMusic", "Map_ForkedPaths", 104, 38, [0, 2, 3, 5, 7, 9, 10], "map",
          [(0, "minor"), (5, "major"), (10, "major"), (0, "minor")], [0, 4, 2, 3, 4, 6, 5, 2])
    score("roomMusic", "Forest_WhisperingBoughs", 84, 38, minor, "room",
          [(0, "minor"), (5, "minor"), (8, "major"), (10, "major")], [0, 2, 1, 4, 3, 2, 6, 4])
    score("combatMusic", "Combat_RollOfSteel", 136, 38, minor, "combat",
          [(0, "minor"), (0, "minor"), (8, "major"), (10, "major")], [0, 0, 4, 2, 0, 4, 5, 4])
    score("bossMusic", "Boss_DemonsWager", 148, 40, [0, 1, 3, 5, 7, 8, 10], "boss",
          [(0, "minor"), (1, "major"), (8, "major"), (7, "minor")], [0, 1, 0, 4, 3, 1, 5, 4])
    score("shopMusic", "Shop_CopperAndCloth", 112, 36, major, "shop",
          [(0, "major"), (5, "major"), (9, "minor"), (7, "major")], [0, 2, 4, 2, 5, 4, 1, 2])
    score("treasureMusic", "Treasure_GlimmeringChest", 88, 41, major, "treasure",
          [(0, "major"), (9, "minor"), (5, "major"), (7, "major")], [0, 4, 7, 4, 2, 6, 5, 4])
    score("eventMusic", "Event_StrangeCrossroads", 108, 45, [0, 2, 3, 5, 7, 8, 11], "event",
          [(0, "minor"), (5, "minor"), (8, "major"), (7, "major")], [0, 1, 4, 2, 6, 4, 3, 1])
    shared_sounds()
    characters()
    AUDIO.mkdir(parents=True, exist_ok=True)
    (AUDIO / "AudioManifest.json").write_text(json.dumps(MANIFEST, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"assets": len(MANIFEST["assets"]), "music": len(MANIFEST["music"]),
        "sharedEffects": len(MANIFEST["shared"]), "characters": len(MANIFEST["characters"]),
        "totalSeconds": round(sum(x["duration"] for x in MANIFEST["assets"]), 2)}, indent=2))


if __name__ == "__main__":
    main()
