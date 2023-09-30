using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SubtextSign : Sign
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _subText;

    [Constructible]
    public SubtextSign(SignType type, SignFacing facing, string subtext) : base(type, facing) => _subText = subtext;

    [Constructible]
    public SubtextSign(int itemID, string subtext) : base(itemID) => _subText = subtext;

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (!string.IsNullOrEmpty(_subText))
        {
            LabelTo(from, _subText);
        }
    }

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        if (!string.IsNullOrEmpty(_subText))
        {
            list.Add(_subText);
        }
    }
}
