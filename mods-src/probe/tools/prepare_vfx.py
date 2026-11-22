"""Normalise the custom energy-gain VFX textures into resources/png/derived/vfx/.

Accepts a few spelling variants (the game calls them fire_burst_*, hand-made files
may be fire_bust_*), and always writes the canonical vanilla names so the runtime
loader only has to know one spelling.
"""

from __future__ import annotations

import shutil
import sys
from pathlib import Path

ROOT = Path(r"D:\STS2")
SRC = ROOT / "resources" / "png" / "vfx"
OUT = ROOT / "resources" / "png" / "derived" / "vfx"

# canonical name -> candidate source names (first match wins)
TARGETS = {
    "fire_burst_flipbook_1.png": ["fire_burst_flipbook_1.png", "fire_bust_flipbook_1.png"],
    "fire_burst_flipbook_2.png": ["fire_burst_flipbook_2.png", "fire_bust_flipbook_2.png"],
    "fire_burst_flipbook_3.png": ["fire_burst_flipbook_3.png", "fire_bust_flipbook_3.png"],
    "fire_burst_ember_straight.png": ["fire_burst_ember_straight.png", "fire_bust_ember_straight.png"],
    "common_glow.png": ["common_glow.png"],
    "common_ring_polar_a.png": ["common_ring_polar_a.png"],
}


def main() -> int:
    if not SRC.exists():
        print(f"no vfx source dir: {SRC}")
        return 0

    OUT.mkdir(parents=True, exist_ok=True)
    found = 0
    for canonical, candidates in TARGETS.items():
        for name in candidates:
            candidate = SRC / name
            if candidate.exists():
                shutil.copyfile(candidate, OUT / canonical)
                print(f"{name} -> derived/vfx/{canonical}")
                found += 1
                break
        else:
            print(f"(missing) {canonical}")

    print(f"done: {found} file(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
