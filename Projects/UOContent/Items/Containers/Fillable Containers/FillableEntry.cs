using System;

namespace Server.Items;

public class FillableEntry
{
    protected Type[] _types;
    protected int _weight;

    public FillableEntry(Type type) : this(1, new[] { type })
    {
    }

    public FillableEntry(int weight, Type type) : this(weight, new[] { type })
    {
    }

    public FillableEntry(Type[] types) : this(1, types)
    {
    }

    public FillableEntry(int weight, Type[] types)
    {
        _weight = weight;
        _types = types;
    }

    public FillableEntry(int weight, Type[] types, int offset, int count)
    {
        _weight = weight;
        _types = new Type[count];
        Array.Copy(types, offset, _types, 0, count);
    }

    public Type[] Types => _types;
    public int Weight => _weight;

    public virtual Item Construct()
    {
        var item = Loot.Construct(_types);

        if (item is Key key)
        {
            key.ItemID = Utility.RandomList(
                (int)KeyType.Copper,
                (int)KeyType.Gold,
                (int)KeyType.Iron,
                (int)KeyType.Rusty
            );
        }
        else if (item is Arrow or Bolt)
        {
            item.Amount = Utility.RandomMinMax(2, 6);
        }
        else if (item is Bandage or Lockpick)
        {
            item.Amount = Utility.RandomMinMax(1, 3);
        }

        return item;
    }
}

public class FillableBvrge : FillableEntry
{
    public FillableBvrge(Type type, BeverageType content) : this(1, type, content)
    {
    }

    public FillableBvrge(int weight, Type type, BeverageType content) : base(weight, type) =>
        Content = content;

    public BeverageType Content { get; }

    public override Item Construct()
    {
        Item item;

        var index = Utility.Random(_types.Length);

        if (_types[index] == typeof(BeverageBottle))
        {
            item = new BeverageBottle(Content);
        }
        else if (_types[index] == typeof(Jug))
        {
            item = new Jug(Content);
        }
        else
        {
            item = base.Construct();

            if (item is BaseBeverage bev)
            {
                bev.Content = Content;
                bev.Quantity = bev.MaxQuantity;
            }
        }

        return item;
    }
}
