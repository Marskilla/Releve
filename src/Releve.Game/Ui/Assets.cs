namespace Releve.Game.Ui;

/// <summary>Localisation du dossier assets, ou qu'il soit lance le jeu.</summary>
public static class Assets
{
    public static string Root { get; } = ResolveRoot();

    public static string RoomImage(string imageKey) => Combine("rooms", imageKey + ".png");

    /// <summary>Construit un chemin vers un asset depuis le dossier résolu.</summary>
    public static string Combine(params string[] parts) => System.IO.Path.Combine(new[] { Root }.Concat(parts).ToArray());

    public static string Font => Path.Combine(Root, "fonts", "DejaVuSans.ttf");

    private static string ResolveRoot()
    {
        // Les assets sont copies a cote de l'exe.
        var besideExecutable = Path.Combine(AppContext.BaseDirectory, "assets");
        if (Directory.Exists(besideExecutable))
        {
            return besideExecutable;
        }

        // Repli : recherche depuis le repertoire courant vers les parents (lancement depuis les sources).
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "assets");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return besideExecutable;
    }
}
