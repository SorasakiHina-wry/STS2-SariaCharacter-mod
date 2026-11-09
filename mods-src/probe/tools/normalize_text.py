#!/usr/bin/env python3
"""Normalise display text in the design sheets and in the C# localization strings.

Two rules, applied to 基础 / 升级 / 描述 / 效果 fields and to every ("description", "...") literal:

  1. no spaces      — Chinese card text has no spaces around numbers: 造成6点伤害
  2. ends with 。   — every effect sentence is closed; missing periods are appended

Usage:
    python tools/normalize_text.py --dry-run
    python tools/normalize_text.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import tag_status as ts  # noqa: E402

DESIGN_FILES = ("cards.md", "relics.md", "powers.md", "mechanics.md")
FIELD_RE = re.compile(r"^([ \t]*)(基础|升级|描述|效果)([:：])(.*)$", re.M)
DESC_LITERAL_RE = re.compile(r'(\("description"\s*,\s*")((?:[^"\\]|\\.)*)("\))')
SPACES_RE = re.compile(r"[ \t\u3000]+")
SENTENCE_END = ("。", "！", "？", "!", "?", "…", "...")
CJK = ("\u4e00", "\u9fff")

# 钙铠 is measured in 层, never 点 — fix "点钙铠" wherever it shows up in display text.
UNIT_FIXES = (("点钙铠", "层钙铠"),)

# 统一用词：状态一律「给予」，不再用「施加」
WORD_FIXES = (("施加", "给予"),)


def _is_cjk(ch: str) -> bool:
    return bool(ch) and CJK[0] <= ch <= CJK[1]


def _has_cjk(text: str) -> bool:
    return any(_is_cjk(ch) for ch in text)


def strip_cjk_spaces(text: str) -> str:
    """Remove a space only when it touches a Chinese character ("造成 6 点伤害" -> "造成6点伤害").

    English sentences keep their spaces - the character blurb, for instance, must not be squashed.
    """
    out: list[str] = []
    for i, ch in enumerate(text):
        if ch in " \t\u3000":
            prev = text[i - 1] if i else ""
            nxt = text[i + 1] if i + 1 < len(text) else ""
            if _is_cjk(prev) or _is_cjk(nxt):
                continue
        out.append(ch)
    return "".join(out)


def normalize(text: str) -> str:
    """Chinese display text: no spaces, closed with 。, unit fixes. English text is left alone."""
    if not _has_cjk(text):
        return text
    text = strip_cjk_spaces(text).rstrip()
    for wrong, right in UNIT_FIXES + WORD_FIXES:
        text = text.replace(wrong, right)
    if text.endswith(".") and any("\u4e00" <= ch <= "\u9fff" for ch in text):
        text = text[:-1] + "。"   # 中文里用全角句号
    if not text or text.endswith(SENTENCE_END):
        return text
    if text.endswith("}") and "。" in text:
        return text   # {IfUpgraded:...} 结尾，句号在各分支里
    return text + "。"


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
        name = name_m.group(1).strip()

        def repl_field(fm: re.Match) -> str:
            indent, field, colon, value = fm.group(1), fm.group(2), fm.group(3), fm.group(4)
            new_value = normalize(value)
            if new_value != value:
                changed.append(f"{path.name}: {name} {field}")
                return f"{indent}{field}{colon}{new_value}"
            return fm.group(0)

        return f"```{match.group(1)}\n{FIELD_RE.sub(repl_field, body)}```"

    new_text = ts.BLOCK_RE.sub(repl_block, text)
    # 用词替换也覆盖表头/说明等非块文本（只改中文用词，不动结构）
    for wrong, right in WORD_FIXES:
        if wrong in new_text:
            new_text = new_text.replace(wrong, right)
            changed.append(f"{path.name}: 全文「{wrong}」→「{right}」")
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
            new_body = normalize(body)
            if new_body != body:
                hits.append(new_body)
            return match.group(1) + new_body + match.group(3)

        new_text = DESC_LITERAL_RE.sub(repl, text)
        if hits and not dry_run:
            cs.write_text(new_text, encoding="utf-8")
        if hits:
            changed.append(f"{cs.name}: {len(hits)} 处描述")
    return changed


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    root = Path(__file__).resolve().parent.parent
    design = root / "design"
    src = root / "src"

    logs: list[str] = []
    for name in DESIGN_FILES:
        logs += normalize_sheet(design / name, dry_run)
    logs += normalize_code(src, dry_run)

    for line in logs:
        print(("【预览】" if dry_run else "✔ ") + line)
    print(f"\n共 {len(logs)} 处" + ("（未写入）" if dry_run else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
