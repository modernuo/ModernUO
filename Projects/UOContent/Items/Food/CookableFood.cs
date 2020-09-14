using System;
using Server.Targeting;

namespace Server.Items
{
    public abstract class CookableFood : Item
    {
        public CookableFood(int itemID, int cookingLevel) : base(itemID) => CookingLevel = cookingLevel;

        public CookableFood(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CookingLevel { get; set; }

        public abstract Food Cook();

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
            // Version 1
            writer.Write(CookingLevel);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        CookingLevel = reader.ReadInt();

                        break;
                    }
            }
        }

        public static bool IsHeatSource(object targeted)
        {
            int itemID;

            if (targeted is Item item)
            {
                itemID = item.ItemID;
            }
            else if (targeted is StaticTarget target)
            {
                itemID = target.ItemID;
            }
            else
            {
                return false;
            }

            if (itemID >= 0xDE3 && itemID <= 0xDE9)
            {
                return true; // Campfire
            }

            if (itemID >= 0x461 && itemID <= 0x48E)
            {
                return true; // Sandstone oven/fireplace
            }

            if (itemID >= 0x92B && itemID <= 0x96C)
            {
                return true; // Stone oven/fireplace
            }

            if (itemID == 0xFAC)
            {
                return true; // Firepit
            }

            if (itemID >= 0x184A && itemID <= 0x184C)
            {
                return true; // Heating stand (left)
            }

            if (itemID >= 0x184E && itemID <= 0x1850)
            {
                return true; // Heating stand (right)
            }

            if (itemID >= 0x398C && itemID <= 0x399F)
            {
                return true; // Fire field
            }

            return false;
        }

        private class InternalTarget : Target
        {
            private readonly CookableFood m_Item;

            public InternalTarget(CookableFood item) : base(1, false, TargetFlags.None) => m_Item = item;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted)
                {
                    return;
                }

                if (IsHeatSource(targeted))
                {
                    if (from.BeginAction<CookableFood>())
                    {
                        from.PlaySound(0x225);

                        m_Item.Consume();

                        var t = new InternalTimer(from, targeted as IPoint3D, from.Map, m_Item);
                        t.Start();
                    }
                    else
                    {
                        from.SendLocalizedMessage(500119); // You must wait to perform another action
                    }
                }
            }

            private class InternalTimer : Timer
            {
                private readonly CookableFood m_CookableFood;
                private readonly Mobile m_From;
                private readonly Map m_Map;
                private readonly IPoint3D m_Point;

                public InternalTimer(Mobile from, IPoint3D p, Map map, CookableFood cookableFood) : base(
                    TimeSpan.FromSeconds(5.0)
                )
                {
                    m_From = from;
                    m_Point = p;
                    m_Map = map;
                    m_CookableFood = cookableFood;
                }

                protected override void OnTick()
                {
                    m_From.EndAction<CookableFood>();

                    if (m_From.Map != m_Map || m_Point != null && m_From.GetDistanceToSqrt(m_Point) > 3)
                    {
                        m_From.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
                        return;
                    }

                    if (m_From.CheckSkill(SkillName.Cooking, m_CookableFood.CookingLevel, 100))
                    {
                        var cookedFood = m_CookableFood.Cook();

                        if (m_From.AddToBackpack(cookedFood))
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

    // ********** RawRibs **********
    public class RawRibs : CookableFood
    {
        [Constructible]
        public RawRibs(int amount = 1) : base(0x9F1, 10)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
        }

        public RawRibs(Serial serial) : base(serial)
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

        public override Food Cook() => new Ribs();
    }

    // ********** RawLambLeg **********
    public class RawLambLeg : CookableFood
    {
        [Constructible]
        public RawLambLeg(int amount = 1) : base(0x1609, 10)
        {
            Stackable = true;
            Amount = amount;
        }

        public RawLambLeg(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version == 0 && Weight == 1)
            {
                Weight = -1;
            }
        }

        public override Food Cook() => new LambLeg();
    }

    // ********** RawChickenLeg **********
    public class RawChickenLeg : CookableFood
    {
        [Constructible]
        public RawChickenLeg() : base(0x1607, 10)
        {
            Weight = 1.0;
            Stackable = true;
        }

        public RawChickenLeg(Serial serial) : base(serial)
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

        public override Food Cook() => new ChickenLeg();
    }

    // ********** RawBird **********
    public class RawBird : CookableFood
    {
        [Constructible]
        public RawBird(int amount = 1) : base(0x9B9, 10)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
        }

        public RawBird(Serial serial) : base(serial)
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

        public override Food Cook() => new CookedBird();
    }

    // ********** UnbakedPeachCobbler **********
    public class UnbakedPeachCobbler : CookableFood
    {
        [Constructible]
        public UnbakedPeachCobbler() : base(0x1042, 25) => Weight = 1.0;

        public UnbakedPeachCobbler(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041335; // unbaked peach cobbler

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

        public override Food Cook() => new PeachCobbler();
    }

    // ********** UnbakedFruitPie **********
    public class UnbakedFruitPie : CookableFood
    {
        [Constructible]
        public UnbakedFruitPie() : base(0x1042, 25) => Weight = 1.0;

        public UnbakedFruitPie(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041334; // unbaked fruit pie

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

        public override Food Cook() => new FruitPie();
    }

    // ********** UnbakedMeatPie **********
    public class UnbakedMeatPie : CookableFood
    {
        [Constructible]
        public UnbakedMeatPie() : base(0x1042, 25) => Weight = 1.0;

        public UnbakedMeatPie(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041338; // unbaked meat pie

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

        public override Food Cook() => new MeatPie();
    }

    // ********** UnbakedPumpkinPie **********
    public class UnbakedPumpkinPie : CookableFood
    {
        [Constructible]
        public UnbakedPumpkinPie() : base(0x1042, 25) => Weight = 1.0;

        public UnbakedPumpkinPie(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041342; // unbaked pumpkin pie

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

        public override Food Cook() => new PumpkinPie();
    }

    // ********** UnbakedApplePie **********
    public class UnbakedApplePie : CookableFood
    {
        [Constructible]
        public UnbakedApplePie() : base(0x1042, 25) => Weight = 1.0;

        public UnbakedApplePie(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041336; // unbaked apple pie

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

        public override Food Cook() => new ApplePie();
    }

    // ********** UncookedCheesePizza **********
    [TypeAlias("Server.Items.UncookedPizza")]
    public class UncookedCheesePizza : CookableFood
    {
        [Constructible]
        public UncookedCheesePizza() : base(0x1083, 20) => Weight = 1.0;

        public UncookedCheesePizza(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041341; // uncooked cheese pizza

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (ItemID == 0x1040)
            {
                ItemID = 0x1083;
            }

            if (Hue == 51)
            {
                Hue = 0;
            }
        }

        public override Food Cook() => new CheesePizza();
    }

    // ********** UncookedSausagePizza **********
    public class UncookedSausagePizza : CookableFood
    {
        [Constructible]
        public UncookedSausagePizza() : base(0x1083, 20) => Weight = 1.0;

        public UncookedSausagePizza(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041337; // uncooked sausage pizza

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

        public override Food Cook() => new SausagePizza();
    }

    // ********** UnbakedQuiche **********
    public class UnbakedQuiche : CookableFood
    {
        [Constructible]
        public UnbakedQuiche() : base(0x1042, 25) => Weight = 1.0;

        public UnbakedQuiche(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041339; // unbaked quiche

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

        public override Food Cook() => new Quiche();
    }

    // ********** Eggs **********
    public class Eggs : CookableFood
    {
        [Constructible]
        public Eggs(int amount = 1) : base(0x9B5, 15)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
        }

        public Eggs(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1)
            {
                Stackable = true;

                if (Weight == 0.5)
                {
                    Weight = 1.0;
                }
            }
        }

        public override Food Cook() => new FriedEggs();
    }

    // ********** BrightlyColoredEggs **********
    public class BrightlyColoredEggs : CookableFood
    {
        [Constructible]
        public BrightlyColoredEggs() : base(0x9B5, 15)
        {
            Weight = 0.5;
            Hue = 3 + Utility.Random(20) * 5;
        }

        public BrightlyColoredEggs(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "brightly colored eggs";

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

        public override Food Cook() => new FriedEggs();
    }

    // ********** EasterEggs **********
    public class EasterEggs : CookableFood
    {
        [Constructible]
        public EasterEggs() : base(0x9B5, 15)
        {
            Weight = 0.5;
            Hue = 3 + Utility.Random(20) * 5;
        }

        public EasterEggs(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1016105; // Easter Eggs

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

        public override Food Cook() => new FriedEggs();
    }

    // ********** CookieMix **********
    public class CookieMix : CookableFood
    {
        [Constructible]
        public CookieMix() : base(0x103F, 20) => Weight = 1.0;

        public CookieMix(Serial serial) : base(serial)
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

        public override Food Cook() => new Cookies();
    }

    // ********** CakeMix **********
    public class CakeMix : CookableFood
    {
        [Constructible]
        public CakeMix() : base(0x103F, 40) => Weight = 1.0;

        public CakeMix(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041002; // cake mix

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

        public override Food Cook() => new Cake();
    }

    public class RawFishSteak : CookableFood
    {
        [Constructible]
        public RawFishSteak(int amount = 1) : base(0x097A, 10)
        {
            Stackable = true;
            Amount = amount;
        }

        public RawFishSteak(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;

        public override Food Cook() => new FishSteak();

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
