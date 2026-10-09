using System;
using System.Collections.Generic;
using System.Globalization;

namespace Server.Engines.Avatar;

public class ItemReward : IReward
{
    private static readonly TextInfo _textInfo = new CultureInfo("en-US", false).TextInfo;
    private static readonly Dictionary<Type, (string Name, int ItemID)> _snapshots = [];

    private ItemReward()
    {
    }

    public Func<Item> OnSelect { get; set; }
    public bool CanSelect { get; set; }
    public bool CanSelectAnywhere { get; set; }
    public int Cost { get; private set; }
    public string Description { get; private set; }
    public int Graphic { get; private set; }
    public string Name { get; private set; }
    public bool Static { get; set; }

    public static ItemReward Create<T>(
        int cost, bool canSelect, Func<T> onSelect, int amount = 0, string name = null, string description = null,
        int graphicOverride = AvatarShopGump.BLANK_ITEM_ID
    ) where T : Item
    {
        var snapshot = GetSnapshot(onSelect);

        if (string.IsNullOrEmpty(name))
        {
            name = _textInfo.ToTitleCase(snapshot.Name);
        }

        if (amount > 0)
        {
            name = $"{name} ({amount})";
        }

        return Create(
            cost,
            graphicOverride != AvatarShopGump.BLANK_ITEM_ID ? graphicOverride : snapshot.ItemID,
            name,
            description ?? "",
            canSelect,
            onSelect
        );
    }

    public static ItemReward Create(
        int cost, int graphic, string name, string description, bool canSelect, Func<Item> onSelect
    ) =>
        new()
        {
            Graphic = graphic,
            Name = name,
            Description = description,
            Cost = cost,
            CanSelect = canSelect,
            OnSelect = onSelect,
            Static = false
        };

    private static (string Name, int ItemID) GetSnapshot<T>(Func<T> factory) where T : Item
    {
        if (_snapshots.TryGetValue(typeof(T), out var snapshot))
        {
            return snapshot;
        }

        var item = factory();
        snapshot = (item.Name ?? item.DefaultName ?? typeof(T).Name, item.ItemID);
        item.Delete();

        return _snapshots[typeof(T)] = snapshot;
    }

    public ItemReward AllowSelectAnywhere()
    {
        CanSelectAnywhere = true;
        return this;
    }

    public ItemReward AsStatic()
    {
        Static = true;
        return this;
    }

    public ItemReward WithDescription(string description)
    {
        Description = description;
        return this;
    }

    public ItemReward WithName(string name)
    {
        Name = name;
        return this;
    }
}
