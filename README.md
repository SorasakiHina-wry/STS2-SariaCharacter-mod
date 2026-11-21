# 塞雷娅（Saria Character）— 《杀戮尖塔 2》自定义角色 Mod

一个**原创机制 + 原创卡池**的《杀戮尖塔 2》角色 Mod：新角色「塞雷娅」、一套以**钙铠**为核心的
89 张卡牌、8 件遗物、31 个自定义状态，以及先古之民对话与 Spine 战斗立绘。

> ⚠️ **非官方同人作品**。与 Mega Crit、鹰角网络均无关联。
> 角色形象源自《明日方舟》的「塞雷娅」，仅作个人练习与非商业同人用途。
> 大部分代码与工程工作由 DeepSeek Harness:DeepSeek-V4.1-Flash 完成。

---

## 现在有什么

| 内容 | 数量 / 说明 |
|---|---|
| 角色 | 塞雷娅（Mod ID `saria`，角色 ID `SARIA-SARIA`），HP 70，起手遗物「钙质技艺」 |
| 卡牌 | **89 张**（含基础牌、普通/罕见/稀有、先古牌、无色 token） |
| 遗物 | **8 件**：初始 1、普通 1、罕见 2、稀有 2、商店 1、先古 1 |
| 状态（power） | **31 个**，核心机制 **钙铠**：回合开始转化格挡、可被卡牌消耗以换取效果 |
| 先古对话 | 9 位先古、54 行对话 |
| 战斗立绘 | 自制 Spine（待机/受击/攻击/能力/死亡） |
| 选人界面 | 背景大图、选择栏头像、顶栏头像 |

仍为占位的部分：能量球、地图标记。

---

## 许可证

代码以 [MIT](LICENSE) 许可发布。仓库**不包含**美术与 Spine 源文件（见下方版权声明）。

## 目录结构

```
.
├─ mods-src/probe/            ★ Mod 源码与设计表
│  ├─ src/                     C# 源码（Cards / Powers / Relics / Patches / Characters）
│  ├─ assets/                  PCK 源：spine 与本地化 JSON
│  ├─ design/                  ★ 设计表（卡牌/状态/遗物/文案），开发以此为准
│  ├─ tools/                   构建、部署、打包与校验脚本
│  ├─ mod_manifest.json        Mod 清单（id=probe，依赖 BaseLib）
│  └─ README.md                开发细节：踩过的坑、必须满足的约束
├─ docs/                       协作约定与格式说明
└─ resources/                  美术与 Spine 源文件（图片、骨骼）
```

`design/` 是整套内容的**唯一事实来源**：卡牌数值、文案、稀有度、状态效果都先在表里写，
再由代码实现，`tools/cardspec.py` 负责交叉校验。

---

## 构建 / 安装 / 打包

环境要求：**.NET 9 SDK**、游戏 **v0.113.x**、Steam 工坊订阅 **BaseLib**（必需依赖）。

```powershell
# 编译
cd mods-src\probe
dotnet build -c Release

# 部署到本机游戏（会先关掉游戏进程）
powershell -ExecutionPolicy Bypass -File tools\apply.ps1

# 打包给测试者：产出 dist\saria\ 与 dist\saria-<version>.zip
powershell -ExecutionPolicy Bypass -File tools\package.ps1
```

**测试者安装**：解压 zip，把其中的 `saria` 文件夹整体放进 `<游戏目录>\mods\`，
确认已订阅 BaseLib，启动游戏后在角色选择界面选「塞雷娅」。

> 发版（版本号、打包、推送、Release）见 [docs/release.md](docs/release.md)。
> 还缺哪些美术/音频资源、各自尺寸规格见 [docs/missing-assets.md](docs/missing-assets.md)。
> 已完成的素材在哪个目录、怎么命名、打包去哪见 [docs/art-assets.md](docs/art-assets.md)。
>
> 注意：`saria.pck` 只装 Spine 与本地化 JSON；图片走散文件（`images/`），
> 详见 `mods-src/probe/README.md` 的「已知的坑」。

---

## 开发工作流

内容以设计表驱动，状态标记写在每条条目的 `备注` 里：

| 标记 | 含义 |
|---|---|
| `待实装` | 写代码 |
| `待修改` | 按备注改 |
| `待删除` | 删代码并删掉表里这一条 |
| `已实装` | 不再改动 |

日常检查：`python tools/cardspec.py --changed` / `--pending`；
全量校验：`python tools/cardspec.py`。协作细节见 [`docs/agent-workflow.md`](docs/agent-workflow.md)。

---

## 版权与分发声明

- 本仓库**不托管**游戏/同人美术资源：`resources/` 已被 `.gitignore` 排除，仓库里只有代码与设计表；
  `mods-src/probe/assets/` 仅含 Spine 骨骼与本地化 JSON（本地构建用）
  部分取自游戏或为同人创作，**仅供本地构建使用，请勿二次分发**。
- 正式发行包（Release 附件 `saria-<version>.zip`）里含立绘与 Spine，属于同人再分发，请勿转售；
  本地若要自己打完整包，需要自备 `resources/` 目录（见 [`docs/release.md`](docs/release.md)）。
- 代码部分可自由参考，但不附带任何担保。
