using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace Saria;

/// <summary>
/// 能量图标（正文内联 / 卡面 / 提示）的统一注册。
///
/// 原版三处用的其实是两张不同的图：
///   * 卡面与悬停提示：`res://images/packed/energy_icons/energy_&lt;卡池&gt;.png`（74×74）
///   * 卡牌描述正文里的 `{Energy}`：RichTextLabel 的 `[img]res://images/packed/sprite_fonts/&lt;前缀&gt;_energy_icon.png[/img]`（24×24）
///
/// 两张都在游戏自己的 PCK 里、且 mod 的 PCK 贴图 `ResourceLoader.Load` 返回 null，
/// 所以这里用 Godot 的 `Resource.TakeOverPath()`：把运行时解码出来的贴图**占住那个 res:// 路径**，
/// 之后 ResourceLoader（包括 RichTextLabel 的 [img] 标签）读该路径时会直接拿到我们这张图。
///
/// 注意：只改自己卡池的 EnergyColorName 为 "saria"，所以不会影响原版角色。
/// </summary>
internal static class ProbeEnergyIcons
{
    /// <summary>与 ProbeCardPool.EnergyColorName 保持一致。</summary>
    internal const string Prefix = "saria";

    /// <summary>正文内联图标（原版 24×24）。</summary>
    internal const string TextIconPath = "res://images/packed/sprite_fonts/" + Prefix + "_energy_icon.png";

    /// <summary>卡面 / 提示用的能量图标（原版 74×74）。</summary>
    internal const string PackedIconPath = "res://images/packed/energy_icons/energy_" + Prefix + ".png";

    private static Texture2D? _texture;
    private static bool _registered;
    private static bool _failed;

    /// <summary>mod 初始化时调用：解码一次贴图，并占用两条虚拟路径。</summary>
    internal static void Register()
    {
        if (_registered || _failed)
            return;

        try
        {
            _texture = ProbeOrbSkin.Load("energy_saria.png");
            if (_texture == null)
            {
                Log.Warn("[saria][energy-icon] source texture missing; inline energy icons stay vanilla");
                _failed = true;
                return;
            }

            _texture.TakeOverPath(TextIconPath);
            _texture.TakeOverPath(PackedIconPath);
            _registered = true;

            // 诊断：确认占位后的路径能被 ResourceLoader 取到（RichTextLabel 的 [img] 走这条）
            var textCheck = ResourceLoader.Load<Texture2D>(TextIconPath);
            var packedCheck = ResourceLoader.Load<Texture2D>(PackedIconPath);
            Log.Info($"[saria][energy-icon] registered: text={textCheck != null} packed={packedCheck != null} " +
                     $"size={_texture.GetSize()}");
        }
        catch (Exception ex)
        {
            _failed = true;
            Log.Error($"[saria][energy-icon] registration failed: {ex.Message}");
        }
    }

    /// <summary>取运行时贴图（其它补丁复用同一张，避免重复解码）。</summary>
    internal static Texture2D? Texture
    {
        get
        {
            if (!_registered && !_failed)
                Register();
            return _texture;
        }
    }
}
