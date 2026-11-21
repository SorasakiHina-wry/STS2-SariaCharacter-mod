using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace Saria;

/// <summary>
/// 能量图标（正文内联 / 卡面 / 提示）的统一注册。
///
/// 原版三处用的是两张不同的图：
///   * 卡面与悬停提示：`res://images/packed/energy_icons/energy_&lt;池&gt;.png`（74×74）
///   * 卡牌描述正文的 `{Energy}`：RichTextLabel `[img]res://images/packed/sprite_fonts/&lt;前缀&gt;_energy_icon.png[/img]`（24×24）
///
/// mod 的 PCK 贴图 `ResourceLoader.Load` 返回 null，所以用 `Resource.TakeOverPath()`：
/// 把运行时解码的贴图**占住那个 res:// 路径**，之后 ResourceLoader（含 RichTextLabel 的 [img]）读该路径即得我们的图。
///
/// ⚠️ 一个 Resource 只能占住**一个**路径：对同一张贴图调用两次 TakeOverPath 会覆盖前一次
/// （曾经的 bug：text=False packed=True，正文图标变成缺失图）。因此这里为每个路径各建一张贴图。
/// </summary>
internal static class ProbeEnergyIcons
{
    /// <summary>与 ProbeCardPool.EnergyColorName 保持一致。</summary>
    internal const string Prefix = "saria";

    /// <summary>正文内联图标（原版 24×24）。</summary>
    internal const string TextIconPath = "res://images/packed/sprite_fonts/" + Prefix + "_energy_icon.png";

    /// <summary>卡面 / 提示用的能量图标（原版 74×74）。</summary>
    internal const string PackedIconPath = "res://images/packed/energy_icons/energy_" + Prefix + ".png";

    private static Texture2D? _textTexture;
    private static Texture2D? _packedTexture;
    private static bool _registered;
    private static bool _failed;

    /// <summary>mod 初始化时调用：解码贴图并占用两条虚拟路径（各用一张独立贴图）。</summary>
    internal static void Register()
    {
        if (_registered || _failed)
            return;

        try
        {
            var image = DecodeSource();
            if (image == null)
            {
                Log.Warn("[saria][energy-icon] source texture missing; inline energy icons stay vanilla");
                _failed = true;
                return;
            }

            _textTexture = ImageTexture.CreateFromImage(image);
            _textTexture.TakeOverPath(TextIconPath);

            _packedTexture = ImageTexture.CreateFromImage(image);
            _packedTexture.TakeOverPath(PackedIconPath);

            _registered = true;

            var textCheck = ResourceLoader.Load<Texture2D>(TextIconPath);
            var packedCheck = ResourceLoader.Load<Texture2D>(PackedIconPath);
            Log.Info($"[saria][energy-icon] registered: text={textCheck != null} packed={packedCheck != null} " +
                     $"size={image.GetWidth()}x{image.GetHeight()}");
        }
        catch (Exception ex)
        {
            _failed = true;
            Log.Error($"[saria][energy-icon] registration failed: {ex.Message}");
        }
    }

    private static Image? DecodeSource()
    {
        string path = ProbeOrbSkin.OrbDir.PathJoin("energy_saria.png");
        if (!FileAccess.FileExists(path))
        {
            Log.Warn($"[saria][energy-icon] missing file: {path}");
            return null;
        }

        byte[] bytes = FileAccess.GetFileAsBytes(path);
        var image = new Image();
        var err = image.LoadPngFromBuffer(bytes);
        if (err != Error.Ok || image.IsEmpty())
        {
            Log.Warn($"[saria][energy-icon] decode failed ({err}): {path}");
            return null;
        }

        return image;
    }

    /// <summary>给其它补丁复用的贴图（优先用卡面那张 74×74 的注册贴图）。</summary>
    internal static Texture2D? Texture
    {
        get
        {
            if (!_registered && !_failed)
                Register();
            return _packedTexture ?? _textTexture;
        }
    }
}
