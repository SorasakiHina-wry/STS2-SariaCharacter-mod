#!/usr/bin/env python3
"""维护 cards.md 顶部的「卡牌进度」指示块。

目标规格：普通 20 / 罕见 35 / 稀有 25（基础牌不计）。

    python tools/card_progress.py --dry-run
    python tools/card_progress.py

块用 HTML 注释包起来，脚本只替换这两个标记之间的内容；标记不存在则插入到「## 当前配比」之前。
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import cardspec as cs  # noqa: E402

TARGETS = (("Common", "普通", 20), ("Uncommon", "罕见", 35), ("Rare", "稀有", 25))
START = "<!-- CARD-PROGRESS:START -->"
END = "<!-- CARD-PROGRESS:END -->"
ANCHOR = "## 当前配比"


def build_block(cards: list[dict]) -> str:
    done = {key: 0 for key, _label, _target in TARGETS}
    pending = {key: 0 for key, _label, _target in TARGETS}

    for card in cards:
        rarity = card.get("rarity")
        if rarity not in done or card.get("disabled") or card.get("status") == "待删除":
            continue
        if not card["name"].strip():
            continue
        if card.get("status") == "已实装":
            done[rarity] += 1
        else:
            pending[rarity] += 1

    lines = [START, "**卡牌进度**（目标：普通 20 / 罕见 35 / 稀有 25，基础牌不计）", ""]
    total_left = 0
    total_done = 0
    for key, label, target in TARGETS:
        left = max(0, target - done[key])
        total_left += left
        total_done += done[key]
        extra = ""
        if pending[key]:
            extra = f"，未实装 {pending[key]}"
        if done[key] > target:
            extra += f"（超出目标 {done[key] - target}）"
        lines.append(f"- **{label}**：已实装 {done[key]} / {target} → 还差 **{left}**{extra}")
    lines.append("")
    lines.append(f"合计：已实装 {total_done}，还差 **{total_left}** 张")
    lines.append(END)
    return "\n".join(lines)


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    cards_md = Path(__file__).resolve().parent.parent / "design" / "cards.md"
    text = cards_md.read_text(encoding="utf-8-sig")
    block = build_block(cs.parse_cards(cards_md))

    if START in text and END in text:
        head, rest = text.split(START, 1)
        _old, tail = rest.split(END, 1)
        new_text = head + block + tail
    else:
        idx = text.find(ANCHOR)
        if idx < 0:
            raise SystemExit(f"找不到锚点「{ANCHOR}」，无法插入进度块")
        new_text = text[:idx] + block + "\n\n" + text[idx:]

    if dry_run:
        print("【预览】将写入 cards.md 顶部：\n")
        print(block)
        return 0

    if new_text == text:
        print("进度块已是最新")
    else:
        cards_md.write_text(new_text, encoding="utf-8")
        print("已更新 cards.md 的进度块")
    print()
    print(block)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
