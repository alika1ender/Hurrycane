"""Builds mix.wav (44.1 kHz stereo 16-bit) for the Hurrycane trailer from edit.json and the game's own sounds.

edit.json: "beds" are looped backgrounds and top-level "sfx" one-shots, both on absolute times; each clip's "sfx" are one-shots relative to the clip's
start (clips play back to back). Standard library only. Usage: python trailer_mix.py <edit.json> <audio folder>
"""
import array, json, math, os, sys, wave

RATE = 44100

def load(folder, name, cache={}):
    if name in cache: return cache[name]
    local = os.path.join(os.path.dirname(os.path.abspath(sys.argv[1])), name + '.wav')   # e.g. music.wav next to edit.json
    w = wave.open(local if os.path.exists(local) else os.path.join(folder, name + '.wav'))
    assert w.getsampwidth() == 2 and w.getframerate() == RATE, name
    ch = w.getnchannels()
    data = array.array('h', w.readframes(w.getnframes()))
    if sys.byteorder == 'big': data.byteswap()
    left = [v / 32768.0 for v in data[0::ch]]
    right = [v / 32768.0 for v in data[ch - 1::ch]]
    cache[name] = (left, right)
    return cache[name]

def main():
    edit_path, folder = sys.argv[1], sys.argv[2]
    edit = json.load(open(edit_path, encoding='utf-8'))
    total = sum(c['dur'] for c in edit['clips'])
    n = int(round(total * RATE))
    L = [0.0] * n; R = [0.0] * n

    def add(name, start, gain, length=None, loop=False, end=None, fade_in=0.0, fade_out=0.0):
        left, right = load(folder, name)
        size = len(left)
        s0 = int(start * RATE)
        stop = int((end if end is not None else start + (length if length else size / RATE)) * RATE)
        stop = min(stop, n)
        fi, fo = max(1, int(fade_in * RATE)), max(1, int(fade_out * RATE))
        if length and not loop: fo = max(fo, int(0.03 * RATE))  # cut one-shots without a click
        span = stop - s0
        for i in range(max(0, -s0), span):
            j = i % size if loop else i
            if j >= size: break
            g = gain
            if i < fi: g *= i / fi
            if span - i < fo: g *= (span - i) / fo
            k = s0 + i
            L[k] += left[j] * g; R[k] += right[j] * g

    for bed in edit.get('beds', []):
        add(bed['file'], bed['start'], bed['gain'], loop=True, end=bed['end'], fade_in=bed.get('fadeIn', 0), fade_out=bed.get('fadeOut', 0))
    for fx in edit.get('sfx', []):                     # one-shots on absolute times (thunder through the whole trailer)
        add(fx['file'], fx['at'], fx['gain'], length=fx.get('len'))
    t = 0.0
    for clip in edit['clips']:
        for fx in clip.get('sfx', []):
            add(fx['file'], t + fx['at'], fx['gain'], length=fx.get('len'))
        t += clip['dur']

    # Soft limiter, then 16-bit.
    out = array.array('h', [0] * (2 * n))
    peak = 0.0
    for i in range(n):
        for c, v in ((0, L[i]), (1, R[i])):
            v = math.tanh(v * 1.2)   # soft limiter, driven for a loud trailer mix
            peak = max(peak, abs(v))
            out[2 * i + c] = int(max(-1.0, min(1.0, v)) * 32000)
    if sys.byteorder == 'big': out.byteswap()
    path = os.path.join(os.path.dirname(edit_path), 'mix.wav')
    w = wave.open(path, 'wb'); w.setnchannels(2); w.setsampwidth(2); w.setframerate(RATE); w.writeframes(out.tobytes()); w.close()
    print('mix.wav %.2f s, peak %.2f' % (total, peak))

main()
