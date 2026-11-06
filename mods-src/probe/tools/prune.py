#!/usr/bin/env python3
"""Housekeeping for the design sheets.

    python tools/prune.py            # remove every block tagged 待删除
    python tools/prune.py --retag    # flip 待实装/待修改 -> 已实装 for entries that exist in src/
    python tools/prune.py --dry-run

Run --retag *after* the code is built, and only for entries you actually implemented.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import cardspec as cs  # noqa: E402
import tag_status as ts  # noqa: E402

DESIGN_FILES = ("cards.md", "relics.md", "powers.md", "mechanics.md")
DONE_TAGS = ("待实装", "待修改")


def process(path: Path, titles: dict[str, str], retag: bool, dry_run: bool) -> list[str]:
    if not path.exists():
        return []
    text = path.read_text(encoding="utf-8-sig")
    log: list[str] = []

    def repl(match: re.Match) -> str:
        body = match.group(2)
        name_m = ts.NAME_RE.search(body)
        name = name_m.group(1).strip() if name_m else ""

        if "待删除" in body:
            log.append(f"{path.name}: 删除 [{name}]")
            return ""

        if retag and name and name in titles and any(t in body for t in DONE_TAGS):
            new_body = body
            for tag in DONE_TAGS:
                new_body = new_body.replace(tag, "已实装")
            if new_body != body:
                log.append(f"{path.name}: {name} 待实装/待修改 -> 已实装")
                return f"```{match.group(1)}\n{new_body}```"

        return match.group(0)

    new_text = ts.BLOCK_RE.sub(repl, text)
    if new_text != text and not dry_run:
        # collapse runs of blank lines left behind by removed blocks
        new_text = re.sub(r"\n{3,}", "\n\n", new_text)
        path.write_text(new_text, encoding="utf-8")
    return log


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    retag = "--retag" in argv
    design_dir = Path(__file__).resolve().parent.parent / "design"
    titles = cs.scan_code_titles(design_dir.parent / "src")

    logs: list[str] = []
    for name in DESIGN_FILES:
        logs += process(design_dir / name, titles, retag, dry_run)

    for line in logs:
        print(("【预览】" if dry_run else "✔ ") + line)
    print(f"\n共 {len(logs)} 项" + ("（未写入）" if dry_run else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
