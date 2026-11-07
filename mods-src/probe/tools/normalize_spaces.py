#!/usr/bin/env python3
"""Strip spaces from card/power/relic display text, in both the design sheets and the C# strings.

Chinese card text in the vanilla game has no spaces around numbers ("造成6点伤害"), so this tool
normalises:

  * design sheets: the 基础 / 升级 fields of cards, and 描述 / 效果 of powers & relics
  * src/**.cs: every ("description", "...") literal

Usage:
    python tools/normalize_spaces.py --dry-run
    python tools/normalize_spaces.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import tag_status as ts  # noqa: E402

DESIGN_FIELDS = ("基础", "升级", "描述", "效果")
FIELD_RE = re.compile(r"^([ \t]*)(基础|升级|描述|效果)([:：])(.*)$", re.M)
DESC_LITERAL_RE = re.compile(r'(\("description"\s*,\s*")((?:[^"\\]|\\.)*)("\))')
SPACES_RE = re.compile(r"[ \t\u3000]+")


def strip_spaces(text: str) -> str:
    return SPACES_RE.sub("", text)


def normalize_sheet(path: Path, dry_run: bool) -> list[str]:
    if not path.exists():
        return []
    text = path.read_text(encoding="utf-8-sig")
    changed: list[str] = []

    def repl_block(match: re.Match) -> str:
        body = match.group(2)
        name_m = ts.NAME_RE.search(body)
        # 样板块（名称为空）不动，否则会把占位符里的空格也吃掉
        if not name_m or not name_m.group(1).strip():
            return match.group(0)

        def repl_field(fm: re.Match) -> str:
            indent, field, colon, value = fm.group(1), fm.group(2), fm.group(3), fm.group(4)
            new_value = strip_spaces(value)
            if new_value != value:
                changed.append(f"{path.name}: {name_m.group(1).strip()} {field} 去空格")
                return f"{indent}{field}{colon}{new_value}"
            return fm.group(0)

        return f"```{match.group(1)}\n{FIELD_RE.sub(repl_field, body)}```"

    new_text = ts.BLOCK_RE.sub(repl_block, text)
    if changed and not dry_run:
        path.write_text(new_text, encoding="utf-8")
    return changed


def normalize_code(path: Path, dry_run: bool) -> list[str]:
    changed: list[str] = []
    for cs in sorted(path.rglob("*.cs")):
        text = cs.read_text(encoding="utf-8-sig")
        hits: list[str] = []

        def repl(match: re.Match) -> str:
            body = match.group(2)
            new_body = strip_spaces(body)
            if new_body != body:
                hits.append(cs.name)
            return match.group(1) + new_body + match.group(3)

        new_text = DESC_LITERAL_RE.sub(repl, text)
        if hits and not dry_run:
            cs.write_text(new_text, encoding="utf-8")
        if hits:
            changed.append(f"{cs.name}: description 去空格")
    return changed


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    root = Path(__file__).resolve().parent.parent
    design = root / "design"
    src = root / "src"

    logs: list[str] = []
    for name in ts.__dict__.get("DESIGN_FILES", ()) or ("cards.md", "relics.md", "powers.md", "mechanics.md"):
        logs += normalize_sheet(design / name, dry_run)
    logs += normalize_code(src, dry_run)

    for line in logs:
        print(("【预览】" if dry_run else "✔ ") + line)
    print(f"\n共 {len(logs)} 处" + ("（未写入）" if dry_run else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
