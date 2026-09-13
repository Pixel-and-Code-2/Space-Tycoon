import os
import sys
import wave
import array
import math

def rms(frames, width):
    if width == 1:
        data = array.array("b", frames)
        vals = [v for v in data]
    elif width == 2:
        data = array.array("h", frames)
        vals = [v for v in data]
    else:
        return 0.0
    if not vals:
        return 0.0
    acc = 0.0
    for v in vals:
        acc += float(v) * float(v)
    return math.sqrt(acc / len(vals))

def loadWav(path):
    with wave.open(path, "rb") as w:
        params = w.getparams()
        frames = w.readframes(w.getnframes())
        return params, frames

def saveWav(path, params, frames):
    with wave.open(path, "wb") as w:
        w.setparams(params)
        w.writeframes(frames)

def scaleFrames(frames, width, gain):
    if width == 1:
        data = array.array("b", frames)
        for i in range(len(data)):
            v = int(data[i] * gain)
            if v > 127:
                v = 127
            if v < -128:
                v = -128
            data[i] = v
        return data.tobytes()
    if width == 2:
        data = array.array("h", frames)
        for i in range(len(data)):
            v = int(data[i] * gain)
            if v > 32767:
                v = 32767
            if v < -32768:
                v = -32768
            data[i] = v
        return data.tobytes()
    return frames

def collectWavs(folder):
    out = []
    for root, dirs, files in os.walk(folder):
        for name in files:
            if name.lower().endswith(".wav"):
                out.append(os.path.join(root, name))
    return out

if __name__ == "__main__":
    folder = "temp2"
    if len(sys.argv) > 1:
        folder = sys.argv[1]
    paths = collectWavs(folder)
    print("files", len(paths))
    levels = []
    loaded = []
    for path in paths:
        try:
            params, frames = loadWav(path)
            level = rms(frames, params.sampwidth)
            if level > 1.0:
                levels.append(level)
                loaded.append((path, params, frames, level))
                print("rms", level, path)
        except Exception as e:
            print("skip", path, e)
    if not levels:
        print("no wav levels (mp3 not supported here; convert to wav or extend with pydub)")
        raise SystemExit(0)
    avg = sum(levels) / float(len(levels))
    print("avg", avg)
    for path, params, frames, level in loaded:
        gain = avg / level
        newFrames = scaleFrames(frames, params.sampwidth, gain)
        saveWav(path, params, newFrames)
        print("normalized", path, "gain", gain)
