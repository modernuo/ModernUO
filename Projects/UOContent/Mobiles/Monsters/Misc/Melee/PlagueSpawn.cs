using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.ContextMenus;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class PlagueSpawn : BaseCreature
    {
        [Constructible]
        public PlagueSpawn(Mobile owner = null) : base(AIType.AI_Melee)
        {
            Owner = owner;
            ExpireTime = Core.Now + TimeSpan.FromMinutes(1.0);

            Hue = Utility.Random(0x11, 15);

            switch (Utility.Random(12))
            {
                case 0: // earth elemental
                    Body = 14;
                    BaseSoundID = 268;
                    break;
                case 1: // headless one
                    Body = 31;
                    BaseSoundID = 0x39D;
                    break;
                case 2: // person
                    Body = Utility.RandomList(400, 401);
                    break;
                case 3: // gorilla
                    Body = 0x1D;
                    BaseSoundID = 0x9E;
                    break;
                case 4: // serpent
                    Body = 0x15;
                    BaseSoundID = 0xDB;
                    break;
                default: // slime
                    Body = 51;
                    BaseSoundID = 456;
                    break;
            }

            SetStr(201, 300);
            SetDex(80);
            SetInt(16, 20);

            SetHits(121, 180);

            SetDamage(11, 17);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 65, 75);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.MagicResist, 25.0);
            SetSkill(SkillName.Tactics, 25.0);
            SetSkill(SkillName.Wrestling, 50.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 20;
        }

        public override string CorpseName => "a plague spawn corpse";

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ExpireTime { get; set; }

        public override bool AlwaysMurderer => true;

        public override string DefaultName => "a plague spawn";

        public override void DisplayPaperdollTo(Mobile to)
        {
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            for (var i = 0; i < list.Count; ++i)
            {
                if (list[i] is PaperdollEntry)
                {
                    list.RemoveAt(i--);
                }
            }
        }

        public override void OnThink()
        {
            if (Owner != null && (Core.Now >= ExpireTime || Owner.Deleted || Map != Owner.Map || !InRange(Owner, 16)))
            {
                PlaySound(GetIdleSound());
                Delete();
            }
            else
            {
                base.OnThink();
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Poor);
            AddLoot(LootPack.Gems);
        }
    }
}
