using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Logging;

namespace Saria;

/// <summary>
/// 角色选择 / 开局转场遮罩。
///
/// 原版每个角色有一个 `<entry>_transition_mat.tres`（`ShaderMaterial` + 内联 shader + 遮罩贴图），
/// 由 `CharacterModel.CharacterSelectTransitionPath` 指向，`NTransition.FadeOut/FadeIn` 通过
/// `PreloadManager.Cache.GetMaterial(path)` 取用并把 `threshold` 从 0 补间到 1。
///
/// 我们的角色继承 `PlaceholderCharacterModel`，`CustomCharacterSelectTransitionPath` 默认指向
/// **铁甲战士的真实材质**（`ironclad_transition_mat.tres`）—— 路径必须真实存在，否则预加载会失败。
/// 所以这里不换路径，而是在 `AssetCache.GetMaterial` 上拦截：
/// 「当前选中的是塞雷娅」且「取的是铁甲战士的转场材质」时，返回运行时构造的同款 `ShaderMaterial`，
/// `transitionTex` 换成散文件解码出的 `saria_transition.png`。
/// </summary>
internal static class ProbeTransition
{
    /// <summary>塞雷娅选中时，拦截的角色转场材质路径后缀。</summary>
    internal const string MaterialSuffix = "ironclad_transition_mat.tres";

    /// <summary>角色选择界面里当前选中的是不是塞雷娅（由 `ProbeCharacterSelectBgPatch` 维护）。</summary>
    internal static bool SariaSelected;

    internal static string TransitionDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("images").PathJoin("transition");

    /// <summary>原版铁甲战士材质里的 shader，原样照抄（只用红通道做阈值扫描）。</summary>
    private const string ShaderCode = @"shader_type canvas_item;

uniform sampler2D transitionTex;
uniform float threshold : hint_range(0,1);

void fragment() {
    float falloff = 1.0 - texture(transitionTex, UV).r;

    // helps with falloff artifacts issues towards the transition extremes
    float remap  = mix(-0.1, 1.1, threshold);
    falloff = step(falloff, remap);
    COLOR.a = falloff;
}
";

    private static Material? _material;
    private static bool _failed;

    internal static Material? Material
    {
        get
        {
            if (_material != null || _failed)
                return _material;

            try
            {
                string path = TransitionDir.PathJoin("saria_transition.png");
                if (!FileAccess.FileExists(path))
                {
                    Log.Warn($"[saria][transition] missing file: {path}");
                    _failed = true;
                    return null;
                }

                var image = new Image();
                var err = image.LoadPngFromBuffer(FileAccess.GetFileAsBytes(path));
                if (err != Error.Ok || image.IsEmpty())
                {
                    Log.Warn($"[saria][transition] decode failed ({err}): {path}");
                    _failed = true;
                    return null;
                }

                var texture = ImageTexture.CreateFromImage(image);
                var shader = new Shader { Code = ShaderCode };
                var material = new ShaderMaterial { Shader = shader };
                material.SetShaderParameter("transitionTex", texture);
                material.SetShaderParameter("threshold", 0f);

                _material = material;
                Log.Info($"[saria][transition] material built ({image.GetWidth()}x{image.GetHeight()})");
            }
            catch (Exception ex)
            {
                _failed = true;
                Log.Error($"[saria][transition] build failed: {ex.Message}");
            }

            return _material;
        }
    }
}

/// <summary>`PreloadManager.Cache.GetMaterial(...)` 命中我们的转场材质路径时返回运行时材质。</summary>
[HarmonyPatch(typeof(AssetCache), nameof(AssetCache.GetMaterial))]
public static class ProbeTransitionMaterialPatch
{
    [HarmonyPrefix]
    private static bool Prefix(string path, ref Material? __result)
    {
        if (path == null || !ProbeTransition.SariaSelected)
            return true;
        if (!path.EndsWith(ProbeTransition.MaterialSuffix, StringComparison.Ordinal))
            return true;

        var material = ProbeTransition.Material;
        if (material == null)
            return true;

        __result = material;
        return false;
    }
}
