#!/usr/bin/env python3
"""给卡牌描述里的「有详情提示的词」加 [gold] 高亮（对齐原版卡面写法）。

原版约定（已从语料核对）：
  获得{Block:diff()}点[gold]格挡[/gold]。
  获得{StrengthPower:diff()}点[gold]力量[/gold]。
  给予{VulnerablePower:diff()}层[gold]易伤[/gold]。
  将一张[gold]{IfUpgraded:show:仆从打击+|仆从打击}[/gold]…

设计表里仍然写纯文本，加标记由这个脚本（或我写卡时）负责；校验器比对时会忽略 [gold]。

    python tools/gold_tags.py --dry-run
    python tools/gold_tags.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

DESC_RE = re.compile(r'(\("description"\s*,\s*")((?:[^"\\]|\\.)*)("\))')
TAG_RE = re.compile(r"\[gold\].*?\[/gold\]", re.S)

# 长词在前，避免「钙铠」被更短的词抢先切断
KEYWORDS = (
    # 角色专属 power
    "科技新星", "充能护服", "技艺超载", "精神回复", "紧密防护",
    "钙质化", "骨矿化", "再塑型", "钙铠", "分神", "凝神", "煅烧", "牵引", "洞察", "急救", "持械", "腿甲",
    # 原版状态
    "格挡", "力量", "敏捷", "虚弱", "易伤", "活力", "脆弱", "中毒", "荆棘",
    # 区域
    "手牌", "抽牌堆", "弃牌堆", "消耗堆",
    # 生成的卡牌
    "升温", "捕集",
)


IFUP_RE = re.compile(r"\{IfUpgraded:show:([^{}]*)\|([^{}]*)\}")


def highlight(text: str) -> str:
    """Wrap keywords outside existing [gold] spans."""
    # 原版写法：IfUpgraded 里是指向另一张牌的名字时，整段包 [gold]
    def _wrap_ifup(m: re.Match) -> str:
        if any(kw in m.group(1) or kw in m.group(2) for kw in KEYWORDS):
            return f"[gold]{m.group(0)}[/gold]"
        return m.group(0)

    text = IFUP_RE.sub(_wrap_ifup, text)

    pieces: list[str] = []
    cursor = 0
    for m in TAG_RE.finditer(text):
        pieces.append(_wrap_keywords(text[cursor:m.start()]))
        pieces.append(m.group(0))
        cursor = m.end()
    pieces.append(_wrap_keywords(text[cursor:]))
    return "".join(pieces)


def _wrap_keywords(segment: str) -> str:
    for kw in KEYWORDS:
        if kw in segment:
            segment = segment.replace(kw, f"[gold]{kw}[/gold]")
    return segment


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    root = Path(__file__).resolve().parent.parent
    cards_dir = root / "src" / "Cards"

    changed: list[str] = []
    for path in sorted(cards_dir.glob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        hits = 0

        def repl(m: re.Match) -> str:
            nonlocal hits
            body = m.group(2)
            new_body = highlight(body)
            if new_body != body:
                hits += 1
            return m.group(1) + new_body + m.group(3)

        new_text = DESC_RE.sub(repl, text)
        if hits:
            changed.append(f"{path.name}: {hits} 处描述")
            if not dry_run:
                path.write_text(new_text, encoding="utf-8")

    for line in changed:
        print(("【预览】" if dry_run else "✔ ") + line)
    print(f"\n共 {len(changed)} 个文件" + ("（未写入）" if dry_run else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
