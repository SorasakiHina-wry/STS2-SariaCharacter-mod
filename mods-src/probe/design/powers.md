# Probe buff / debuff 表（工作稿）

格式说明见 [`docs/card-spec-format.md`](../../../docs/card-spec-format.md) 第 9 节。
**这里定义「会显示在生物头顶的层数状态」**；卡牌用「施加 N 层X / 获得 N 层X」引用。
只影响规则、不显示层数的，写到 [`mechanics.md`](mechanics.md)。

原版已有状态直接用，不需要在这里定义：
`易伤 Vulnerable`、`虚弱 Weak`、`脆弱 Frail`、`中毒 Poison`、`荆棘 Thorns`、`力量 Strength`、`敏捷 Dexterity`。

---

## 新状态从下面往下写

```power
名称: 钙铠
类型: buff
叠层: 计数
衰减: 从卡牌中获得格挡时
触发: 回合开始时
效果:获得与层数相等的格挡
描述:回合开始时，获得与层数相等的格挡。当你从卡牌中获得格挡时，失去获得格挡数值一半的钙铠
图标:
备注：已实装
```
