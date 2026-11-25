"""prepare_icons.py —— 用「成品名」作为源（2026-11-25 调整）。

源文件（resources/png/）与目标尺寸：
  character_icon_saria.png  170×170   （顶栏 / 先古 / 多人头像）
  char_select_saria.png     264×390   （选人栏头像）
  char_select_bg_saria.png  1600×750  （选人背景）
  map_marker_saria.png       98×128   （地图标记）

输出统一写到 resources/png/derived/（打包脚本读这里）。
源图已经是目标尺寸时只做居中裁切 + 缩放（无损失）。

用法：
    python tools/prepare_icons.py
"""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[4]           # 仓库根 D:\STS2
MOD_ROOT = ROOT / "saria"                              # 角色根 D:\STS2\saria
SRC = MOD_ROOT / "resources" / "png"
OUT = SRC / "derived"

SUNDRY = "sundry"

TARGETS = [
    (f"{SUNDRY}/character_icon_saria.png", "character_icon_saria.png", (170, 170)),
    (f"{SUNDRY}/char_select_saria.png", "char_select_saria.png", (264, 390)),
    (f"{SUNDRY}/char_select_bg_saria.png", "char_select_bg_saria.png", (1600, 750)),
    (f"{SUNDRY}/map_marker_saria.png", "map_marker_saria.png", (98, 128)),
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
    for source_name, output_name, size in TARGETS:
        source = SRC / source_name
        if not source.exists():
            print(f"missing source: {source}")
            return 1

        image = Image.open(source).convert("RGBA")
        before = image.size
        image = center_crop(image, size[0] / size[1])
        if image.size != size:
            image = image.resize(size, Image.LANCZOS)
        image.save(OUT / output_name, optimize=True)
        print(f"{source_name} {before[0]}x{before[1]} -> derived/{output_name} {size[0]}x{size[1]}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
