using System;
using ModernUO.Serialization;

namespace Server.Items;

public enum TrophyRank
{
    Bronze,
    Silver,
    Gold
}

[Flippable(5020, 4647)]
[SerializationGenerator(2, false)]
public partial class Trophy : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _title;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(3, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _date;

    public override string DefaultName => $"{GetRankName(_rank)} trophy";

    private static string GetRankName(TrophyRank rank) =>
        rank switch
        {
            TrophyRank.Silver => "silver",
            TrophyRank.Gold   => "gold",
            _                 => "bronze"
        };

    [Constructible]
    public Trophy(string title, TrophyRank rank) : base(5020)
    {
        _title = title;
        _rank = rank;
        _date = Core.Now;

        LootType = LootType.Blessed;

        UpdateStyle();
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public TrophyRank Rank
    {
        get => _rank;
        set
        {
            _rank = value;
            UpdateStyle();
            this.MarkDirty();
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _title = reader.ReadString();
        _rank = (TrophyRank)reader.ReadInt();
        _owner = reader.ReadEntity<Mobile>();
        _date = reader.ReadDateTime();
    }

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        Owner ??= RootParent as Mobile;
    }

    public override void GetProperties(IPropertyList list)
    {
        if (Owner != null)
        {
            list.Add($"{Title} -- {Owner.RawName}");
        }
        else if (Title != null)
        {
            list.Add(Title);
        }

        if (Date != DateTime.MinValue)
        {
            list.Add(Date.ToString("d"));
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (Owner != null)
        {
            LabelTo(from, $"{Title} -- {Owner.RawName}");
        }
        else if (Title != null)
        {
            LabelTo(from, Title);
        }

        if (Date != DateTime.MinValue)
        {
            LabelTo(from, Date.ToString("d"));
        }
    }

    public void UpdateStyle()
    {
        Hue = _rank switch
        {
            TrophyRank.Gold   => 2213,
            TrophyRank.Silver => 0,
            _ => 2206, // bronze
        };
    }
}
