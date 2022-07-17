using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Candelabra : BaseLight, IShipwreckedItem
{
    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private bool _isShipwreckedItem;

    [Constructible]
    public Candelabra() : base(0xA27)
    {
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle225;
        Weight = 3.0;
    }

    public override int LitItemID => 0xB1D;
    public override int UnlitItemID => 0xA27;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        if (IsShipwreckedItem)
        {
            list.Add(1041645); // recovered from a shipwreck
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (IsShipwreckedItem)
        {
            LabelTo(from, 1041645); // recovered from a shipwreck
        }
    }
}
