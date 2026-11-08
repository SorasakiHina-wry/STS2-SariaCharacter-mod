#!/usr/bin/env python3
"""给卡牌补 ExtraHoverTips：让卡面里提到的状态/能力真的能在详情里悬停查看。

原版做法（Bash.cs）：
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<VulnerablePower>() };

脚本按卡牌描述里出现的关键词推断需要哪些提示（易伤/虚弱/力量/敏捷/活力/钙铠/…），
缺则在 `CustomPortraitPath` 之前插入。

    python tools/hover_tips.py --dry-run
    python tools/hover_tips.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

# 关键词 -> (类型名, 命名空间)
POWER_TIPS = {
    "易伤": ("VulnerablePower", "MegaCrit.Sts2.Core.Models.Powers"),
    "虚弱": ("WeakPower", "MegaCrit.Sts2.Core.Models.Powers"),
    "力量": ("StrengthPower", "MegaCrit.Sts2.Core.Models.Powers"),
    "敏捷": ("DexterityPower", "MegaCrit.Sts2.Core.Models.Powers"),
    "活力": ("VigorPower", "MegaCrit.Sts2.Core.Models.Powers"),
    "钙铠": ("CalciumArmorPower", "Probe.Powers"),
    "分神": ("ProbeDistractPower", "Probe.Powers"),
    "凝神": ("ProbeFocusMindPower", "Probe.Powers"),
    "煅烧": ("ProbeBurnPower", "Probe.Powers"),
    "骨矿化": ("ProbeBoneMineralizePower", "Probe.Powers"),
    "技艺超载": ("ProbeTechniqueOverloadPower", "Probe.Powers"),
    "精神回复": ("ProbeSpiritRecoveryPower", "Probe.Powers"),
    "牵引": ("ProbeTractionPower", "Probe.Powers"),
    "充能护服": ("ProbeChargeSuitPower", "Probe.Powers"),
    "钙质化": ("ProbeVulnerableBonusPower", "Probe.Powers"),
    "再塑型": ("ProbeReshapePower", "Probe.Powers"),
    "洞察": ("ProbeInsightPower", "Probe.Powers"),
    "急救": ("ProbeFirstAidPower", "Probe.Powers"),
    "紧密防护": ("ProbeTightGuardPower", "Probe.Powers"),
    "持械": ("ProbeArmPower", "Probe.Powers"),
    "腿甲": ("ProbeLegArmorPower", "Probe.Powers"),
    "科技新星": ("ProbeTechNovaPower", "Probe.Powers"),
}

CLASS_RE = re.compile(r"(?m)^\[Pool\(typeof\(ProbeCardPool\)\)\]\s*$")
DESC_RE = re.compile(r'\("description"\s*,\s*"((?:[^"\\]|\\.)*)"\)')
ANCHOR = "    public override string? CustomPortraitPath"


def split_classes(text: str) -> list[str]:
    """Return chunks, each starting at its [Pool] attribute."""
    starts = [m.start() for m in CLASS_RE.finditer(text)]
    if not starts:
        return []
    return [text[s:e] for s, e in zip(starts, starts[1:] + [len(text)])]


def needed_tips(chunk: str) -> list[str]:
    text = " ".join(DESC_RE.findall(chunk))
    tips: list[str] = []
    for keyword, (type_name, ns) in POWER_TIPS.items():
        if keyword in text and type_name not in tips:
            tips.append(type_name)
    if "格挡" in text:
        tips.append("__BLOCK__")
    return tips


def build_override(tips: list[str]) -> str:
    lines = []
    for tip in tips:
        if tip == "__BLOCK__":
            lines.append("        HoverTipFactory.Static(StaticHoverTip.Block),")
        else:
            lines.append(f"        HoverTipFactory.FromPower<{tip}>(),")
    body = "\n".join(lines)
    return (
        "    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]\n"
        "    {\n"
        f"{body}\n"
        "    };\n\n"
    )


def main(argv: list[str]) -> int:
    dry_run = "--dry-run" in argv
    cards_dir = Path(__file__).resolve().parent.parent / "src" / "Cards"

    touched: list[str] = []
    for path in sorted(cards_dir.glob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        chunks = split_classes(text)
        if not chunks:
            continue

        changed_file = False
        for chunk in chunks:
            if "ExtraHoverTips" in chunk:
                continue
            tips = needed_tips(chunk)
            if not tips:
                continue
            if ANCHOR not in chunk:
                continue
            replacement = chunk.replace(ANCHOR, build_override(tips) + ANCHOR, 1)
            text = text.replace(chunk, replacement, 1)
            changed_file = True

        if not changed_file:
            continue

        if "using MegaCrit.Sts2.Core.HoverTips;" not in text:
            text = text.replace("using MegaCrit.Sts2.Core.GameActions.Multiplayer;",
                                "using MegaCrit.Sts2.Core.GameActions.Multiplayer;\nusing MegaCrit.Sts2.Core.HoverTips;", 1)
        if "using MegaCrit.Sts2.Core.Models.Powers;" not in text and "Models.Powers" in text:
            pass
        touched.append(path.name)
        if not dry_run:
            path.write_text(text, encoding="utf-8")

    for name in touched:
        print(("【预览】" if dry_run else "✔ ") + name)
    print(f"\n共 {len(touched)} 个文件" + ("（未写入）" if dry_run else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
