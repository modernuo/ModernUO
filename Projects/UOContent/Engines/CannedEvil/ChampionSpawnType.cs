/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionSpawnType.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using Server.Mobiles;

namespace Server.Engines.CannedEvil
{
    public enum ChampionSpawnType
    {
        Abyss,
        Arachnid,
        ColdBlood,
        ForestLord,
        VerminHorde,
        UnholyTerror,
        SleepingDragon,
        Glade,
        Pestilence,
        Graveyard
    }

    public class ChampionSpawnInfo
    {
        public string Name { get; }

        public Type Champion { get; }

        public Type[][] SpawnTypes { get; }

        public string[] LevelNames { get; }

        public ChampionSpawnInfo(string name, Type champion, string[] levelNames, Type[][] spawnTypes)
        {
            Name = name;
            Champion = champion;
            LevelNames = levelNames;
            SpawnTypes = spawnTypes;
        }

        public static ChampionSpawnInfo[] Table { get; } = {
            new("Abyss", typeof(Semidar), new[]{ "Foe", "Assassin", "Conqueror" }, new[]
            {
                new[]{ typeof(GreaterMongbat), typeof(Imp) },                           // Level 1
                new[]{ typeof(Gargoyle), typeof(Harpy) },                               // Level 2
                new[]{ typeof(FireGargoyle), typeof(StoneGargoyle) },                   // Level 3
                new[]{ typeof(Daemon), typeof(Succubus) }                               // Level 4
            }),
            new("Arachnid", typeof(Mephitis), new[]{ "Bane", "Killer", "Vanquisher" }, new[]
            {
                new[]{ typeof(Scorpion), typeof(GiantSpider) },                         // Level 1
                new[]{ typeof(TerathanDrone), typeof(TerathanWarrior) },                // Level 2
                new[]{ typeof(DreadSpider), typeof(TerathanMatriarch) },                // Level 3
                new[]{ typeof(PoisonElemental), typeof(TerathanAvenger) }               // Level 4
            }),
            new("Cold Blood", typeof(Rikktor), new[]{ "Blight", "Slayer", "Destroyer" }, new[]
            {
                new[]{ typeof(Lizardman), typeof(GiantSerpent) },                       // Level 1
                new[]{ typeof(LavaLizard), typeof(OphidianWarrior) },                   // Level 2
                new[]{ typeof(Drake), typeof(OphidianArchmage) },                       // Level 3
                new[]{ typeof(Dragon), typeof(OphidianKnight) }                         // Level 4
            }),
            new("Forest Lord", typeof(LordOaks), new[]{ "Enemy", "Curse", "Slaughterer" }, new[]
            {
                new[]{ typeof(Pixie), typeof(ShadowWisp) },                             // Level 1
                new[]{ typeof(Kirin), typeof(Wisp) },                                   // Level 2
                new[]{ typeof(Centaur), typeof(Unicorn) },                              // Level 3
                new[]{ typeof(EtherealWarrior), typeof(SerpentineDragon) }              // Level 4
            }),
            new("Vermin Horde", typeof(Barracoon), new[]{ "Adversary", "Subjugator", "Eradicator" }, new[]
            {
                new[]{ typeof(GiantRat), typeof(Slime) },                               // Level 1
                new[]{ typeof(DireWolf), typeof(Ratman) },                              // Level 2
                new[]{ typeof(HellHound), typeof(RatmanMage) },                         // Level 3
                new[]{ typeof(RatmanArcher), typeof(SilverSerpent) }                    // Level 4
            }),
            new("Unholy Terror", typeof(Neira), new[]{ "Scourge", "Punisher", "Nemesis" }, new[]
            {
                Core.AOS ?                                                              // Level 1
                    new[]{ typeof(Bogle), typeof(Ghoul), typeof(Shade), typeof(Spectre), typeof(Wraith) }
                    : new[]{ typeof(Ghoul), typeof(Shade), typeof(Spectre), typeof(Wraith) },

                new[]{ typeof(BoneMagi), typeof(Mummy), typeof(SkeletalMage) },         // Level 2
                new[]{ typeof(BoneKnight), typeof(Lich), typeof(SkeletalKnight) },      // Level 3
                new[]{ typeof(LichLord), typeof(RottingCorpse) }                        // Level 4
            }),
            new("Sleeping Dragon", typeof(Serado), new[]{ "Rival", "Challenger", "Antagonist" } , new[]
            {
                new[]{ typeof(DeathwatchBeetleHatchling), typeof(Lizardman) },          // Level 1
                new[]{ typeof(DeathwatchBeetle), typeof(Kappa) },                       // Level 2
                new[]{ typeof(LesserHiryu), typeof(RevenantLion) },                     // Level 3
                new[]{ typeof(Hiryu), typeof(Oni) }                                     // Level 4
            }),
            new("Glade", typeof(Twaulo), new[]{ "Banisher", "Enforcer", "Eradicator" } , new[]
            {
                new[]{ typeof(Pixie), typeof(ShadowWisp) },                             // Level 1
                new[]{ typeof(Centaur), typeof(MLDryad) },                              // Level 2
                new[]{ typeof(Satyr), typeof(CuSidhe) },                                // Level 3
                new[]{ typeof(FeralTreefellow), typeof(RagingGrizzlyBear) }             // Level 4
            }),
            new("The Corrupt", typeof(Ilhenir), new[]{ "Cleanser", "Expunger", "Depurator" } , new[]
            {
                new[]{ typeof(PlagueSpawn), typeof(Bogling) },                          // Level 1
                new[]{ typeof(PlagueBeast), typeof(BogThing) },                         // Level 2
                new[]{ typeof(PlagueBeastLord), typeof(InterredGrizzle) },              // Level 3
                new[]{ typeof(FetidEssence), typeof(PestilentBandage) }                 // Level 4
            }),
        };

        public static ChampionSpawnInfo GetInfo(ChampionSpawnType type)
        {
            var v = (int)type;

            if (v < 0 || v >= Table.Length)
            {
                v = 0;
            }

            return Table[v];
        }
    }
}
