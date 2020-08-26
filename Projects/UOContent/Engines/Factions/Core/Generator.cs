using System;
using System.Linq;

namespace Server.Factions
{
    public class Generator
    {
        public static void Initialize()
        {
            CommandSystem.Register("GenerateFactions", AccessLevel.Administrator, GenerateFactions_OnCommand);
        }

        public static void GenerateFactions_OnCommand(CommandEventArgs e)
        {
            new FactionPersistance();

            var factions = Faction.Factions;

            foreach (var faction in factions)
                Generate(faction);

            var towns = Town.Towns;

            foreach (var town in towns)
                Generate(town);
        }

        public static void Generate(Town town)
        {
            var facet = Faction.Facet;

            var def = town.Definition;

            if (!CheckExistance(def.Monolith, facet, typeof(TownMonolith)))
            {
                var mono = new TownMonolith(town);
                mono.MoveToWorld(def.Monolith, facet);
                mono.Sigil = new Sigil(town);
            }

            if (!CheckExistance(def.TownStone, facet, typeof(TownStone)))
                new TownStone(town).MoveToWorld(def.TownStone, facet);
        }

        public static void Generate(Faction faction)
        {
            var facet = Faction.Facet;

            var towns = Town.Towns;

            var stronghold = faction.Definition.Stronghold;

            if (!CheckExistance(stronghold.JoinStone, facet, typeof(JoinStone)))
                new JoinStone(faction).MoveToWorld(stronghold.JoinStone, facet);

            if (!CheckExistance(stronghold.FactionStone, facet, typeof(FactionStone)))
                new FactionStone(faction).MoveToWorld(stronghold.FactionStone, facet);

            for (var i = 0; i < stronghold.Monoliths.Length; ++i)
            {
                var monolith = stronghold.Monoliths[i];

                if (!CheckExistance(monolith, facet, typeof(StrongholdMonolith)))
                    new StrongholdMonolith(towns[i], faction).MoveToWorld(monolith, facet);
            }
        }

        private static bool CheckExistance(Point3D loc, Map facet, Type type) =>
            facet.GetItemsInRange(loc, 0).Any(type.IsInstanceOfType);
    }
}
