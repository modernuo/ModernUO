using System;
using Server.Engines.ConPVP;
using Server.Spells;

namespace Server.Items
{
    public abstract class BaseAgilityPotion : BasePotion
    {
        public BaseAgilityPotion(PotionEffect effect) : base(0xF08, effect)
        {
        }

        public BaseAgilityPotion(Serial serial) : base(serial)
        {
        }

        public abstract int DexOffset { get; }
        public abstract TimeSpan Duration { get; }

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

        public bool DoAgility(Mobile from)
        {
            // TODO: Verify scaled; is it offset, duration, or both?
            if (SpellHelper.AddStatOffset(from, StatType.Dex, Scale(from, DexOffset), Duration))
            {
                from.FixedEffect(0x375A, 10, 15);
                from.PlaySound(0x1E7);
                return true;
            }

            from.SendLocalizedMessage(502173); // You are already under a similar effect.
            return false;
        }

        public override void Drink(Mobile from)
        {
            if (DoAgility(from))
            {
                PlayDrinkEffect(from);

                if (!DuelContext.IsFreeConsume(from))
                {
                    Consume();
                }
            }
        }
    }
}
