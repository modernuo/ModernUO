using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[Flippable(0x456E, 0x456F)]
[SerializationGenerator(0)]
public partial class SnowStatuePegasus : Item
{
    [Constructible]
    public SnowStatuePegasus() : base(0x456E)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[Flippable(0x4578, 0x4579)]
[SerializationGenerator(0)]
public partial class SnowStatueSeahorse : Item
{
    [Constructible]
    public SnowStatueSeahorse() : base(0x4578)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[Flippable(0x457A, 0x457B)]
[SerializationGenerator(0)]
public partial class SnowStatueMermaid : Item
{
    [Constructible]
    public SnowStatueMermaid() : base(0x457A)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[Flippable(0x457C, 0x457D)]
[SerializationGenerator(0)]
public partial class SnowStatueGriffon : Item
{
    [Constructible]
    public SnowStatueGriffon() : base(0x457C)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0)]
public partial class SnowStatueDeed : Item
{
    [Constructible]
    public SnowStatueDeed() : base(0x14F0) => LootType = LootType.Blessed;

    public override int LabelNumber => 1114296; // snow statue deed
    public override double DefaultWeight => 1.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            SnowStatueGump.DisplayTo(from, this);
        }
        else
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
        }
    }

    private class SnowStatueGump : StaticGump<SnowStatueGump>
    {
        private readonly SnowStatueDeed _deed;

        public override bool Singleton => true;

        private SnowStatueGump(SnowStatueDeed deed) : base(100, 200)
        {
            _deed = deed;
        }

        public static void DisplayTo(Mobile from, SnowStatueDeed deed)
        {
            if (from?.NetState == null || deed?.Deleted != false)
            {
                return;
            }

            from.SendGump(new SnowStatueGump(deed));
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddBackground(0, 0, 360, 225, 0xA28);

            builder.AddPage(1);
            builder.AddHtmlLocalized(45, 15, 200, 20, 1156487); // Select One:

            builder.AddItem(35, 75, 0x456E);
            builder.AddButton(65, 50, 0x845, 0x846, 1);

            builder.AddItem(120, 75, 0x4578);
            builder.AddButton(135, 50, 0x845, 0x846, 2);

            builder.AddItem(190, 75, 0x457A);
            builder.AddButton(205, 50, 0x845, 0x846, 3);

            builder.AddItem(250, 75, 0x457C);
            builder.AddButton(275, 50, 0x845, 0x846, 4);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
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

            Item statue = info.ButtonID switch
            {
                1 => new SnowStatuePegasus(),
                2 => new SnowStatueSeahorse(),
                3 => new SnowStatueMermaid(),
                4 => new SnowStatueGriffon(),
                _ => null
            };

            if (statue == null)
            {
                return;
            }

            if (!from.PlaceInBackpack(statue))
            {
                statue.Delete();
                from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
            }
            else
            {
                _deed.Delete();
            }
        }
    }
}
