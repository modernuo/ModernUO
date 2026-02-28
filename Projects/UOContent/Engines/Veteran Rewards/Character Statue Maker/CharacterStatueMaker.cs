using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(1)]
public partial class CharacterStatueMaker : Item, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    public CharacterStatueMaker(StatueType type) : base(0x32F0)
    {
        _statueType = type;

        InvalidateHue();

        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 5.0;

    public override int LabelNumber => 1076173; // Character Statue Maker

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public StatueType StatueType
    {
        get => _statueType;
        set
        {
            _statueType = value;
            InvalidateHue();
            this.MarkDirty();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this, new object[] { _statueType }))
        {
            return;
        }

        if (IsChildOf(from.Backpack))
        {
            if (!from.IsBodyMod)
            {
                from.SendLocalizedMessage(1076194); // Select a place where you would like to put your statue.
                from.Target = new CharacterStatueTarget(this, _statueType);
            }
            else
            {
                from.SendLocalizedMessage(1073648); // You may only proceed while in your original state...
            }
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076222); // 6th Year Veteran Reward
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _isRewardItem = reader.ReadBool();
        _statueType = (StatueType)reader.ReadInt();
    }

    public void InvalidateHue()
    {
        Hue = 0xB8F + (int)_statueType * 4;
    }
}

[SerializationGenerator(0)]
public partial class MarbleStatueMaker : CharacterStatueMaker
{
    [Constructible]
    public MarbleStatueMaker() : base(StatueType.Marble)
    {
    }
}

[SerializationGenerator(0)]
public partial class JadeStatueMaker : CharacterStatueMaker
{
    [Constructible]
    public JadeStatueMaker() : base(StatueType.Jade)
    {
    }
}

[SerializationGenerator(0)]
public partial class BronzeStatueMaker : CharacterStatueMaker
{
    [Constructible]
    public BronzeStatueMaker() : base(StatueType.Bronze)
    {
    }
}
