#!/usr/bin/env python3
"""生成「卡牌中文名,英文代号」对照表（设计表 ↔ 代码类名）。

每行一张卡：`中文名,英文代号`，按 cards.md 里的出现顺序排列。
英文代号就是卡牌类的类名，也就是卡牌 ID 去掉前缀后的部分
（例如 `ProbeStrike` → 游戏内 ID `PROBE-PROBE_STRIKE`）。

    python tools/card_ids.py            # 写入 design/card-ids.md
    python tools/card_ids.py --dry-run  # 只打印
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import cardspec as cs  # noqa: E402

CLASS_RE = re.compile(r"(?m)^(?:public|internal)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*class\s+(\w+)")
TITLE_RE = re.compile(r'\("title",\s*"([^"]+)"\)')

HEADER = """# 卡牌中文名 ↔ 英文代号

> 由 `tools/card_ids.py` 自动生成，**不要手改**。改设计表后重新跑一次即可。
>
> 用法：按 `英文代号` 给卡图命名（例如 `ProbeStrike.png`），我据此批量接入。

"""


def code_titles(cards_dir: Path) -> dict[str, str]:
    """title -> class name"""
    mapping: dict[str, str] = {}
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
            mapping[title_m.group(1)] = class_m.group(1)
    return mapping


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    root = Path(__file__).resolve().parent.parent
    cards = cs.parse_cards(root / "design" / "cards.md")
    titles = code_titles(root / "src" / "Cards")

    rows: list[str] = []
    missing: list[str] = []
    for card in cards:
        name = card["name"].strip()
        if not name or card.get("disabled"):
            continue
        cls = titles.get(name)
        if not cls:
            missing.append(name)
            continue
        rows.append(f"{name},{cls}")

    out = HEADER + "\n".join(rows) + "\n"
    if dry_run:
        print(out)
    else:
        (root / "design" / "card-ids.md").write_text(out, encoding="utf-8")
        print(f"已写入 design/card-ids.md：{len(rows)} 张卡")

    if missing:
        print(f"\n还没写代码的卡（{len(missing)} 张，未列入）：")
        for name in missing:
            print(f"  - {name}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
