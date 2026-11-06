using System;
using System.IO;
using Godot;

namespace StrikeSprite;

/// <summary>
/// Resolves the effect texture, in this order:
///   1. imported Godot resource (works when a PCK with a valid .import is shipped);
///   2. raw bytes behind the res:// path, decoded at runtime (survives broken .ctex);
///   3. a loose file next to the DLL: assets/textures/&lt;file&gt; (dev workflow, no PCK).
/// </summary>
internal static class TextureLoader
{
    public static Texture2D? Load(string resPath, string fileName)
    {
        Texture2D? texture = GD.Load<Texture2D>(resPath);
        if (texture != null)
        {
            ModLog.Info($"texture loaded from {resPath}");
            return texture;
        }

        try
        {
            if (Godot.FileAccess.FileExists(resPath))
            {
                var bytes = Godot.FileAccess.GetFileAsBytes(resPath);
                var decoded = Decode(bytes);
                if (decoded != null)
                {
                    ModLog.Info($"texture decoded from {resPath} ({bytes?.Length ?? 0} bytes)");
                    return decoded;
                }
            }
        }
        catch (Exception ex)
        {
            ModLog.Warn($"res:// decode failed: {ex.Message}");
        }

        try
        {
            string? dir = Path.GetDirectoryName(typeof(TextureLoader).Assembly.Location);
            if (dir != null)
            {
                string abs = Path.Combine(dir, "assets", "textures", fileName);
                if (File.Exists(abs))
                {
                    var image = Image.LoadFromFile(abs);
                    if (image != null && image.GetWidth() > 0)
                    {
                        ModLog.Info($"texture loaded from {abs}");
                        return ImageTexture.CreateFromImage(image);
                    }
                    ModLog.Warn($"could not decode image at {abs}");
                }
                else
                {
                    ModLog.Warn($"texture not found: {abs}");
                }
            }
        }
        catch (Exception ex)
        {
            ModLog.Error($"loose texture load failed: {ex.Message}");
        }

        return null;
    }

    private static Texture2D? Decode(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;

        var image = new Image();
        Error err = image.LoadPngFromBuffer(bytes);
        if (err != Error.Ok)
            err = image.LoadJpgFromBuffer(bytes);

        return err == Error.Ok ? ImageTexture.CreateFromImage(image) : null;
    }
}
