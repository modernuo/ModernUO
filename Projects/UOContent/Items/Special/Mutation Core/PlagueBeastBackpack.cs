using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PlagueBeastBackpack : BaseContainer
{
    private static readonly Point3D[][] _positions =
    {
        new []
        {
            new Point3D(275, 85, 0), new Point3D(360, 111, 0), new Point3D(375, 184, 0),
            new Point3D(332, 228, 0), new Point3D(141, 105, 0), new Point3D(189, 75, 0)
        },
        new []
        {
            new Point3D(274, 34, 0), new Point3D(327, 89, 0), new Point3D(354, 168, 0),
            new Point3D(304, 225, 0), new Point3D(113, 86, 0), new Point3D(189, 75, 0)
        },
        new []
        {
            new Point3D(276, 79, 0), new Point3D(369, 117, 0), new Point3D(372, 192, 0),
            new Point3D(336, 230, 0), new Point3D(141, 116, 0), new Point3D( 189, 75, 0)
        }
    };

    private static readonly int[] _brainHues = { 0x2B, 0x42, 0x54, 0x60 };

    public PlagueBeastBackpack() : base(0x261B) => Layer = Layer.Backpack;

    public override int DefaultMaxWeight => 0;
    public override int DefaultMaxItems => 0;
    public override int DefaultGumpID => 0x2A63;
    public override int DefaultDropSound => 0x23F;

    public void Initialize()
    {
        AddInnard(0x1CF6, 0x0, 227, 128);
        AddInnard(0x1D10, 0x0, 251, 128);
        AddInnard(0x1FBE, 0x21, 240, 83);

        AddInnard(new PlagueBeastHeart(), 229, 104);

        AddInnard(0x1D06, 0x0, 283, 91);
        AddInnard(0x1FAF, 0x21, 315, 107);
        AddInnard(0x1FB9, 0x21, 289, 87);
        AddInnard(0x9E7, 0x21, 304, 96);
        AddInnard(0x1B1A, 0x66D, 335, 102);
        AddInnard(0x1D10, 0x0, 338, 146);
        AddInnard(0x1FB3, 0x21, 358, 167);
        AddInnard(0x1D0B, 0x0, 357, 155);
        AddInnard(0x9E7, 0x21, 339, 184);
        AddInnard(0x1B1A, 0x66D, 157, 172);
        AddInnard(0x1D11, 0x0, 147, 157);
        AddInnard(0x1FB9, 0x21, 121, 131);
        AddInnard(0x9E7, 0x21, 166, 176);
        AddInnard(0x1D0B, 0x0, 122, 138);
        AddInnard(0x1D0D, 0x0, 118, 150);
        AddInnard(0x1FB3, 0x21, 97, 123);
        AddInnard(0x1D08, 0x0, 115, 113);
        AddInnard(0x9E7, 0x21, 109, 109);
        AddInnard(0x9E7, 0x21, 91, 122);
        AddInnard(0x9E7, 0x21, 94, 160);
        AddInnard(0x1B19, 0x66D, 170, 121);
        AddInnard(0x1FAF, 0x21, 161, 111);
        AddInnard(0x1D0B, 0x0, 158, 112);
        AddInnard(0x9E7, 0x21, 159, 101);
        AddInnard(0x1D10, 0x0, 132, 177);
        AddInnard(0x1D0E, 0x0, 110, 178);
        AddInnard(0x1FB3, 0x21, 95, 194);
        AddInnard(0x1FAF, 0x21, 154, 203);
        AddInnard(0x1B1A, 0x66D, 110, 237);
        AddInnard(0x9E7, 0x21, 111, 171);
        AddInnard(0x9E7, 0x21, 90, 197);
        AddInnard(0x9E7, 0x21, 166, 205);
        AddInnard(0x9E7, 0x21, 96, 242);
        AddInnard(0x1D10, 0x0, 334, 196);
        AddInnard(0x1D0B, 0x0, 322, 270);

        var organs = new List<PlagueBeastOrgan>();
        PlagueBeastOrgan organ;

        for (var i = 0; i < 6; i++)
        {
            var random = i == 5 ? 0 : Utility.Random(3);

            organ = random switch
            {
                0 => new PlagueBeastRockOrgan(),
                1 => new PlagueBeastMaidenOrgan(),
                2 => new PlagueBeastRubbleOrgan(),
                _ => new PlagueBeastRockOrgan()
            };

            organs.Add(organ);
            var pos = _positions[random][i];
            AddInnard(organ, pos);
        }

        organ = new PlagueBeastBackupOrgan();
        organs.Add(organ);
        AddInnard(organ, 129, 214);

        for (var i = 0; i < _brainHues.Length; i++)
        {
            organ = organs.RandomElement();
            organ.BrainHue = _brainHues[i];
            organs.Remove(organ);
        }

        organs.Clear();

        AddInnard(new PlagueBeastMainOrgan(), 240, 161);
    }

    public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage) =>
        dropped is PlagueBeastInnard or PlagueBeastGland && base.TryDropItem(from, dropped, sendFullMessage);

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (!IsAccessibleTo(from) || item is not (PlagueBeastInnard or PlagueBeastGland))
        {
            return false;
        }

        var ir = ItemBounds.Table[item.ItemID];
        int x, y;
        var cx = p.X + ir.X + ir.Width / 2;
        var cy = p.Y + ir.Y + ir.Height / 2;

        for (var i = Items.Count - 1; i >= 0; i--)
        {
            if (Items[i] is PlagueBeastComponent innard)
            {
                var r = ItemBounds.Table[innard.ItemID];

                x = innard.X + r.X;
                y = innard.Y + r.Y;

                if (cx >= x && cx <= x + r.Width && cy >= y && cy <= y + r.Height)
                {
                    innard.OnDragDrop(from, item);
                    break;
                }
            }
        }

        return base.OnDragDropInto(from, item, p);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddInnard(int itemID, int hue, int x, int y) =>
        AddInnard(new PlagueBeastInnard(itemID, hue), x, y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddInnard(PlagueBeastInnard innard, int x, int y) => AddInnard(innard, new Point3D(x, y, 0));

    public void AddInnard(PlagueBeastInnard innard, Point3D pos)
    {
        AddItem(innard);
        innard.Location = pos;
        innard.Map = Map;
    }
}
