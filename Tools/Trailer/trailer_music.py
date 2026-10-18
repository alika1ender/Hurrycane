"""Synthesizes an original trailer score for the Hurrycane trailer (music.wav, 44.1 kHz stereo 16-bit).

Standard library only. 120 BPM (a beat is 0.5 s); the section times match edit.json:
  0-10.5   intro: impact on frame one, heartbeat then a driving pulse, booms on the title hits, riser into
  10.5     impact ("PREPARE.")
  11-21    groove A: kick, snare, hats, pulsing bass; riser into
  21       impact ("SURVIVE.")
  21.5-44.5 groove B: double-time hats and bass, stabs, fills; riser, then a hard stop
  45.5-51.5 tense pad under the ending
  51.5     final impact under the logo, ringing out to 60 s
Usage: python trailer_music.py <output folder>
"""
import array, math, os, random, sys

RATE, LENGTH = 44100, 60.0
N = int(RATE * LENGTH)
L = [0.0] * N
R = [0.0] * N
random.seed(7)
NOISE = [random.uniform(-1, 1) for _ in range(RATE * 3)]

def put(start, samples, gain=1.0, pan=0.0):
    i0 = int(start * RATE)
    gl, gr = gain * (1 - max(0, pan)), gain * (1 + min(0, pan))
    for k, v in enumerate(samples):
        i = i0 + k
        if 0 <= i < N:
            L[i] += v * gl; R[i] += v * gr

def kick(length=0.35):
    out, phase = [], 0.0
    for k in range(int(length * RATE)):
        t = k / RATE
        f = 45 + 95 * math.exp(-t * 28)
        phase += 2 * math.pi * f / RATE
        out.append(math.sin(phase) * math.exp(-t * 7.5) * (1.4 if t < .004 else 1))
    return out

def snare(length=0.25):
    out, prev = [], 0.0
    for k in range(int(length * RATE)):
        t = k / RATE
        n = NOISE[k]; hp = n - prev; prev = n
        out.append(hp * .55 * math.exp(-t * 16) + math.sin(2 * math.pi * 185 * t) * .5 * math.exp(-t * 25))
    return out

def hat(length=0.06, offset=0):
    out, prev = [], 0.0
    for k in range(int(length * RATE)):
        n = NOISE[(k + offset) % len(NOISE)]; hp = n - prev; prev = n
        out.append(hp * .35 * math.exp(-k / RATE * 70))
    return out

def bass(freq, length, drive=2.2):
    out, y = [], 0.0
    for k in range(int(length * RATE)):
        t = k / RATE
        saw = 2 * ((freq * t) % 1) - 1
        y += (saw - y) * 0.08                                  # one-pole low-pass
        env = min(1, t / .005) * math.exp(-t * 5)
        out.append(math.tanh((y + .6 * math.sin(2 * math.pi * freq * t)) * drive) * env * .55)
    return out

def impact(length=3.0, freq=41.0):
    out, y = [], 0.0
    for k in range(int(length * RATE)):
        t = k / RATE
        f = freq * (1 + .5 * math.exp(-t * 6))
        saw = (2 * ((f * t) % 1) - 1) + (2 * ((f * 1.007 * t) % 1) - 1)
        y += (saw - y) * 0.05
        body = math.tanh((y * 1.4 + math.sin(2 * math.pi * f * .5 * t) * 1.2) * 1.8) * math.exp(-t * 1.3)
        crack = NOISE[k % len(NOISE)] * math.exp(-t * 9) * .5
        out.append((body + crack) * .8)
    return out

def boom(length=1.2):
    return [math.sin(2 * math.pi * (38 + 30 * math.exp(-k / RATE * 12)) * k / RATE) * math.exp(-k / RATE * 3.5) * .9 for k in range(int(length * RATE))]

def riser(length):
    out, prev = [], 0.0
    phase = 0.0
    for k in range(int(length * RATE)):
        t = k / RATE; x = t / length
        n = NOISE[k % len(NOISE)]; hp = n - prev * (1 - x * .9); prev = n
        phase += 2 * math.pi * (180 + 1400 * x * x) / RATE
        out.append((hp * .35 + math.sin(phase) * .25) * x ** 2.2)
    return out

def drone(freqs, length, attack=1.5, release=1.0, gain=.18):
    out = []
    for k in range(int(length * RATE)):
        t = k / RATE
        env = min(1, t / attack) * min(1, (length - t) / release)
        v = sum(math.sin(2 * math.pi * f * t + i) + .35 * math.sin(2 * math.pi * f * 2.003 * t) for i, f in enumerate(freqs))
        out.append(v / len(freqs) * env * gain * (1 + .25 * math.sin(2 * math.pi * .7 * t)))
    return out

ROOTS = [55.0, 43.65, 65.41, 49.0]   # A, F, C, G (one per bar)
def root(t): return ROOTS[int(t // 2) % 4]

def groove(start, end, intense):
    t = start
    beat = 0
    while t < end - 1e-6:
        bar_pos = beat % 4
        put(t, kick(), .95)
        if intense and bar_pos in (1, 3): put(t + .375, kick(.2), .55)
        if bar_pos in (1, 3): put(t, snare(), .7)
        steps = 4 if intense else 2
        for s in range(steps):
            put(t + s * .5 / steps, hat(offset=beat * 97 + s * 31), .5 if s % 2 else .32, pan=.3)
        f = root(t)
        for s in range(4 if intense else 2):
            dt = .5 / (4 if intense else 2)
            put(t + s * dt, bass(f * (2 if intense and s % 2 else 1), dt * .95), .55)
        if intense and bar_pos == 0:
            put(t, bass(f * 4, .35, drive=3), .25, pan=-.3)          # stab on the bar
        t += .5; beat += 1

# Intro
# Loud from the first second: an impact on frame one, a heartbeat pulse that speeds up, then a driving low pulse.
put(0.0, impact(2.0, 41.0), .9)
put(0.0, drone([55, 82.4, 110.0], 10.6, attack=.4, release=.2, gain=.7))
for beat in (0.0, 1.0, 2.0):                                    # heartbeat: lub-dub once a second
    put(beat, kick(.3), .9); put(beat + .22, kick(.25), .6)
t = 3.0
while t < 10.5 - 1e-6:                                          # then a pulse on every beat with bass
    put(t, kick(), .85)
    put(t, bass(root(t), .45), .5)
    if t >= 7.5: put(t + .25, hat(offset=int(t * 100)), .45, pan=.3)
    t += .5
for hit in (3.0, 5.5, 7.5, 8.0, 8.5, 9.0): put(hit, boom(), 1.0)
put(8.5, riser(2.0), 1.0)
put(10.5, impact(2.5), 1.0)
# Groove A
groove(11.0, 21.0, intense=False)
put(19.0, riser(2.0), .85)
put(21.0, impact(2.0, 36.7), 1.0)
# Groove B
groove(21.5, 44.5, intense=True)
for fill in (29.0, 36.5):                                           # tom-ish fills before section turns
    for s in range(4): put(fill + s * .125, kick(.15), .6 - s * .05, pan=(-.4 + s * .25))
put(42.5, riser(2.0), 1.0)
# Ending pad, then the logo hit
put(45.5, drone([55, 65.41, 82.4, 110.0], 6.2, attack=3.0, release=.4, gain=.5))
put(51.5, impact(5.0, 36.7), 1.1)
put(51.5, drone([55, 82.4, 110.0, 164.8], 8.5, attack=.05, release=4.0, gain=.4))

peak = max(max(abs(v) for v in L), max(abs(v) for v in R)) or 1
scale = .9 / peak
out = array.array('h', [0] * (2 * N))
for i in range(N):
    out[2 * i] = int(math.tanh(L[i] * scale * 1.9) * 31000)       # driven harder into the soft clip: louder
    out[2 * i + 1] = int(math.tanh(R[i] * scale * 1.9) * 31000)
if sys.byteorder == 'big': out.byteswap()
import wave
w = wave.open(os.path.join(sys.argv[1], 'music.wav'), 'wb')
w.setnchannels(2); w.setsampwidth(2); w.setframerate(RATE); w.writeframes(out.tobytes()); w.close()
print('music.wav written, peak scale %.2f' % scale)
