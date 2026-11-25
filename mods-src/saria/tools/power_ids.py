#!/usr/bin/env python3
"""生成「状态中文名 ↔ 英文代号 ↔ 源文件」对照表（设计表 ↔ 代码）。

输出 `design/power-ids.md`：**条目顺序与 `design/powers.md` 完全一致**，
用途是给状态（power）小图命名 —— 英文代号就是类名，图标文件名按它来。

每行一个状态：`中文名,英文代号,源文件`
英文代号 = 类名，也就是卡牌/状态 ID 去掉前缀后的部分（`ProbeCalciumArmorPower` → `SARIA-CALCIUM_ARMOR_POWER`）。

    python tools/power_ids.py            # 写入 design/power-ids.md
    python tools/power_ids.py --dry-run  # 只打印
"""
from __future__ import annotations

import re
import sys
from collections import OrderedDict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import cardspec as cs  # noqa: E402

CLASS_RE = re.compile(r"(?m)^(?:public|internal)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*class\s+(\w+)")
TITLE_RE = re.compile(r'\("title",\s*"([^"]+)"\)')
HEADING_RE = re.compile(r"^##\s+(.+?)\s*$")

HEADER = """# 状态中文名 ↔ 英文代号 ↔ 源文件（按设计表顺序）

> 由 `tools/power_ids.py` 自动生成，**不要手改**。改设计表后重新跑一次即可。
>
> 顺序与 [`powers.md`](powers.md) 一致。
> 三列：`中文名,英文代号,源文件`。
> 英文代号就是类名（游戏内状态 ID = `SARIA-` + 大写下划线形式，如
> `ProbeCalciumArmorPower` → `SARIA-CALCIUM_ARMOR_POWER`）；
> **状态小图按它命名**（例如 `ProbeCalciumArmorPower.png`）。
>
> 小图规格：`CustomPackedIconPath`（图集小图，游戏内显示约 32×32）与
> `CustomBigIconPath`（悬停大图，约 64×64），两者都要提供。

"""


def code_index(powers_dir: Path) -> dict[str, tuple[str, str]]:
    """title -> (class name, file name)"""
    mapping: dict[str, tuple[str, str]] = {}
    for path in sorted(powers_dir.glob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        starts = [m.start() for m in CLASS_RE.finditer(text)]
        for idx, start in enumerate(starts):
            end = starts[idx + 1] if idx + 1 < len(starts) else len(text)
            chunk = text[start:end]
            class_m = CLASS_RE.search(chunk)
            title_m = TITLE_RE.search(chunk)
            if not class_m or not title_m:
                continue
            mapping.setdefault(title_m.group(1), (class_m.group(1), path.name))
    return mapping


def sections_of(path: Path) -> list[tuple[int, str]]:
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    out: list[tuple[int, str]] = []
    for idx, line in enumerate(lines, start=1):
        m = HEADING_RE.match(line)
        if m:
            out.append((idx, m.group(1)))
    return out


def section_for(line_no: int, sections: list[tuple[int, str]]) -> str:
    current = "其他"
    for heading_line, title in sections:
        if heading_line < line_no:
            current = title
        else:
            break
    return re.sub(r"[（(][^（()）]*[)）]\s*$", "", current).strip() or current


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    root = Path(__file__).resolve().parent.parent
    powers_md = root / "design" / "powers.md"
    powers = cs.parse_powers(powers_md)
    sections = sections_of(powers_md)
    index = code_index(root / "src" / "Powers")

    by_section: "OrderedDict[str, list[str]]" = OrderedDict()
    missing: list[str] = []
    for power in sorted(powers, key=lambda p: p.get("line", 0)):
        name = (power.get("name") or "").strip()
        if not name:
            continue
        found = index.get(name)
        if not found:
            missing.append(name)
            continue
        cls, file_name = found
        section = section_for(power.get("line", 0), sections)
        by_section.setdefault(section, []).append(f"{name},{cls},{file_name}")

    total = sum(len(v) for v in by_section.values())
    parts = [HEADER, f"> 当前共 **{total}** 个状态。\n\n"]
    single = len(by_section) == 1
    for section, rows in by_section.items():
        title = "全部状态" if single else section
        parts.append(f"## {title}（{len(rows)} 个）\n\n")
        parts.extend(row + "\n" for row in rows)
        parts.append("\n")
    out = "".join(parts).rstrip() + "\n"

    if dry_run:
        print(out)
    else:
        (root / "design" / "power-ids.md").write_text(out, encoding="utf-8")
        print(f"已写入 design/power-ids.md：{total} 个状态，{len(by_section)} 个小节")

    if missing:
        print(f"\n还没写代码的状态（{len(missing)} 个，未列入）：")
        for name in missing:
            print(f"  - {name}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
