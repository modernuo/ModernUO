namespace Server.Maps;

public static class ExpansionMapSelectionFlags
{
    public static readonly MapSelectionFlags[] PreT2A = {
        MapSelectionFlags.Felucca
    };

    public static readonly MapSelectionFlags[] T2A = {
        MapSelectionFlags.Trammel, MapSelectionFlags.Felucca
    };

    public static readonly MapSelectionFlags[] LBR = {
        MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar
    };

    public static readonly MapSelectionFlags[] AOS = {
        MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar,
        MapSelectionFlags.Malas
    };

    public static readonly MapSelectionFlags[] SE = {
        MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar,
        MapSelectionFlags.Malas, MapSelectionFlags.Tokuno
    };

    public static readonly MapSelectionFlags[] SA = {
        MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar,
        MapSelectionFlags.Malas, MapSelectionFlags.Tokuno, MapSelectionFlags.TerMur
    };

    public static readonly MapSelectionFlags[] TOL = {
        MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar,
        MapSelectionFlags.Malas, MapSelectionFlags.Tokuno, MapSelectionFlags.TerMur
    };

    public static MapSelectionFlags[] FromExpansion(Expansion expansion) =>
        expansion switch
        {
            >= Expansion.TOL => TOL,
            >= Expansion.SA  => SA,
            >= Expansion.SE  => SE,
            >= Expansion.AOS => AOS,
            >= Expansion.LBR => LBR,
            >= Expansion.T2A => T2A,
            _                => PreT2A
        };
}
