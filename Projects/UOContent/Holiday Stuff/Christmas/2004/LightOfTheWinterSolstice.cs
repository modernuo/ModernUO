using ModernUO.Serialization;
using Server.Misc;

namespace Server.Items;

[Flippable(0x236E, 0x2371)]
[SerializationGenerator(0, false)]
public partial class LightOfTheWinterSolstice : Item
{
    [InternString]
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _dipper;

    [Constructible]
    public LightOfTheWinterSolstice(string dipper = null) : base(0x236E)
    {
        _dipper = dipper?.Intern() ?? StaffInfo.GetRandomStaff();

        LootType = LootType.Blessed;
        Light = LightType.Circle300;
        Hue = Utility.RandomDyedHue();
    }

    public override double DefaultWeight => 1.0;

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        LabelTo(from, 1070881, _dipper); // Hand Dipped by ~1_name~
        LabelTo(from, 1070880);          // Winter 2004
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1070881, _dipper); // Hand Dipped by ~1_name~
        list.Add(1070880);          // Winter 2004
    }
}
