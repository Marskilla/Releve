using System.Numerics;
using Raylib_cs;

namespace Releve.Game.Ui;

/// <summary>
/// Vue du lieu (§9.3) : image mise en cache et mise a l'echelle en conservant les proportions,
/// ou rectangle de remplacement portant le titre si l'image est introuvable (§9.2).
/// </summary>
public sealed class RoomViewPanel
{
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.Ordinal);
    private readonly Fonts _fonts;

    public RoomViewPanel(Fonts fonts)
    {
        _fonts = fonts;
    }

    public void Draw(string imageKey, string title)
    {
        var bounds = Layout.ViewBounds;

        Raylib.DrawRectangle((int)bounds.X - Layout.Padding, (int)bounds.Y - Layout.Padding,
            (int)bounds.Width + 2 * Layout.Padding, (int)bounds.Height + 2 * Layout.Padding, Palette.Panel);

        if (TryGetTexture(imageKey, out var texture))
        {
            DrawStretched(texture, bounds);
        }
        else
        {
            DrawReplacement(title, bounds);
        }

        Raylib.DrawRectangleLinesEx(bounds, 1, Palette.Border);
    }

    private void DrawStretched(Texture2D texture, Rectangle bounds)
    {
        var source = new Rectangle(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, source, bounds, Vector2.Zero, 0f, Color.White);
    }

    private void DrawReplacement(string title, Rectangle bounds)
    {
        Raylib.DrawRectangleRec(bounds, Palette.Replacement);

        var size = _fonts.MeasureWidth(title);
        var position = new Vector2(
            bounds.X + (bounds.Width - size) / 2f,
            bounds.Y + bounds.Height / 2f - Fonts.Size);

        _fonts.Draw(title, position, Palette.DimText);
    }

    private bool TryGetTexture(string imageKey, out Texture2D texture)
    {
        if (string.IsNullOrWhiteSpace(imageKey))
        {
            texture = default;
            return false;
        }

        if (_textures.TryGetValue(imageKey, out texture))
        {
            return Raylib.IsTextureValid(texture);
        }

        var path = Assets.RoomImage(imageKey);
        if (!File.Exists(path))
        {
            texture = default;
            return false;
        }

        texture = Raylib.LoadTexture(path);
        if (!Raylib.IsTextureValid(texture))
        {
            return false;
        }

        _textures[imageKey] = texture;
        return true;
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            if (Raylib.IsTextureValid(texture))
            {
                Raylib.UnloadTexture(texture);
            }
        }

        _textures.Clear();
    }
}
