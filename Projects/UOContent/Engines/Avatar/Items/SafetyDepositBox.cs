using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Engines.Avatar;

/// <summary>
/// Bank-box sub-container whose contents survive an Avatar's death. Usable only while the owner's bank is open.
/// </summary>
[SerializationGenerator(0)]
public partial class SafetyDepositBox : Container
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    public SafetyDepositBox(Mobile owner) : base(0xE7C)
    {
        Name = "safety deposit box";
        _owner = owner;
        GumpID = 0x4A;
        MaxItems = 1;
    }

    public override int DefaultMaxWeight => 0;

    public override bool IsVirtualItem => true;

    public bool Opened => IsBankOpen(_owner);

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject) => true;

    public override bool IsAccessibleTo(Mobile check) => CanAccess(check) && base.IsAccessibleTo(check);

    public override void OnDoubleClick(Mobile from)
    {
        if (_owner != null && IsBankOpen(from))
        {
            DisplayTo(_owner);
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped) => CanAccess(from) && base.OnDragDrop(from, dropped);

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p) =>
        CanAccess(from) && base.OnDragDropInto(from, item, p);

    public override void OnSingleClick(Mobile from)
    {
    }

    private bool CanAccess(Mobile from) => IsBankOpen(from) || from.AccessLevel >= AccessLevel.GameMaster;

    private bool IsBankOpen(Mobile from) =>
        _owner?.FindBankNoCreate() is { Opened: true } || from.AccessLevel > AccessLevel.Player;

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_owner == null)
        {
            Timer.DelayCall(Delete);
        }
    }
}
