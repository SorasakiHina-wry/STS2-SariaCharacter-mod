#!/usr/bin/env python3
"""Reorder the ```card blocks inside each rarity section of design/cards.md.

Rule (per user request): within 普通 / 罕见 / 稀有, cards are ordered by type —
攻击 → 技能 → 能力；其它类型（状态、诅咒…）排在最后。同类型之间保持原有相对顺序（稳定排序）。

只有卡片块本身会被重排：片段里的空行、```---``` 分隔线、小节标题等**位置都不动**，
脚本只是把块文本按新顺序填回原来的槽位，所以不会打乱表格的结构。

    python tools/sort_cards.py --dry-run
    python tools/sort_cards.py            # 写入
    python tools/sort_cards.py --check    # 不写入，顺序不对时返回 1
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

DOC = Path(__file__).resolve().parent.parent / "design" / "cards.md"

SECTIONS = ("普通", "罕见", "稀有")
TYPE_ORDER = {"攻击": 0, "技能": 1, "能力": 2}

SECTION_RE = re.compile(r"^## ")
NEW_SECTION_RE = re.compile(r"^## (普通|罕见|稀有)\b")
BLOCK_RE = re.compile(r"```card\s*\n(.*?)```", re.S)
TYPE_RE = re.compile(r"^类型[:：][ \t]*(.+?)[ \t]*$", re.M)
NAME_RE = re.compile(r"^名称[:：][ \t]*(.+?)[ \t]*$", re.M)


def rank(block_body: str) -> int:
    m = TYPE_RE.search(block_body)
    if not m:
        return len(TYPE_ORDER) + 1
    return TYPE_ORDER.get(m.group(1).strip(), len(TYPE_ORDER))


def is_template(block_body: str) -> bool:
    """占位样板（名称留空 / 备注写「空样板」）不参与排序，留在原位。"""
    m = NAME_RE.search(block_body)
    if not m or not m.group(1).strip():
        return True
    return "空样板" in block_body


def sort_section(text: str) -> tuple[str, list[str], list[str]]:
    """Return (possibly reordered text, before_names, after_names) for the whole file."""
    lines = text.splitlines(keepends=True)
    # 逐个找到目标小节的行范围 [start, end)
    ranges: list[tuple[int, int]] = []
    heads = [i for i, line in enumerate(lines) if SECTION_RE.match(line)]
    for pos, idx in enumerate(heads):
        if not NEW_SECTION_RE.match(lines[idx]):
            continue
        end = heads[pos + 1] if pos + 1 < len(heads) else len(lines)
        ranges.append((idx, end))

    before_all: list[str] = []
    after_all: list[str] = []
    pending: list[tuple[int, int, str]] = []   # (start, end, 新小节文本)

    for start, end in ranges:
        section = "".join(lines[start:end])
        blocks = list(BLOCK_RE.finditer(section))
        if len(blocks) < 2:
            continue

        bodies = [b.group(1) for b in blocks]
        names = []
        for body in bodies:
            m = NAME_RE.search(body)
            names.append(m.group(1).strip() if m else "?")
        before_all.extend(names)

        # 全部块一起排：真实卡按 攻击→技能→能力，空样板（类型为空）自然落到小节末尾
        order = sorted(range(len(bodies)), key=lambda i: (rank(bodies[i]), i))

        ordered_names = [names[i] for i in order]
        after_all.extend(ordered_names)

        if order == list(range(len(bodies))):
            continue

        # 把块文本按新顺序回填到原来的位置（位置、分隔线、空行都不变）
        pieces: list[str] = []
        cursor = 0
        for slot, block in enumerate(blocks):
            pieces.append(section[cursor:block.start()])
            pieces.append("```card\n" + bodies[order[slot]] + "```")
            cursor = block.end()
        pieces.append(section[cursor:])
        pending.append((start, end, "".join(pieces)))

    # 从后往前替换，避免前面的替换把后面的行号顶掉
    for start, end, new_section in reversed(pending):
        lines[start:end] = [new_section]

    return "".join(lines), before_all, after_all


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    check = "--check" in argv

    text = DOC.read_text(encoding="utf-8")
    new_text, before, after = sort_section(text)

    if before == after:
        print(f"顺序已经是目标顺序（共 {len(before)} 张）")
        return 0

    moved = sum(1 for a, b in zip(before, after) if a != b)
    if check:
        print(f"顺序不符合规则：{moved} 张需要移动。运行 tools/sort_cards.py 修正")
        return 1

    if dry_run:
        print("【预览】调整后的顺序：")
        for i, (b, a) in enumerate(zip(before, after), 1):
            print(f"  {i:2d}. {a}" + ("" if a == b else "  ← 移动"))
        print(f"\n共 {moved} 张需要移动（未写入，去掉 --dry-run 执行）")
        return 0

    DOC.write_text(new_text, encoding="utf-8")
    print(f"已重排 cards.md：{moved} 张移动（共 {len(after)} 张）")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
