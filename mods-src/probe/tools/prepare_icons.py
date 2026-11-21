"""Generate the deployable icon sizes from the master art in resources/png.

Sources (masters):
  resources/png/head.png            -> square avatar (top bar / ancient dialogue / multiplayer)
                                    -> map marker (portrait crop, 49:64)
  resources/png/38_i01_fix.png      -> character-select background (32:15)
  resources/png/38_i01_head.png     -> character-select side portrait (0.677)

Outputs go to resources/png/derived/ (git-ignored, consumed by tools/package.ps1 and the manual deploy).

Rules requested by the user:
  * aspect mismatch -> center crop (keep image centre aligned)
  * too large       -> normal downscale (Lanczos, no sharpening)
"""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image

ROOT = Path(r"D:\STS2")
SRC = ROOT / "resources" / "png"
OUT = SRC / "derived"

# vanilla reference sizes (measured from the game):
#   square avatar 85x85, map marker 49x64, select portrait 132x195, select bg 2560x1200
TARGETS = [
    # (source, output, target_size, mode)
    ("head.png", "character_icon_saria.png", (170, 170), "square"),
    ("head.png", "map_marker_saria.png", (98, 128), "portrait"),
    ("38_i01_head.png", "char_select_saria.png", (264, 390), "portrait"),
    ("38_i01_fix.png", "char_select_bg_saria.png", (1600, 750), "wide"),
]


def center_crop(image: Image.Image, target_ratio: float) -> Image.Image:
    width, height = image.size
    current_ratio = width / height
    if abs(current_ratio - target_ratio) < 1e-3:
        return image

    if current_ratio > target_ratio:
        new_width = round(height * target_ratio)
        left = (width - new_width) // 2
        return image.crop((left, 0, left + new_width, height))
    new_height = round(width / target_ratio)
    top = (height - new_height) // 2
    return image.crop((0, top, width, top + new_height))


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    for source_name, output_name, size, mode in TARGETS:
        source = SRC / source_name
        if not source.exists():
            print(f"missing source: {source}")
            return 1

        image = Image.open(source).convert("RGBA")
        before = image.size
        image = center_crop(image, size[0] / size[1])
        if image.size != size:
            image = image.resize(size, Image.LANCZOS)
        destination = OUT / output_name
        image.save(destination, optimize=True)
        print(f"{source_name} {before[0]}x{before[1]} -> {output_name} {size[0]}x{size[1]} "
              f"(cropped {image.size[0]}x{image.size[1]})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
