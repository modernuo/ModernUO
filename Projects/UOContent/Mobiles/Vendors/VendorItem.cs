using System;
using System.Globalization;
using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class VendorItem
{
    [SerializableField(0)]
    private Item _item;

    [SerializableField(1)]
    private int _price;

    [SerializableField(3)]
    private DateTime _created;

    public VendorItem()
    {
    }

    public VendorItem(Item item, int price, string description, DateTime created)
    {
        _item = item;
        _price = price;
        _description = description ?? "";
        _created = created;
        Valid = true;
    }

    public string FormattedPrice =>
        Core.ML ? Price.ToString("N0", CultureInfo.GetCultureInfo("en-US")) : Price.ToString();

    [SerializableProperty(2)]
    public string Description
    {
        get => _description;
        set
        {
            _description = value ?? "";

            if (Valid)
            {
                Item.InvalidateProperties();
            }
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
