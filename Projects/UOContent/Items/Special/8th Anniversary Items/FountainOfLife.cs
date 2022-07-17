using System;

namespace Server.Items
{
    public class EnhancedBandage : Bandage
    {
        [Constructible]
        public EnhancedBandage(int amount = 1)
            : base(amount) =>
            Hue = 0x8A5;

        public EnhancedBandage(Serial serial)
            : base(serial)
        {
        }

        public static int HealingBonus => 10;

        public override int LabelNumber => 1152441; // enhanced bandage

        public override bool Dye(Mobile from, DyeTub sender) => false;

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1075216); // these bandages have been enhanced
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

    [Flippable(0x2AC0, 0x2AC3)]
    public class FountainOfLife : BaseAddonContainer
    {
        private int m_Charges;

        private TimerExecutionToken _timerToken;

        [Constructible]
        public FountainOfLife(int charges = 10)
            : base(0x2AC0)
        {
            m_Charges = charges;

            Timer.StartTimer(RechargeTime, RechargeTime, Recharge, out _timerToken);
        }

        public FountainOfLife(Serial serial)
            : base(serial)
        {
        }

        public override BaseAddonContainerDeed Deed => new FountainOfLifeDeed(m_Charges);

        public virtual TimeSpan RechargeTime => TimeSpan.FromDays(1);

        public override int LabelNumber => 1075197; // Fountain of Life
        public override int DefaultGumpID => 0x484;
        public override int DefaultDropSound => 66;
        public override int DefaultMaxItems => 125;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get => m_Charges;
            set
            {
                m_Charges = Math.Min(value, 10);
                InvalidateProperties();
            }
        }

        public override bool OnDragLift(Mobile from) => false;

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is Bandage)
            {
                var allow = base.OnDragDrop(from, dropped);

                if (allow)
                {
                    Enhance(from);
                }

                return allow;
            }

            from.SendLocalizedMessage(1075209); // Only bandages may be dropped into the fountain.
            return false;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (item is Bandage)
            {
                var allow = base.OnDragDropInto(from, item, p);

                if (allow)
                {
                    Enhance(from);
                }

                return allow;
            }

            from.SendLocalizedMessage(1075209); // Only bandages may be dropped into the fountain.
            return false;
        }

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1075217, m_Charges); // ~1_val~ charges remaining
        }

        public override void OnDelete()
        {
            _timerToken.Cancel();

            base.OnDelete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Charges);
            writer.Write(_timerToken.Next);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Charges = reader.ReadInt();

            var next = reader.ReadDateTime();

            var now = Core.Now;

            if (next < now)
            {
                Timer.StartTimer(RechargeTime, Recharge, out _timerToken);
            }
            else
            {
                Timer.StartTimer(next - now, RechargeTime, Recharge, out _timerToken);
            }
        }

        public void Recharge()
        {
            m_Charges = 10;

            Enhance(null);
        }

        public void Enhance(Mobile from)
        {
            for (var i = Items.Count - 1; i >= 0 && m_Charges > 0; --i)
            {
                if (Items[i] is EnhancedBandage)
                {
                    continue;
                }

                if (Items[i] is Bandage bandage)
                {
                    Item enhanced;

                    if (bandage.Amount > m_Charges)
                    {
                        bandage.Amount -= m_Charges;
                        enhanced = new EnhancedBandage(m_Charges);
                        m_Charges = 0;
                    }
                    else
                    {
                        enhanced = new EnhancedBandage(bandage.Amount);
                        m_Charges -= bandage.Amount;
                        bandage.Delete();
                    }

                    if (from == null || !TryDropItem(from, enhanced, false)) // try stacking first
                    {
                        DropItem(enhanced);
                    }
                }
            }

            InvalidateProperties();
        }
    }

    public class FountainOfLifeDeed : BaseAddonContainerDeed
    {
        private int m_Charges;

        [Constructible]
        public FountainOfLifeDeed(int charges = 10)
        {
            LootType = LootType.Blessed;
            m_Charges = charges;
        }

        public FountainOfLifeDeed(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1075197; // Fountain of Life
        public override BaseAddonContainer Addon => new FountainOfLife(m_Charges);

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get => m_Charges;
            set
            {
                m_Charges = Math.Min(value, 10);
                InvalidateProperties();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Charges);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Charges = reader.ReadInt();
        }
    }
}
