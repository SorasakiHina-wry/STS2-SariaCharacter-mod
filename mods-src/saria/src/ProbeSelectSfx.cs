using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Logging;

namespace Saria;

/// <summary>
/// 角色选择界面的选中音效。
///
/// 原版这一项是 FMOD 事件路径（`event:/sfx/characters/&lt;id&gt;/&lt;id&gt;_select`），mod 无法注册新事件；
/// 所以走"哨兵值 + 拦截 SfxCmd.Play"：<see cref="Saria.CharacterSelectSfxKey"/> 被播到时就换成
/// 播放 mods/saria/audio/selected.ogg（Godot 的 AudioStreamOggVorbis，走文件而不是 FMOD）。
/// </summary>
public static class ProbeSelectSfx
{
    /// <summary>哨兵值：Saria.CharacterSelectSfx 返回它，SfxCmd.Play 拦截到就播 ogg。</summary>
    public const string Key = "saria:select";

    private static AudioStream? _stream;
    private static AudioStreamPlayer? _player;
    private static bool _loadFailed;

    public static void Play(float volume)
    {
        try
        {
            if (!EnsureStream())
                return;

            var player = EnsurePlayer();
            if (player == null)
                return;

            player.Stream = _stream;
            player.VolumeDb = Mathf.LinearToDb(Math.Max(volume, 0.001f));

            // 素材本身没有留白，按需求在播放前加 0.2 秒空档（不改文件，避免重新编码 ogg）
            const float leadIn = 0.2f;
            if (Engine.GetMainLoop() is SceneTree tree)
            {
                var timer = tree.CreateTimer(leadIn);
                timer.Timeout += () =>
                {
                    if (GodotObject.IsInstanceValid(player))
                        player.Play();
                };
            }
            else
            {
                player.Play();
            }

            Log.Info($"[saria] character select sfx scheduled (+{leadIn}s lead-in)");
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria] select sfx failed: {ex.Message}");
        }
    }

    private static string Path =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("audio").PathJoin("selected.ogg");

    private static bool EnsureStream()
    {
        if (_stream != null)
            return true;
        if (_loadFailed)
            return false;

        string path = Path;
        if (!FileAccess.FileExists(path))
        {
            Log.Warn($"[saria] select sfx missing: {path}");
            _loadFailed = true;
            return false;
        }

        byte[] bytes = FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0)
        {
            Log.Warn($"[saria] select sfx empty: {path}");
            _loadFailed = true;
            return false;
        }

        _stream = AudioStreamOggVorbis.LoadFromBuffer(bytes);
        if (_stream == null)
        {
            Log.Warn("[saria] select sfx decode failed");
            _loadFailed = true;
            return false;
        }

        return true;
    }

    private static AudioStreamPlayer? EnsurePlayer()
    {
        if (_player != null && GodotObject.IsInstanceValid(_player))
            return _player;

        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
            return null;

        _player = new AudioStreamPlayer { Name = "SariaSelectSfx" };
        tree.Root.AddChild(_player);
        return _player;
    }
}

/// <summary>
/// 角色选择界面点选角色时播我们自己的 ogg。
/// 直接挂在 NCharacterSelectScreen.SelectCharacter 上：SfxCmd.Play 的分发在不同版本里不总是走同一条路，
/// 挂在界面上更稳；CharacterSelectSfx 仍返回哨兵值，保证原版不会同时播铁甲战士的音效。
/// </summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen), "SelectCharacter")]
public static class ProbeSelectSfxPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCharacterSelectButton charSelectButton)
    {
        try
        {
            var character = charSelectButton?.Character;
            if (character == null)
                return;
            if (!character.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal))
                return;

            ProbeSelectSfx.Play(1f);
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria] select sfx hook failed: {ex.Message}");
        }
    }
}
