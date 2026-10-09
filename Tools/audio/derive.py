"""
Hearth & Hollows: derived sound effects (4i-C). Edits approved library sounds into game-ready clips, from the raw files in
C:\\Dev\\Music\\SFX (never changed), into Tools/audio/derived/. The game imports from there (see docs/ASSET_MAP.md and
docs/CREDITS.md). Run:  python Tools/audio/derive.py

  TapPourLoop.wav   OwlishMedia "tap-water-1.wav": a steady 1.6 s stretch of the running tap, crossfaded end into start so it
                    loops seamlessly (the tap's pour loop).
  GardenWater.wav   OwlishMedia "spray-bottle.wav": the first spray, from its onset, 0.5 s with a short fade out (tending a bed).
  fruit1-3, scrape1-2, clamour1-11 (.wav)
                    OwlishMedia's Impacts, peak-normalised to -1 dBFS (the 4i-C balance pass: as recorded they peak near -17 dBFS,
                    some 10 dB under the Kenney packs, too quiet to reach their level at full volume). Nothing else changes.
  EggFlip.wav       Leohpaz (Crafting and Professions II) "Flipping_Eggs_single.wav": its first flip, from the onset, 0.6 s with a
                    short fade out (the grill's flip; round 2, 2026-10-09).
"""
import os
import struct
import wave

SFX = r"C:\Dev\Music\SFX"
LEOHPAZ = r"C:\Dev\Music\Leohpaz SFX"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "derived")


def read(path):
    with wave.open(path, "rb") as w:
        ch, width, rate, n = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
        raw = w.readframes(n)
    if width == 2:
        data = struct.unpack("<%dh" % (n * ch), raw)
    elif width == 3:
        # 24-bit (the Leohpaz packs), brought to the 16-bit scale the rest of this script works in.
        data = [int.from_bytes(raw[i:i + 3], "little", signed=True) / 256.0 for i in range(0, len(raw), 3)]
    else:
        raise ValueError(f"{path}: {width * 8}-bit isn't handled")
    frames = [data[i:i + ch] for i in range(0, len(data), ch)]
    return frames, ch, rate


def write(path, frames, ch, rate):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(ch)
        w.setsampwidth(2)
        w.setframerate(rate)
        flat = [max(-32768, min(32767, int(round(v)))) for f in frames for v in f]
        w.writeframes(struct.pack("<%dh" % len(flat), *flat))


def loop(frames, start, length, fade):
    """frames[start : start+length], with its last `fade` frames crossfaded into the frames just before `start`, so the end runs
    into the beginning without a click."""
    body = [list(f) for f in frames[start:start + length]]
    lead = frames[start - fade:start]
    for i in range(fade):
        t = i / float(fade)
        j = length - fade + i
        body[j] = [body[j][c] * (1 - t) + lead[i][c] * t for c in range(len(body[j]))]
    return body


def onset(frames, threshold):
    for i, f in enumerate(frames):
        if max(abs(v) for v in f) >= threshold:
            return i
    return 0


def normalise(name, peak_db=-1.0):
    frames, ch, rate = read(os.path.join(SFX, "Impacts", name))
    peak = max(abs(v) for f in frames for v in f) or 1
    gain = (32767 * 10 ** (peak_db / 20.0)) / peak
    write(os.path.join(OUT, name), [[v * gain for v in f] for f in frames], ch, rate)


NORMALISED = ["fruit1.wav", "fruit2.wav", "fruit3.wav", "scrape1.wav", "scrape2.wav"] + ["clamour%d.wav" % i for i in range(1, 12)]


def trim_onset(path, out, seconds, fade_seconds, threshold=4000):
    frames, ch, rate = read(path)
    at = max(0, onset(frames, threshold) - int(0.01 * rate))
    clip = [list(f) for f in frames[at:at + int(seconds * rate)]]
    fade = int(fade_seconds * rate)
    for i in range(fade):
        j = len(clip) - fade + i
        clip[j] = [v * (1 - i / float(fade)) for v in clip[j]]
    write(os.path.join(OUT, out), clip, ch, rate)


def main():
    for name in NORMALISED:
        normalise(name)
    trim_onset(os.path.join(LEOHPAZ, "Minifantasy_CraftingAndProfessions2_SFX", "Crafting_Professions", "Cooking", "Flipping_Eggs_single.wav"),
               "EggFlip.wav", 0.6, 0.15)
    frames, ch, rate = read(os.path.join(SFX, "Water", "tap-water-1.wav"))
    pour = loop(frames, start=int(4.0 * rate), length=int(1.6 * rate), fade=int(0.12 * rate))
    write(os.path.join(OUT, "TapPourLoop.wav"), pour, ch, rate)

    frames, ch, rate = read(os.path.join(SFX, "Water", "spray-bottle.wav"))
    at = max(0, onset(frames, 6000) - int(0.01 * rate))
    burst = [list(f) for f in frames[at:at + int(0.5 * rate)]]
    fade = int(0.12 * rate)
    for i in range(fade):
        j = len(burst) - fade + i
        burst[j] = [v * (1 - i / float(fade)) for v in burst[j]]
    write(os.path.join(OUT, "GardenWater.wav"), burst, ch, rate)
    print("derived:", os.listdir(OUT))


if __name__ == "__main__":
    main()
