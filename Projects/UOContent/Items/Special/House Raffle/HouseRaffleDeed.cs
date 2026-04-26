using System;
using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HouseRaffleDeed : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    private HouseRaffleStone _stone;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    private Point3D _plotLocation;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    private Map _plotFacet;

    [InvalidateProperties]
    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    private Mobile _awardedTo;

    [Constructible]
    public HouseRaffleDeed(HouseRaffleStone stone = null, Mobile m = null) : base(0x2830)
    {
        _stone = stone;

        if (stone != null)
        {
            _plotLocation = stone.GetPlotCenter();
            _plotFacet = stone.PlotFacet;
        }

        _awardedTo = m;

        LootType = LootType.Blessed;
        Hue = 0x501;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
    public bool IsExpired => _stone?.Deleted != false || _stone.IsExpired;

    public override string DefaultName => "a writ of lease";

    public override double DefaultWeight => 1.0;

    public bool ValidLocation() => _plotLocation != Point3D.Zero && _plotFacet != null && _plotFacet != Map.Internal;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (ValidLocation())
        {
            list.Add(
                1060658, // ~1_val~: ~2_val~
                $"{"location"}\t{HouseRaffleStone.FormatLocation(_plotLocation, _plotFacet, false)}"
            );
            list.Add(1060659, $"{"facet"}\t{_plotFacet}"); // ~1_val~: ~2_val~
            list.Add(1150486); // [Marked Item]
        }

        if (IsExpired)
        {
            list.Add(1150487); // [Expired]
        }

        // list.Add( 1060660, "shard\t{0}", ServerList.ServerName ); // ~1_val~: ~2_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!ValidLocation())
        {
            return;
        }

        if (IsChildOf(from.Backpack))
        {
            WritOfLeaseGump.DisplayTo(from, this);
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    private class WritOfLeaseGump : DynamicGump
    {
        private readonly HouseRaffleDeed _deed;

        public override bool Singleton => true;

        private WritOfLeaseGump(HouseRaffleDeed deed) : base(150, 50)
        {
            _deed = deed;
        }

        public static void DisplayTo(Mobile from, HouseRaffleDeed deed)
        {
            if (from?.NetState == null || deed?.Deleted != false)
            {
                return;
            }

            from.SendGump(new WritOfLeaseGump(deed));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddImage(0, 0, 9380);
            builder.AddImage(114, 0, 9381);
            builder.AddImage(171, 0, 9382);
            builder.AddImage(0, 140, 9383);
            builder.AddImage(114, 140, 9384);
            builder.AddImage(171, 140, 9385);
            builder.AddImage(0, 182, 9383);
            builder.AddImage(114, 182, 9384);
            builder.AddImage(171, 182, 9385);
            builder.AddImage(0, 224, 9383);
            builder.AddImage(114, 224, 9384);
            builder.AddImage(171, 224, 9385);
            builder.AddImage(0, 266, 9386);
            builder.AddImage(114, 266, 9387);
            builder.AddImage(171, 266, 9388);

            builder.AddHtmlLocalized(30, 48, 229, 20, 1150484, 200); // WRIT OF LEASE
            builder.AddHtml(28, 75, 231, 280, FormatDescription(_deed), background: false, scrollbar: true);
        }

        private static string FormatDescription(HouseRaffleDeed deed)
        {
            if (deed == null)
            {
                return string.Empty;
            }

            if (deed.IsExpired)
            {
                return
                    $"<bodytextblack>This deed once entitled the bearer to build a house on the plot of land located at {HouseRaffleStone.FormatLocation(deed.PlotLocation, deed.PlotFacet, false)} on the {deed.PlotFacet} facet.<br><br>The deed has expired, and now the indicated plot of land is subject to normal house construction rules.<br><br>This deed functions as a recall rune marked for the location of the plot it represents.</bodytextblack>";
            }

            var daysLeft = (int)Math.Ceiling(
                (deed.Stone.Started + deed.Stone.Duration +
                    HouseRaffleStone.ExpirationTime - Core.Now).TotalDays
            );

            return
                $"<bodytextblack>This deed entitles the bearer to build a house on the plot of land located at {HouseRaffleStone.FormatLocation(deed.PlotLocation, deed.PlotFacet, false)} on the {deed.PlotFacet} facet.<br><br>The deed will expire after {daysLeft} more day{(daysLeft == 1 ? "" : "s")} have passed, and at that time the right to place a house reverts to normal house construction rules.<br><br>This deed functions as a recall rune marked for the location of the plot it represents.<br><br>To place a house on the deeded plot, you must simply have this deed in your backpack or bank box when using a House Placement Tool there.</bodytextblack>";
        }
    }
}
