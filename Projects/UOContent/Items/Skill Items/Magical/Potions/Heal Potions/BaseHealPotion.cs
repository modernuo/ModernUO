using System;
using Server.Engines.ConPVP;
using Server.Network;

namespace Server.Items
{
    public abstract class BaseHealPotion : BasePotion
    {
        public BaseHealPotion(PotionEffect effect) : base(0xF0C, effect)
        {
        }

        public BaseHealPotion(Serial serial) : base(serial)
        {
        }

        public abstract int MinHeal { get; }
        public abstract int MaxHeal { get; }
        public abstract double Delay { get; }

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

        public void DoHeal(Mobile from)
        {
            var min = Scale(from, MinHeal);
            var max = Scale(from, MaxHeal);

            from.Heal(Utility.RandomMinMax(min, max));
        }

        public override void Drink(Mobile from)
        {
            if (from.Hits < from.HitsMax)
            {
                if (from.Poisoned || MortalStrike.IsWounded(from))
                {
                    // You can not heal yourself in your current state.
                    from.LocalOverheadMessage(MessageType.Regular, 0x22, 1005000);
                }
                else
                {
                    if (from.BeginAction<BaseHealPotion>())
                    {
                        DoHeal(from);

                        PlayDrinkEffect(from);

                        if (!DuelContext.IsFreeConsume(from))
                        {
                            Consume();
                        }

                        Timer.StartTimer(TimeSpan.FromSeconds(Delay), from.EndAction<BaseHealPotion>);
                    }
                    else
                    {
                        // You must wait 10 seconds before using another healing potion.
                        from.LocalOverheadMessage(MessageType.Regular, 0x22, 500235);
                    }
                }
            }
            else
            {
                // You decide against drinking this potion, as you are already at full health.
                from.SendLocalizedMessage(1049547);
            }
        }
    }
}
