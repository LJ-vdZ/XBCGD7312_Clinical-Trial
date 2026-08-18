import math, struct, wave, os, random

out_dir = os.path.join("Assets", "Resources", "Sounds")
os.makedirs(out_dir, exist_ok=True)

def write_wav(path, samples, sr=22050):
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        frames = b"".join(
            struct.pack("<h", max(-32767, min(32767, int(s * 32767)))) for s in samples
        )
        w.writeframes(frames)

def tone(freq, dur, sr=22050, vol=0.35, fade=0.02):
    n = int(sr * dur)
    fade_n = max(1, int(sr * fade))
    samples = []
    for i in range(n):
        env = 1.0
        if i < fade_n:
            env = i / fade_n
        if i > n - fade_n:
            env = (n - i) / fade_n
        samples.append(vol * env * math.sin(2 * math.pi * freq * (i / sr)))
    return samples

def noise_burst(dur, sr=22050, vol=0.2):
    n = int(sr * dur)
    return [vol * (1.0 - i / n) * (random.random() * 2 - 1) for i in range(n)]

def thud(dur=0.09, sr=22050):
    a = tone(90, dur, sr, vol=0.4)
    b = tone(55, dur, sr, vol=0.25)
    return [x * 0.7 + y * 0.3 for x, y in zip(a, b)]

sfx = {
    "Footstep.wav": thud(0.09),
    "Interact.wav": tone(660, 0.08) + tone(880, 0.06),
    "Equipment.wav": tone(420, 0.05) + tone(520, 0.08) + tone(360, 0.1),
    "Pickup.wav": tone(520, 0.05) + tone(780, 0.1),
    "Drop.wav": tone(300, 0.12, vol=0.3),
    "Success.wav": tone(523, 0.08) + tone(659, 0.08) + tone(784, 0.14),
    "Fail.wav": tone(220, 0.15, vol=0.35) + tone(180, 0.2, vol=0.3),
    "Purchase.wav": tone(880, 0.06) + tone(1175, 0.12),
    "Notification.wav": tone(740, 0.06) + tone(990, 0.1),
    "Clean.wav": noise_burst(0.15, vol=0.2),
    "OpenPanel.wav": tone(500, 0.05) + tone(700, 0.08),
    "ClosePanel.wav": tone(400, 0.08, vol=0.25),
}

for name, samples in sfx.items():
    path = os.path.join(out_dir, name)
    write_wav(path, samples)
    print("wrote", path)

print("done", len(sfx))
