using Server.Items;

namespace Server.Mobiles
{
    public class ExodusMinion : BaseCreature
    {
        [Constructible]
        public ExodusMinion() : base(AIType.AI_Melee)
        {
            Body = 0x2F5;

            SetStr(851, 950);
            SetDex(71, 80);
            SetInt(61, 90);

            SetHits(511, 570);

            SetDamage(16, 22);

            SetResistance(ResistanceType.Physical, 60, 70);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 15, 25);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.MagicResist, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 18000;
            Karma = -18000;
            VirtualArmor = 65;

            PackItem(new PowerCrystal());
            PackItem(new ArcaneGem());
            PackItem(new ClockworkAssembly());

            switch (Utility.Random(3))
            {
                case 0:
                    PackItem(new PowerCrystal());
                    break;
                case 1:
                    PackItem(new ArcaneGem());
                    break;
                case 2:
                    PackItem(new ClockworkAssembly());
                    break;
            }

            FieldActive = CanUseField;
        }

        public ExodusMinion(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a minion's corpse";
        public bool FieldActive { get; private set; }

        public bool CanUseField // TODO: an OSI bug prevents to verify this
            => Hits >= HitsMax * 9 / 10;

        public override bool IsScaredOfScaryThings => false;
        public override bool IsScaryToPets => true;

        public override string DefaultName => "an exodus minion";

        public override bool AutoDispel => true;
        public override bool BardImmune => !Core.AOS;
        public override Poison PoisonImmune => Poison.Lethal;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Rich);
        }

        public override int GetIdleSound() => 0x218;

        public override int GetAngerSound() => 0x26C;

        public override int GetDeathSound() => 0x211;

        public override int GetAttackSound() => 0x232;

        public override int GetHurtSound() => 0x140;

        public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
        {
            if (FieldActive)
            {
                damage = 0; // no melee damage when the field is up
            }
        }

        public override void AlterSpellDamageFrom(Mobile from, ref int damage)
        {
            if (!FieldActive)
            {
                damage = 0; // no spell damage when the field is down
            }
        }

        public override void OnDamagedBySpell(Mobile from)
        {
            if (from?.Alive == true && Utility.RandomDouble() < 0.4)
            {
                SendEBolt(from);
            }

            if (!FieldActive)
            {
                // should there be an effect when spells nullifying is on?
                FixedParticles(0, 10, 0, 0x2522, EffectLayer.Waist);
            }
            else if (FieldActive && !CanUseField)
            {
                FieldActive = false;

                // TODO: message and effect when field turns down; cannot be verified on OSI due to a bug
                FixedParticles(0x3735, 1, 30, 0x251F, EffectLayer.Waist);
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (FieldActive)
            {
                FixedParticles(0x376A, 20, 10, 0x2530, EffectLayer.Waist);

                PlaySound(0x2F4);

                attacker.SendAsciiMessage("Your weapon cannot penetrate the creature's magical barrier");
            }

            if (attacker?.Alive == true && attacker.Weapon is BaseRanged && Utility.RandomDouble() < 0.4)
            {
                SendEBolt(attacker);
            }
        }

        public override void OnThink()
        {
            base.OnThink();

            // TODO: an OSI bug prevents to verify if the field can regenerate or not
            if (!FieldActive && !IsHurt())
            {
                FieldActive = true;
            }
        }

        public override bool Move(Direction d)
        {
            var move = base.Move(d);

            if (move && FieldActive && Combatant != null)
            {
                FixedParticles(0, 10, 0, 0x2530, EffectLayer.Waist);
            }

            return move;
        }

        public void SendEBolt(Mobile to)
        {
            MovingParticles(to, 0x379F, 7, 0, false, true, 0xBE3, 0xFCB, 0x211);
            to.PlaySound(0x229);
            DoHarmful(to);
            AOS.Damage(to, this, 50, 0, 0, 0, 0, 100);
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

            FieldActive = CanUseField;

            if (Name == "Exodus Minion")
            {
                Name = null;
            }
        }
    }
}
