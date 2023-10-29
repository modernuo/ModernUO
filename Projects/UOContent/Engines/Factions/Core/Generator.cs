using System;

namespace Server.Factions
{
    public static class Generator
    {
        public static void Initialize()
        {
            CommandSystem.Register("GenerateFactions", AccessLevel.Administrator, GenerateFactions_OnCommand);
        }

        public static void GenerateFactions_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            FactionSystem.Enable();

            var factions = Faction.Factions;

            foreach (var faction in factions)
            {
                Generate(faction);
                from.SendMessage($"Generated {faction}");
            }

            var towns = Town.Towns;

            foreach (var town in towns)
            {
                Generate(town);
                from.SendMessage($"Generated {town}");
            }

            from.SendMessage("Faction generation completed.");
        }

        public static void Generate(Town town)
        {
            var facet = Faction.Facet;

            var def = town.Definition;

            if (!CheckExistence(def.Monolith, facet, typeof(TownMonolith)))
            {
                var mono = new TownMonolith(town);
                mono.MoveToWorld(def.Monolith, facet);
                mono.Sigil = new Sigil(town);
            }

            if (!CheckExistence(def.TownStone, facet, typeof(TownStone)))
            {
                new TownStone(town).MoveToWorld(def.TownStone, facet);
            }
        }

        public static void Generate(Faction faction)
        {
            var facet = Faction.Facet;

            var towns = Town.Towns;

            var stronghold = faction.Definition.Stronghold;

            if (!CheckExistence(stronghold.JoinStone, facet, typeof(JoinStone)))
            {
                new JoinStone(faction).MoveToWorld(stronghold.JoinStone, facet);
            }

            if (!CheckExistence(stronghold.FactionStone, facet, typeof(FactionStone)))
            {
                new FactionStone(faction).MoveToWorld(stronghold.FactionStone, facet);
            }

            for (var i = 0; i < stronghold.Monoliths.Length; ++i)
            {
                var monolith = stronghold.Monoliths[i];

                if (!CheckExistence(monolith, facet, typeof(StrongholdMonolith)))
                {
                    new StrongholdMonolith(towns[i], faction).MoveToWorld(monolith, facet);
                }
            }
        }

        private static bool CheckExistence(Point3D loc, Map facet, Type type)
        {
            foreach (var item in facet.GetItemsAt(loc))
            {
                if (type.IsInstanceOfType(item))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
