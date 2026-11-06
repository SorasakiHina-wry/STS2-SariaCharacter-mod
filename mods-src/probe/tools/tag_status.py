#!/usr/bin/env python3
"""Bulk-tag design entries with the workflow status 已实装.

An entry counts as "implemented" when its 名称 appears as a ("title", "...") string in
src/**.cs. Existing 备注 text is preserved after a "·" separator, and entries that already
carry one of the status tags are left untouched.

Usage:
    python tools/tag_status.py --dry-run     # show what would change
    python tools/tag_status.py               # rewrite design/*.md in place
    python tools/tag_status.py --status 待实装
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import cardspec as cs  # noqa: E402

BLOCK_RE = re.compile(r"```(card|power|mechanic|relic)\n(.*?)```", re.S)
NAME_RE = re.compile(r"^名称[:：][ \t]*(.+?)[ \t]*$", re.M)
NOTE_RE = re.compile(r"^备注[:：]\s*(.*)$", re.M)


def tag_file(path: Path, titles: dict[str, str], status: str, dry_run: bool) -> list[str]:
    if not path.exists():
        return []
    text = path.read_text(encoding="utf-8-sig")
    changes: list[str] = []

    def repl(match: re.Match) -> str:
        kind, body = match.group(1), match.group(2)
        name_m = NAME_RE.search(body)
        if not name_m:
            return match.group(0)
        name = name_m.group(1).strip()
        if not name or name not in titles:
            return match.group(0)
        if any(tag in body for tag in cs.STATUS_TAGS):
            return match.group(0)

        note_m = NOTE_RE.search(body)
        if note_m:
            old = note_m.group(1).strip()
            new_line = f"备注：{status}" + (f" · {old}" if old else "")
            new_body = body[: note_m.start()] + new_line + body[note_m.end():]
        else:
            new_body = body.rstrip("\n") + f"\n备注：{status}\n"

        changes.append(f"{path.name}: {name} -> {status}")
        return f"```{kind}\n{new_body}```"

    new_text = BLOCK_RE.sub(repl, text)
    if changes and not dry_run:
        path.write_text(new_text, encoding="utf-8")
    return changes


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    status = "已实装"
    for i, arg in enumerate(argv):
        if arg == "--status" and i + 1 < len(argv):
            status = argv[i + 1]

    design_dir = Path(__file__).resolve().parent.parent / "design"
    titles = cs.scan_code_titles(design_dir.parent / "src")
    if not titles:
        print("src 里没扫到任何 title，先确认路径")
        return 2

    all_changes: list[str] = []
    for name in ("cards.md", "relics.md", "powers.md", "mechanics.md"):
        all_changes += tag_file(design_dir / name, titles, status, dry_run)

    if not all_changes:
        print("没有需要标记的条目")
        return 0

    for line in all_changes:
        print(("【预览】" if dry_run else "已标记 ") + line)
    print(f"\n共 {len(all_changes)} 条" + ("（未写入，去掉 --dry-run 执行）" if dry_run else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
