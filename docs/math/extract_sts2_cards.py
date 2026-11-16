#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""从 STS2 Modding MCP 的反编译/罗森索引数据中提取原版卡牌数值。

数据来源（均为 STS2 Modding MCP 自己维护的索引）：
  * D:\\STS2\\vendor\\sts2-modding-mcp\\decompiled\\roslyn_index.json
  * D:\\STS2\\vendor\\sts2-modding-mcp\\decompiled\\MegaCrit.Sts2.Core.Models.Cards\\*.cs
  * D:\\STS2\\.cache\\loc_extract\\localization\\zhs\\cards.json （中文名）
  * D:\\STS2\\vendor\\sts2-modding-mcp\\decompiled\\MegaCrit.Sts2.Core.Models.CardPools\\*.cs

输出列：中文名,费用,作用范围,伤害,格挡,易伤,虚弱
  * 作用范围：0 = 单体（含只作用于自己的牌），1 = 全体敌人
  * 数值均取「升级后」的值；多段攻击的伤害为总伤害
  * X 费牌、随机目标牌、伤害为动态计算的牌会被跳过（见 --skipped 报告）

用法：
  python extract_sts2_cards.py --out cards.csv [--skipped skipped.txt]
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(r"D:\STS2\vendor\sts2-modding-mcp")
DECOMPILED = ROOT / "decompiled"
INDEX = DECOMPILED / "roslyn_index.json"
LOC = Path(r"D:\STS2\.cache\loc_extract\localization\zhs\cards.json")

NUM = r"(-?\d+(?:\.\d+)?)"

# ---------------------------------------------------------------------------
# 基础读取
# ---------------------------------------------------------------------------


def load_index() -> dict:
    with INDEX.open(encoding="utf-8") as f:
        return json.load(f)


def load_loc() -> dict:
    with LOC.open(encoding="utf-8") as f:
        return json.load(f)


def class_to_key(class_name: str) -> str:
    """StrikeIronclad -> STRIKE_IRONCLAD (游戏存档/本地化里的 Entry 写法)。"""
    s = re.sub(r"(?<!^)(?=[A-Z])", "_", class_name)
    return s.upper()


def read_method_body(index_cls: dict, class_name: str, method: str) -> str:
    """按索引里的行号取出某个方法的源码正文。"""
    meta = None
    for m in index_cls.get("methods", []):
        if m["name"] == method:
            meta = m
            break
    if meta is None:
        return ""
    path = DECOMPILED / index_cls["file"]
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except OSError:
        return ""
    start = max(0, meta["line_start"] - 1)
    end = meta["line_end"]
    return "\n".join(lines[start:end])


# ---------------------------------------------------------------------------
# 卡牌静态数据解析
# ---------------------------------------------------------------------------


def base_args(index_cls: dict) -> list[str]:
    return index_cls.get("constructor_base_args") or []


def arg_value(args: list[str], prefix: str) -> str | None:
    for a in args:
        if a.startswith(prefix):
            return a[len(prefix):]
    return None


def prop_expr(index_cls: dict, name: str) -> str | None:
    for p in index_cls.get("properties", []):
        if p["name"] == name:
            return p.get("expression_value")
    return None


def canonical_vars(index_cls: dict) -> str:
    return prop_expr(index_cls, "CanonicalVars") or ""


def parse_m(value: str) -> float | None:
    m = re.fullmatch(r"\s*(-?\d+(?:\.\d+)?)m?\s*", value)
    return float(m.group(1)) if m else None


def int_if_whole(v: float | None) -> int | float | None:
    if v is None:
        return None
    return int(v) if float(v).is_integer() else v


def parse_vars(cv: str) -> dict:
    """从 CanonicalVars 表达式里取出关注的基础数值。"""
    out: dict[str, float] = {}

    # 注意：DynamicVars.X / DynamicVars["X"] 里的 X 大小写不定（Damage/Block/Vulnerable/Power），
    # 统一用小写键保存，取值时也一律 lower()。
    for var_name, pattern in (
        ("damage", r"new (?:Osty)?DamageVar\(\s*" + NUM + r"m?"),
        ("block", r"new BlockVar\(\s*" + NUM + r"m?"),
        ("vulnerable", r"new PowerVar<VulnerablePower>\(\s*" + NUM + r"m?"),
        ("weak", r"new PowerVar<WeakPower>\(\s*" + NUM + r"m?"),
        ("repeat", r"new RepeatVar\(\s*" + NUM + r"m?"),
    ):
        m = re.search(pattern, cv)
        if m:
            out[var_name] = float(m.group(1))

    # 通用 DynamicVar("Power", N)：Uppercut/Shockwave/Putrefy/Expose 等用它承载层数
    for vm in re.finditer(r'new DynamicVar\("(\w+)",\s*' + NUM + r"m?\)", cv):
        out.setdefault(vm.group(1).lower(), float(vm.group(2)))

    # 动态数值标记（只认会改变伤害/格挡结果的写法）
    out["_calc_damage"] = "CalculatedDamageVar" in cv
    out["_calc_block"] = "CalculatedBlockVar" in cv
    return out


def parse_upgrades(body: str) -> dict:
    """解析 OnUpgrade 里的增量。"""
    ups: dict[str, float] = {}

    def add(key: str, val: float) -> None:
        ups[key] = ups.get(key, 0.0) + val

    for m in re.finditer(
        r'DynamicVars(?:\.(\w+)|\["(\w+)"\])\.UpgradeValueBy\(\s*' + NUM + r"m?\)", body
    ):
        key = (m.group(1) or m.group(2)).lower()
        add(key, float(m.group(3)))

    m = re.search(r"EnergyCost\.UpgradeBy\(\s*" + NUM + r"\)", body)
    if m:
        add("_cost", float(m.group(1)))

    return ups


def apply_upgrades(base: dict, ups: dict) -> dict:
    out = dict(base)
    for key, delta in ups.items():
        if key == "_cost":
            continue
        if key in out:
            out[key] += delta
        else:
            out[key] = delta
    return out


# ---------------------------------------------------------------------------
# 效果解析：攻击段数、是否全体、施加的易伤/虚弱
# ---------------------------------------------------------------------------


def parse_hit_count(body: str, vars_: dict) -> int | None:
    """返回攻击段数；无法确定（动态）时返回 None。"""
    hits = re.findall(r"WithHitCount\(([^)]*)\)", body)
    if not hits:
        return 1
    if len(set(hits)) > 1:
        return None
    expr = hits[0].strip()
    if re.fullmatch(r"\d+", expr):
        return int(expr)
    if "DynamicVars.Repeat" in expr:
        rep = vars_.get("repeat")
        if rep is not None and float(rep).is_integer():
            return int(rep)
        return None
    return None


def parse_scope(target_type: str | None, body: str) -> int | None:
    """返回 1=全体敌人 / 0=单体（含自己）；随机目标返回 None。"""
    if target_type == "AllEnemies":
        return 1
    if target_type == "RandomEnemy":
        return None
    if "TargetingAllOpponents" in body:
        return 1
    return 0


def debuff_amount(body: str, power: str, vars_: dict) -> float | None:
    """从 OnPlay 里找出施加某个 power 的层数；无法确定时返回 None。"""
    key = f"PowerCmd.Apply<{power}>"
    if key not in body:
        return 0.0  # 这张牌根本不施加该状态

    # 抓出 Apply 调用的参数串
    idx = body.find(key)
    depth = 0
    start = body.find("(", idx)
    end = start
    for i in range(start, len(body)):
        if body[i] == "(":
            depth += 1
        elif body[i] == ")":
            depth -= 1
            if depth == 0:
                end = i
                break
    call = body[start + 1:end]
    parts = split_args(call)
    if len(parts) < 3:
        return None
    amount = parts[2].strip()

    # 1) DynamicVars.X.BaseValue / .IntValue
    m = re.match(r"DynamicVars\.(\w+)\.(?:BaseValue|IntValue|Value)", amount)
    if m:
        return vars_.get(m.group(1).lower())
    # 2) DynamicVars["X"]...
    m = re.match(r'DynamicVars\["(\w+)"\]\.(?:BaseValue|IntValue|Value)', amount)
    if m:
        return vars_.get(m.group(1).lower())
    # 3) 字面量
    v = parse_m(amount)
    if v is not None:
        return v
    # 4) 变量名（局部变量 amount/num/...）——看它赋值自哪里
    if re.fullmatch(r"\w+", amount):
        assign = re.search(
            r"int\s+" + re.escape(amount) + r"\s*=\s*([^;]+);", body
        )
        if assign:
            rhs = assign.group(1).strip()
            m = re.match(r"DynamicVars\.(\w+)\.(?:BaseValue|IntValue|Value)", rhs)
            if m:
                return vars_.get(m.group(1).lower())
            m = re.match(r'DynamicVars\["(\w+)"\]\.(?:BaseValue|IntValue|Value)', rhs)
            if m:
                return vars_.get(m.group(1).lower())
            v = parse_m(rhs)
            if v is not None:
                return v
    return None


def split_args(s: str) -> list[str]:
    """按顶层逗号切分参数。"""
    out, depth, cur = [], 0, []
    for ch in s:
        if ch in "([{<":
            depth += 1
        elif ch in ")]}>":
            depth -= 1
        if ch == "," and depth == 0:
            out.append("".join(cur))
            cur = []
        else:
            cur.append(ch)
    if cur:
        out.append("".join(cur))
    return out


# ---------------------------------------------------------------------------
# 池子（判断是否可获得）
# ---------------------------------------------------------------------------


def load_pools() -> dict[str, set[str]]:
    pools: dict[str, set[str]] = {}
    for f in (DECOMPILED / "MegaCrit.Sts2.Core.Models.CardPools").glob("*.cs"):
        txt = f.read_text(encoding="utf-8")
        pools[f.stem] = set(re.findall(r"ModelDb\.Card<(\w+)>\(\)", txt))
    return pools


NON_OBTAINABLE_POOLS = {
    "TokenCardPool",
    "StatusCardPool",
    "CurseCardPool",
    "QuestCardPool",
    "DeprecatedCardPool",
    "DeprivedCardPool",
    "MockCardPool",
}


# ---------------------------------------------------------------------------
# 主流程
# ---------------------------------------------------------------------------

SKIP_REASONS = {
    "type": "不是攻击/技能牌",
    "token": "衍生/Token/状态/诅咒牌",
    "xcost": "X 费牌",
    "random": "随机目标",
    "dynamic_damage": "伤害为动态计算",
    "dynamic_hit": "攻击段数动态",
    "no_damage_var": "有伤害指令但取不到固定伤害",
    "dynamic_block": "格挡为动态计算",
    "dynamic_debuff": "易伤/虚弱层数动态",
    "mock": "测试占位牌",
}


def extract(skipped: list[tuple[str, str, str]]) -> list[dict]:
    index = load_index()
    loc = load_loc()
    pools = load_pools()
    non_obtainable_classes: set[str] = set()
    for p in NON_OBTAINABLE_POOLS:
        non_obtainable_classes |= pools.get(p, set())

    rows: list[dict] = []
    for name, cls in sorted(index["classes"].items()):
        if cls.get("base_class") != "CardModel":
            continue

        args = base_args(cls)
        card_type = arg_value(args, "CardType.")
        if card_type not in ("Attack", "Skill"):
            continue

        rarity = arg_value(args, "CardRarity.")
        cv = canonical_vars(cls)
        on_play = read_method_body(cls, name, "OnPlay")
        on_upgrade = read_method_body(cls, name, "OnUpgrade")

        if name == "MockCardModel":
            skipped.append((name, "mock", SKIP_REASONS["mock"]))
            continue
        # 构造参数第 5 位 shouldShowInCardLibrary=false 的牌不进卡牌图鉴（事件专用/测试用）
        if len(args) > 4 and args[4] == "false":
            skipped.append((name, "token", "不在卡牌图鉴中"))
            continue
        if rarity in ("Token", "Status", "Curse", "Quest", "None") or name in non_obtainable_classes:
            skipped.append((name, "token", SKIP_REASONS["token"]))
            continue

        # X 费（能量或星）
        if (
            (prop_expr(cls, "HasEnergyCostX") or "").strip() == "true"
            or (prop_expr(cls, "HasStarCostX") or "").strip() == "true"
            or "ResolveEnergyXValue" in on_play
            or "ResolveStarXValue" in on_play
        ):
            skipped.append((name, "xcost", SKIP_REASONS["xcost"]))
            continue

        target_type = arg_value(args, "TargetType.")
        scope = parse_scope(target_type, on_play)
        if scope is None:
            skipped.append((name, "random", SKIP_REASONS["random"]))
            continue

        base = parse_vars(cv)
        ups = parse_upgrades(on_upgrade)
        vals = apply_upgrades(base, ups)

        cost = parse_m(args[0]) if args and parse_m(args[0]) is not None else None
        if cost is None:
            skipped.append((name, "xcost", SKIP_REASONS["xcost"]))
            continue
        cost = cost + ups.get("_cost", 0.0)

        has_damage_cmd = "DamageCmd.Attack" in on_play
        damage = vals.get("damage")
        if has_damage_cmd and (damage is None or base.get("_calc_damage")):
            reason = "dynamic_damage" if base.get("_calc_damage") else "no_damage_var"
            skipped.append((name, reason, SKIP_REASONS[reason]))
            continue

        hits = parse_hit_count(on_play, vals)
        if has_damage_cmd and hits is None:
            skipped.append((name, "dynamic_hit", SKIP_REASONS["dynamic_hit"]))
            continue
        total_damage = (damage or 0.0) * (hits or 1)

        # 易伤 / 虚弱
        vuln = debuff_amount(on_play, "VulnerablePower", vals)
        weak = debuff_amount(on_play, "WeakPower", vals)
        if vuln is None or weak is None:
            skipped.append((name, "dynamic_debuff", SKIP_REASONS["dynamic_debuff"]))
            continue

        block = vals.get("block", 0.0)
        if base.get("_calc_block"):
            skipped.append((name, "dynamic_block", SKIP_REASONS["dynamic_block"]))
            continue
        # 有获得格挡的行为但拿不到固定 BlockVar
        gains_block = "GainBlock" in on_play or (prop_expr(cls, "GainsBlock") or "").strip() == "true"
        if gains_block and "block" not in vals:
            skipped.append((name, "dynamic_block", SKIP_REASONS["dynamic_block"]))
            continue

        title_key = class_to_key(name) + ".title"
        title = loc.get(title_key)
        if not title:
            skipped.append((name, "type", "缺少中文名"))
            continue

        rows.append(
            {
                "id": name,
                "中文名": title,
                "费用": int_if_whole(cost),
                "作用范围": scope,
                "伤害": int_if_whole(total_damage),
                "格挡": int_if_whole(block),
                "易伤": int_if_whole(vuln),
                "虚弱": int_if_whole(weak),
                "hits": hits or 1,
            }
        )
    return rows


def main() -> int:
    ap = argparse.ArgumentParser(description="提取原版 STS2 攻击/技能牌数值")
    ap.add_argument("--out", default="cards.csv", help="输出 CSV 路径")
    ap.add_argument("--skipped", default="", help="跳过清单输出路径（可选）")
    ap.add_argument("--stats", action="store_true", help="只打印统计，不写文件")
    args = ap.parse_args()

    skipped: list[tuple[str, str, str]] = []
    rows = extract(skipped)

    # 只保留在「伤害/格挡/易伤/虚弱」四项里至少有一项非 0 的牌
    empty = [r for r in rows if not any(r[k] for k in ("伤害", "格挡", "易伤", "虚弱"))]
    rows = [r for r in rows if any(r[k] for k in ("伤害", "格挡", "易伤", "虚弱"))]

    # 同名卡只计一次（5 个角色的「打击」「防御」等）
    seen: dict[str, dict] = {}
    dup_conflicts: list[tuple[dict, dict]] = []
    for r in rows:
        key = r["中文名"]
        if key not in seen:
            seen[key] = r
        else:
            prev = seen[key]
            if any(prev[k] != r[k] for k in ("费用", "作用范围", "伤害", "格挡", "易伤", "虚弱")):
                dup_conflicts.append((prev, r))
    rows = list(seen.values())
    rows.sort(key=lambda r: (r["费用"], r["中文名"]))

    print(f"提取卡牌：{len(rows)} 张（去重后）")
    print(f"因四项数值全为 0 而不计：{len(empty)} 张")
    print("作用范围：", dict(Counter(r["作用范围"] for r in rows)))
    print("跳过：", len(skipped))
    for reason, n in Counter(s[1] for s in skipped).most_common():
        print(f"  - {reason}: {n} ({SKIP_REASONS.get(reason, reason)})")
    if dup_conflicts:
        print(f"⚠ 同名但数值不同的牌 {len(dup_conflicts)} 组，已保留先出现的一张：")
        for a, b in dup_conflicts:
            print(f"  {a['中文名']}: {a}  vs  {b}")

    if args.skipped:
        with open(args.skipped, "w", encoding="utf-8-sig") as f:
            f.write("卡牌类名\t原因\n")
            for r in empty:
                f.write(f"{r['id']}\t四项数值全为 0，不计\n")
            for name, reason, desc in skipped:
                f.write(f"{name}\t{desc}\n")
        print(f"跳过清单已写入 {args.skipped}")

    if args.stats:
        return 0

    import csv

    with open(args.out, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(["中文名", "费用", "作用范围", "伤害", "格挡", "易伤", "虚弱"])
        for r in rows:
            w.writerow(
                [r["中文名"], r["费用"], r["作用范围"], r["伤害"], r["格挡"], r["易伤"], r["虚弱"]]
            )
    print(f"已写入 {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
