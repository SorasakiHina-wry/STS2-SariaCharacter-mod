#!/usr/bin/env python3
"""导出原版角色 × 各位先古之民的对话，供阅读参考。

数据来自游戏内置本地化 `localization/zhs/ancients.json`，输出到
`design/ancient-dialogue-vanilla.md`，格式与 `character-writing.md` 一致（```talk 块）。

    python tools/export_ancient_dialogue.py --dry-run
    python tools/export_ancient_dialogue.py
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

LOC = Path(r"D:\STS2\.cache\loc_extract\localization\zhs")
OUT = Path(__file__).resolve().parent.parent / "design" / "ancient-dialogue-vanilla.md"

KEY_RE = re.compile(
    r"^(?P<anc>[A-Z_]+)\.talk\.(?P<who>[A-Za-z_]+)\.(?P<set>\d+)-(?P<line>\d+)(?P<r>r)?\.(?P<kind>ancient|char|next)$"
)
ATTACK_RE = re.compile(r"^(?P<anc>[A-Z_]+)\.talk\.(?P<who>[A-Za-z_]+)\.\d+-attack$")

ORDER = ["NEOW", "DARV", "NONUPEIPE", "OROBAS", "PAEL", "TANX", "TEZCATARA", "VAKUU", "THE_ARCHITECT"]

# 富文本标记 → 作用（颜色类来自游戏内 RichTextTags，其余是 Godot BBCode 或内置效果）
TAG_HELP = [
    ("gold", "金色高亮：关键词、名字、数值"),
    ("blue", "蓝色高亮：次要关键词"),
    ("red", "红色高亮：危险、敌意、负面"),
    ("green", "绿色高亮：正面、恢复"),
    ("orange", "橙色高亮"),
    ("purple", "紫色高亮"),
    ("pink", "粉色高亮"),
    ("aqua", "青色高亮"),
    ("b", "粗体"),
    ("i", "斜体（常与 font_size 一起做旁白）"),
    ("font_size=22", "字号，22 是原版旁白/拟声用的小字号"),
    ("center", "整段居中"),
    ("sine", "上下缓慢起伏的波浪（飘忽、幽灵感；幅度约 0.8 像素）"),
    ("jitter", "快速随机抖动（颤抖、情绪激动；幅度约 3 像素）"),
    ("thinky_dots", "逐字轻微上浮的波浪，用来表现「正在思考/正在说」"),
    ("shake", "Godot 内置抖动效果"),
    ("rainbow", "Godot 内置彩虹渐变"),
    ("fade_in", "淡入出现（常用于旁白）"),
    ("fly_in", "飞入出现"),
    ("scramble", "乱码/解码效果（不可名状的低语）"),
    ("lb", "左方括号：`[lb]文字` 显示成「文字"),
    ("rb", "右方括号：`文字[rb]` 显示成文字」"),
]


def load(name: str) -> dict:
    p = LOC / name
    if not p.exists():
        raise SystemExit(f"找不到本地化文件：{p}")
    return json.loads(p.read_text(encoding="utf-8"))


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    anc = load("ancients.json")
    chars = load("characters.json")

    titles = {k.split(".")[0]: v for k, v in chars.items() if k.endswith(".title")}

    # 收集：{ancient: {who: {set_key: {line: {kind: text}}}}}
    data: dict[str, dict[str, dict[str, dict[int, dict[str, str]]]]] = {}
    attack: dict[tuple[str, str], str] = {}

    for key, value in anc.items():
        m = ATTACK_RE.match(key)
        if m:
            attack[(m.group("anc"), m.group("who"))] = value
            continue
        m = KEY_RE.match(key)
        if not m:
            continue
        ancient, who = m.group("anc"), m.group("who")
        set_key = m.group("set") + ("r" if m.group("r") else "")
        line = int(m.group("line"))
        data.setdefault(ancient, {}).setdefault(who, {}).setdefault(set_key, {}).setdefault(line, {})[m.group("kind")] = value

    lines: list[str] = []
    lines.append("# 原版角色 × 先古之民 对话一览")
    lines.append("")
    lines.append("> 由 `tools/export_ancient_dialogue.py` 从游戏内 `ancients.json` 自动导出，**不要手改**。")
    lines.append("> 格式说明见 [`character-writing.md`](character-writing.md)。")
    lines.append("> 方括号里是先古（`[古]`）或该角色；`[钮]` 是继续按钮上的字（与原版不同时才列出）。")
    lines.append("> 原文里的换行 `\\n` 在这里直接显示成下一行（该行没有前缀）。")
    lines.append("")

    # 统计本文档里实际出现的富文本标记
    used: set[str] = set()
    for _k, _v in anc.items():
        if isinstance(_v, str):
            for _m in re.finditer(r"\[([a-zA-Z_]+)", _v):
                used.add(_m.group(1).lower())

    lines.append("## 富文本标记说明")
    lines.append("")
    lines.append("| 标记 | 作用 | 本文档中出现 |")
    lines.append("|---|---|---|")
    for tag, desc in TAG_HELP:
        lines.append(f"| `[{tag}]` | {desc} | {'✓' if tag.split('=')[0] in used else ''} |")
    lines.append("")
    lines.append("> 颜色类标记（gold/blue/red/…）只改颜色、不改语义，和卡牌描述里的高亮是同一套。")
    lines.append("")

    custom = [a for a in anc if a.endswith(".title") and a.split(".")[0] not in ORDER]
    ancients = [a for a in ORDER if a in data] + sorted({a for a in data if a not in ORDER})

    for ancient in ancients:
        title = anc.get(ancient + ".title", ancient)
        epithet = anc.get(ancient + ".epithet", "")
        head = f"## {title} {ancient}"
        if epithet:
            head += f" · {epithet}"
        lines.append(head)
        lines.append("")

        chars_of_ancient = data[ancient]
        order = ["firstVisitEver", "ANY"] + sorted([w for w in chars_of_ancient if w not in ("firstVisitEver", "ANY")])
        seen = set()
        for who in order:
            if who not in chars_of_ancient or who in seen:
                continue
            seen.add(who)
            if who == "firstVisitEver":
                label = "全存档首次相遇"
                speaker = "[古]"
            elif who == "ANY":
                label = "通用（该角色没有专属对话时使用）"
                speaker = "[角色]"
            else:
                label = f"{titles.get(who, who)}（{who}）"
                speaker = f"[{titles.get(who, who)}]"
            lines.append(f"### {label}")
            lines.append("")

            for set_key in sorted(chars_of_ancient[who], key=lambda s: (int(s.rstrip("r")), s.endswith("r"))):
                block = [f"先古: {ancient}"]
                if who == "firstVisitEver":
                    block.append(f"套号: firstVisitEver-{set_key}")
                else:
                    block.append(f"角色: {who}" + ("（通用/ANY）" if who == "ANY" else ""))
                    block.append(f"套号: {set_key}")
                block.append(f"可重复: {'是' if set_key.endswith('r') else '否'}")
                if (ancient, who) in attack:
                    block.append(f"攻击动画: {attack[(ancient, who)]}")
                for line_no in sorted(chars_of_ancient[who][set_key]):
                    entry = chars_of_ancient[who][set_key][line_no]
                    if "ancient" in entry:
                        block.append(f"[古] {entry['ancient']}")
                    if "char" in entry:
                        block.append(f"{speaker} {entry['char']}")
                    if "next" in entry and entry["next"] != "继续":
                        block.append(f"[钮] {entry['next']}")
                lines.append("```talk")
                lines.extend(block)
                lines.append("```")
                lines.append("")

    out = "\n".join(lines).rstrip() + "\n"
    if dry_run:
        print(out[:3000])
        print(f"\n…（共 {len(lines)} 行）")
    else:
        OUT.write_text(out, encoding="utf-8")
        print(f"已写入 {OUT.name}：{len(lines)} 行，覆盖 {len(ancients)} 位先古")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
