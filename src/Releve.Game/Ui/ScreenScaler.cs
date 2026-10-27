using Raylib_cs;
using Releve.Game.Ui;

namespace Releve.Game.Ui;

/// <summary>
/// Calcul d'échelle et de placement pour le rendu logique 1280x720 dans une fenêtre redimensionnée.
/// Applique SetMouseOffset et SetMouseScale pour que GetMousePosition renvoie des coordonnées logiques.
/// </summary>
public sealed class ScreenScaler
{
    private const float LogicalWidth = Layout.ScreenWidth;
    private const float LogicalHeight = Layout.ScreenHeight;

    public float Scale { get; private set; } = 1f;
    public Rectangle DestRect { get; private set; } = new(0, 0, LogicalWidth, LogicalHeight);

    /// <summary>
    /// Met a jour l'echelle et le rectangle de destination. Appelle SetMouseOffset/SetMouseScale.
    /// </summary>
    public void Update(int windowWidth, int windowHeight)
    {
        // Echelle = min(largeur/1280, hauteur/720), sans arrondi a l'entier
        Scale = MathF.Min(windowWidth / LogicalWidth, windowHeight / LogicalHeight);

        // Rectangle centre dans la fenetre
        var destWidth = LogicalWidth * Scale;
        var destHeight = LogicalHeight * Scale;
        var destX = (windowWidth - destWidth) / 2f;
        var destY = (windowHeight - destHeight) / 2f;

        DestRect = new Rectangle(destX, destY, destWidth, destHeight);

        // Appliquer offset et echelle pour coordonnees logiques de la souris
        // Raylib: position logique = (position brute + offset) * scale
        // Donc: offset = -destX, scale = 1/Scale
        Raylib.SetMouseOffset((int)(-destX), (int)(-destY));
        Raylib.SetMouseScale(1f / Scale, 1f / Scale);
    }
}