#!/usr/bin/env python3
"""把「## 样板」的空卡牌结构补到各稀有度小节末尾，直到填满计划卡牌数。

计划规格：普通 20 / 罕见 35 / 稀有 25（基础牌与专属无色不计）。
每个占位块带标记 `备注：空样板`，`cardspec.py --pending` 以及各类校验都会忽略它。

    python tools/fill_templates.py --dry-run
    python tools/fill_templates.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import cardspec as cs  # noqa: E402

TARGETS = (("Common", "普通", "## 普通 Common", 20),
           ("Uncommon", "罕见", "## 罕见 Uncommon", 35),
           ("Rare", "稀有", "## 稀有 Rare", 25))
TEMPLATE_MARK = "空样板"

TEMPLATE = """```card
名称: 
稀有度: {rarity}
类型: 
目标: 
基础: 费用N|造成N点伤害。
升级: 费用N|造成N点伤害。
备注：{mark}
```
"""


def section_end(lines: list[str], heading: str) -> int:
    """Return the index of the next '## ' heading after `heading` (or len(lines))."""
    start = next((i for i, line in enumerate(lines) if line.strip() == heading), -1)
    if start < 0:
        raise SystemExit(f"找不到小节标题：{heading}")
    for i in range(start + 1, len(lines)):
        if lines[i].startswith("## "):
            return i
    return len(lines)


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    path = Path(__file__).resolve().parent.parent / "design" / "cards.md"
    text = path.read_text(encoding="utf-8-sig")
    cards = cs.parse_cards(path)

    existing: dict[str, int] = {}
    for card in cards:
        rarity = card.get("rarity")
        if not card["name"].strip() or card.get("disabled") or card.get("status") == "待删除":
            continue
        existing[rarity] = existing.get(rarity, 0) + 1

    lines = text.splitlines()
    added: list[str] = []
    # 从后往前插入，避免前面的插入影响后面的行号
    for rarity, label, heading, target in reversed(TARGETS):
        need = max(0, target - existing.get(rarity, 0))
        if need == 0:
            continue
        end = section_end(lines, heading)
        blocks = [TEMPLATE.format(rarity=label, mark=TEMPLATE_MARK) for _ in range(need)]
        lines[end:end] = blocks
        added.append(f"{label}: 补 {need} 个空样板（现有 {existing.get(rarity, 0)}/{target}）")

    if not added:
        print("计划卡牌数已填满，无需补样板")
        return 0

    for line in added:
        print(("【预览】" if dry_run else "✔ ") + line)

    if not dry_run:
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        print("\n已写入 design/cards.md")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
