using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HolidayPottedPlant : Item
{
    [Constructible]
    public HolidayPottedPlant() : this(Utility.RandomMinMax(0x11C8, 0x11CC))
    {
    }

    [Constructible]
    public HolidayPottedPlant(int itemID) : base(itemID)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0)]
public partial class PottedPlantDeed : Item
{
    [Constructible]
    public PottedPlantDeed() : base(0x14F0) => LootType = LootType.Blessed;

    public override int LabelNumber => 1041114; // A deed for a potted plant.
    public override double DefaultWeight => 1.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.CloseGump<InternalGump>();
            from.SendGump(new InternalGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
        }
    }

    private class InternalGump : Gump
    {
        private readonly PottedPlantDeed _deed;

        public InternalGump(PottedPlantDeed deed) : base(100, 200)
        {
            _deed = deed;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 360, 195, 0xA28);

            AddPage(1);
            AddLabel(45, 15, 0, "Choose a Potted Plant:");

            AddItem(45, 75, 0x11C8);
            AddButton(55, 50, 0x845, 0x846, 1);

            AddItem(100, 75, 0x11C9);
            AddButton(115, 50, 0x845, 0x846, 2);

            AddItem(160, 75, 0x11CA);
            AddButton(175, 50, 0x845, 0x846, 3);

            AddItem(225, 75, 0x11CB);
            AddButton(235, 50, 0x845, 0x846, 4);

            AddItem(280, 75, 0x11CC);
            AddButton(295, 50, 0x845, 0x846, 5);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_deed?.Deleted != false)
            {
                return;
            }

            var from = sender.Mobile;

            if (!_deed.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it
                return;
            }

            var index = info.ButtonID - 1;

            if (index >= 0 && index <= 4)
            {
                var plant = new HolidayPottedPlant(0x11C8 + index);

                if (!from.PlaceInBackpack(plant))
                {
                    plant.Delete();
                    from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
                }
                else
                {
                    _deed.Delete();
                }
            }
        }
    }
}
