using System;
using Server.Items;
using Server.Targeting;

namespace Server.Engines.Harvest
{
    public class Lumberjacking : HarvestSystem
    {
        private static Lumberjacking m_System;

        private static readonly int[] m_TreeTiles =
        {
            0x0CCA, 0x0CCB, 0x0CCC, 0x0CCD, 0x0CD0, 0x0CD3, 0x0CD6, 0x0CD8,
            0x0CDA, 0x0CDD, 0x0CE0, 0x0CE3, 0x0CE6, 0x0CF8, 0x0CFB, 0x0CFE,
            0x0D01, 0x0D41, 0x0D42, 0x0D43, 0x0D44, 0x0D57, 0x0D58, 0x0D59,
            0x0D5A, 0x0D5B, 0x0D6E, 0x0D6F, 0x0D70, 0x0D71, 0x0D72, 0x0D84,
            0x0D85, 0x0D86, 0x12B5, 0x12B6, 0x12B7, 0x12B8, 0x12B9, 0x12BA,
            0x12BB, 0x12BC, 0x12BD,

            0x0CCE, 0x0CCF, 0x0CD1, 0x0CD2, 0x0CD4, 0x0CD5, 0x0CD7, 0x0CD9,
            0x0CDB, 0x0CDC, 0x0CDE, 0x0CDF, 0x0CE1, 0x0CE2, 0x0CE4, 0x0CE5,
            0x0CE7, 0x0CE8, 0x0CF9, 0x0CFA, 0x0CFC, 0x0CFD, 0x0CFF, 0x0D00,
            0x0D02, 0x0D03, 0x0D45, 0x0D46, 0x0D47, 0x0D48, 0x0D49, 0x0D4A,
            0x0D4B, 0x0D4C, 0x0D4D, 0x0D4E, 0x0D4F, 0x0D50, 0x0D51, 0x0D52,
            0x0D53, 0x0D5C, 0x0D5D, 0x0D5E, 0x0D5F, 0x0D60, 0x0D61, 0x0D62,
            0x0D63, 0x0D64, 0x0D65, 0x0D66, 0x0D67, 0x0D68, 0x0D69, 0x0D73,
            0x0D74, 0x0D75, 0x0D76, 0x0D77, 0x0D78, 0x0D79, 0x0D7A, 0x0D7B,
            0x0D7C, 0x0D7D, 0x0D7E, 0x0D7F, 0x0D87, 0x0D88, 0x0D89, 0x0D8A,
            0x0D8B, 0x0D8C, 0x0D8D, 0x0D8E, 0x0D8F, 0x0D90, 0x0D95, 0x0D96,
            0x0D97, 0x0D99, 0x0D9A, 0x0D9B, 0x0D9D, 0x0D9E, 0x0D9F, 0x0DA1,
            0x0DA2, 0x0DA3, 0x0DA5, 0x0DA6, 0x0DA7, 0x0DA9, 0x0DAA, 0x0DAB,
            0x12BE, 0x12BF, 0x12C0, 0x12C1, 0x12C2, 0x12C3, 0x12C4, 0x12C5,
            0x12C6, 0x12C7
        };

        private Lumberjacking()
        {
            HarvestResource[] res;
            HarvestVein[] veins;

            var lumber = new HarvestDefinition
            {
                BankWidth = 4,
                BankHeight = 3,
                MinTotal = 20,
                MaxTotal = 45,
                MinRespawn = TimeSpan.FromMinutes(20.0),
                MaxRespawn = TimeSpan.FromMinutes(30.0),
                Skill = SkillName.Lumberjacking,
                LandTiles = Array.Empty<int>(),
                StaticTiles = m_TreeTiles,
                MaxRange = 2,
                ConsumedPerHarvest = 10,
                ConsumedPerFeluccaHarvest = 20,
                EffectActions = new[] { 13 },
                EffectSounds = new[] { 0x13E },
                EffectCounts = Core.AOS ? new[] { 1 } : new[] { 1, 2, 2, 2, 3 },
                EffectDelay = TimeSpan.FromSeconds(1.6),
                EffectSoundDelay = TimeSpan.FromSeconds(0.9),
                NoResourcesMessage = 500493, // There's not enough wood here to harvest.
                FailMessage = 500495,        // You hack at the tree for a while, but fail to produce any useable wood.
                OutOfRangeMessage = 500446,  // That is too far away.
                PackFullMessage = 500497,    // You can't place any wood into your backpack!
                ToolBrokeMessage = 500499    // You broke your axe.
            };

            if (Core.ML)
            {
                res = new[]
                {
                    new HarvestResource(00.0, 00.0, 100.0, 1072540, typeof(Log)),
                    new HarvestResource(65.0, 25.0, 105.0, 1072541, typeof(OakLog)),
                    new HarvestResource(80.0, 40.0, 120.0, 1072542, typeof(AshLog)),
                    new HarvestResource(95.0, 55.0, 135.0, 1072543, typeof(YewLog)),
                    new HarvestResource(100.0, 60.0, 140.0, 1072544, typeof(HeartwoodLog)),
                    new HarvestResource(100.0, 60.0, 140.0, 1072545, typeof(BloodwoodLog)),
                    new HarvestResource(100.0, 60.0, 140.0, 1072546, typeof(FrostwoodLog))
                };

                veins = new[]
                {
                    new HarvestVein(490, 0.0, res[0], null),   // Ordinary Logs
                    new HarvestVein(300, 0.5, res[1], res[0]), // Oak
                    new HarvestVein(100, 0.5, res[2], res[0]), // Ash
                    new HarvestVein(050, 0.5, res[3], res[0]), // Yew
                    new HarvestVein(030, 0.5, res[4], res[0]), // Heartwood
                    new HarvestVein(020, 0.5, res[5], res[0]), // Bloodwood
                    new HarvestVein(010, 0.5, res[6], res[0])  // Frostwood
                };

                lumber.BonusResources = new[]
                {
                    new BonusHarvestResource(0, 83.9, null, null), // Nothing
                    new BonusHarvestResource(100, 10.0, 1072548, typeof(BarkFragment)),
                    new BonusHarvestResource(100, 03.0, 1072550, typeof(LuminescentFungi)),
                    new BonusHarvestResource(100, 02.0, 1072547, typeof(SwitchItem)),
                    new BonusHarvestResource(100, 01.0, 1072549, typeof(ParasiticPlant)),
                    new BonusHarvestResource(100, 00.1, 1072551, typeof(BrilliantAmber))
                };
            }
            else
            {
                res = new[]
                {
                    new HarvestResource(00.0, 00.0, 100.0, 500498, typeof(Log))
                };

                veins = new[]
                {
                    new HarvestVein(1000, 0.0, res[0], null)
                };
            }

            lumber.Resources = res;
            lumber.Veins = veins;

            lumber.RaceBonus = Core.ML;
            lumber.RandomizeVeins = Core.ML;

            Definitions = new[] { lumber };
        }

        public static Lumberjacking System => m_System ??= new Lumberjacking();

        public override bool CheckHarvest(Mobile from, Item tool)
        {
            if (!base.CheckHarvest(from, tool))
            {
                return false;
            }

            if (tool.Parent != from)
            {
                from.SendLocalizedMessage(500487); // The axe must be equipped for any serious wood chopping.
                return false;
            }

            return true;
        }

        public override bool CheckHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            if (!base.CheckHarvest(from, tool, def, toHarvest))
            {
                return false;
            }

            if (tool.Parent != from)
            {
                from.SendLocalizedMessage(500487); // The axe must be equipped for any serious wood chopping.
                return false;
            }

            return true;
        }

        public override void OnBadHarvestTarget(Mobile from, Item tool, object toHarvest)
        {
            if (toHarvest is Mobile mobile)
            {
                // You can only skin dead creatures.
                mobile.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500450, from.NetState);
            }
            else if (toHarvest is Item item)
            {
                item.LabelTo(from, 500464); // Use this on corpses to carve away meat and hide
            }
            else if (toHarvest is StaticTarget or LandTarget)
            {
                from.SendLocalizedMessage(500489); // You can't use an axe on that.
            }
            else
            {
                from.SendLocalizedMessage(1005213); // You can't do that
            }
        }

        public override void OnHarvestStarted(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            base.OnHarvestStarted(from, tool, def, toHarvest);

            if (Core.ML)
            {
                from.RevealingAction();
            }
        }

        public static void Initialize()
        {
            Array.Sort(m_TreeTiles);
        }
    }
}
