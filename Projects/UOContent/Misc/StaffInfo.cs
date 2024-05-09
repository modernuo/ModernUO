namespace Server.Misc;

public static class StaffInfo
{
    private static readonly string[] _staff =
    {
        // ModernUO Sponsors
        "Prayer",
        "Arch Lich Oro",
        "Tamashii",

        // ModernUO Contributors
        "nibbio",
        "what the moose",

        // RunUO Contributors
        "Aenima",
        "Alkiser",
        "ASayre",
        "David",
        "Krrios",
        "Mark",
        "Merlin",
        "Merlix",  //LordMerlix
        "nerun",
        "Outkast", //TheOutkastDev
        "Phantom",
        "Phenos",
        "psz",
        "Quantos",
        "Ryan",
        "Sp1der", // Kamron
        "V",       //Admin_V
        "Zippy",

        // ServUO Contributors
        "Vorspire",
        "kevin-10",
        "Argalep",
        "dmurphy22",
        "qbradq",

        // Outlands Staff
        "Owyn",
        "Luthius",
        "Jaedan"
    };

    public static string GetRandomStaff() => _staff.RandomElement();
}
