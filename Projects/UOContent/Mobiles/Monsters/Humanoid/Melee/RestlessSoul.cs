using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.Quests.Haven;

namespace Server.Mobiles
{
    public class RestlessSoul : BaseCreature
    {
        [Constructible]
        public RestlessSoul() : base(AIType.AI_Melee)
        {
            Body = 0x3CA;
            Hue = 0x453;

            SetStr(26, 40);
            SetDex(26, 40);
            SetInt(26, 40);

            SetHits(16, 24);

            SetDamage(1, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 25);
            SetResistance(ResistanceType.Fire, 5, 15);
            SetResistance(ResistanceType.Cold, 25, 40);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.MagicResist, 20.1, 30.0);
            SetSkill(SkillName.Swords, 20.1, 30.0);
            SetSkill(SkillName.Tactics, 20.1, 30.0);
            SetSkill(SkillName.Wrestling, 20.1, 30.0);

            Fame = 500;
            Karma = -500;

            VirtualArmor = 6;
        }

        public RestlessSoul(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a ghostly corpse";
        public override string DefaultName => "a restless soul";

        public override bool AlwaysAttackable => true;
        public override bool BleedImmune => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Poor);
        }

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

        public override int GetIdleSound() => 0x107;

        public override int GetAngerSound() => 0x1BF;

        public override int GetDeathSound() => 0xFD;

        public override bool IsEnemy(Mobile m)
        {
            // Schmendrick's cave
            if (m is PlayerMobile player && Map == Map.Trammel && X is >= 5199 and <= 5271 && Y is >= 1812 and <= 1865)
            {
                var qs = player.Quest;

                if (qs is UzeraanTurmoilQuest && qs.IsObjectiveInProgress(typeof(FindSchmendrickObjective)))
                {
                    return false;
                }
            }

            return base.IsEnemy(m);
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
    }
}
