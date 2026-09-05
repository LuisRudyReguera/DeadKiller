"""
Genera los sonidos de placeholder del hito 1 en `audio/`.

ASSETS.md: ninguna tarea de código se bloquea esperando arte. Estos WAV son
provisionales y se sustituyen en el hito 6; existen para que el telegrafiado del
licántropo sea audible, que es criterio de cierre del hito 1.

Sin dependencias: solo biblioteca estándar. Reejecutable, resultado idéntico.

    python tools/placeholder-audio/make_sounds.py
"""

import math
import random
import struct
import wave
from pathlib import Path

RATE = 22050
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "audio"


def envelope(position, attack=0.1, release=0.35):
    """Rampa de entrada y caída para que no chasquee en los extremos."""
    if position < attack:
        return position / attack
    if position > 1.0 - release:
        return (1.0 - position) / release
    return 1.0


def write(name, samples):
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / name

    with wave.open(str(path), "w") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        handle.writeframes(b"".join(
            struct.pack("<h", max(-32767, min(32767, int(value * 32767))))
            for value in samples
        ))

    print(f"{path.relative_to(ROOT).as_posix()}  ({path.stat().st_size} bytes)")


def growl(duration=0.45):
    """Gruñido: dos armónicos graves que suben de tono, con algo de aspereza."""
    random.seed(1)
    total = int(RATE * duration)
    phase_low = 0.0
    phase_high = 0.0

    for index in range(total):
        position = index / total
        # Sube de 70 a 110 Hz: da sensación de que algo se prepara.
        frequency = 70.0 + 40.0 * position

        phase_low += 2.0 * math.pi * frequency / RATE
        phase_high += 2.0 * math.pi * frequency * 2.5 / RATE

        value = math.sin(phase_low) * 0.6 + math.sin(phase_high) * 0.2
        value += random.uniform(-1.0, 1.0) * 0.12       # aspereza
        value *= envelope(position, attack=0.08, release=0.30)

        yield value * 0.85


def swing(duration=0.18):
    """Silbido de la espada: ruido filtrado que barre de agudo a grave."""
    random.seed(2)
    total = int(RATE * duration)
    previous = 0.0

    for index in range(total):
        position = index / total
        noise = random.uniform(-1.0, 1.0)

        # Paso bajo de un polo cuyo corte baja con el tiempo: suena a barrido.
        alpha = 0.55 - 0.35 * position
        previous += alpha * (noise - previous)

        yield previous * envelope(position, attack=0.05, release=0.55) * 0.9


def impact(duration=0.14):
    """Golpe seco: click grave con caída rápida."""
    random.seed(3)
    total = int(RATE * duration)
    phase = 0.0

    for index in range(total):
        position = index / total
        phase += 2.0 * math.pi * (160.0 - 90.0 * position) / RATE

        value = math.sin(phase) * 0.7 + random.uniform(-1.0, 1.0) * 0.3
        value *= math.pow(1.0 - position, 2.2)          # caída percusiva

        yield value * 0.9


if __name__ == "__main__":
    write("werewolf_growl.wav", growl())
    write("sword_swing.wav", swing())
    write("hit_impact.wav", impact())
