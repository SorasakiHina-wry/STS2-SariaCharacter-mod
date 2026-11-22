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
/// 原版两种规格：小图是图集区域（每块尺寸/九宫格边距都不同），大图是 `images/relics/&lt;entry&gt;.png` 256×256。
/// 两者都是 preload 的导入贴图，mod 的 PCK 加载不了，所以运行时用散文件解码后替换**显示节点**。
///
/// 素材：`mods/saria/images/relics/&lt;类名&gt;.png`（256）与 `cropped/&lt;类名&gt;.png`（80）。
///
/// 注意（v0.6.4 修正）：`NRelic.Icon` 是**属性**不是字段，早先按字段反射拿不到，导致完全没替换；
/// 现在直接用公开属性 `Icon`，尺寸从私有字段 `_iconSize` 读。
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

    internal static Texture2D? Big(RelicModel? model) => model == null ? null : Load(model.GetType().Name, small: false);

    internal static Texture2D? Small(RelicModel? model) => model == null ? null : Load(model.GetType().Name, small: true);

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
    private static readonly System.Reflection.FieldInfo? IconSizeField =
        AccessTools.Field(typeof(NRelic), "_iconSize");

    [HarmonyPostfix]
    private static void Postfix(NRelic __instance)
    {
        try
        {
            var model = __instance.Model;
            var rect = __instance.Icon;
            if (model == null || rect == null)
                return;

            bool small = true;
            if (IconSizeField?.GetValue(__instance) is object raw)
                small = Convert.ToInt32(raw) == 0;   // IconSize.Small == 0

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

            if (relics[index] is not RelicModel model)
                return;

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
