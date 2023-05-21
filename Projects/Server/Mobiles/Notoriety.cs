namespace Server;

public delegate int NotorietyHandler(Mobile source, Mobile target);

public static class Notoriety
{
    public const int Innocent = 1;
    public const int Ally = 2;
    public const int CanBeAttacked = 3;
    public const int Criminal = 4;
    public const int Enemy = 5;
    public const int Murderer = 6;
    public const int Invulnerable = 7;

    public static NotorietyHandler Handler { get; set; }

    public static int[] Hues { get; set; } =
    {
        0x000,
        0x059,
        0x03F,
        0x3B2,
        0x3B2,
        0x090,
        0x022,
        0x035
    };

    public static int GetHue(int noto) => noto < 0 || noto >= Hues.Length ? 0 : Hues[noto];

    public static int Compute(Mobile source, Mobile target) => Handler?.Invoke(source, target) ?? CanBeAttacked;
}
