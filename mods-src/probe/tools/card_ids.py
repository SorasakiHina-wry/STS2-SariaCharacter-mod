#!/usr/bin/env python3
"""生成「卡牌中文名 ↔ 英文代号 ↔ 源文件」对照表（设计表 ↔ 代码）。

输出 `design/card-ids.md`，**按源文件分组**，所以它同时也是一份「批次索引」：
想知道 ProbeBatch7Cards.cs 里都有哪几张卡，直接看对应小节即可。

每行一张卡：`中文名,英文代号,类名所在文件`
英文代号就是卡牌类的类名，也就是卡牌 ID 去掉前缀后的部分
（例如 `ProbeStrike` → 游戏内 ID `PROBE-PROBE_STRIKE`）。

    python tools/card_ids.py            # 写入 design/card-ids.md
    python tools/card_ids.py --dry-run  # 只打印
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

HEADER = """# 卡牌中文名 ↔ 英文代号 ↔ 源文件（按批次分组）

> 由 `tools/card_ids.py` 自动生成，**不要手改**。改设计表后重新跑一次即可。
>
> 三列：`中文名,英文代号,源文件`。
> 英文代号就是类名（游戏内 ID = `PROBE-` + 大写下划线形式，如 `ProbeStrike` → `PROBE-PROBE_STRIKE`）；
> 按它给卡图命名即可（例如 `ProbeStrike.png`）。
>
> 「批次」就是源文件：`ProbeStrike.cs` 是首批，`ProbeBatch2Cards.cs` 是第二批，以此类推。

"""


def code_index(cards_dir: Path) -> dict[str, tuple[str, str]]:
    """title -> (class name, file name)"""
    mapping: dict[str, tuple[str, str]] = {}
    for path in sorted(cards_dir.glob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        starts = [m.start() for m in CLASS_RE.finditer(text)]
        for idx, start in enumerate(starts):
            end = starts[idx + 1] if idx + 1 < len(starts) else len(text)
            chunk = text[start:end]
            class_m = CLASS_RE.search(chunk)
            title_m = TITLE_RE.search(chunk)
            if not class_m or not title_m:
                continue
            mapping[title_m.group(1)] = (class_m.group(1), path.name)
    return mapping


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    root = Path(__file__).resolve().parent.parent
    cards = cs.parse_cards(root / "design" / "cards.md")
    index = code_index(root / "src" / "Cards")

    by_file: "OrderedDict[str, list[str]]" = OrderedDict()
    missing: list[str] = []
    for card in cards:
        name = card["name"].strip()
        if not name or card.get("disabled"):
            continue
        found = index.get(name)
        if not found:
            missing.append(name)
            continue
        cls, file_name = found
        by_file.setdefault(file_name, []).append(f"{name},{cls},{file_name}")

    total = sum(len(v) for v in by_file.values())
    parts = [HEADER]
    parts.append(f"> 当前共 **{total}** 张卡，分布在 **{len(by_file)}** 个源文件里。\n\n")
    for file_name, rows in by_file.items():
        parts.append(f"## {file_name}（{len(rows)} 张）\n\n")
        parts.extend(row + "\n" for row in rows)
        parts.append("\n")
    out = "".join(parts).rstrip() + "\n"

    if dry_run:
        print(out)
    else:
        (root / "design" / "card-ids.md").write_text(out, encoding="utf-8")
        print(f"已写入 design/card-ids.md：{total} 张卡，{len(by_file)} 个批次文件")

    if missing:
        print(f"\n还没写代码的卡（{len(missing)} 张，未列入）：")
        for name in missing:
            print(f"  - {name}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
