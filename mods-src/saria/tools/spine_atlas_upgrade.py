#!/usr/bin/env python3
"""把 Spine 3.x 的 .atlas 转成 4.x 写法（游戏运行时是 4.2.43）。

4.x 规范（https://en.esotericsoftware.com/spine-atlas-format）：
    页头   name.png / size / format / filter / repeat / pma      ← 3.x 与 4.x 相同
    区域   bounds: x,y,w,h     ← 3.x 是 xy: x,y + size: w,h
           offsets: ox,oy,ow,oh ← 3.x 是 offset: ox,oy + orig: ow,oh
           rotate: 90 / true / false

3.x 的 rotate: true 在 4.x 里依然合法（true = 逆时针 90 度），所以只改几何key。

    python tools/spine_atlas_upgrade.py in.atlas [out.atlas]
"""
from __future__ import annotations

import re
import sys
from pathlib import Path


def upgrade(text: str) -> str:
    out: list[str] = []
    xy = size = orig = offset = rotate = None

    def flush() -> None:
        nonlocal xy, size, orig, offset, rotate
        if xy and size:
            x, y = xy
            w, h = size
            out.append(f"  bounds:{x},{y},{w},{h}")
        if orig or offset:
            ox, oy = offset if offset else (0, 0)
            ow, oh = orig if orig else (0, 0)
            out.append(f"  offsets:{ox},{oy},{ow},{oh}")
        if rotate is not None:
            out.append(f"  rotate:{'90' if rotate else 'false'}")
        xy = size = orig = offset = rotate = None

    def nums(value: str) -> tuple[int, ...]:
        return tuple(int(float(v)) for v in value.replace(" ", "").split(",") if v != "")

    for raw in text.splitlines():
        line = raw.rstrip()
        stripped = line.strip()

        m = re.match(r"^xy:\s*(.+)$", stripped)
        if m:
            xy = nums(m.group(1))
            continue
        m = re.match(r"^size:\s*(.+)$", stripped)          # 区域里的 size（缩进的）
        if m and line.startswith((" ", "\t")):
            size = nums(m.group(1))
            continue
        m = re.match(r"^orig:\s*(.+)$", stripped)
        if m:
            orig = nums(m.group(1))
            continue
        m = re.match(r"^offset:\s*(.+)$", stripped)
        if m:
            offset = nums(m.group(1))
            continue
        m = re.match(r"^rotate:\s*(\S+)$", stripped)
        if m:
            rotate = m.group(1).lower() == "true"
            continue
        m = re.match(r"^index:\s*-?1\s*$", stripped)
        if m:
            continue                                        # 4.x 里 -1 是默认值，可省
        if not stripped:
            flush()
            out.append("")
            continue
        if not line.startswith((" ", "\t")):
            flush()                                         # 新的区域名或页名
        out.append(line)
    flush()

    # 去掉多余空行，页与页之间保留一个
    cleaned: list[str] = []
    for line in out:
        if line == "" and cleaned and cleaned[-1] == "":
            continue
        cleaned.append(line)
    while cleaned and cleaned[-1] == "":
        cleaned.pop()
    return "\n".join(cleaned) + "\n"


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print(__doc__)
        return 2
    src = Path(argv[1])
    dst = Path(argv[2]) if len(argv) > 2 else src.with_suffix(".4x.atlas")
    dst.write_text(upgrade(src.read_text(encoding="utf-8-sig")), encoding="utf-8")
    print(f"已生成 {dst}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
