using System;

namespace Server.Factions
{
    public class FactionSpikeTrap : BaseFactionTrap
    {
        [Constructible]
        public FactionSpikeTrap(Faction f = null, Mobile m = null) : base(f, m, 0x11A0)
        {
        }

        public FactionSpikeTrap(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1044601; // faction spike trap

        public override int AttackMessage => 1010545; // Large spikes in the ground spring up piercing your skin!

        public override int DisarmMessage =>
            1010541; // You carefully dismantle the trigger on the spikes and disable the trap.

        public override int EffectSound => 0x22E;
        public override int MessageHue => 0x5A;

        public override AllowedPlacing AllowedPlacing => AllowedPlacing.ControlledFactionTown;

        public override void DoVisibleEffect()
        {
            Effects.SendLocationEffect(Location, Map, 0x11A4, 12, 6);
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

    public class FactionSpikeTrapDeed : BaseFactionTrapDeed
    {
        public FactionSpikeTrapDeed() : base(0x11A5)
        {
        }

        public FactionSpikeTrapDeed(Serial serial) : base(serial)
        {
        }

        public override Type TrapType => typeof(FactionSpikeTrap);
        public override int LabelNumber => 1044605; // faction spike trap deed

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
