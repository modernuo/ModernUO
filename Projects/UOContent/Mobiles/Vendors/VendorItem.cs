using System;
using System.Globalization;
using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class VendorItem
{
    [DirtyTrackingEntity]
    private PlayerVendor _vendor;

    [SerializableField(0)]
    private Item _item;

    [SerializableField(1)]
    private int _price;

    [SerializableField(3)]
    private DateTime _created;

    public VendorItem(PlayerVendor vendor) => _vendor = vendor;

    // Generator 4.0.0 constructs dictionary values without the owner; PlayerVendor relinks
    // them after deserialization. Declared after the owner constructor on purpose.
    public VendorItem()
    {
    }

    internal void AttachTo(PlayerVendor vendor) => _vendor ??= vendor;

    public VendorItem(PlayerVendor vendor, Item item, int price, string description, DateTime created)
    {
        _vendor = vendor;
        _item = item;
        _price = price;
        _description = description ?? "";
        _created = created;
        Valid = true;
    }

    public string FormattedPrice =>
        Core.ML ? Price.ToString("N0", CultureInfo.GetCultureInfo("en-US")) : Price.ToString();

    [SerializableField(2, fieldChanged: nameof(OnDescriptionChanged), allowFieldChange: nameof(AllowDescriptionChange))]
    private string _description;

    private bool AllowDescriptionChange(ref string value)
    {
        value = value ?? "";
        return true;
    }

    private void OnDescriptionChanged(string oldValue, string newValue)
    {
        if (Valid)
        {
            Item.InvalidateProperties();
        }
    }

    public bool IsForSale => Price >= 0;
    public bool IsForFree => Price == 0;
    public bool Valid { get; private set; }

    public void Invalidate() => Valid = false;

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_price > 100000000)
        {
            _price = 100000000;
        }

        Valid = true;
    }
}
