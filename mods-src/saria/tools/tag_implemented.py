#!/usr/bin/env python3
"""把已经实装的条目从 待实装 / 待修改 改成 已实装，并清掉备注里的括号说明。

判定：条目的 名称 在本体代码里出现过 ("title", "名称")。
备注后面的补充说明会保留；「修改了牌效/数值」这类临时说明会被丢掉。

    python tools/tag_implemented.py --dry-run
    python tools/tag_implemented.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "src"
DESIGN = ROOT / "design"

BLOCK_RE = re.compile(r"```(card|power|mechanic|relic)\n(.*?)```", re.S)
TITLE_RE = re.compile(r'\("title",\s*"([^"]+)"\)')
NAME_RE = re.compile(r"^名称[:：][ \t]*(.+?)[ \t]*$", re.M)
NOTE_RE = re.compile(r"^备注[:：][ \t]*(.*)$", re.M)

STALE = {"（修改了牌效）", "（修改了数值）", "（修改了牌效/数值）"}
CLEAN_RE = re.compile(r"(已实装)[ \t]*[（(][^）)\n]*[）)]")


def _strip_parenthetical(note: str, *, keep_tail: bool = True) -> str:
    """去掉备注里开头的括号说明（支持嵌套括号），可选保留括号之后的补充文字。

    例：`（修改了牌效；...（仅限战斗中）...）` -> ``（全部丢弃）
    """
    text = note.strip()
    if not text or text[0] not in "（(":
        return note

    depth = 0
    for index, ch in enumerate(text):
        if ch in "（(":
            depth += 1
        elif ch in "）)":
            depth -= 1
            if depth == 0:
                tail = text[index + 1:].strip()
                return tail if keep_tail else ""
    return ""  # 括号没闭合：整段丢弃


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv

    titles: set[str] = set()
    for path in SRC.rglob("*.cs"):
        titles.update(TITLE_RE.findall(path.read_text(encoding="utf-8")))

    changed: list[tuple[str, str, str]] = []
    cleaned: list[tuple[str, str]] = []
    clean = "--clean" in argv
    for doc in sorted(DESIGN.glob("*.md")):
        text = doc.read_text(encoding="utf-8")
        pieces: list[str] = []
        pos = 0
        for m in BLOCK_RE.finditer(text):
            pieces.append(text[pos:m.start()])
            block, body = m.group(0), m.group(2)
            name = NAME_RE.search(body)
            note = NOTE_RE.search(body)
            if name and note and name.group(1) in titles:
                old = note.group(1)
                new = old
                if old.startswith(("待实装", "待修改")):
                    rest = old[3:].strip()
                    if rest in STALE:
                        rest = ""
                    # 括号里的说明是写给智能体的临时指令，标记完成时整段丢掉（支持嵌套括号）
                    rest = _strip_parenthetical(rest)
                    new = "已实装" + rest
                    changed.append((doc.name, name.group(1), old))
                # 括号里的说明是给智能体看的，进 已实装 之后就没有意义了
                if new.startswith("已实装"):
                    new = CLEAN_RE.sub(r"\1", new)
                    if new != old and not old.startswith(("待实装", "待修改")):
                        cleaned.append((doc.name, name.group(1)))
                if new != old:
                    block = block.replace(body, body[:note.start(1)] + new + body[note.end(1):])
            pieces.append(block)
            pos = m.end()
        pieces.append(text[pos:])
        new_text = "".join(pieces)
        if new_text != text and not dry_run:
            doc.write_text(new_text, encoding="utf-8")

    verb = "将标记" if dry_run else "已标记"
    print(f"{verb} {len(changed)} 项")
    for doc, name, old in changed:
        print(f"  {doc}: {name}  （{old[:24]}…）")
    if cleaned:
        print(f"{'将清理' if dry_run else '已清理'} {len(cleaned)} 条已实装备注说明")
        for doc, name in cleaned:
            print(f"  {doc}: {name}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
