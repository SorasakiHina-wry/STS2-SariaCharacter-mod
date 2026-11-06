#!/usr/bin/env python3
"""Parse and cross-check the Probe design sheets.

Reads three sibling files in design/:
    cards.md      cards      (```card blocks, plus bulk table rows)
    powers.md     buffs      (```power blocks)
    mechanics.md  mechanics  (```mechanic blocks)

The sheets are written for a human, so parsing is deliberately forgiving: Chinese or English
colons, half or full width pipes, missing optional fields, free-form notes.

Usage:
    python tools/cardspec.py                # check all three sheets
    python tools/cardspec.py --json         # dump everything as JSON (for codegen)
    python tools/cardspec.py cards.md       # check a specific cards sheet (siblings still read)
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REQUIRED_CARD = ("名称", "稀有度", "基础", "升级")
RARITY = {
    "基础": "Basic", "basic": "Basic",
    "普通": "Common", "common": "Common",
    "罕见": "Uncommon", "uncommon": "Uncommon",
    "稀有": "Rare", "rare": "Rare",
}
TARGET = {
    "单体敌人": "AnyEnemy", "敌人": "AnyEnemy", "anyenemy": "AnyEnemy",
    "全体敌人": "AllEnemies", "allenemies": "AllEnemies",
    "随机敌人": "RandomEnemy", "randomenemy": "RandomEnemy",
    "自己": "Self", "自身": "Self", "self": "Self",
    "无": "None", "none": "None",
}
# RelicRarity: None, Starter, Common, Uncommon, Rare, Shop, Event, Ancient
RARITY_RELIC = {
    "初始": "Starter", "starter": "Starter",
    "普通": "Common", "common": "Common",
    "罕见": "Uncommon", "uncommon": "Uncommon",
    "稀有": "Rare", "rare": "Rare",
    "商店": "Shop", "shop": "Shop",
    "事件": "Event", "event": "Event",
    "远古": "Ancient", "ancient": "Ancient",
}
# statuses the game already ships - no power definition needed
VANILLA_STATUS = {"易伤", "虚弱", "脆弱", "中毒", "荆棘", "力量", "敏捷"}
VANILLA_EN = {"vulnerable", "weak", "frail", "poison", "thorns", "strength", "dexterity"}

DAMAGE_RE = re.compile(r"造成|伤害")
BLOCK_RE = re.compile(r"格挡")
SKILL_HINT_RE = re.compile(r"抽|施加|回复|能量|失去生命|丢弃|消耗")
STATUS_RE = re.compile(r"(?:施加|获得)\s*(\d+)\s*层([^\s|｜，,。.；;、]+)")

# Workflow tags the author puts inside 备注. 已实装 = implemented, leave it alone.
STATUS_TAGS = ("已实装", "待实装", "待修改", "待删除")
TITLE_RE = re.compile(r'\("title"\s*,\s*"([^"]+)"\)')
# cards whose localization is built by a helper, e.g. ProbeCardKit.DamageLoc("Probe Tap")
TITLE_HELPER_RE = re.compile(r'(?:DamageLoc|BlockLoc)\(\s*"([^"]+)"\s*\)')

KV_RE = re.compile(r"^([^:：]{1,8})[:：]\s*(.*)$")
FENCE_RE = re.compile(r"^\s*```+\s*(\w*)\s*$")


# --------------------------------------------------------------------------- generic parsing


def parse_blocks(path: Path, kind: str) -> list[dict]:
    """Parse every ```<kind> fenced block in a markdown file."""
    if not path.exists():
        return []
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    blocks: list[dict] = []
    i = 0
    while i < len(lines):
        m = FENCE_RE.match(lines[i])
        if m and (m.group(1) or "").lower() == kind:
            start = i + 1
            j = start
            while j < len(lines) and not FENCE_RE.match(lines[j]):
                j += 1
            raw: dict[str, str] = {}
            for line in lines[start:j]:
                line = line.strip()
                if not line or line.startswith("#"):
                    continue
                kv = KV_RE.match(line)
                if kv:
                    raw[kv.group(1).strip()] = kv.group(2).strip()
                else:
                    raw["备注"] = (raw.get("备注", "") + " " + line).strip()
            blocks.append({"_raw": raw, "line": start + 1})
            i = j + 1
            continue
        i += 1
    return blocks


def _split_effects(value: str) -> list[str]:
    return [p.strip() for p in re.split(r"[|｜]", value or "") if p.strip()]


def _extract_cost(text: str) -> int | None:
    m = re.search(r"费用\s*(\d+)", text or "")
    return int(m.group(1)) if m else None


def _extract_status(notes: str) -> tuple[str, str]:
    """Split the workflow tag out of 备注 -> (status, remaining notes).

    Tolerates spaces inside the tag ("已 实装") and multiple tags (the most actionable wins:
    待删除 > 待修改 > 待实装 > 已实装), so a half-edited entry is never mistaken for done.
    """
    if not notes:
        return "", ""
    for tag in ("待删除", "待修改", "待实装", "已实装"):
        pattern = r"\s*".join(tag)  # 已实装 -> 已\s*实装
        m = re.search(pattern, notes)
        if m:
            rest = notes[:m.start()] + " " + notes[m.end():]
            # drop any other status tags that were left behind, then tidy separators
            for other in STATUS_TAGS:
                rest = re.sub(r"\s*".join(other), "", rest)
            return tag, rest.strip(" \t：:，,;；。、")
    return "", notes.strip()


def scan_code_titles(src_dir: Path) -> dict[str, str]:
    """Map every localization title found in src/**.cs to its file (relative to the project)."""
    titles: dict[str, str] = {}
    if not src_dir.exists():
        return titles
    for path in src_dir.rglob("*.cs"):
        try:
            text = path.read_text(encoding="utf-8-sig")
        except Exception:
            continue
        for m in TITLE_RE.finditer(text):
            titles.setdefault(m.group(1).strip(), str(path.relative_to(src_dir.parent)))
        for m in TITLE_HELPER_RE.finditer(text):
            titles.setdefault(m.group(1).strip(), str(path.relative_to(src_dir.parent)))
    return titles


def _normalise_type(raw: str, effects: list[str]) -> str:
    r = (raw or "").strip().lower()
    if r in ("攻击", "attack"):
        return "Attack"
    if r in ("技能", "skill"):
        return "Skill"
    if r in ("能力", "power"):
        return "Power"
    joined = " ".join(effects)
    if DAMAGE_RE.search(joined):
        return "Attack"
    if BLOCK_RE.search(joined) or SKILL_HINT_RE.search(joined):
        return "Skill"
    return "Skill"


def _normalise_target(raw: str, card_type: str) -> str:
    r = (raw or "").strip()
    return TARGET.get(r.lower(), TARGET.get(r, "AnyEnemy" if card_type == "Attack" else "Self"))


def _build_card(raw: dict, line_no: int) -> dict:
    status, notes = _extract_status(raw.get("备注", ""))
    card = {
        "name": raw.get("名称", ""),
        "rarity_raw": raw.get("稀有度", ""),
        "type_raw": raw.get("类型", ""),
        "target_raw": raw.get("目标", ""),
        "base_raw": raw.get("基础", ""),
        "upgrade_raw": raw.get("升级", ""),
        "english": raw.get("英文名", ""),
        "notes": notes,
        "status": status,
        "disabled": raw.get("停用", "").strip() in ("是", "yes", "true", "y"),
        "line": line_no,
        "_raw": raw,
    }
    card["rarity"] = RARITY.get(card["rarity_raw"].lower()) or RARITY.get(card["rarity_raw"])
    card["effects_base"] = _split_effects(card["base_raw"])
    card["effects_upgrade"] = _split_effects(card["upgrade_raw"])
    card["cost_base"] = _extract_cost(card["base_raw"])
    card["cost_upgrade"] = _extract_cost(card["upgrade_raw"])
    card["type"] = _normalise_type(card["type_raw"], card["effects_base"])
    card["target"] = _normalise_target(card["target_raw"], card["type"])
    return card


def parse_cards(path: Path) -> list[dict]:
    """Parse ```card blocks plus bulk table rows whose header starts with 名称."""
    cards: list[dict] = []
    if not path.exists():
        return cards

    for block in parse_blocks(path, "card"):
        cards.append(_build_card(block["_raw"], block["line"]))

    lines = path.read_text(encoding="utf-8-sig").splitlines()
    i = 0
    while i < len(lines) - 1:
        if lines[i].lstrip().startswith("|") and "名称" in lines[i]:
            header = [c.strip() for c in lines[i].strip().strip("|").split("|")]
            j = i + 2
            while j < len(lines) and lines[j].lstrip().startswith("|"):
                cells = [c.strip() for c in lines[j].strip().strip("|").split("|")]
                if len(cells) >= 4 and cells[0]:
                    row = dict(zip(header, cells))
                    raw = {
                        "名称": row.get("名称", ""),
                        "稀有度": row.get("稀有度", ""),
                        "基础": row.get("基础", ""),
                        "升级": row.get("升级", ""),
                    }
                    for key in ("类型", "目标", "英文名", "备注"):
                        if row.get(key):
                            raw[key] = row[key]
                    cards.append(_build_card(raw, j + 1))
                j += 1
            i = j
            continue
        i += 1

    return cards


def parse_powers(path: Path) -> list[dict]:
    powers = []
    for b in parse_blocks(path, "power"):
        raw = b["_raw"]
        status, notes = _extract_status(raw.get("备注", ""))
        powers.append({
            "name": raw.get("名称", ""),
            "kind": raw.get("类型", "").strip().lower(),
            "stack": raw.get("叠层", ""),
            "decay": raw.get("衰减", ""),
            "trigger": raw.get("触发", ""),
            "effect": raw.get("效果", ""),
            "description": raw.get("描述", ""),
            "icon": raw.get("图标", ""),
            "notes": notes,
            "status": status,
            "line": b["line"],
            "_raw": raw,
        })
    return powers


def parse_mechanics(path: Path) -> list[dict]:
    mechs = []
    for b in parse_blocks(path, "mechanic"):
        raw = b["_raw"]
        status, notes = _extract_status(raw.get("备注", ""))
        mechs.append({
            "name": raw.get("名称", ""),
            "scope": raw.get("范围", ""),
            "summary": raw.get("一句话", ""),
            "timing": raw.get("时机", ""),
            "details": raw.get("细节", ""),
            "cards": raw.get("涉及", ""),
            "notes": notes,
            "status": status,
            "line": b["line"],
            "_raw": raw,
        })
    return mechs


def parse_relics(path: Path) -> list[dict]:
    relics = []
    for b in parse_blocks(path, "relic"):
        raw = b["_raw"]
        rarity_raw = raw.get("稀有度", "").strip()
        status, notes = _extract_status(raw.get("备注", ""))
        relics.append({
            "name": raw.get("名称", ""),
            "rarity_raw": rarity_raw,
            "rarity": RARITY_RELIC.get(rarity_raw.lower()) or RARITY_RELIC.get(rarity_raw),
            "trigger": raw.get("触发", ""),
            "effect": raw.get("效果", ""),
            "effects": _split_effects(raw.get("效果", "")),
            "description": raw.get("描述", ""),
            "icon": raw.get("图标", ""),
            "notes": notes,
            "status": status,
            "disabled": raw.get("停用", "").strip() in ("是", "yes", "true", "y"),
            "line": b["line"],
            "_raw": raw,
        })
    return relics


# --------------------------------------------------------------------------- validation


def validate_cards(cards: list[dict]) -> list[str]:
    problems: list[str] = []
    seen: dict[str, int] = {}

    for c in cards:
        if c["disabled"] or not c["name"].strip() or c.get("status") == "待删除":
            continue
        where = f"  cards.md 第 {c['line']} 行 [{c['name']}]"
        for field in REQUIRED_CARD:
            if not c["_raw"].get(field):
                problems.append(f"{where}: 缺少必填字段「{field}」")
        if c["rarity_raw"] and not c["rarity"]:
            problems.append(f"{where}: 稀有度「{c['rarity_raw']}」无法识别（用 基础/普通/罕见/稀有）")
        if c["cost_base"] is None:
            problems.append(f"{where}: 「基础」里没找到「费用 N」")
        seen[c["name"]] = seen.get(c["name"], 0) + 1
        if seen[c["name"]] > 1:
            problems.append(f"{where}: 卡名重复")
        if c["base_raw"] and c["base_raw"] == c["upgrade_raw"]:
            problems.append(f"{where}: 升级前后完全一样（是不是忘了写升级效果？）")

    counts: dict[str, int] = {}
    types: dict[str, int] = {}
    for c in cards:
        if c["disabled"] or not c["name"].strip() or c.get("status") == "待删除":
            continue
        if c["rarity"]:
            counts[c["rarity"]] = counts.get(c["rarity"], 0) + 1
        types[c["type"]] = types.get(c["type"], 0) + 1

    for rarity in ("Common", "Uncommon", "Rare"):
        if counts.get(rarity, 0) < 6:
            problems.append(
                f"  卡池: {rarity} 只有 {counts.get(rarity, 0)} 张，至少需要 6 张（奖励抽干会抛异常）"
            )
    non_basic: dict[str, int] = {}
    for c in cards:
        if c["disabled"] or not c["name"].strip() or c["rarity"] in ("Basic", None):
            continue
        non_basic[c["type"]] = non_basic.get(c["type"], 0) + 1
    for t, need in (("Attack", 2), ("Skill", 2), ("Power", 1)):
        if non_basic.get(t, 0) < need:
            problems.append(
                f"  卡池: 非基础 {t} 只有 {non_basic.get(t, 0)} 张，至少需要 {need} 张（商店五槽会卡死）"
            )
    return problems


def validate_powers(powers: list[dict]) -> list[str]:
    problems: list[str] = []
    seen: set[str] = set()
    for p in powers:
        if not p["name"].strip() or p.get("status") == "待删除":
            continue
        where = f"  powers.md 第 {p['line']} 行 [{p['name']}]"
        if p["name"] in seen:
            problems.append(f"{where}: 状态名重复")
        seen.add(p["name"])
        for field in ("类型", "叠层", "效果", "描述"):
            if not p["_raw"].get(field):
                problems.append(f"{where}: 缺少必填字段「{field}」")
        if p["kind"] and p["kind"] not in ("buff", "debuff"):
            problems.append(f"{where}: 类型「{p['kind']}」只能是 buff 或 debuff")
        if p["stack"] and p["stack"] not in ("计数", "单次"):
            problems.append(f"{where}: 叠层「{p['stack']}」只能是 计数 或 单次")
        if p["name"] in VANILLA_STATUS:
            problems.append(f"{where}: 「{p['name']}」是原版就有的状态，不需要自定义（会冲突）")
    return problems


def validate_relics(relics: list[dict]) -> tuple[list[str], list[str]]:
    problems: list[str] = []
    infos: list[str] = []
    seen: set[str] = set()
    starters = 0
    counts: dict[str, int] = {}

    for r in relics:
        if r["disabled"] or not r["name"].strip() or r.get("status") == "待删除":
            continue
        where = f"  relics.md 第 {r['line']} 行 [{r['name']}]"
        if r["name"] in seen:
            problems.append(f"{where}: 遗物名重复")
        seen.add(r["name"])
        for field in ("稀有度", "效果"):
            if not r["_raw"].get(field):
                problems.append(f"{where}: 缺少必填字段「{field}」")
        if not r["description"].strip():
            infos.append(f"{where}: 没写「描述」——玩家看到的文本会退化成「效果」原文")
        if r["rarity_raw"] and not r["rarity"]:
            problems.append(
                f"{where}: 稀有度「{r['rarity_raw']}」无法识别（初始/普通/罕见/稀有/商店/事件/远古）"
            )
        if r["rarity"] == "Starter":
            starters += 1
        if r["rarity"]:
            counts[r["rarity"]] = counts.get(r["rarity"], 0) + 1

    if starters == 0:
        infos.append("relics.md: 没有「初始」遗物，角色会没有起手遗物")
    elif starters > 1:
        problems.append(f"  relics.md: 有 {starters} 个「初始」遗物，一个角色只能有 1 个")

    for rarity in ("Common", "Uncommon", "Rare"):
        if counts.get(rarity, 0) < 3:
            infos.append(
                f"relics.md: {rarity} 只有 {counts.get(rarity, 0)} 个，建议至少 3 个（奖励按档位抽）"
            )
    return problems, infos


def validate_mechanics(mechs: list[dict]) -> list[str]:
    problems: list[str] = []
    seen: set[str] = set()
    for m in mechs:
        if not m["name"].strip() or m.get("status") == "待删除":
            continue
        where = f"  mechanics.md 第 {m['line']} 行 [{m['name']}]"
        if m["name"] in seen:
            problems.append(f"{where}: 机制名重复")
        seen.add(m["name"])
        for field in ("范围", "一句话"):
            if not m["_raw"].get(field):
                problems.append(f"{where}: 缺少必填字段「{field}」")
        if m["scope"] and m["scope"] not in ("关键字", "单卡"):
            problems.append(f"{where}: 范围「{m['scope']}」只能是 关键字 或 单卡")
    return problems


def cross_check(
    cards: list[dict], powers: list[dict], mechs: list[dict], relics: list[dict]
) -> tuple[list[str], list[str]]:
    """Card/relic text may only reference defined powers; report unused definitions."""
    problems: list[str] = []
    infos: list[str] = []
    defined = {p["name"] for p in powers if p["name"].strip()}
    used_powers: set[str] = set()
    used_mechs: set[str] = set()

    def check_statuses(text: str, where: str) -> None:
        for status in STATUS_RE.findall(text):
            name = status[1].strip()
            if name in VANILLA_STATUS or name.lower() in VANILLA_EN:
                continue
            if name in defined:
                used_powers.add(name)
            else:
                problems.append(
                    f"{where}: 用到了未定义的「{name}」——请先写进 powers.md，或改用原版状态"
                )

    for c in cards:
        if c["disabled"] or not c["name"].strip() or c.get("status") == "待删除":
            continue
        where = f"  cards.md 第 {c['line']} 行 [{c['name']}]"
        text = " ".join([c["base_raw"], c["upgrade_raw"], c["notes"]])
        check_statuses(text, where)
        for m in mechs:
            if m["name"].strip() and m["name"] in text:
                used_mechs.add(m["name"])

    for r in relics:
        if r["disabled"] or not r["name"].strip() or r.get("status") == "待删除":
            continue
        where = f"  relics.md 第 {r['line']} 行 [{r['name']}]"
        text = " ".join([r["effect"], r["notes"]])
        check_statuses(text, where)
        for m in mechs:
            if m["name"].strip() and m["name"] in text:
                used_mechs.add(m["name"])

    for p in powers:
        if p["name"].strip() and p["name"] not in used_powers:
            infos.append(f"powers.md: 「{p['name']}」还没有任何卡牌或遗物引用")
    for m in mechs:
        if m["name"].strip() and m["name"] not in used_mechs:
            infos.append(f"mechanics.md: 机制「{m['name']}」还没有任何卡牌提到（记得在卡牌文本或备注里写出名字）")

    return problems, infos


# --------------------------------------------------------------------------- workflow queue


def build_queue(items: list[tuple[str, dict]], code_titles: dict[str, str]) -> tuple[list[str], list[str]]:
    """Consistency checks between the workflow tags and the actual source files."""
    problems: list[str] = []
    infos: list[str] = []
    for kind, item in items:
        name = item["name"].strip()
        if not name or item.get("disabled"):
            continue
        where = f"  {kind} 第 {item['line']} 行 [{name}]"
        status = item.get("status", "")
        in_code = name in code_titles

        if not status:
            infos.append(f"{where}: 没有状态标记（{'/'.join(STATUS_TAGS)}）")
        elif status == "已实装" and not in_code:
            problems.append(f"{where}: 标了「已实装」，但 src 里找不到同名 title")
        elif status == "待实装" and in_code:
            problems.append(f"{where}: 标了「待实装」，但 {code_titles[name]} 已存在同名")
        elif status == "待删除" and not in_code:
            infos.append(f"{where}: 标了「待删除」，代码里已经没有了——这一条可以直接从 md 删掉")
    return problems, infos


def print_queue(items: list[tuple[str, dict]], code_titles: dict[str, str]) -> None:
    printed = False
    for tag in ("待实装", "待修改", "待删除"):
        rows = [(k, i) for k, i in items if i.get("status") == tag and i["name"].strip()]
        if not rows:
            continue
        printed = True
        print(f"\n[{tag}] {len(rows)} 条")
        for kind, item in rows:
            detail = item.get("notes", "")
            target = f" → {code_titles[item['name']]}" if item["name"] in code_titles else ""
            print(f"  - {kind}: {item['name']}（第 {item['line']} 行）{(' · ' + detail) if detail else ''}{target}")
    if printed:
        print()


def _spec_line(kind: str, item: dict) -> str:
    """One compact line containing everything needed to implement the entry."""
    name = item["name"]
    if kind == "cards":
        return (f"{name} | {item['rarity'] or '?'} | {item['type']} | {item['target']} | "
                f"基础: {item['base_raw']} | 升级: {item['upgrade_raw']}"
                + (f" | 备注: {item['notes']}" if item["notes"] else ""))
    if kind == "powers":
        bits = [f"{name}", item["kind"] or "?"]
        for label, key in (("叠层", "stack"), ("衰减", "decay"), ("触发", "trigger"),
                           ("效果", "effect"), ("描述", "description")):
            if item.get(key):
                bits.append(f"{label}: {item[key]}")
        return " | ".join(bits)
    if kind == "mechanics":
        bits = [f"{name}", item["scope"] or "?"]
        for label, key in (("规则", "summary"), ("时机", "timing"), ("细节", "details")):
            if item.get(key):
                bits.append(f"{label}: {item[key]}")
        return " | ".join(bits)
    # relics
    bits = [f"{name}", item["rarity"] or "?"]
    for label, key in (("触发", "trigger"), ("效果", "effect"), ("描述", "description")):
        if item.get(key):
            bits.append(f"{label}: {item[key]}")
    return " | ".join(bits)


def print_pending(items: list[tuple[str, dict]], code_titles: dict[str, str]) -> int:
    """Full specs of everything that still needs work - the only thing worth reading per round."""
    count = 0
    for tag in ("待实装", "待修改", "待删除", ""):
        rows = [(k, i) for k, i in items
                if i.get("status") == tag and i["name"].strip() and not i.get("disabled")]
        if not rows:
            continue
        label = tag or "未标记"
        print(f"\n[{label}] {len(rows)} 条")
        for kind, item in rows:
            target = f" → {code_titles[item['name']]}" if item["name"] in code_titles else ""
            print(f"  - {kind}: {_spec_line(kind, item)}{target}")
            count += 1
    if count == 0:
        print("没有待处理的条目（全部已实装）")
    return count


def _blocks_by_name(text: str, kind: str) -> dict[str, str]:
    out: dict[str, str] = {}
    for m in re.finditer(r"```" + kind + r"\s*\n(.*?)```", text, re.S):
        body = m.group(1)
        nm = re.search(r"^名称[:：][ \t]*(.+?)[ \t]*$", body, re.M)
        if nm:
            out[nm.group(1).strip()] = body.strip()
    return out


def _head_text(path: Path) -> str | None:
    """File content as committed in HEAD ('' when the file is new), None if git is unavailable."""
    import subprocess
    try:
        root = subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=str(path.parent),
                              capture_output=True, text=True, encoding="utf-8")
        if root.returncode != 0:
            return None
        repo = Path(root.stdout.strip())
        rel = path.resolve().relative_to(repo.resolve()).as_posix()
        show = subprocess.run(["git", "show", f"HEAD:{rel}"], cwd=str(repo),
                              capture_output=True, text=True, encoding="utf-8")
        return show.stdout if show.returncode == 0 else ""
    except Exception:
        return None


def print_changed(design_dir: Path) -> None:
    """Entries added or edited since the last commit - catches untagged edits too."""
    total = 0
    for name, kind in (("cards.md", "card"), ("powers.md", "power"),
                       ("mechanics.md", "mechanic"), ("relics.md", "relic")):
        path = design_dir / name
        if not path.exists():
            continue
        head = _head_text(path)
        if head is None:
            print("（不在 git 仓库里，跳过 --changed）")
            return

        old = _blocks_by_name(head, kind)
        new = _blocks_by_name(path.read_text(encoding="utf-8-sig"), kind)
        added = [n for n in new if n not in old]
        removed = [n for n in old if n not in new]
        modified = [n for n in new if n in old and new[n] != old[n]]
        if added or removed or modified:
            total += len(added) + len(removed) + len(modified)
            print(f"\n{name}")
            for label, rows in (("新增", added), ("修改", modified), ("删除", removed)):
                if rows:
                    print(f"  {label}（{len(rows)}）: " + "、".join(rows))
    if total == 0:
        print("设计表自上次提交以来没有变化")


# --------------------------------------------------------------------------- main


def main(argv: list[str]) -> int:
    args = [a for a in argv[1:] if not a.startswith("--")]
    as_json = "--json" in argv
    todo_only = "--todo" in argv
    default_dir = Path(__file__).resolve().parent.parent / "design"
    cards_path = Path(args[0]) if args else default_dir / "cards.md"
    design_dir = cards_path.parent

    cards = parse_cards(cards_path)
    powers = parse_powers(design_dir / "powers.md")
    mechs = parse_mechanics(design_dir / "mechanics.md")
    relics = parse_relics(design_dir / "relics.md")

    active_cards = [c for c in cards if not c["disabled"] and c["name"].strip()]
    active_powers = [p for p in powers if p["name"].strip()]
    active_mechs = [m for m in mechs if m["name"].strip()]
    active_relics = [r for r in relics if not r["disabled"] and r["name"].strip()]

    if as_json:
        pending_only = "--pending" in argv
        if pending_only:
            active_cards = [c for c in active_cards if c.get("status") != "已实装"]
            active_powers = [p for p in active_powers if p.get("status") != "已实装"]
            active_mechs = [m for m in active_mechs if m.get("status") != "已实装"]
            active_relics = [r for r in active_relics if r.get("status") != "已实装"]
        print(json.dumps(
            {"cards": active_cards, "powers": active_powers, "mechanics": active_mechs, "relics": active_relics},
            ensure_ascii=False, indent=2,
        ))
        return 0

    items_for_modes: list[tuple[str, dict]] = (
        [("cards", c) for c in cards]
        + [("powers", p) for p in powers]
        + [("mechanics", m) for m in mechs]
        + [("relics", r) for r in relics]
    )

    # Cheap modes for the per-round workflow: no full-sheet reading needed.
    if "--pending" in argv:
        print_pending(items_for_modes, scan_code_titles(design_dir.parent / "src"))
        return 0
    if "--changed" in argv:
        print_changed(design_dir)
        return 0

    print(f"卡牌表: {cards_path}")
    print(f"  解析到 {len(cards)} 条（有效 {len(active_cards)} 张）")
    print(f"状态表: {design_dir / 'powers.md'}  →  {len(active_powers)} 个自定义状态")
    print(f"机制表: {design_dir / 'mechanics.md'}  →  {len(active_mechs)} 个机制")
    print(f"遗物表: {design_dir / 'relics.md'}  →  {len(active_relics)} 个遗物\n")

    counts: dict[str, int] = {}
    types: dict[str, int] = {}
    for c in active_cards:
        counts[c["rarity"] or "?"] = counts.get(c["rarity"] or "?", 0) + 1
        types[c["type"]] = types.get(c["type"], 0) + 1
    print("稀有度: " + "  ".join(f"{r}={counts.get(r, 0)}" for r in ("Basic", "Common", "Uncommon", "Rare")))
    print("类型  : " + "  ".join(f"{t}={n}" for t, n in sorted(types.items())))

    if active_relics:
        rcounts: dict[str, int] = {}
        for r in active_relics:
            rcounts[r["rarity"] or "?"] = rcounts.get(r["rarity"] or "?", 0) + 1
        print("遗物  : " + "  ".join(f"{k}={v}" for k, v in sorted(rcounts.items())))
    print()

    relic_problems, relic_infos = validate_relics(relics)
    problems = validate_cards(cards) + validate_powers(powers) + validate_mechanics(mechs) + relic_problems
    cross_problems, infos = cross_check(cards, powers, mechs, relics)
    problems += cross_problems
    infos += relic_infos

    # ---- workflow queue (已实装 / 待实装 / 待修改 / 待删除) ----
    src_dir = design_dir.parent / "src"
    code_titles = scan_code_titles(src_dir)
    items: list[tuple[str, dict]] = (
        [("cards", c) for c in cards]
        + [("powers", p) for p in powers]
        + [("mechanics", m) for m in mechs]
        + [("relics", r) for r in relics]
    )
    queue_problems, queue_infos = build_queue(items, code_titles)
    problems += queue_problems
    infos += queue_infos

    print_queue(items, code_titles)

    for info in infos:
        print(f"提示: {info}")

    if problems:
        print(f"\n发现 {len(problems)} 个问题:")
        for p in problems:
            print(p)
        return 1

    if todo_only:
        untouched = sum(1 for _, i in items if i.get("status") == "已实装")
        print(f"\n已实装 {untouched} 条（跳过），其余见上。")
        return 0

    print("\n校验通过 ✅")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
