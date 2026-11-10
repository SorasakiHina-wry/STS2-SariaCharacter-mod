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

## 游戏内加载测试（2026-11-10，已通过 ✅）

用 spine-godot 的运行时 API 在 `ModInitializer` 里加载，日志结果：

```
[probe][spine] atlas textures = 1
[probe][spine] loaded=True version=4.2.43
[probe][spine] animations(6): Default, Interact, Move, Relax, Sit, Sleep
```

**结论：转换后的 4.2.43 数据被运行时完整接受**（图集 1 页、版本号正确、6 个动画可枚举）。

### 必须在运行时用 API，不能用 ResourceLoader

`ResourceLoader.Load("*.skel")` 会失败（`No loader found for resource`）——
`.skel` / `.atlas` 的导入器只在 Godot 编辑器里存在。正确顺序：

```csharp
var atlas    = ClassDB.Instantiate("SpineAtlasResource").AsGodotObject();
var skelFile = ClassDB.Instantiate("SpineSkeletonFileResource").AsGodotObject();
var data     = ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject();

atlas.Call("load_from_atlas_file", atlasPath);   // 图集页/贴图
skelFile.Call("load_from_file", skelPath);       // 骨骼
data.Call("set_atlas_res", atlas);
data.Call("set_skeleton_file_res", skelFile);
data.Call("update_skeleton_data");

data.Call("is_skeleton_data_loaded").AsBool();   // true
data.Call("get_version").AsString();             // 4.2.43
data.Call("get_animations");                     // 6 个
```

文件用**散装绝对路径**（如 `<游戏目录>/mods/probe/spine/xxx.skel`）；
放进 PCK 也能读（`FileAccess` 支持 `res://`），但 `.atlas` 里的贴图页按相对路径找 PNG，散装最省事。

### 参考：官方社区教程

<https://tutorials.sts2modding.com/en/docs/05-card-art-and-skin-replacement/>
明确写着 STS2 用 Spine **4.2.43**、低版本不能直接用、并推荐同一个转换器；
战斗角色需要动画名 `idle_loop / attack / cast / hurt / die`。

## 打包注意

`build_pck` 会把 `.skel` / `.atlas` 原样打包；PNG 同时产出 `.ctex` + 原始 PNG + `.import`，
spine 按图集里写的文件名找图，不会被打断。
