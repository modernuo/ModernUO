using System;

namespace Server.Items;

public class FillableEntry
{
    protected Type[] m_Types;
    protected int m_Weight;

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
        m_Weight = weight;
        m_Types = types;
    }

    public FillableEntry(int weight, Type[] types, int offset, int count)
    {
        m_Weight = weight;
        m_Types = new Type[count];

        for (var i = 0; i < m_Types.Length; ++i)
        {
            m_Types[i] = types[offset + i];
        }
    }

    public Type[] Types => m_Types;
    public int Weight => m_Weight;

    public virtual Item Construct()
    {
        var item = Loot.Construct(m_Types);

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

        var index = Utility.Random(m_Types.Length);

        if (m_Types[index] == typeof(BeverageBottle))
        {
            item = new BeverageBottle(Content);
        }
        else if (m_Types[index] == typeof(Jug))
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
