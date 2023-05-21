using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EarringBoxSet : RedVelvetGiftBox
{
    [Constructible]
    public EarringBoxSet()
    {
        DropItem(new EarringsOfProtection(AosElementAttribute.Physical));
        DropItem(new EarringsOfProtection(AosElementAttribute.Fire));
        DropItem(new EarringsOfProtection(AosElementAttribute.Cold));
        DropItem(new EarringsOfProtection(AosElementAttribute.Poison));
        DropItem(new EarringsOfProtection(AosElementAttribute.Energy));
    }
}

[SerializationGenerator(0, false)]
public partial class EarringsOfProtection : BaseJewel
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private AosElementAttribute _attribute;

    [Constructible]
    public EarringsOfProtection() : this(RandomType())
    {
    }

    [Constructible]
    public EarringsOfProtection(AosElementAttribute element) : base(0x1087, Layer.Earrings)
    {
        Resistances[element] = 2;

        _attribute = element;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => GetItemData(_attribute, true);

    public override int Hue => GetItemData(_attribute, false);

    private void Deserialize(IGenericReader reader, int version)
    {
        _attribute = (AosElementAttribute)reader.ReadInt();
    }

    public static AosElementAttribute RandomType() => GetTypes(Utility.Random(5));

    public static AosElementAttribute GetTypes(int value)
    {
        return value switch
        {
            0 => AosElementAttribute.Physical,
            1 => AosElementAttribute.Fire,
            2 => AosElementAttribute.Cold,
            3 => AosElementAttribute.Poison,
            _ => AosElementAttribute.Energy
        };
    }

    public static int GetItemData(AosElementAttribute element, bool label)
    {
        return element switch
        {
            AosElementAttribute.Physical => label ? 1071091 : 0,     // Earring of Protection (Physical)  1071091
            AosElementAttribute.Fire     => label ? 1071092 : 0x4ec, // Earring of Protection (Fire)      1071092
            AosElementAttribute.Cold     => label ? 1071093 : 0x4f2, // Earring of Protection (Cold)      1071093
            AosElementAttribute.Poison   => label ? 1071094 : 0x4f8, // Earring of Protection (Poison)    1071094
            AosElementAttribute.Energy   => label ? 1071095 : 0x4fe, // Earring of Protection (Energy)    1071095
            _                            => -1
        };
    }
}
