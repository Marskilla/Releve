using Raylib_cs;

namespace Releve.Game.Ui;

/// <summary>Disposition de l'écran, valeurs initiales en 1280x720.</summary>
public static class Layout
{
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;

    // Zone haute : les deux tiers.
    public const int TopHeight = 480;

    // Vue du lieu : trois quarts de la largeur de la zone haute.
    public const int ViewWidth = 960;
    public const int ViewHeight = TopHeight;

    // Colonne laterale : un quart de la largeur.
    public const int SidebarX = ViewWidth;
    public const int SidebarWidth = ScreenWidth - ViewWidth;

    // Deplacements : hauteur reduite au strict necessaire.
    public const int ArrowsHeight = 132;

    // Inventaire : reste de la colonne.
    public const int InventoryX = SidebarX;
    public const int InventoryY = ArrowsHeight;
    public const int InventoryWidth = SidebarWidth;
    public const int InventoryHeight = TopHeight - ArrowsHeight;

    // Console : un tiers de la hauteur, sur toute la largeur.
    public const int ConsoleX = 0;
    public const int ConsoleY = TopHeight;
    public const int ConsoleWidth = ScreenWidth;
    public const int ConsoleHeight = ScreenHeight - TopHeight;

    public const int Padding = 10;

    public static Rectangle ViewBounds => new(Padding, Padding, ViewWidth - 2 * Padding, ViewHeight - 2 * Padding);
    public static Rectangle ArrowsBounds => new(SidebarX, 0, SidebarWidth, ArrowsHeight);
    public static Rectangle InventoryBounds => new(SidebarX, ArrowsHeight, SidebarWidth, TopHeight - ArrowsHeight);
    public static Rectangle ConsoleBounds => new(ConsoleX, ConsoleY, ConsoleWidth, ConsoleHeight);

    // Modales (dialogues texte, image, options)
    // DialogBounds : 60% de 1280x720 = 768x432, centre = (256, 144)
    public static Rectangle DialogBounds => new(256, 144, 768, 432);
    // ImageDialogBounds : carre de 60% de ScreenHeight = 432x432, centre = (424, 144)
    public static Rectangle ImageDialogBounds => new(424, 144, 432, 432);
}
