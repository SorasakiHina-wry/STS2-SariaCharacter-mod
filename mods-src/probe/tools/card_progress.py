#!/usr/bin/env python3
"""维护 cards.md 顶部的一行「卡牌进度」指示（已实装数量/需求总量）。

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

    for card in cards:
        rarity = card.get("rarity")
        if rarity not in done or card.get("disabled") or card.get("status") == "待删除":
            continue
        if card["name"].strip() and card.get("status") == "已实装":
            done[rarity] += 1

    parts = [f"{label} {done[key]}/{target}" for key, label, target in TARGETS]
    return START + "\n**卡牌进度**（已实装 / 需求）：" + " · ".join(parts) + "\n" + END

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
