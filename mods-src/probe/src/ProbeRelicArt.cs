using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace Saria;

/// <summary>
/// 遗物图标（大图 256×256 / 小图 80×80 / 描边剪影 80×80）。
///
/// 原版三种图：
///   * 小图：图集区域 `atlases/relic_atlas.sprites/&lt;entry&gt;.tres`
///   * 大图：`images/relics/&lt;entry&gt;.png` 256×256
///   * **描边**：`relic_outline_atlas` 里的**纯白剪影**，由 `NRelic` 用半透明黑染色，形成图标背后的影子
///
/// mod 的 PCK 贴图加载不了，所以运行时用散文件解码后替换**显示节点**：
///   `images/relics/&lt;类名&gt;.png`（256）、`cropped/&lt;类名&gt;.png`（80）、`outline/&lt;类名&gt;.png`（80，白色剪影）
///
/// 注意：`NRelic.Icon` / `Outline` 是**属性**不是字段（v0.6.4 的教训）。
/// </summary>
internal static class ProbeRelicArt
{
    internal static string RelicDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("images").PathJoin("relics");

    private static readonly Dictionary<string, Texture2D?> Cache = new();

    private static Texture2D? Load(string className, string folder)
    {
        string key = folder + ":" + className;
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        Texture2D? texture = null;
        try
        {
            string dir = folder.Length == 0 ? RelicDir : RelicDir.PathJoin(folder);
            string path = dir.PathJoin(className + ".png");
            if (FileAccess.FileExists(path))
            {
                var image = new Image();
                var err = image.LoadPngFromBuffer(FileAccess.GetFileAsBytes(path));
                if (err == Error.Ok && !image.IsEmpty())
                    texture = ImageTexture.CreateFromImage(image);
                else
                    Log.Warn($"[saria][relic] decode failed ({err}): {path}");
            }
            else
            {
                Log.Warn($"[saria][relic] missing file: {path}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][relic] load threw for {className}: {ex.Message}");
        }

        Cache[key] = texture;
        return texture;
    }

    internal static Texture2D? Big(RelicModel? model) => model == null ? null : Load(model.GetType().Name, "");
    internal static Texture2D? Small(RelicModel? model) => model == null ? null : Load(model.GetType().Name, "cropped");
    internal static Texture2D? Outline(RelicModel? model) => model == null ? null : Load(model.GetType().Name, "outline");

    internal static void Apply(TextureRect? rect, RelicModel? model, bool small)
    {
        if (rect == null || model == null)
            return;

        var texture = small ? Small(model) : Big(model);
        if (texture != null)
            rect.Texture = texture;
    }

    internal static void ApplyOutline(TextureRect? rect, RelicModel? model)
    {
        if (rect == null || model == null)
            return;

        var texture = Outline(model);
        if (texture != null)
            rect.Texture = texture;
    }

    /// <summary>当前场景里选中的角色（角色选择界面用）。</summary>
    internal static CharacterModel? SelectedCharacter(NCharacterSelectScreen screen)
    {
        var field = AccessTools.Field(typeof(NCharacterSelectScreen), "_selectedButton");
        if (field?.GetValue(screen) is NCharacterSelectButton button)
            return button.Character;
        return null;
    }
}

/// <summary>遗物栏：`NRelic.Reload` 之后替换图标、描边影子。</summary>
[HarmonyPatch(typeof(NRelic), "Reload")]
public static class ProbeRelicReloadPatch
{
    private static readonly System.Reflection.FieldInfo? IconSizeField =
        AccessTools.Field(typeof(NRelic), "_iconSize");

    [HarmonyPostfix]
    private static void Postfix(NRelic __instance)
    {
        try
        {
            var model = __instance.Model;
            if (model == null)
                return;

            bool small = true;
            if (IconSizeField?.GetValue(__instance) is object raw)
                small = Convert.ToInt32(raw) == 0;   // IconSize.Small == 0

            ProbeRelicArt.Apply(__instance.Icon, model, small);
            ProbeRelicArt.ApplyOutline(__instance.Outline, model);
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][relic] NRelic hook failed: {ex.Message}");
        }
    }
}

/// <summary>获得 / 触发遗物时，顶栏的闪光粒子贴图。</summary>
[HarmonyPatch(typeof(NRelicInventoryHolder), "DoFlash")]
public static class ProbeRelicFlashPatch
{
    [HarmonyPostfix]
    private static void Postfix(NRelicInventoryHolder __instance)
    {
        try
        {
            var relicField = AccessTools.Field(typeof(NRelicInventoryHolder), "_relic");
            if (relicField?.GetValue(__instance) is not NRelic relic || relic.Model == null)
                return;

            var texture = ProbeRelicArt.Small(relic.Model);
            if (texture == null)
                return;

            var container = NRun.Instance?.GlobalUi?.AboveTopBarVfxContainer;
            if (container == null || container.GetChildCount() == 0)
                return;

            var vfx = container.GetChild(container.GetChildCount() - 1);
            if (vfx?.GetNodeOrNull<GpuParticles2D>("Particles") is GpuParticles2D particles)
                particles.Texture = texture;
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][relic] flash hook failed: {ex.Message}");
        }
    }
}

/// <summary>角色选择界面右下角的初始遗物图标 + 影子。</summary>
[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class ProbeRelicCharacterSelectPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCharacterSelectScreen __instance)
    {
        try
        {
            var character = ProbeRelicArt.SelectedCharacter(__instance);
            if (character == null || !character.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal))
                return;

            var startingRelics = character.StartingRelics;
            if (startingRelics == null || startingRelics.Count == 0)
                return;

            var model = startingRelics[0];
            ProbeRelicArt.Apply(
                AccessTools.Field(typeof(NCharacterSelectScreen), "_relicIcon")?.GetValue(__instance) as TextureRect,
                model, small: true);
            ProbeRelicArt.ApplyOutline(
                AccessTools.Field(typeof(NCharacterSelectScreen), "_relicIconOutline")?.GetValue(__instance) as TextureRect,
                model);
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][relic] character select hook failed: {ex.Message}");
        }
    }
}

/// <summary>遗物检视界面的大图。</summary>
[HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
public static class ProbeRelicInspectPatch
{
    private static readonly System.Reflection.FieldInfo? RelicsField =
        AccessTools.Field(typeof(NInspectRelicScreen), "_relics");

    private static readonly System.Reflection.FieldInfo? IndexField =
        AccessTools.Field(typeof(NInspectRelicScreen), "_index");

    private static readonly System.Reflection.FieldInfo? ImageField =
        AccessTools.Field(typeof(NInspectRelicScreen), "_relicImage");

    [HarmonyPostfix]
    private static void Postfix(NInspectRelicScreen __instance)
    {
        try
        {
            if (RelicsField?.GetValue(__instance) is not System.Collections.IList relics || relics.Count == 0)
                return;

            int index = IndexField?.GetValue(__instance) is object raw ? Convert.ToInt32(raw) : 0;
            if (index < 0 || index >= relics.Count)
                return;

            if (relics[index] is RelicModel model)
                ProbeRelicArt.Apply(ImageField?.GetValue(__instance) as TextureRect, model, small: false);
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][relic] inspect screen hook failed: {ex.Message}");
        }
    }
}

/// <summary>遗物奖励弹窗的图标。</summary>
[HarmonyPatch(typeof(RelicReward), nameof(RelicReward.CreateIcon))]
public static class ProbeRelicRewardPatch
{
    private static readonly System.Reflection.FieldInfo? RelicField =
        AccessTools.Field(typeof(RelicReward), "_relic");

    [HarmonyPostfix]
    private static void Postfix(RelicReward __instance, TextureRect __result)
    {
        try
        {
            if (RelicField?.GetValue(__instance) is RelicModel model)
                ProbeRelicArt.Apply(__result, model, small: false);
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][relic] reward hook failed: {ex.Message}");
        }
    }
}
