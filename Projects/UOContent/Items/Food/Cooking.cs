using System;
using Server.Targeting;

namespace Server.Items
{
    // ********** Dough **********
    public class Dough : Item
    {
        [Constructible]
        public Dough() : base(0x103d)
        {
            Stackable = Core.ML;
            Weight = 1.0;
        }

        public Dough(Serial serial) : base(serial)
        {
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

        private class InternalTarget : Target
        {
            private readonly Dough m_Item;

            public InternalTarget(Dough item) : base(1, false, TargetFlags.None) => m_Item = item;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted)
                {
                    return;
                }

                if (!(targeted is Item targetItem) || targetItem.Deleted)
                {
                    return;
                }

                m_Item.Consume();

                if (targeted is Eggs)
                {
                    from.AddToBackpack(new UnbakedQuiche());
                    from.AddToBackpack(new Eggshells());
                }
                else if (targeted is CheeseWheel)
                {
                    from.AddToBackpack(new CheesePizza());
                }
                else if (targeted is Sausage)
                {
                    from.AddToBackpack(new SausagePizza());
                }
                else if (targeted is Apple)
                {
                    from.AddToBackpack(new UnbakedApplePie());
                }
                else if (targeted is Peach)
                {
                    from.AddToBackpack(new UnbakedPeachCobbler());
                }
                else
                {
                    return;
                }

                targetItem.Consume();
            }
        }
    }

    // ********** SweetDough **********
    public class SweetDough : Item
    {
        [Constructible]
        public SweetDough() : base(0x103d)
        {
            Stackable = Core.ML;
            Weight = 1.0;
            Hue = 150;
        }

        public SweetDough(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041340; // sweet dough

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Hue == 51)
            {
                Hue = 150;
            }
        }

        private class InternalTarget : Target
        {
            private readonly SweetDough m_Item;

            public InternalTarget(SweetDough item) : base(1, false, TargetFlags.None) => m_Item = item;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted)
                {
                    return;
                }

                m_Item.Consume();

                if (targeted is BowlFlour flour)
                {
                    flour.Delete();

                    from.AddToBackpack(new CakeMix());
                }
                else if (targeted is Campfire campfire)
                {
                    from.PlaySound(0x225);
                    var t = new InternalTimer(from, campfire);
                    t.Start();
                }
            }

            private class InternalTimer : Timer
            {
                private readonly Campfire m_Campfire;
                private readonly Mobile m_From;

                public InternalTimer(Mobile from, Campfire campfire) : base(TimeSpan.FromSeconds(5.0))
                {
                    m_From = from;
                    m_Campfire = campfire;
                }

                protected override void OnTick()
                {
                    if (m_From.GetDistanceToSqrt(m_Campfire) > 3)
                    {
                        m_From.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
                        return;
                    }

                    if (m_From.CheckSkill(SkillName.Cooking, 0, 10))
                    {
                        if (m_From.AddToBackpack(new Muffins()))
                        {
                            m_From.PlaySound(0x57);
                        }
                    }
                    else
                    {
                        m_From.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
                    }
                }
            }
        }
    }

    // ********** JarHoney **********
    public class JarHoney : Item
    {
        [Constructible]
        public JarHoney() : base(0x9ec)
        {
            Weight = 1.0;
            Stackable = true;
        }

        public JarHoney(Serial serial) : base(serial)
        {
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
            Stackable = true;
        }

        /*public override void OnDoubleClick( Mobile from )
        {
          if (!Movable)
            return;

          from.Target = new InternalTarget( this );
        }*/

        private class InternalTarget : Target
        {
            private readonly JarHoney m_Item;

            public InternalTarget(JarHoney item) : base(1, false, TargetFlags.None) => m_Item = item;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted)
                {
                    return;
                }

                m_Item.Consume();

                if (targeted is Dough dough)
                {
                    dough.Consume();

                    from.AddToBackpack(new SweetDough());
                }

                if (targeted is BowlFlour flour)
                {
                    flour.Delete();

                    from.AddToBackpack(new CookieMix());
                }
            }
        }
    }

    // ********** BowlFlour **********
    public class BowlFlour : Item
    {
        [Constructible]
        public BowlFlour() : base(0xa1e) => Weight = 1.0;

        public BowlFlour(Serial serial) : base(serial)
        {
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

    // ********** WoodenBowl **********
    public class WoodenBowl : Item
    {
        [Constructible]
        public WoodenBowl() : base(0x15f8) => Weight = 1.0;

        public WoodenBowl(Serial serial) : base(serial)
        {
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

    // ********** PitcherWater **********
    /*public class PitcherWater : Item
    {
      [Constructible]
      public PitcherWater() : base(Utility.Random( 0x1f9d, 2 ))
      {
        Weight = 1.0;
      }

      public PitcherWater( Serial serial ) : base( serial )
      {
      }

      public override void Serialize(IGenericWriter writer)
      {
        base.Serialize( writer );

        writer.Write( (int) 0 ); // version
      }

      public override void Deserialize(IGenericReader reader)
      {
        base.Deserialize( reader );

        int version = reader.ReadInt();
      }

      public override void OnDoubleClick( Mobile from )
      {
        if (!Movable)
          return;

        from.Target = new InternalTarget( this );
      }

      private class InternalTarget : Target
      {
        private PitcherWater m_Item;

        public InternalTarget( PitcherWater item ) : base( 1, false, TargetFlags.None )
        {
          m_Item = item;
        }

        protected override void OnTarget( Mobile from, object targeted )
        {
          if (m_Item.Deleted ) return;

          if (targeted is BowlFlour)
          {
            m_Item.Delete();
            ((BowlFlour)targeted).Delete();

            from.AddToBackpack( new Dough() );
            from.AddToBackpack( new WoodenBowl() );
          }
        }
      }
    }*/

    // ********** SackFlour **********
    [TypeAlias("Server.Items.SackFlourOpen")]
    public class SackFlour : Item, IHasQuantity
    {
        private int m_Quantity;

        [Constructible]
        public SackFlour() : base(0x1039)
        {
            Weight = 5.0;
            m_Quantity = 20;
        }

        public SackFlour(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Quantity
        {
            get => m_Quantity;
            set
            {
                m_Quantity = Math.Min(20, Math.Max(0, value));

                if (m_Quantity == 0)
                {
                    Delete();
                }
                else if (m_Quantity < 20 && (ItemID == 0x1039 || ItemID == 0x1045))
                {
                    ++ItemID;
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2); // version

            writer.Write(m_Quantity);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 2:
                case 1:
                    {
                        m_Quantity = reader.ReadInt();
                        break;
                    }
                case 0:
                    {
                        m_Quantity = 20;
                        break;
                    }
            }

            if (version < 2 && Weight == 1.0)
            {
                Weight = 5.0;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Movable)
            {
                return;
            }

            if (ItemID == 0x1039 || ItemID == 0x1045)
            {
                ++ItemID;
            }
        }
    }

    // ********** Eggshells **********
    public class Eggshells : Item
    {
        [Constructible]
        public Eggshells() : base(0x9b4) => Weight = 0.5;

        public Eggshells(Serial serial) : base(serial)
        {
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

    public class WheatSheaf : Item
    {
        [Constructible]
        public WheatSheaf(int amount = 1) : base(7869)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
        }

        public WheatSheaf(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Movable)
            {
                return;
            }

            from.BeginTarget(4, false, TargetFlags.None, OnTarget);
        }

        public virtual void OnTarget(Mobile from, object obj)
        {
            if (obj is AddonComponent addon)
            {
                obj = addon.Addon;
            }

            if (obj is IFlourMill mill)
            {
                var needs = mill.MaxFlour - mill.CurFlour;

                if (needs > Amount)
                {
                    needs = Amount;
                }

                mill.CurFlour += needs;
                Consume(needs);
            }
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
}
