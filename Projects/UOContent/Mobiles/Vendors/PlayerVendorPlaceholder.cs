using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class PlayerVendorPlaceholder : Item
{
    private readonly Timer _timer;

    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PlayerVendor _vendor;

    public PlayerVendorPlaceholder(PlayerVendor vendor) : base(0x1F28)
    {
        Hue = 0x672;
        Movable = false;

        _vendor = vendor;

        _timer = Timer.DelayCall(TimeSpan.FromMinutes(2.0), Delete);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Vendor != null)
        {
            list.Add(1062498, Vendor.Name); // reserved for vendor ~1_NAME~
        }
    }

    public void RestartTimer()
    {
        _timer.Stop();
        _timer.Start();
    }

    public override void OnDelete()
    {
        if (Vendor?.Deleted == false)
        {
            Vendor.MoveToWorld(Location, Map);
            Vendor.Placeholder = null;
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization() => Delete();
}
