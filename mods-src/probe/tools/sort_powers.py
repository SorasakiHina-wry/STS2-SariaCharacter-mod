#!/usr/bin/env python3
"""Reorder the ```power blocks in design/powers.md.

Rules (per user request):
  1. 钙铠 is always first.
  2. Powers whose name matches a card in design/cards.md follow, ordered by that card's first
     appearance in cards.md.
  3. Every other power goes last (original relative order kept).

The file header (everything up to and including「## 新状态从下面往下写」) and the 样板 block are
left untouched. Anything that is not a ```power block in the region below the heading makes the
script abort instead of silently dropping content.

Usage:
    python tools/sort_powers.py --dry-run
    python tools/sort_powers.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

HEADING = "## 新状态从下面往下写"
ALWAYS_FIRST = "钙铠"
BLOCK_RE = re.compile(r"```power\s*\n(.*?)```", re.S)
NAME_RE = re.compile(r"^名称[:：][ \t]*(.*?)[ \t]*$", re.M)


def read_power_blocks(text: str) -> tuple[str, list[tuple[str, str]], str]:
    """Return (head, [(name, raw block)], tail) for powers.md content."""
    idx = text.find(HEADING)
    if idx < 0:
        raise SystemExit(f"找不到标题「{HEADING}」，不做任何修改")

    head_end = text.find("\n", idx)
    head_end = len(text) if head_end < 0 else head_end + 1
    head = text[:head_end]
    region = text[head_end:]

    blocks: list[tuple[str, str]] = []
    stray: list[str] = []
    cursor = 0
    for m in BLOCK_RE.finditer(region):
        gap = region[cursor:m.start()]
        if gap.strip():
            stray.append(gap.strip())
        name_m = NAME_RE.search(m.group(1))
        name = name_m.group(1).strip() if name_m else ""
        blocks.append((name, m.group(0).strip()))
        cursor = m.end()

    rest = region[cursor:]
    if rest.strip():
        stray.append(rest.strip())

    if stray:
        raise SystemExit(
            "标题下方存在非 ```power 的内容，为了避免丢内容已中止：\n  "
            + "\n  ".join(s.splitlines()[0] for s in stray)
        )
    if not blocks:
        raise SystemExit("标题下方没有找到任何 ```power 块")

    return head, blocks, ""


def card_order(cards_md: Path) -> dict[str, int]:
    """name -> first appearance index in cards.md."""
    order: dict[str, int] = {}
    if not cards_md.exists():
        return order
    text = cards_md.read_text(encoding="utf-8-sig")
    for i, m in enumerate(re.finditer(r"```card\s*\n(.*?)```", text, re.S)):
        name_m = NAME_RE.search(m.group(1))
        if not name_m:
            continue
        name = name_m.group(1).strip()
        if name and name not in order:
            order[name] = i
    return order


def sort_blocks(blocks: list[tuple[str, str]], order: dict[str, int]) -> list[tuple[str, str]]:
    first: list[tuple[str, str]] = []
    matched: list[tuple[int, int, tuple[str, str]]] = []
    tail: list[tuple[str, str]] = []

    for pos, (name, raw) in enumerate(blocks):
        if name == ALWAYS_FIRST:
            first.append((name, raw))
        elif name in order:
            matched.append((order[name], pos, (name, raw)))
        else:
            tail.append((name, raw))

    matched.sort(key=lambda item: (item[0], item[1]))
    return first + [b for _i, _p, b in matched] + tail


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    design = Path(__file__).resolve().parent.parent / "design"
    powers_md = design / "powers.md"
    cards_md = design / "cards.md"

    text = powers_md.read_text(encoding="utf-8-sig")
    head, blocks, tail = read_power_blocks(text)
    order = card_order(cards_md)
    sorted_blocks = sort_blocks(blocks, order)

    before = [n for n, _ in blocks]
    after = [n for n, _ in sorted_blocks]

    if before == after:
        print("顺序已经是目标顺序，无需改动")
        print("当前顺序：" + " / ".join(after))
        return 0

    if dry_run:
        print("【预览】调整后的顺序：")
        for i, name in enumerate(after, 1):
            marker = "" if name in before and before.index(name) == i - 1 else "  ← 移动"
            print(f"  {i:2d}. {name}{marker}")
        print("\n（未写入，去掉 --dry-run 执行）")
        return 0

    body = "\n\n".join(raw for _n, raw in sorted_blocks) + "\n"
    new_text = head.rstrip("\n") + "\n\n" + body
    powers_md.write_text(new_text, encoding="utf-8")
    print("已重排 powers.md：")
    for i, name in enumerate(after, 1):
        print(f"  {i:2d}. {name}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
