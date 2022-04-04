using System.Collections.Generic;
using Server.ContextMenus;

namespace Server.Items
{
    public abstract class Food : Item
    {
        public Food(int itemID, int amount = 1) : base(itemID)
        {
            Stackable = true;
            Amount = amount;
            FillFactor = 1;
        }

        public Food(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Poisoner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Poison Poison { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FillFactor { get; set; }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive)
            {
                list.Add(new EatEntry(from, this));
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Movable)
            {
                return;
            }

            if (from.InRange(GetWorldLocation(), 1))
            {
                Eat(from);
            }
        }

        public virtual bool Eat(Mobile from)
        {
            // Fill the Mobile with FillFactor
            if (CheckHunger(from))
            {
                // Play a random "eat" sound
                from.PlaySound(Utility.Random(0x3A, 3));

                if (from.Body.IsHuman && !from.Mounted)
                {
                    from.Animate(34, 5, 1, true, false, 0);
                }

                if (Poison != null)
                {
                    from.ApplyPoison(Poisoner, Poison);
                }

                Consume();

                return true;
            }

            return false;
        }

        public virtual bool CheckHunger(Mobile from) => FillHunger(from, FillFactor);

        public static bool FillHunger(Mobile from, int fillFactor)
        {
            if (from.Hunger >= 20)
            {
                from.SendLocalizedMessage(500867); // You are simply too full to eat any more!
                return false;
            }

            var iHunger = from.Hunger + fillFactor;

            if (from.Stam < from.StamMax)
            {
                from.Stam += Utility.Random(6, 3) + fillFactor / 5;
            }

            if (iHunger >= 20)
            {
                from.Hunger = 20;
                from.SendLocalizedMessage(500872); // You manage to eat the food, but you are stuffed!
            }
            else
            {
                from.Hunger = iHunger;

                if (iHunger < 5)
                {
                    from.SendLocalizedMessage(500868); // You eat the food, but are still extremely hungry.
                }
                else if (iHunger < 10)
                {
                    from.SendLocalizedMessage(500869); // You eat the food, and begin to feel more satiated.
                }
                else if (iHunger < 15)
                {
                    from.SendLocalizedMessage(500870); // After eating the food, you feel much less hungry.
                }
                else
                {
                    from.SendLocalizedMessage(500871); // You feel quite full after consuming the food.
                }
            }

            return true;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(4); // version

            writer.Write(Poisoner);

            Poison.Serialize(Poison, writer);
            writer.Write(FillFactor);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Poison = reader.ReadInt() switch
                        {
                            0 => null,
                            1 => Poison.Lesser,
                            2 => Poison.Regular,
                            3 => Poison.Greater,
                            4 => Poison.Deadly,
                            _ => Poison
                        };

                        break;
                    }
                case 2:
                    {
                        Poison = Poison.Deserialize(reader);
                        break;
                    }
                case 3:
                    {
                        Poison = Poison.Deserialize(reader);
                        FillFactor = reader.ReadInt();
                        break;
                    }
                case 4:
                    {
                        Poisoner = reader.ReadEntity<Mobile>();
                        goto case 3;
                    }
            }
        }
    }

    public class BreadLoaf : Food
    {
        [Constructible]
        public BreadLoaf(int amount = 1) : base(0x103B, amount)
        {
            Weight = 1.0;
            FillFactor = 3;
        }

        public BreadLoaf(Serial serial) : base(serial)
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

    public class Bacon : Food
    {
        [Constructible]
        public Bacon(int amount = 1) : base(0x979, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Bacon(Serial serial) : base(serial)
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

    public class SlabOfBacon : Food
    {
        [Constructible]
        public SlabOfBacon(int amount = 1) : base(0x976, amount)
        {
            Weight = 1.0;
            FillFactor = 3;
        }

        public SlabOfBacon(Serial serial) : base(serial)
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

    public class FishSteak : Food
    {
        [Constructible]
        public FishSteak(int amount = 1) : base(0x97B, amount) => FillFactor = 3;

        public FishSteak(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;

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

    public class CheeseWheel : Food
    {
        [Constructible]
        public CheeseWheel(int amount = 1) : base(0x97E, amount) => FillFactor = 3;

        public CheeseWheel(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;

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

    public class CheeseWedge : Food
    {
        [Constructible]
        public CheeseWedge(int amount = 1) : base(0x97D, amount) => FillFactor = 3;

        public CheeseWedge(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;

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

    public class CheeseSlice : Food
    {
        [Constructible]
        public CheeseSlice(int amount = 1) : base(0x97C, amount) => FillFactor = 1;

        public CheeseSlice(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;

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

    public class FrenchBread : Food
    {
        [Constructible]
        public FrenchBread(int amount = 1) : base(0x98C, amount)
        {
            Weight = 2.0;
            FillFactor = 3;
        }

        public FrenchBread(Serial serial) : base(serial)
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

    public class FriedEggs : Food
    {
        [Constructible]
        public FriedEggs(int amount = 1) : base(0x9B6, amount)
        {
            Weight = 1.0;
            FillFactor = 4;
        }

        public FriedEggs(Serial serial) : base(serial)
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

    public class CookedBird : Food
    {
        [Constructible]
        public CookedBird(int amount = 1) : base(0x9B7, amount)
        {
            Weight = 1.0;
            FillFactor = 5;
        }

        public CookedBird(Serial serial) : base(serial)
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

    public class RoastPig : Food
    {
        [Constructible]
        public RoastPig(int amount = 1) : base(0x9BB, amount)
        {
            Weight = 45.0;
            FillFactor = 20;
        }

        public RoastPig(Serial serial) : base(serial)
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

    public class Sausage : Food
    {
        [Constructible]
        public Sausage(int amount = 1) : base(0x9C0, amount)
        {
            Weight = 1.0;
            FillFactor = 4;
        }

        public Sausage(Serial serial) : base(serial)
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

    public class Ham : Food
    {
        [Constructible]
        public Ham(int amount = 1) : base(0x9C9, amount)
        {
            Weight = 1.0;
            FillFactor = 5;
        }

        public Ham(Serial serial) : base(serial)
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

    public class Cake : Food
    {
        [Constructible]
        public Cake() : base(0x9E9)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 10;
        }

        public Cake(Serial serial) : base(serial)
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

    public class Ribs : Food
    {
        [Constructible]
        public Ribs(int amount = 1) : base(0x9F2, amount)
        {
            Weight = 1.0;
            FillFactor = 5;
        }

        public Ribs(Serial serial) : base(serial)
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

    public class Cookies : Food
    {
        [Constructible]
        public Cookies() : base(0x160b)
        {
            Stackable = Core.ML;
            Weight = 1.0;
            FillFactor = 4;
        }

        public Cookies(Serial serial) : base(serial)
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

    public class Muffins : Food
    {
        [Constructible]
        public Muffins() : base(0x9eb)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 4;
        }

        public Muffins(Serial serial) : base(serial)
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

    [TypeAlias("Server.Items.Pizza")]
    public class CheesePizza : Food
    {
        [Constructible]
        public CheesePizza() : base(0x1040)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 6;
        }

        public CheesePizza(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1044516; // cheese pizza

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

    public class SausagePizza : Food
    {
        [Constructible]
        public SausagePizza() : base(0x1040)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 6;
        }

        public SausagePizza(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1044517; // sausage pizza

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

    public class FruitPie : Food
    {
        [Constructible]
        public FruitPie() : base(0x1041)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 5;
        }

        public FruitPie(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041346; // baked fruit pie

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

    public class MeatPie : Food
    {
        [Constructible]
        public MeatPie() : base(0x1041)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 5;
        }

        public MeatPie(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041347; // baked meat pie

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

    public class PumpkinPie : Food
    {
        [Constructible]
        public PumpkinPie() : base(0x1041)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 5;
        }

        public PumpkinPie(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041348; // baked pumpkin pie

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

    public class ApplePie : Food
    {
        [Constructible]
        public ApplePie() : base(0x1041)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 5;
        }

        public ApplePie(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041343; // baked apple pie

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

    public class PeachCobbler : Food
    {
        [Constructible]
        public PeachCobbler() : base(0x1041)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 5;
        }

        public PeachCobbler(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041344; // baked peach cobbler

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

    public class Quiche : Food
    {
        [Constructible]
        public Quiche() : base(0x1041)
        {
            Stackable = Core.ML;
            Weight = 1.0;
            FillFactor = 5;
        }

        public Quiche(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041345; // baked quiche

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

    public class LambLeg : Food
    {
        [Constructible]
        public LambLeg(int amount = 1) : base(0x160a, amount)
        {
            Weight = 2.0;
            FillFactor = 5;
        }

        public LambLeg(Serial serial) : base(serial)
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

    public class ChickenLeg : Food
    {
        [Constructible]
        public ChickenLeg(int amount = 1) : base(0x1608, amount)
        {
            Weight = 1.0;
            FillFactor = 4;
        }

        public ChickenLeg(Serial serial) : base(serial)
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

    [Flippable(0xC74, 0xC75)]
    public class HoneydewMelon : Food
    {
        [Constructible]
        public HoneydewMelon(int amount = 1) : base(0xC74, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public HoneydewMelon(Serial serial) : base(serial)
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

    [Flippable(0xC64, 0xC65)]
    public class YellowGourd : Food
    {
        [Constructible]
        public YellowGourd(int amount = 1) : base(0xC64, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public YellowGourd(Serial serial) : base(serial)
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

    [Flippable(0xC66, 0xC67)]
    public class GreenGourd : Food
    {
        [Constructible]
        public GreenGourd(int amount = 1) : base(0xC66, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public GreenGourd(Serial serial) : base(serial)
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

    [Flippable(0xC7F, 0xC81)]
    public class EarOfCorn : Food
    {
        [Constructible]
        public EarOfCorn(int amount = 1) : base(0xC81, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public EarOfCorn(Serial serial) : base(serial)
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

    public class Turnip : Food
    {
        [Constructible]
        public Turnip(int amount = 1) : base(0xD3A, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Turnip(Serial serial) : base(serial)
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

    public class SheafOfHay : Item
    {
        [Constructible]
        public SheafOfHay() : base(0xF36) => Weight = 10.0;

        public SheafOfHay(Serial serial) : base(serial)
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
}
