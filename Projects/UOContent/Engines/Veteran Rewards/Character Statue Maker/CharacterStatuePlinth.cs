using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class CharacterStatuePlinth : Static, IAddon
{
    [SerializableField(0, setter: "private")]
    private CharacterStatue _statue;

    public CharacterStatuePlinth(CharacterStatue statue) : base(0x32F2)
    {
        _statue = statue;

        InvalidateHue();
    }

    public override int LabelNumber => 1076201; // Character Statue
    public Item Deed => new CharacterStatueDeed(_statue);

    public virtual bool CouldFit(IPoint3D p, Map map)
    {
        var point = new Point3D(p.X, p.Y, p.Z);

        if (map?.CanFit(point, 20) != true)
        {
            return false;
        }

        var house = BaseHouse.FindHouseAt(point, map, 20);

        if (house == null)
        {
            return false;
        }

        var result = CharacterStatueTarget.CheckDoors(point, 20, house);

        if (result == AddonFitResult.Valid)
        {
            return true;
        }

        return false;
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (_statue?.Deleted == false)
        {
            _statue.Delete();
        }
    }

    public override void OnMapChange()
    {
        if (_statue != null)
        {
            _statue.Map = Map;
        }
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        if (_statue != null)
        {
            _statue.Location = new Point3D(X, Y, Z + 5);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_statue != null)
        {
            CharacterPlinthGump.DisplayTo(from, _statue);
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (string.IsNullOrEmpty(_statue?.SculptedBy) || Map == Map.Internal)
        {
            Delete();
        }
    }

    public void InvalidateHue()
    {
        if (_statue != null)
        {
            Hue = 0xB8F + (int)_statue.StatueType * 4 + (int)_statue.Material;
        }
    }

    private class CharacterPlinthGump : DynamicGump
    {
        private readonly CharacterStatue _statue;

        public override bool Singleton => true;

        private CharacterPlinthGump(CharacterStatue statue) : base(60, 30) => _statue = statue;

        public static void DisplayTo(Mobile from, CharacterStatue statue)
        {
            if (from?.NetState == null || statue == null || statue.Deleted)
            {
                return;
            }

            from.SendGump(new CharacterPlinthGump(statue));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddImage(0, 0, 0x24F4);
            builder.AddHtml(55, 50, 150, 20, _statue.Name);
            builder.AddHtml(55, 75, 150, 20, _statue.SculptedOn.ToString());
            builder.AddHtmlLocalized(55, 100, 150, 20, GetTypeNumber(_statue.StatueType), 0);
        }

        public static int GetTypeNumber(StatueType type) =>
            type switch
            {
                StatueType.Marble => 1076181,
                StatueType.Jade   => 1076180,
                StatueType.Bronze => 1076230,
                _                 => 1076181
            };
    }
}
