#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""用卡牌数值回归拟合「费用」，并给出每张牌的「牌效值」。

模型（作用范围只以乘积形式出现，不单独成项）：

    pred_cost = w0
              + w1 * (α * 伤害)
              + w2 * (α * 易伤)
              + w3 * (α * 虚弱)
              + w4 * 格挡

其中 α 固定为全体敌人的放大系数（默认 2），单体牌取 1。

牌效值（同费用组内比较，再压到 [0.85, 1.15]）：

    base = 同费用所有牌的 pred_cost 中位数
    r    = (pred_cost - base) / (base + 1)
    v    = 1 + 0.15 * tanh(k * r)

k 自动取 2 / P95(|r|)。tanh 天然不越界，最后再 clip 一次兜底。
v > 1 表示：在同样费用的牌里，这张牌的数值更超模。

用法：
  python fit_card_value.py --csv sts2_cards_raw.csv --out sts2_cards.csv --alpha 2
"""

from __future__ import annotations

import argparse
import csv
import itertools
import math
import statistics

LOW, HIGH = 0.85, 1.15


# ---------------------------------------------------------------------------
# 最小二乘（纯 Python，不依赖 numpy）
# ---------------------------------------------------------------------------


def solve_linear(mat: list[list[float]], vec: list[float]) -> list[float] | None:
    n = len(mat)
    a = [row[:] + [vec[i]] for i, row in enumerate(mat)]
    for col in range(n):
        pivot = max(range(col, n), key=lambda r: abs(a[r][col]))
        if abs(a[pivot][col]) < 1e-12:
            return None
        a[col], a[pivot] = a[pivot], a[col]
        pv = a[col][col]
        for j in range(col, n + 1):
            a[col][j] /= pv
        for r in range(n):
            if r == col:
                continue
            factor = a[r][col]
            if factor:
                for j in range(col, n + 1):
                    a[r][j] -= factor * a[col][j]
    return [a[i][n] for i in range(n)]


def ols(rows: list[list[float]], y: list[float]) -> tuple[list[float], float]:
    k = len(rows[0])
    xtx = [[sum(r[i] * r[j] for r in rows) for j in range(k)] for i in range(k)]
    xty = [sum(r[i] * yy for r, yy in zip(rows, y)) for i in range(k)]
    coef = solve_linear(xtx, xty)
    if coef is None:
        raise RuntimeError("回归矩阵奇异，无法求解")
    sse = sum((sum(c * x for c, x in zip(coef, r)) - yy) ** 2 for r, yy in zip(rows, y))
    return coef, sse


def nnls(rows: list[list[float]], y: list[float]) -> tuple[list[float], float]:
    """非负最小二乘：枚举非零特征子集（截距始终保留），取 SSE 最小者。"""
    k = len(rows[0])
    best: tuple[list[float], float] | None = None
    for size in range(k):
        for active in itertools.combinations(range(1, k), size):
            cols = [0, *active]
            sub = [[r[c] for c in cols] for r in rows]
            coef_sub, sse = ols(sub, y)
            if any(c < -1e-9 for c in coef_sub[1:]):
                continue
            coef = [0.0] * k
            for c, v in zip(cols, coef_sub):
                coef[c] = v
            if best is None or sse < best[1] - 1e-12:
                best = (coef, sse)
    if best is None:
        raise RuntimeError("无法求出非负解")
    return best


# ---------------------------------------------------------------------------
# 主流程
# ---------------------------------------------------------------------------


def main() -> int:
    ap = argparse.ArgumentParser(description="回归拟合费用并计算牌效值")
    ap.add_argument("--csv", default="sts2_cards_raw.csv", help="含原始 7 列的 CSV")
    ap.add_argument("--out", default="", help="输出 CSV；默认覆盖 --csv")
    ap.add_argument("--alpha", type=float, default=2.0, help="全体敌人的作用范围系数")
    ap.add_argument("--nonneg", action="store_true", help="强制权重非负")
    args = ap.parse_args()

    with open(args.csv, encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        rows = list(reader)
        fields = [c for c in (reader.fieldnames or []) if c != "牌效值"]

    x: list[list[float]] = []
    y: list[float] = []
    for r in rows:
        a = args.alpha if int(r["作用范围"]) == 1 else 1.0
        x.append(
            [
                1.0,
                a * float(r["伤害"]),
                a * float(r["易伤"]),
                a * float(r["虚弱"]),
                float(r["格挡"]),
            ]
        )
        y.append(float(r["费用"]))

    coef, sse = nnls(x, y) if args.nonneg else ols(x, y)
    pred = [sum(c * v for c, v in zip(coef, row)) for row in x]

    mean_y = sum(y) / len(y)
    sst = sum((v - mean_y) ** 2 for v in y)
    r2 = 1 - sse / sst if sst else 0.0
    rmse = math.sqrt(sse / len(y))

    # 同费用组内比较 → tanh 压缩 → 牌效值
    groups: dict[float, list[float]] = {}
    for cost, p in zip(y, pred):
        groups.setdefault(cost, []).append(p)
    base = {c: statistics.median(v) for c, v in groups.items()}

    resid = [(p - base[c]) / (abs(base[c]) + 1.0) for p, c in zip(pred, y)]
    ordered = sorted(abs(r) for r in resid)
    idx = int(round(0.95 * (len(ordered) - 1)))
    p95 = ordered[idx]
    k = 2.0 / p95 if p95 > 1e-9 else 1.0

    values = []
    for r in resid:
        v = 1.0 + 0.15 * math.tanh(k * r)
        values.append(min(HIGH, max(LOW, v)))

    # 写回（牌效值追加到最后一列）
    out_fields = fields + ["牌效值"]
    for r, v in zip(rows, values):
        r["牌效值"] = f"{v:.4f}"
    with open(args.out or args.csv, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=out_fields)
        w.writeheader()
        w.writerows(rows)

    # 报告
    names = ["截距", "α×伤害", "α×易伤", "α×虚弱", "格挡"]
    print(f"样本 {len(rows)} 张，α = {args.alpha:g}，{'非负' if args.nonneg else '无约束'}最小二乘")
    for n, c in zip(names, coef):
        print(f"  {n:8s} {c:+.4f}")
    print(f"R² = {r2:.4f}   RMSE = {rmse:.3f} 费   tanh 系数 k = {k:.3f}")
    print(f"牌效值：min {min(values):.4f} / max {max(values):.4f} / 均值 {sum(values)/len(values):.4f}")

    print("\n按费用分档（组内中位数=1.0 基准）：")
    for c in sorted(groups):
        sel = [v for v, yy in zip(values, y) if yy == c]
        tag = "  ← 单张，无同费可比，记 1.0" if len(sel) == 1 else ""
        print(
            f"  {c:>4g} 费：{len(sel):3d} 张  均值 {sum(sel)/len(sel):.3f}  "
            f"范围 {min(sel):.3f}~{max(sel):.3f}{tag}"
        )

    order = sorted(zip(values, rows), key=lambda t: t[0])
    print("\n最亏模 5 张：", ", ".join(f"{r['中文名']}({v:.3f})" for v, r in order[:5]))
    print("最超模 5 张：", ", ".join(f"{r['中文名']}({v:.3f})" for v, r in order[-5:][::-1]))
    print(f"\n已写入 {args.out or args.csv}（新增「牌效值」列）")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
