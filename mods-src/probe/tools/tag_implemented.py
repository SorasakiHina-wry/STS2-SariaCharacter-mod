#!/usr/bin/env python3
"""把已经实装的条目从 待实装 / 待修改 改成 已实装。

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


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv

    titles: set[str] = set()
    for path in SRC.rglob("*.cs"):
        titles.update(TITLE_RE.findall(path.read_text(encoding="utf-8")))

    changed: list[tuple[str, str, str]] = []
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
                if old.startswith(("待实装", "待修改")):
                    rest = old[3:]
                    if rest.strip() in STALE:
                        rest = ""
                    new = "已实装" + rest
                    block = block.replace(body, body[:note.start(1)] + new + body[note.end(1):])
                    changed.append((doc.name, name.group(1), old))
            pieces.append(block)
            pos = m.end()
        pieces.append(text[pos:])
        new_text = "".join(pieces)
        if new_text != text and not dry_run:
            doc.write_text(new_text, encoding="utf-8")

    print(f"{'将标记' if dry_run else '已标记'} {len(changed)} 项")
    for doc, name, old in changed:
        print(f"  {doc}: {name}  （{old[:24]}…）")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
