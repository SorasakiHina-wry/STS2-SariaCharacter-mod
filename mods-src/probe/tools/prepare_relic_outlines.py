"""Generate relic outline silhouettes (white alpha) from the cropped small icons.

Vanilla uses a separate `relic_outline_atlas` where each relic is a **pure white silhouette**
(NRelic tints it with half-transparent black to draw the drop shadow behind the icon).
So we derive ours from the small icon's alpha: dilate a couple of pixels and fill white.
"""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(r"D:\STS2")
SRC = ROOT / "resources" / "png" / "relics" / "derived" / "cropped"
OUT = ROOT / "resources" / "png" / "relics" / "derived" / "outline"

DILATE_PX = 2


def main() -> int:
    if not SRC.exists():
        print(f"no source dir: {SRC}")
        return 1

    OUT.mkdir(parents=True, exist_ok=True)
    count = 0
    for source in sorted(SRC.glob("*.png")):
        image = Image.open(source).convert("RGBA")
        alpha = image.getchannel("A")

        # 膨胀：用 MaxFilter 放大 alpha 区域，得到比本体略大的剪影
        size = DILATE_PX * 2 + 1
        dilated = alpha.filter(ImageFilter.MaxFilter(size))

        outline = Image.new("RGBA", image.size, (255, 255, 255, 0))
        outline.putalpha(dilated)
        outline.paste((255, 255, 255), mask=dilated)

        destination = OUT / source.name
        outline.save(destination, optimize=True)
        count += 1
        print(f"{source.name} -> outline/{source.name}")

    print(f"done: {count} outline(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
