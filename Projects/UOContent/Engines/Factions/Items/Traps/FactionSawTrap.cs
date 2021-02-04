using System;

namespace Server.Factions
{
    public class FactionSawTrap : BaseFactionTrap
    {
        [Constructible]
        public FactionSawTrap(Faction f = null, Mobile m = null) : base(f, m, 0x11AC)
        {
        }

        public FactionSawTrap(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041047; // faction saw trap

        public override int AttackMessage => 1010544; // The blade cuts deep into your skin!
        public override int DisarmMessage => 1010540; // You carefully dismantle the saw mechanism and disable the trap.
        public override int EffectSound => 0x218;
        public override int MessageHue => 0x5A;

        public override AllowedPlacing AllowedPlacing => AllowedPlacing.ControlledFactionTown;

        public override void DoVisibleEffect()
        {
            Effects.SendLocationEffect(Location, Map, 0x11AD, 25);
        }

        public override void DoAttackEffect(Mobile m)
        {
            m.Damage(Utility.Dice(6, 10, 40), m);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class FactionSawTrapDeed : BaseFactionTrapDeed
    {
        public FactionSawTrapDeed() : base(0x1107)
        {
        }

        public FactionSawTrapDeed(Serial serial) : base(serial)
        {
        }

        public override Type TrapType => typeof(FactionSawTrap);
        public override int LabelNumber => 1044604; // faction saw trap deed

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
