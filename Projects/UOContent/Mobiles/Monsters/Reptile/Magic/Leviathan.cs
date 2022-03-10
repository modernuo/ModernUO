using System;
using Server.Items;

namespace Server.Mobiles
{
    public class Leviathan : BaseCreature
    {
        [Constructible]
        public Leviathan(Mobile fisher = null) : base(AIType.AI_Mage)
        {
            Fisher = fisher;

            // May not be OSI accurate; mostly copied from krakens
            Body = 77;
            BaseSoundID = 353;

            Hue = 0x481;

            SetStr(1000);
            SetDex(501, 520);
            SetInt(501, 515);

            SetHits(1500);

            SetDamage(25, 33);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Cold, 30);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 45, 55);
            SetResistance(ResistanceType.Cold, 45, 55);
            SetResistance(ResistanceType.Poison, 35, 45);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.EvalInt, 97.6, 107.5);
            SetSkill(SkillName.Magery, 97.6, 107.5);
            SetSkill(SkillName.MagicResist, 97.6, 107.5);
            SetSkill(SkillName.Meditation, 97.6, 107.5);
            SetSkill(SkillName.Tactics, 97.6, 107.5);
            SetSkill(SkillName.Wrestling, 97.6, 107.5);

            Fame = 24000;
            Karma = -24000;

            VirtualArmor = 50;

            CanSwim = true;
            CantWalk = true;

            PackItem(new MessageInABottle());

            var rope = new Rope();
            rope.ItemID = 0x14F8;
            PackItem(rope);

            rope = new Rope();
            rope.ItemID = 0x14FA;
            PackItem(rope);
        }

        public Leviathan(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a leviathan corpse";

        public Mobile Fisher { get; set; }

        public override string DefaultName => "a leviathan";

        public override bool HasBreath => true;
        public override int BreathPhysicalDamage => 70; // TODO: Verify damage type
        public override int BreathColdDamage => 30;
        public override int BreathFireDamage => 0;
        public override int BreathEffectHue => 0x1ED;
        public override double BreathDamageScalar => 0.05;
        public override double BreathMinDelay => 5.0;
        public override double BreathMaxDelay => 7.5;

        public override int TreasureMapLevel => 5;

        public static Type[] Artifacts { get; } =
        {
            // Decorations
            typeof(CandelabraOfSouls),
            typeof(GhostShipAnchor),
            typeof(GoldBricks),
            typeof(PhillipsWoodenSteed),
            typeof(SeahorseStatuette),
            typeof(ShipModelOfTheHMSCape),
            typeof(AdmiralsHeartyRum),

            // Equipment
            typeof(AlchemistsBauble),
            typeof(ArcticDeathDealer),
            typeof(BlazeOfDeath),
            typeof(BurglarsBandana),
            typeof(CaptainQuacklebushsCutlass),
            typeof(CavortingClub),
            typeof(DreadPirateHat),
            typeof(EnchantedTitanLegBone),
            typeof(GwennosHarp),
            typeof(IolosLute),
            typeof(LunaLance),
            typeof(NightsKiss),
            typeof(NoxRangersHeavyCrossbow),
            typeof(PolarBearMask),
            typeof(VioletCourage)
        };

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 5);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public static void GiveArtifactTo(Mobile m)
        {
            var item = Loot.Construct(Artifacts);

            if (item == null)
            {
                return;
            }

            // TODO: Confirm messages
            if (m.AddToBackpack(item))
            {
                m.SendMessage("As a reward for slaying the mighty leviathan, an artifact has been placed in your backpack.");
            }
            else
            {
                m.SendMessage(
                    "As your backpack is full, your reward for destroying the legendary leviathan has been placed at your feet."
                );
            }
        }

        public override void OnKilledBy(Mobile mob)
        {
            base.OnKilledBy(mob);

            if (Paragon.CheckArtifactChance(mob, this))
            {
                GiveArtifactTo(mob);

                if (mob == Fisher)
                {
                    Fisher = null;
                }
            }
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (Fisher != null && Utility.Random(100) < 25)
            {
                GiveArtifactTo(Fisher);
            }

            Fisher = null;
        }
    }
}
