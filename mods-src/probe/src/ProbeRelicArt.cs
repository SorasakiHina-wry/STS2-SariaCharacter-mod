using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Rewards;

namespace Saria;

/// <summary>
/// 遗物图标（大图 256×256 / 小图 80×80）。
///
/// 原版两种规格：
///   * 小图：图集区域 `atlases/relic_atlas.sprites/&lt;entry&gt;.tres`（每块尺寸、九宫格边距都不同）
///   * 大图：`images/relics/&lt;entry&gt;.png` 256×256
/// 两者都是 preload 的导入贴图，mod 的 PCK 加载不了，所以运行时用散文件解码后替换显示节点。
///
/// 素材：`mods/saria/images/relics/&lt;类名&gt;.png`（256×256）
///       `mods/saria/images/relics/cropped/&lt;类名&gt;.png`（80×80）
/// 文件名用**类名**（CalciumCraft 等），和卡图/状态图标的约定一致。
/// </summary>
internal static class ProbeRelicArt
{
    internal static string RelicDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("images").PathJoin("relics");

    private static readonly Dictionary<string, Texture2D?> Cache = new();

    private static Texture2D? Load(string className, bool small)
    {
        string key = (small ? "s:" : "b:") + className;
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        Texture2D? texture = null;
        try
        {
            string dir = small ? RelicDir.PathJoin("cropped") : RelicDir;
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
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][relic] load threw for {className}: {ex.Message}");
        }

        Cache[key] = texture;
        return texture;
    }

    internal static Texture2D? Big(RelicModel? model) => model == null ? null : Load(model.GetType().Name, small: false);

    internal static Texture2D? Small(RelicModel? model) => model == null ? null : Load(model.GetType().Name, small: true);

    /// <summary>把某个 TextureRect 换成对应尺寸的自制图标；没有素材就保持原样。</summary>
    internal static void Apply(TextureRect? rect, RelicModel? model, bool small)
    {
        if (rect == null || model == null)
            return;

        var texture = small ? Small(model) : Big(model);
        if (texture != null)
            rect.Texture = texture;
    }
}

/// <summary>遗物栏 / 大图标：`NRelic.Reload` 之后替换。</summary>
[HarmonyPatch(typeof(NRelic), "Reload")]
public static class ProbeRelicReloadPatch
{
    private static readonly AccessTools.FieldRef<NRelic, object> IconSizeRef =
        AccessTools.FieldRefAccess<NRelic, object>("_iconSize");

    [HarmonyPostfix]
    private static void Postfix(NRelic __instance)
    {
        try
        {
            var model = __instance.Model;
            if (model == null)
                return;

            // IconSize.Small == 0，Large == 1（原版枚举顺序）
            bool small = Convert.ToInt32(IconSizeRef(__instance)) == 0;
            var field = AccessTools.Field(typeof(NRelic), "Icon") ?? AccessTools.Field(typeof(NRelic), "_icon");
            if (field?.GetValue(__instance) is TextureRect rect)
                ProbeRelicArt.Apply(rect, model, small);
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][relic] NRelic hook failed: {ex.Message}");
        }
    }
}

/// <summary>遗物检视界面的大图。</summary>
[HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
public static class ProbeRelicInspectPatch
{
    [HarmonyPostfix]
    private static void Postfix(NInspectRelicScreen __instance)
    {
        try
        {
            var modelField = AccessTools.Field(typeof(NInspectRelicScreen), "_relic")
                             ?? AccessTools.Field(typeof(NInspectRelicScreen), "_relicModel");
            if (modelField?.GetValue(__instance) is not RelicModel model)
                return;

            var imageField = AccessTools.Field(typeof(NInspectRelicScreen), "_relicImage");
            if (imageField?.GetValue(__instance) is TextureRect rect)
                ProbeRelicArt.Apply(rect, model, small: false);
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
    [HarmonyPostfix]
    private static void Postfix(RelicReward __instance, TextureRect __result)
    {
        try
        {
            var field = AccessTools.Field(typeof(RelicReward), "_relic");
            if (field?.GetValue(__instance) is RelicModel model)
                ProbeRelicArt.Apply(__result, model, small: false);
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][relic] reward hook failed: {ex.Message}");
        }
    }
}
