namespace Server;

public static class PoisonKinds
{
    private static Poison _lesser;
    private static Poison _regular;
    private static Poison _greater;
    private static Poison _deadly;
    private static Poison _lethal;

    private static Poison _lesserDarkglow;
    private static Poison _regularDarkglow;
    private static Poison _greaterDarkglow;
    private static Poison _deadlyDarkglow;

    private static Poison _lesserParasitic;
    private static Poison _regularParasitic;
    private static Poison _greaterParasitic;
    private static Poison _deadlyParasitic;
    private static Poison _lethalParasitic;

    extension(Poison poison)
    {
        public static Poison Lesser => _lesser ??= Poison.GetPoison("Lesser");
        public static Poison Regular => _regular ??= Poison.GetPoison("Regular");
        public static Poison Greater => _greater ??= Poison.GetPoison("Greater");
        public static Poison Deadly => _deadly ??= Poison.GetPoison("Deadly");
        public static Poison Lethal => _lethal ??= Poison.GetPoison("Lethal");

        public static Poison LesserDarkglow => _lesserDarkglow ??= Poison.GetPoison("LesserDarkglow");
        public static Poison RegularDarkglow => _regularDarkglow ??= Poison.GetPoison("RegularDarkglow");
        public static Poison GreaterDarkglow => _greaterDarkglow ??= Poison.GetPoison("GreaterDarkglow");
        public static Poison DeadlyDarkglow => _deadlyDarkglow ??= Poison.GetPoison("DeadlyDarkglow");

        public static Poison LesserParasitic => _lesserParasitic ??= Poison.GetPoison("LesserParasitic");
        public static Poison RegularParasitic => _regularParasitic ??= Poison.GetPoison("RegularParasitic");
        public static Poison GreaterParasitic => _greaterParasitic ??= Poison.GetPoison("GreaterParasitic");
        public static Poison DeadlyParasitic => _deadlyParasitic ??= Poison.GetPoison("DeadlyParasitic");
        public static Poison LethalParasitic => _lethalParasitic ??= Poison.GetPoison("LethalParasitic");

        public bool IsDarkglow => poison.Family == PoisonFamily.Darkglow;
        public bool IsParasitic => poison.Family == PoisonFamily.Parasitic;

        public static Poison GetPoison(int level)
        {
            for (var i = 0; i < Poison.Poisons.Count; ++i)
            {
                var p = Poison.Poisons[i];

                if (p.Family == PoisonFamily.Standard && p.Level == level)
                {
                    return p;
                }
            }

            return null;
        }

        public static Poison GetPoisonByFamilyAndLevel(PoisonFamily family, int level)
        {
            for (var i = 0; i < Poison.Poisons.Count; ++i)
            {
                var p = Poison.Poisons[i];

                if (p.Family == family && p.Level == level)
                {
                    return p;
                }
            }

            return null;
        }
    }

    [CallPriority(10)]
    public static void Configure()
    {
        if (Core.AOS)
        {
            Poison.Register(new PoisonImpl("Lesser", 0, 0, 4, 16, 7.5, 3.0, 2.25, 10, 4));
            Poison.Register(new PoisonImpl("Regular", 1, 1, 8, 18, 10.0, 3.0, 3.25, 10, 3));
            Poison.Register(new PoisonImpl("Greater", 2, 2, 12, 20, 15.0, 3.0, 4.25, 10, 2));
            Poison.Register(new PoisonImpl("Deadly", 3, 3, 16, 30, 30.0, 3.0, 5.25, 15, 2));
            Poison.Register(new PoisonImpl("Lethal", 4, 4, 20, 50, 35.0, 3.0, 5.25, 20, 2));
        }
        else
        {
            Poison.Register(new PoisonImpl("Lesser", 0, 0, 4, 26, 2.5, 3.5, 3.0, 10, 2));
            Poison.Register(new PoisonImpl("Regular", 1, 1, 5, 26, 3.125, 3.5, 3.0, 10, 2));
            Poison.Register(new PoisonImpl("Greater", 2, 2, 6, 26, 6.25, 3.5, 3.0, 10, 2));
            Poison.Register(new PoisonImpl("Deadly", 3, 3, 7, 26, 12.5, 3.5, 4.0, 10, 2));
            Poison.Register(new PoisonImpl("Lethal", 4, 4, 9 , 26, 25.0, 3.5, 5.0, 10, 2));
        }

        if (Core.ML)
        {
            Poison.Register(new PoisonImpl("LesserDarkglow", 10, 0, 4, 16, 7.5, 3.0, 2.25, 10, 4, PoisonFamily.Darkglow));
            Poison.Register(new PoisonImpl("RegularDarkglow", 11, 1, 8, 18, 10.0, 3.0, 3.25, 10, 3, PoisonFamily.Darkglow));
            Poison.Register(new PoisonImpl("GreaterDarkglow", 12, 2, 12, 20, 15.0, 3.0, 4.25, 10, 2, PoisonFamily.Darkglow));
            Poison.Register(new PoisonImpl("DeadlyDarkglow", 13, 3, 16, 30, 30.0, 3.0, 5.25, 15, 2, PoisonFamily.Darkglow));

            Poison.Register(new PoisonImpl("LesserParasitic", 20, 0, 4, 16, 7.5, 3.0, 2.25, 10, 4, PoisonFamily.Parasitic));
            Poison.Register(new PoisonImpl("RegularParasitic", 21, 1, 8, 18, 10.0, 3.0, 3.25, 10, 3, PoisonFamily.Parasitic));
            Poison.Register(new PoisonImpl("GreaterParasitic", 22, 2, 12, 20, 15.0, 3.0, 4.25, 10, 2, PoisonFamily.Parasitic));
            Poison.Register(new PoisonImpl("DeadlyParasitic", 23, 3, 16, 30, 30.0, 3.0, 5.25, 15, 2, PoisonFamily.Parasitic));
            Poison.Register(new PoisonImpl("LethalParasitic", 24, 4, 20, 50, 35.0, 3.0, 5.25, 20, 2, PoisonFamily.Parasitic));
        }
    }
}
