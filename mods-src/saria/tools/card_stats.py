#!/usr/bin/env python3
"""统计 design/cards.md 里各稀有度 × 各类型的卡牌数量，并把结果写回文档。

写回位置：`## 当前配比` 下的 `<!-- card-stats:begin -->` / `<!-- card-stats:end -->` 之间。
每次运行都会整段重写，所以不要在标记之间手写内容。

    python tools/card_stats.py            # 统计并写回
    python tools/card_stats.py --check    # 只检查，不写入（文档过期时返回码 1）
    python tools/card_stats.py --print    # 只打印
"""
from __future__ import annotations

import re
import sys
from collections import Counter
from pathlib import Path

DOC = Path(__file__).resolve().parent.parent / "design" / "cards.md"

BEGIN = "<!-- card-stats:begin 由 tools/card_stats.py 自动生成，不要手改 -->"
END = "<!-- card-stats:end -->"

CARD_RE = re.compile(r"```card\r?\n(.*?)```", re.S)
FIELD_RE = re.compile(r"^([^:：]+)[:：]\s*(.*)$")

RARITY_ORDER = ["基础", "普通", "罕见", "稀有", "先古", "无色", "其他"]
TYPE_ORDER = ["攻击", "技能", "能力", "状态", "诅咒"]

# 硬性下限（只有非基础牌参与）
MIN_PER_RARITY = 6
MIN_TYPE = {"攻击": 2, "技能": 2, "能力": 1}


def parse_cards(text: str) -> list[dict[str, str]]:
    cards = []
    for body in CARD_RE.findall(text):
        fields: dict[str, str] = {}
        for line in body.splitlines():
            m = FIELD_RE.match(line.strip())
            if m:
                fields[m.group(1).strip()] = m.group(2).strip()
        if not fields.get("名称") or "空样板" in fields.get("备注", ""):
            continue
        cards.append(fields)
    return cards


def build_table(cards: list[dict[str, str]]) -> tuple[str, bool]:
    """返回（markdown 片段, 是否满足硬性下限）。"""
    counts: Counter[tuple[str, str]] = Counter()
    for card in cards:
        counts[(card.get("稀有度", ""), card.get("类型", ""))] += 1

    rarities = [r for r in RARITY_ORDER if any(k[0] == r for k in counts)]
    for r in sorted({k[0] for k in counts}):
        if r not in rarities:
            rarities.append(r)

    types = [t for t in TYPE_ORDER if any(k[1] == t for k in counts)]
    for t in sorted({k[1] for k in counts}):
        if t not in types:
            types.append(t)

    head = "| 稀有度 | " + " | ".join(types) + " | 合计 |"
    sep = "|---|" + "---|" * (len(types) + 1)
    lines = [head, sep]

    row_totals = Counter()
    col_totals = Counter()
    grand = 0
    for rarity in rarities:
        cells = []
        row_total = 0
        for typ in types:
            n = counts[(rarity, typ)]
            row_total += n
            col_totals[typ] += n
            cells.append(str(n) if n else "")
        row_totals[rarity] = row_total
        grand += row_total
        lines.append(f"| {rarity} | " + " | ".join(cells) + f" | {row_total} |")

    lines.append("| **合计** | " + " | ".join(str(col_totals[t]) for t in types) + f" | **{grand}** |")

    # 硬性下限检查（基础牌不计入各稀有度）
    problems = []
    for rarity in ("普通", "罕见", "稀有"):
        n = row_totals.get(rarity, 0)
        if n < MIN_PER_RARITY:
            problems.append(f"{rarity} 只有 {n} 张（需要 ≥{MIN_PER_RARITY}）")
    non_basic = Counter()
    for card in cards:
        if card.get("稀有度") != "基础":
            non_basic[card.get("类型", "")] += 1
    for typ, need in MIN_TYPE.items():
        if non_basic.get(typ, 0) < need:
            problems.append(f"非基础{typ} 只有 {non_basic.get(typ, 0)} 张（需要 ≥{need}）")

    note = "硬性下限：Common/Uncommon/Rare 各 ≥6；非基础牌里 攻击 ≥2、技能 ≥2、能力 ≥1。"
    if problems:
        note += "**未满足：** " + "；".join(problems)
    else:
        note += "**已满足。**"

    body = "\n".join(lines) + "\n\n" + note + "\n"
    return body, not problems


def main(argv: list[str]) -> int:
    text = DOC.read_text(encoding="utf-8")
    cards = parse_cards(text)
    table, ok = build_table(cards)

    if "--print" in argv:
        print(f"卡牌总数（不含空样板）：{len(cards)}\n")
        print(table)
        return 0 if ok else 1

    if BEGIN not in text or END not in text:
        raise SystemExit(f"找不到标记，请先在 design/cards.md 里放好：\n  {BEGIN}\n  {END}")

    new_text = re.sub(
        re.escape(BEGIN) + r".*?" + re.escape(END),
        BEGIN + "\n\n" + table + "\n" + END,
        text,
        count=1,
        flags=re.S,
    )

    if "--check" in argv:
        if new_text != text:
            print("配比表已过期，运行 tools/card_stats.py 更新")
            return 1
        print(f"配比表最新（共 {len(cards)} 张）")
        return 0 if ok else 1

    if new_text != text:
        DOC.write_text(new_text, encoding="utf-8")
        print(f"已更新配比表（共 {len(cards)} 张）")
    else:
        print(f"配比表无变化（共 {len(cards)} 张）")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
