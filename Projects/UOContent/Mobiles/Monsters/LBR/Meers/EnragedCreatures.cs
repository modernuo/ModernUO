using Server.Network;

namespace Server.Mobiles
{
    public class EnragedRabbit : BaseEnraged
    {
        public EnragedRabbit(Mobile summoner) : base(summoner) => Body = 0xcd;

        public EnragedRabbit(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a hare corpse";
        public override string DefaultName => "a rabbit";

        public override int GetAttackSound() => 0xC9;

        public override int GetHurtSound() => 0xCA;

        public override int GetDeathSound() => 0xCB;

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

    public class EnragedHart : BaseEnraged
    {
        public EnragedHart(Mobile summoner) : base(summoner) => Body = 0xea;

        public EnragedHart(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a deer corpse";
        public override string DefaultName => "a great hart";

        public override int GetAttackSound() => 0x82;

        public override int GetHurtSound() => 0x83;

        public override int GetDeathSound() => 0x84;

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

    public class EnragedHind : BaseEnraged
    {
        public EnragedHind(Mobile summoner) : base(summoner) => Body = 0xed;

        public EnragedHind(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a deer corpse";
        public override string DefaultName => "a hind";

        public override int GetAttackSound() => 0x82;

        public override int GetHurtSound() => 0x83;

        public override int GetDeathSound() => 0x84;

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

    public class EnragedBlackBear : BaseEnraged
    {
        public EnragedBlackBear(Mobile summoner) : base(summoner)
        {
            Body = 0xd3;
            BaseSoundID = 0xa3;
        }

        public EnragedBlackBear(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a bear corpse";
        public override string DefaultName => "a black bear";

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

    public class EnragedEagle : BaseEnraged
    {
        public EnragedEagle(Mobile summoner) : base(summoner)
        {
            Body = 0x5;
            BaseSoundID = 0x2ee;
        }

        public EnragedEagle(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an eagle corpse";
        public override string DefaultName => "an eagle";

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

    public class BaseEnraged : BaseCreature
    {
        public BaseEnraged(Mobile summoner) : base(AIType.AI_Melee)
        {
            SetStr(50, 200);
            SetDex(50, 200);

            /*
            * On OSI, all stats are random 50-200, but
            * str is never less than hits, and dex is never
            * less than stam.
            */
            SetHits(50, Str);
            SetStam(50, Dex);

            Karma = -1000;
            Tamable = false;

            SummonMaster = summoner;
        }

        public BaseEnraged(Serial serial) : base(serial)
        {
        }

        public override void OnThink()
        {
            if (SummonMaster?.Deleted != false)
            {
                Delete();
            }
            /*
              On OSI, without combatant, they behave as if they have been
              given "come" command, ie they wander towards their summoner,
              but never actually "follow".
            */
            else if (!Combat(this))
            {
                AIObject?.MoveTo(SummonMaster, false, 5);
            }
            /*
              On OSI, if the summon attacks a mobile, the summoner meer also
              attacks them, regardless of karma, etc. as long as the combatant
              is a player or controlled/summoned, and the summoner is not already
              engaged in combat.
            */
            else if (!Combat(SummonMaster))
            {
                if (Combatant.Player || Combatant is BaseCreature bc && (bc.Controlled || bc.SummonMaster != null))
                {
                    SummonMaster.Combatant = Combatant;
                }
            }
            else
            {
                base.OnThink();
            }
        }

        private bool Combat(Mobile mobile)
        {
            var combatant = mobile.Combatant;
            return combatant?.Deleted == false && !combatant.IsDeadBondedPet && combatant.Alive;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1060768, from.NetState); // enraged
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);
            list.Add(1060768); // enraged
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
