namespace Server.Gumps;

public static class GoLocations
{
    public static readonly LocationTree Felucca = new("felucca", Map.Felucca);
    public static readonly LocationTree Trammel = new("trammel", Map.Trammel);
    public static readonly LocationTree Ilshenar = new("ilshenar", Map.Ilshenar);
    public static readonly LocationTree Malas = new("malas", Map.Malas);
    public static readonly LocationTree Tokuno = new("tokuno", Map.Tokuno);
    public static readonly LocationTree TerMur = new("termur", Map.TerMur);

    public static LocationTree GetLocations(Map map)
    {
        if (map == Map.Ilshenar)
        {
            return Ilshenar;
        }

        if (map == Map.TerMur)
        {
            return TerMur;
        }

        if (map == Map.Trammel)
        {
            return Trammel;
        }

        if (map == Map.Malas)
        {
            return Malas;
        }

        if (map == Map.Tokuno)
        {
            return Tokuno;
        }

        return Felucca;
    }
}
