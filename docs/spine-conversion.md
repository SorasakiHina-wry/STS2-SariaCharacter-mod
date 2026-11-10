# Spine 3.8 → 4.2 转换记录（无工程文件）

游戏运行时：**spine-godot / spine-cpp 4.2.43**（原版 `architect.skel`、`necrobinder.skel` 头部可验证）。
spine-godot 对数据版本做硬校验，版本不符会报
`Skeleton version %s does not match runtime version %s` 并加载失败。

素材：`resources/spine/build_char_202_demkni.{skel,atlas,png}` —— Spine **3.8.99**，
80 骨骼 / 51 槽位 / 1 皮肤 / 52 个贴图区域（含 mesh 附件），
动画：`Default, Interact, Move, Relax, Sit, Sleep`，事件：`OnAttack, OnStart`。

## 用到的工具

| 用途 | 工具 | 说明 |
|---|---|---|
| 骨骼 3.8 → 4.2（可直接跑） | [SpineSkeletonDataConverter](https://github.com/wang606/SpineSkeletonDataConverter) | 支持 3.5–4.2 的 skel/json 互转，带 Windows 预编译版（Releases → `v3.8`） |
| 图集 3.x → 4.x | `tools/spine_atlas_upgrade.py`（本仓库） | 只改区域几何 key：`xy/size→bounds`、`offset/orig→offsets`；`rotate: true` 在 4.x 仍合法 |
| 批量（可选） | 同仓库 `tools/SpineConverter.py` | 适合成批目录；但它的 atlas 工具只做 4.x→3.x 降级 |

> 该转换器采用 PolyForm Noncommercial 1.0.0，用于免费 mod 没问题。

## 实际命令

```powershell
# 1) 骨骼：3.8.99 -> 4.2.43（-v 必须写全 x.y.z，且要和游戏运行时对齐）
SpineSkeletonDataConverter.exe build_char_202_demkni.skel out.skel -v 4.2.43

# 2) 图集：3.x -> 4.x
python tools/spine_atlas_upgrade.py build_char_202_demkni.atlas out.atlas
```

转换器会自动处理 3.x → 4.x 的两处关键差异（有日志确认）：
`rotate` 时间轴改为 4.x 的绝对角度、曲线格式由 relative 改为 absolute。

## 验证结果

- 输出 `.skel` 头部版本 = **4.2.43**；
- 4.2 再转回 JSON 与原 3.8 JSON 对照：**80 骨骼 / 51 槽位 / 1 皮肤 / 51 附件槽 / 6 动画 / 2 事件 全部一致**；
- 图集 52 个区域全部转换，结构与原版 4.2 图集一致（`bounds` + `offsets` + `rotate`）。

产物在 `resources/spine/4.2/`。

## 剩余问题：动画不匹配

这套骨骼的动画是 `Default / Interact / Move / Relax / Sit / Sleep`，
**没有** 战斗角色需要的 `idle_loop / attack / cast / hurt / die`。

所以：

- ✅ 适合做 **NPC**（休息处 `Relax`、商人 `Sit/Sleep`、事件 `Interact`）；
- ❌ 现阶段不能直接当可玩角色的战斗视觉；
- 若要做角色，需要补 `attack / cast / hurt / die`（Spine 里补动画后重新导出 4.2）。

## 打包注意

`build_pck` 会把 `.skel` / `.atlas` 原样打包；PNG 同时产出 `.ctex` + 原始 PNG + `.import`，
spine 按图集里写的文件名找图，不会被打断。
