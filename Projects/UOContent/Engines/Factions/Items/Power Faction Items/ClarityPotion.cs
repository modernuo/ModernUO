using System;

namespace Server
{
    public sealed class ClarityPotion : PowerFactionItem
    {
        public ClarityPotion()
            : base(3628) =>
            Hue = 1154;

        public ClarityPotion(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "clarity potion";

        public override bool Use(Mobile from)
        {
            if (from.BeginAction<ClarityPotion>())
            {
                var amount = Utility.Dice(3, 3, 3);
                var time = Utility.RandomMinMax(5, 30);

                from.PlaySound(0x2D6);

                if (from.Body.IsHuman)
                {
                    from.Animate(34, 5, 1, true, false, 0);
                }

                from.FixedParticles(0x375A, 10, 15, 5011, EffectLayer.Head);
                from.PlaySound(0x1EB);

                var mod = from.GetStatMod("Concussion");

                if (mod != null)
                {
                    from.RemoveStatMod("Concussion");
                    from.Mana -= mod.Offset;
                }

                from.PlaySound(0x1EE);
                from.AddStatMod(new StatMod(StatType.Int, "clarity-potion", amount, TimeSpan.FromMinutes(time)));

                Timer.DelayCall(TimeSpan.FromMinutes(time), from.EndAction<ClarityPotion>);

                return true;
            }

            return false;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
