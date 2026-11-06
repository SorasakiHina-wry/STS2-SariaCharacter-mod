using System;
using System.IO;
using Godot;

namespace StrikeSprite;

/// <summary>
/// Tiny logger: appends to the game's user:// folder and mirrors to the Godot console.
/// Never throws — a broken log must not break the effect.
/// </summary>
internal static class ModLog
{
    private static string? _path;

    public static void Init()
    {
        try
        {
            string dir = OS.GetUserDataDir();
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
                _path = Path.Combine(dir, ModConfig.LogFileName);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{ModConfig.ModId}] log init failed: {ex.Message}");
        }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    public static void Debug(string message)
    {
        if (ModConfig.Verbose)
            Write("DEBUG", message);
    }

    private static void Write(string level, string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
        try
        {
            if (_path != null)
                File.AppendAllText(_path, line + "\n");
        }
        catch
        {
            // ignore: logging is best-effort
        }

        GD.Print($"[{ModConfig.ModId}] {message}");
    }
}
