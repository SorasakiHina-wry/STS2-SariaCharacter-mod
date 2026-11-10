#!/usr/bin/env python3
"""把 `design/character-writing.md` 里写的文案转成游戏本地化文件。

产出：
  * `assets/localization/zhs/ancients.json` —— 先古之民的对话
  * 控制台打印角色选择界面字段（需要我同步进 C# 的那几项）

    python tools/import_writing.py --dry-run
    python tools/import_writing.py
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DOC = ROOT / "design" / "character-writing.md"
OUT = ROOT / "assets" / "localization" / "zhs" / "ancients.json"

CHAR_ID = "PROBE-SARIA"
DEFAULT_NEXT = "继续"

TALK_RE = re.compile(r"```talk\r?\n(.*?)```", re.S)
CHAR_RE = re.compile(r"```character\r?\n(.*?)```", re.S)
SPEAKER_RE = re.compile(r"^\[(古|她|钮)\]\s?(.*)$")
HEADER_RE = re.compile(r"^(先古|套号|可重复|角色|攻击动画)\s*[:：]\s*(.*)$")


def parse_character_block(text: str) -> dict[str, str]:
    m = CHAR_RE.search(text)
    if not m:
        return {}
    out: dict[str, str] = {}
    for line in m.group(1).splitlines():
        if not line.strip():
            continue
        key, _, value = line.partition(":")
        out[key.strip()] = value.strip()
    return out


def parse_talk_blocks(text: str) -> tuple[dict[str, str], list[str]]:
    entries: dict[str, str] = {}
    warnings: list[str] = []

    for block in TALK_RE.findall(text):
        header: dict[str, str] = {}
        lines: list[tuple[str, str]] = []

        for raw in block.splitlines():
            line = raw.rstrip()
            if not line.strip():
                continue
            hm = HEADER_RE.match(line)
            if hm and not lines:
                header[hm.group(1)] = hm.group(2).strip()
                continue
            sm = SPEAKER_RE.match(line)
            if sm:
                lines.append((sm.group(1), sm.group(2)))
            elif lines:
                speaker, prev = lines[-1]
                lines[-1] = (speaker, prev + "\n" + line.strip())
            else:
                warnings.append(f"无法解析的行：{line[:40]}")

        ancient = header.get("先古", "").strip()
        set_raw = header.get("套号", "").strip()
        repeat = header.get("可重复", "").strip() == "是"
        if not ancient or not set_raw:
            continue
        if not any(body.strip() for _sp, body in lines):
            continue  # 空模板，跳过

        who = "firstVisitEver" if set_raw.lower().startswith("firstvisit") else CHAR_ID
        set_index = "0" if who == "firstVisitEver" else set_raw.strip()
        set_key = set_index + ("r" if repeat and who != "firstVisitEver" else "")

        for line_no, (speaker, body) in enumerate(lines):
            body = body.strip("\n")
            if not body.strip():
                continue
            kind = {"古": "ancient", "她": "char", "钮": "next"}[speaker]
            if kind == "next" and body.strip() == DEFAULT_NEXT:
                continue
            key = f"{ancient}.talk.{who}.{set_key}-{line_no}.{kind}"
            if key in entries:
                warnings.append(f"重复的键：{key}")
            entries[key] = body

    return entries, warnings


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    text = DOC.read_text(encoding="utf-8")

    info = parse_character_block(text)
    entries, warnings = parse_talk_blocks(text)

    print("角色选择界面字段：")
    for key in ("显示名", "第三人称称呼", "角色描述", "图鉴引语", "事件死亡保护台词"):
        value = info.get(key, "")
        print(f"  {key}: {value if value else '（空）'}")

    print(f"\n先古对话条目：{len(entries)} 条")

    grouped: dict[tuple[str, str], list[str]] = {}
    for key in entries:
        m = re.match(r"^([A-Z_]+)\.talk\.([A-Za-z_]+)\.(\d+)r?-(\d+)\.", key)
        if m:
            grouped.setdefault((m.group(1), m.group(2)), []).append(key)
    for (ancient, who), keys in sorted(grouped.items()):
        sets = sorted({re.match(r"^[A-Z_]+\.talk\.[A-Za-z_]+\.(\d+)r?-\d+\.", k).group(1) for k in keys})
        print(f"  {ancient:<14} {who:<16} 套号 {','.join(sets)}  共 {len(keys)} 行")

    for w in warnings:
        print(f"  ⚠ {w}")

    if dry_run:
        print("\n预览模式，未写入")
        return 0

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(entries, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"\n已写入 {OUT.relative_to(ROOT)}")

    # 生成 C# 数据表：先古 → [(对话序号, 行数)]
    by_ancient: dict[str, dict[int, int]] = {}
    for key in entries:
        m = re.match(r"^([A-Z_]+)\.talk\.[A-Za-z_-]+\.(\d+)r?-(\d+)\.", key)
        if not m:
            continue
        ancient, set_index, line_no = m.group(1), int(m.group(2)), int(m.group(3))
        by_ancient.setdefault(ancient, {})[set_index] = max(
            by_ancient.get(ancient, {}).get(set_index, 0), line_no + 1
        )

    rows = []
    for ancient in sorted(by_ancient):
        sets = ", ".join(f"({idx}, {cnt})" for idx, cnt in sorted(by_ancient[ancient].items()))
        rows.append(f'        ["{ancient}"] = new[] {{ {sets} }},')

    cs = (
        "using System.Collections.Generic;\n"
        "\n"
        "namespace Probe;\n"
        "\n"
        "/// <summary>由 tools/import_writing.py 生成，不要手改。</summary>\n"
        "internal static class ProbeAncientDialogueData\n"
        "{\n"
        "    /// <summary>先古 ID → (对话序号, 行数)。对话序号就是 loc 键里的 X。</summary>\n"
        "    public static readonly Dictionary<string, (int Index, int Lines)[]> Sets = new()\n"
        "    {\n" + "\n".join(rows) + "\n"
        "    };\n"
        "\n"
        "    public const string CharacterEntry = \"PROBE-SARIA\";\n"
        "}\n"
    )
    cs_path = ROOT / "src" / "ProbeAncientDialogueData.cs"
    cs_path.write_text(cs, encoding="utf-8")
    print(f"已写入 {cs_path.relative_to(ROOT)}：{len(by_ancient)} 位先古")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
