#!/usr/bin/env python3
"""生成「卡牌中文名 ↔ 英文代号 ↔ 源文件」对照表（设计表 ↔ 代码）。

输出 `design/card-ids.md`：**条目顺序与 `design/cards.md` 完全一致**（同样的分节、同样的先后），
所以对着设计表从上往下找卡，这份表也能从上往下找到。

每行一张卡：`中文名,英文代号,源文件`
英文代号就是卡牌类的类名，也就是卡牌 ID 去掉前缀后的部分
（例如 `ProbeStrike` → 游戏内 ID `SARIA-PROBE_STRIKE`）。

    python tools/card_ids.py            # 写入 design/card-ids.md
    python tools/card_ids.py --dry-run  # 只打印
"""
from __future__ import annotations

import re
import sys
from collections import OrderedDict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import cardspec as cs  # noqa: E402

CLASS_RE = re.compile(r"(?m)^(?:public|internal)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*class\s+(\w+)")
TITLE_RE = re.compile(r'\("title",\s*"([^"]+)"\)')
HEADING_RE = re.compile(r"^##\s+(.+?)\s*$")

HEADER = """# 卡牌中文名 ↔ 英文代号 ↔ 源文件（按设计表顺序）

> 由 `tools/card_ids.py` 自动生成，**不要手改**。改设计表后重新跑一次即可。
>
> 顺序与 [`cards.md`](cards.md) 一致（同样的分节，同样的先后）。
> 三列：`中文名,英文代号,源文件`。
> 英文代号就是类名（游戏内 ID = `SARIA-` + 大写下划线形式，如 `ProbeStrike` → `SARIA-PROBE_STRIKE`）；
> 按它给卡图命名即可（例如 `ProbeStrike.png`）。

"""


def code_index(cards_dir: Path) -> dict[str, tuple[str, str]]:
    """title -> (class name, file name)"""
    mapping: dict[str, tuple[str, str]] = {}
    for path in sorted(cards_dir.glob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        starts = [m.start() for m in CLASS_RE.finditer(text)]
        for idx, start in enumerate(starts):
            end = starts[idx + 1] if idx + 1 < len(starts) else len(text)
            chunk = text[start:end]
            class_m = CLASS_RE.search(chunk)
            title_m = TITLE_RE.search(chunk)
            if not class_m or not title_m:
                continue
            mapping[title_m.group(1)] = (class_m.group(1), path.name)
    return mapping


def sections_of(cards_md: Path) -> list[tuple[int, str]]:
    """[(line_no, heading_text)]，用来把每张卡归到它上方的 `## ` 小节。"""
    lines = cards_md.read_text(encoding="utf-8-sig").splitlines()
    out: list[tuple[int, str]] = []
    for idx, line in enumerate(lines, start=1):
        m = HEADING_RE.match(line)
        if m:
            out.append((idx, m.group(1)))
    return out


def section_for(line_no: int, sections: list[tuple[int, str]]) -> str:
    current = "其他"
    for heading_line, title in sections:
        if heading_line < line_no:
            current = title
        else:
            break
    # 设计表的小节名自带数量说明（如「基础牌（起手 10 张）」），这里去掉，避免和本表统计重复
    return re.sub(r"[（(][^（()）]*[)）]\s*$", "", current).strip() or current


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    root = Path(__file__).resolve().parent.parent
    cards_md = root / "design" / "cards.md"
    cards = cs.parse_cards(cards_md)
    sections = sections_of(cards_md)
    index = code_index(root / "src" / "Cards")

    by_section: "OrderedDict[str, list[str]]" = OrderedDict()
    missing: list[str] = []
    for card in sorted(cards, key=lambda c: c.get("line", 0)):
        name = card["name"].strip()
        if not name or card.get("disabled"):
            continue
        found = index.get(name)
        if not found:
            missing.append(name)
            continue
        cls, file_name = found
        section = section_for(card.get("line", 0), sections)
        by_section.setdefault(section, []).append(f"{name},{cls},{file_name}")

    total = sum(len(v) for v in by_section.values())
    parts = [HEADER]
    parts.append(f"> 当前共 **{total}** 张卡。\n\n")
    for section, rows in by_section.items():
        parts.append(f"## {section}（{len(rows)} 张）\n\n")
        parts.extend(row + "\n" for row in rows)
        parts.append("\n")
    out = "".join(parts).rstrip() + "\n"

    if dry_run:
        print(out)
    else:
        (root / "design" / "card-ids.md").write_text(out, encoding="utf-8")
        print(f"已写入 design/card-ids.md：{total} 张卡，{len(by_section)} 个小节")

    if missing:
        print(f"\n还没写代码的卡（{len(missing)} 张，未列入）：")
        for name in missing:
            print(f"  - {name}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
