using System;
using Server.Engines.Quests.Collector;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.Engines.Harvest
{
    public class Fishing : HarvestSystem
    {
        private static Fishing _system;

        private static readonly Type[] _magicFish =
        [
            typeof(PrizedFish), typeof(WondrousFish), typeof(TrulyRareFish), typeof(PeculiarFish)
        ];

        private static readonly Type[] _footwear = [typeof(Boots), typeof(Shoes), typeof(Sandals), typeof(ThighBoots)];

        private static readonly MutateEntry[] _mutateTable =
        [
            new(80.0, 80.0, 4080.0, true, Expansion.UOR, typeof(SpecialFishingNet)),
            new(80.0, 80.0, 4080.0, true, Expansion.UOR, typeof(BigFish)),
            // T2A-UOTD: needs 90; 0.25% at 90, 0.5% at 100.
            // LBR onward: needs 90; 0.42% at 90, 0.83% at 100.
            new(90.0, 80.0, 4080.0, true, Expansion.T2A, Expansion.UOTD, typeof(TreasureMap)),
            new(90.0, 80.0, 2480.0, true, Expansion.LBR, typeof(TreasureMap)),
            // T2A-UOTD: needs 100; 0.5% at 100.
            // LBR onward: needs 100; 1.33% at 100.
            new(100.0, 80.0, 4080.0, true, Expansion.T2A, Expansion.UOTD, typeof(MessageInABottle)),
            new(100.0, 80.0, 1580.0, true, Expansion.LBR, typeof(MessageInABottle)),
            // T2A-UOTD: needs 80; 2.0% at 80, 2.5% at 100.
            // LBR-SA: no minimum; 0% at 0, 5% at 100.
            // HS onward: needs 80; 4.0% at 80, 5.0% at 100.
            new(80.0, 0.0, 4000.0, false, Expansion.T2A, Expansion.UOTD, _magicFish),
            new(0.0, 0.0, 2000.0, false, Expansion.LBR, Expansion.SA, _magicFish),
            new(80.0, 0.0, 2000.0, false, Expansion.HS, _magicFish),
            new(0.0, 103.5, -34.5, false, Expansion.T2A, Expansion.UOTD, _footwear),
            // From LBR the two footwear rows make one curve: 75% at 0 skill, 10% at 100 and 5% at 120.
            new(0.0, 100.0, -62.5, false, Expansion.LBR, _footwear),
            new(0.0, 140.0, -260.0, false, Expansion.LBR, _footwear)
        ];

        // From UO:R deep water needs 65 skill to catch anything, and a GM succeeds 70% of the time.
        private static readonly HarvestResource _deepWaterFish = new(0.0, 65.0, 115.0, 1043297, typeof(Fish));

        private static readonly int[] _waterLandTiles =
        [
            0x00A8, 0x00AB,
            0x0136, 0x0137
        ];

        private static readonly int[] waterStaticTiles =
        [
            0x1797, 0x179C,
            0x346E, 0x3485,
            0x3490, 0x34AB,
            0x34B5, 0x35D5
        ];

        private static readonly int[] bodyParts =
        [
            0x1CDD, 0x1CE5, // arm
            0x1CE0, 0x1CE8, // torso
            0x1CE1, 0x1CE9, // head
            0x1CE2, 0x1CEC  // leg
        ];

        private static readonly int[] boneParts =
        [
            0x1AE0, 0x1AE1, 0x1AE2, 0x1AE3, 0x1AE4,                         // skulls
            0x1B09, 0x1B0A, 0x1B0B, 0x1B0C, 0x1B0D, 0x1B0E, 0x1B0F, 0x1B10, // bone piles
            0x1B15, 0x1B16                                                  // pelvis bones
        ];

        private static readonly int[] miscItems =
        [
            0x1EB5,                        // unfinished barrel
            0xA2A,                         // stool
            0xC1F,                         // broken clock
            0x1047, 0x1048,                // globe
            0x1EB1, 0x1EB2, 0x1EB3, 0x1EB4 // barrel staves
        ];

        private Fishing()
        {
            var fish = new HarvestDefinition
            {
                BankWidth = 8,
                BankHeight = 8,
                MinTotal = 5,
                MaxTotal = 15,
                MinRespawn = TimeSpan.FromMinutes(10.0),
                MaxRespawn = TimeSpan.FromMinutes(20.0),
                Skill = SkillName.Fishing,
                LandTiles = _waterLandTiles,
                StaticTiles = waterStaticTiles,
                RangedTiles = true,
                MaxRange = 4,
                ConsumedPerHarvest = 1,
                ConsumedPerFeluccaHarvest = 1,
                EffectActions = [12],
                EffectSounds = [],
                EffectCounts = [1],
                EffectDelay = TimeSpan.Zero,
                EffectSoundDelay = TimeSpan.FromSeconds(8.0),
                NoResourcesMessage = 503172,     // The fish don't seem to be biting here.
                FailMessage = 503171,            // You fish a while, but fail to catch anything.
                TimedOutOfRangeMessage = 500976, // You need to be closer to the water to fish!
                OutOfRangeMessage = 500976,      // You need to be closer to the water to fish!
                PackFullMessage = 503176,        // You do not have room in your backpack for a fish.
                ToolBrokeMessage = 503174        // You broke your fishing pole.
            };

            HarvestResource[] res = [new(00.0, 00.0, 100.0, 1043297, typeof(Fish))];

            HarvestVein[] veins = [new(1000, 0.0, res[0], null)];

            fish.Resources = res;
            fish.Veins = veins;

            if (Core.ML)
            {
                fish.BonusResources =
                [
                    new BonusHarvestResource(0, 99.4, null, null), // set to same chance as mining ml gems
                    new BonusHarvestResource(80.0, .6, 1072597, typeof(WhitePearl))
                ];
            }

            Definitions = [fish];
        }

        public static Fishing System => _system ??= new Fishing();

        public override void OnConcurrentHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            from.SendLocalizedMessage(500972); // You are already fishing.
        }

        public override bool SpecialHarvest(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc)
        {
            if (from is PlayerMobile player)
            {
                var qs = player.Quest;

                if (qs is CollectorQuest)
                {
                    var obj = qs.FindObjective<FishPearlsObjective>();

                    if (obj?.Completed == false)
                    {
                        if (Utility.RandomBool())
                        {
                            // You pull a shellfish out of the water, and find a rainbow pearl inside of it.
                            player.SendLocalizedMessage(1055086, "", 0x59);
                            obj.CurProgress++;
                        }
                        else
                        {
                            // You pull a shellfish out of the water, but it doesn't have a rainbow pearl.
                            player.SendLocalizedMessage(1055087, "", 0x2C);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public override HarvestResource MutateResource(
            Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc,
            HarvestVein vein, HarvestResource primary, HarvestResource fallback
        ) => Core.UOR && SpecialFishingNet.FullValidation(map, loc.X, loc.Y)
            ? _deepWaterFish
            : base.MutateResource(from, tool, def, map, loc, vein, primary, fallback);

        public override Type MutateType(
            Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc,
            HarvestResource resource
        )
        {
            var deepWater = SpecialFishingNet.FullValidation(map, loc.X, loc.Y);

            var skillBase = from.Skills.Fishing.Base;
            var skillValue = from.Skills.Fishing.Value;

            for (var i = 0; i < _mutateTable.Length; ++i)
            {
                var entry = _mutateTable[i];

                if (Core.Expansion < entry.m_Expansion || Core.Expansion > entry.m_MaxExpansion)
                {
                    continue;
                }

                if (!deepWater && entry.m_DeepWater)
                {
                    continue;
                }

                if (skillBase >= entry.m_ReqSkill)
                {
                    var chance = (skillValue - entry.m_MinSkill) / (entry.m_MaxSkill - entry.m_MinSkill);

                    if (chance > Utility.RandomDouble())
                    {
                        return entry.m_Types.RandomElement();
                    }
                }
            }

            return type;
        }

        // From UO:R the chest needs 65 skill, and below that an SOS changes nothing.
        private static bool CanFishUpShipwreck(Mobile from) => !Core.UOR || from.Skills.Fishing.Base >= 65.0;

        public override bool CheckResources(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, bool timed)
        {
            var pack = from.Backpack;

            if (pack != null && CanFishUpShipwreck(from))
            {
                foreach (var sos in pack.FindItemsByType<SOS>())
                {
                    if (from.Map == sos.TargetMap && from.InRange(sos.TargetLocation, 60))
                    {
                        return true;
                    }
                }
            }

            return base.CheckResources(from, tool, def, map, loc, timed);
        }

        public override Item Construct(Type type, Mobile from)
        {
            if (type == typeof(TreasureMap))
            {
                int level;
                if (from is PlayerMobile mobile && mobile.Young && mobile.Map == Map.Trammel &&
                    TreasureMap.IsInHavenIsland(from))
                {
                    level = 0;
                }
                else
                {
                    level = 1;
                }

                return new TreasureMap(level, from.Map == Map.Felucca ? Map.Felucca : Map.Trammel);
            }

            if (type == typeof(MessageInABottle))
            {
                return new MessageInABottle(from.Map == Map.Felucca ? Map.Felucca : Map.Trammel);
            }

            // The pearl is a bonus on top of the catch, so an SOS never takes its place.
            if (type == typeof(WhitePearl))
            {
                return base.Construct(type, from);
            }

            var pack = from.Backpack;

            if (pack != null && CanFishUpShipwreck(from))
            {
                // We don't have to queue since we are returning on the first SOS.
                foreach (var sos in pack.FindItemsByType<SOS>())
                {
                    if (from.Map != sos.TargetMap || !from.InRange(sos.TargetLocation, 60))
                    {
                        continue;
                    }

                    if (Utility.Random(4) != 0)
                    {
                        return Utility.Random(7) switch
                        {
                            0 => new ShipwreckedItem(bodyParts.RandomElement()),  // Body Parts
                            1 => new ShipwreckedItem(boneParts.RandomElement()),  // Bone Parts
                            2 => new ShipwreckedItem(Utility.Random(0xE9F, 10)),  // Paintings and portraits
                            3 => new ShipwreckedItem(Utility.Random(0x13A4, 11)), // Pillows
                            4 => new ShipwreckedItem(Utility.Random(0xFC4, 9)),   // Shells
                            5 => Utility.RandomBool()                             // Hats
                                ? new SkullCap { IsShipwreckedItem = true }
                                : new TricorneHat { IsShipwreckedItem = true },
                            6 => Utility.Random(miscItems.Length + 1) switch // Misc
                            {
                                var r when r < miscItems.Length => new ShipwreckedItem(miscItems[r]),
                                _                               => new Candelabra { IsShipwreckedItem = true },
                            }
                        };
                    }

                    LockableContainer chest = Utility.RandomBool() ? new MetalGoldenChest() : new WoodenChest();

                    if (sos.IsAncient && Core.SE)
                    {
                        chest.Hue = 0x481;
                    }

                    TreasureMapChest.Fill(chest, Math.Max(1, Math.Min(4, sos.Level)));

                    if (Core.UOR)
                    {
                        chest.DropItem(sos.IsAncient ? new FabledFishingNet() : new SpecialFishingNet());
                    }

                    chest.Movable = true;
                    chest.Locked = false;
                    chest.TrapType = TrapType.None;
                    chest.TrapPower = 0;
                    chest.TrapLevel = 0;

                    sos.Delete();

                    return chest;
                }
            }

            return base.Construct(type, from);
        }

        public override bool Give(Mobile m, Item item, bool placeAtFeet)
        {
            if (item is TreasureMap or MessageInABottle or SpecialFishingNet)
            {
                SurfaceSerpent(m).PackItem(item);

                return true; // we don't want to give the item to the player, it's on the serpent
            }

            return base.Give(m, item, placeAtFeet || item is BigFish or WoodenChest or MetalGoldenChest);
        }

        private static BaseCreature SurfaceSerpent(Mobile m)
        {
            BaseCreature serp = Utility.RandomDouble() < 0.1 ? new DeepSeaSerpent() : new SeaSerpent();

            int x = m.X, y = m.Y;

            var map = m.Map;

            for (var i = 0; map != null && i < 20; ++i)
            {
                var tx = m.X - 10 + Utility.Random(21);
                var ty = m.Y - 10 + Utility.Random(21);

                var t = map.Tiles.GetLandTile(tx, ty);

                if (t.Z == -5 && (t.ID >= 0xA8 && t.ID <= 0xAB || t.ID >= 0x136 && t.ID <= 0x137) &&
                    !SpellHelper.CheckMulti(new Point3D(tx, ty, -5), map))
                {
                    x = tx;
                    y = ty;
                    break;
                }
            }

            serp.MoveToWorld(new Point3D(x, y, -5), map);

            serp.Home = serp.Location;
            serp.RangeHome = 10;
            serp.Combatant = m;

            Timer.StartTimer(TimeSpan.FromMinutes(5.0), () => ReturnToDeepWater(serp));

            m.SendLocalizedMessage(503170); // Uh oh! That doesn't look like a fish!

            return serp;
        }

        // Damage entries last 2 minutes and clear at full health, so a serpent nobody is fighting goes back to deep water.
        private static void ReturnToDeepWater(BaseCreature serpent)
        {
            if (!serpent.Deleted && serpent.FindMostRecentDamager(false) == null)
            {
                serpent.Delete();
            }
        }

        public override void SendSuccessTo(Mobile from, Item item, HarvestResource resource)
        {
            if (item is BigFish fish)
            {
                from.SendLocalizedMessage(1042635); // Your fishing pole bends as you pull a big fish from the depths!
                fish.Fisher = from;
            }
            else if (item is WoodenChest or MetalGoldenChest)
            {
                from.SendLocalizedMessage(503175); // You pull up a heavy chest from the depths of the ocean!
            }
            else
            {
                int number;
                string name;

                if (item is BaseMagicFish)
                {
                    number = 1008124;
                    name = "a mess of small fish";
                }
                else if (item is Fish or BaseShoes)
                {
                    number = 1008124;
                    name = item.ItemData.Name;
                }
                else if (item is TreasureMap)
                {
                    number = 1008125;
                    name = "a sodden piece of parchment";
                }
                else if (item is MessageInABottle)
                {
                    number = 1008125;
                    name = "a bottle, with a message in it";
                }
                else if (item is SpecialFishingNet)
                {
                    number = 1008125;
                    name = "a special fishing net"; // TODO: this is just a guess--what should it really be named?
                }
                else
                {
                    number = 1043297;

                    if ((item.ItemData.Flags & TileFlag.ArticleA) != 0)
                    {
                        name = $"a {item.ItemData.Name}";
                    }
                    else if ((item.ItemData.Flags & TileFlag.ArticleAn) != 0)
                    {
                        name = $"an {item.ItemData.Name}";
                    }
                    else
                    {
                        name = item.ItemData.Name;
                    }
                }

                var ns = from.NetState;

                if (number == 1043297 || ns?.HighSeas == true)
                {
                    from.SendLocalizedMessage(number, name);
                }
                else
                {
                    from.SendLocalizedMessage(number, true, name);
                }
            }
        }

        public override void OnHarvestStarted(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            base.OnHarvestStarted(from, tool, def, toHarvest);

            if (GetHarvestDetails(from, tool, toHarvest, out _, out var map, out var loc, out _))
            {
                Timer.StartTimer(
                    TimeSpan.FromSeconds(1.5),
                    () =>
                    {
                        if (Core.T2A)
                        {
                            from.RevealingAction();
                        }

                        Effects.SendLocationEffect(loc, map, 0x352D, 16, 4);
                        Effects.PlaySound(loc, map, 0x364);
                    }
                );
            }
        }

        public override void OnHarvestFinished(
            Mobile from, Item tool, HarvestDefinition def, HarvestVein vein,
            HarvestBank bank, HarvestResource resource, object harvested
        )
        {
            base.OnHarvestFinished(from, tool, def, vein, bank, resource, harvested);

            if (Core.T2A)
            {
                from.RevealingAction();
            }

            // From T2A, 1% of casts in deep water at 80 skill or more bring up an empty sea serpent, whatever the catch.
            if (Core.T2A && from.Skills.Fishing.Base >= 80.0 && Utility.RandomDouble() < 0.01 &&
                GetHarvestDetails(from, tool, harvested, out _, out var map, out var loc, out _) &&
                SpecialFishingNet.FullValidation(map, loc.X, loc.Y))
            {
                SurfaceSerpent(from);
            }
        }

        public override object GetLock(Mobile from, Item tool, HarvestDefinition def, object toHarvest) => this;

        public override bool BeginHarvesting(Mobile from, Item tool)
        {
            if (!base.BeginHarvesting(from, tool))
            {
                return false;
            }

            from.SendLocalizedMessage(500974); // What water do you want to fish in?
            return true;
        }

        public override bool CheckHarvest(Mobile from, Item tool)
        {
            if (!base.CheckHarvest(from, tool))
            {
                return false;
            }

            if (from.Mounted)
            {
                from.SendLocalizedMessage(500971); // You can't fish while riding!
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

            if (from.Mounted)
            {
                from.SendLocalizedMessage(500971); // You can't fish while riding!
                return false;
            }

            return true;
        }

        private class MutateEntry
        {
            public readonly bool m_DeepWater;
            public readonly Expansion m_Expansion;
            public readonly Expansion? m_MaxExpansion;
            public readonly double m_MaxSkill;
            public readonly double m_MinSkill;
            public readonly double m_ReqSkill;
            public readonly Type[] m_Types;

            public MutateEntry(
                double reqSkill, double minSkill, double maxSkill, bool deepWater, Expansion expansion, params Type[] types
            ) : this(reqSkill, minSkill, maxSkill, deepWater, expansion, null, types)
            {
            }

            public MutateEntry(
                double reqSkill, double minSkill, double maxSkill, bool deepWater, Expansion expansion,
                Expansion? maxExpansion, params Type[] types
            )
            {
                m_ReqSkill = reqSkill;
                m_MinSkill = minSkill;
                m_MaxSkill = maxSkill;
                m_DeepWater = deepWater;
                m_Expansion = expansion;
                m_MaxExpansion = maxExpansion;
                m_Types = types;
            }
        }
    }
}
