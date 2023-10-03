using System;
using Server.Engines.ConPVP;
using Server.Targeting;

namespace Server.Items
{
    public class PileOfGlacialSnow : Item
    {
        private static Type[] _snowPileTypes = { typeof(SnowPile), typeof(PileOfGlacialSnow) };

        [Constructible]
        public PileOfGlacialSnow() : base(0x913)
        {
            Hue = 0x480;
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public PileOfGlacialSnow(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1070874; // a Pile of Glacial Snow

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version == 0)
            {
                Weight = 1.0;
                LootType = LootType.Blessed;
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, 1070880); // Winter 2004
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1070880); // Winter 2004
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042010); // You must have the object in your backpack to use it.
            }
            else if (from.Mounted)
            {
                from.SendLocalizedMessage(1010097); // You cannot use this while mounted.
            }
            else if (from.CanBeginAction<SnowPile>())
            {
                from.SendLocalizedMessage(1005575); // You carefully pack the snow into a ball...
                from.Target = new SnowTarget(from, this);
            }
            else
            {
                from.SendLocalizedMessage(1005574); // The snow is not ready to be packed yet.  Keep trying.
            }
        }

        private class InternalTimer : Timer
        {
            private readonly Mobile m_From;

            public InternalTimer(Mobile from) : base(TimeSpan.FromSeconds(5.0)) => m_From = from;

            protected override void OnTick()
            {
                m_From.EndAction<SnowPile>();
            }
        }

        private class SnowTarget : Target
        {
            private Item m_Snow;
            private Mobile m_Thrower;

            public SnowTarget(Mobile thrower, Item snow) : base(10, false, TargetFlags.None)
            {
                m_Thrower = thrower;
                m_Snow = snow;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target == from)
                {
                    from.SendLocalizedMessage(1005576); // You can't throw this at yourself.
                }
                else if (target is Mobile targ)
                {
                    var pack = targ.Backpack;

                    if (from.Region.IsPartOf<SafeZone>() || targ.Region.IsPartOf<SafeZone>())
                    {
                        from.SendMessage("You may not throw snow here.");
                    }
                    else if (pack?.FindItemByType(_snowPileTypes) != null)
                    {
                        if (from.BeginAction<SnowPile>())
                        {
                            new InternalTimer(from).Start();

                            from.PlaySound(0x145);

                            from.Animate(9, 1, 1, true, false, 0);

                            targ.SendLocalizedMessage(1010572); // You have just been hit by a snowball!
                            from.SendLocalizedMessage(1010573); // You throw the snowball and hit the target!

                            Effects.SendMovingEffect(from, targ, 0x36E4, 7, 0, false, true, 0x47F);
                        }
                        else
                        {
                            from.SendLocalizedMessage(1005574); // The snow is not ready to be packed yet.  Keep trying.
                        }
                    }
                    else
                    {
                        // You can only throw a snowball at something that can throw one back.
                        from.SendLocalizedMessage(1005577);
                    }
                }
                else
                {
                    // You can only throw a snowball at something that can throw one back.
                    from.SendLocalizedMessage(1005577);
                }
            }
        }
    }
}
