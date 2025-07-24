using ModernUO.Serialization;
using Server.Items;
using Server.Targeting;

namespace Server.Engines.Plants;

[SerializationGenerator(0, false)]
public partial class PlantBowl : Item
{
    private static readonly int[] _dirtPatchLandTiles =
    {
        0x9, 0x15,
        0x71, 0x7C,
        0x82, 0xA7,
        0xDC, 0xE3,
        0xE8, 0xEB,
        0x141, 0x144,
        0x14C, 0x15C,
        0x169, 0x174,
        0x1DC, 0x1EF,
        0x272, 0x275,
        0x27E, 0x281,
        0x2D0, 0x2D7,
        0x2E5, 0x2FF,
        0x303, 0x31F,
        0x32C, 0x32F,
        0x33D, 0x340,
        0x345, 0x34C,
        0x355, 0x358,
        0x367, 0x36E,
        0x377, 0x37A,
        0x38D, 0x390,
        0x395, 0x39C,
        0x3A5, 0x3A8,
        0x3F6, 0x405,
        0x547, 0x54E,
        0x553, 0x556,
        0x597, 0x59E,
        0x623, 0x63A,
        0x6F3, 0x6FA,
        0x777, 0x791,
        0x79A, 0x7A9,
        0x7AE, 0x7B1,
        0x98C, 0x99F,
        0x9AC, 0x9BF,
        0x5B27, 0x5B3E,
        0x71F4, 0x71FB,
        0x72C9, 0x72CA
    };

    private static readonly int[] _dirtPatchStaticTiles =
    {
        0x1B27, 0x1B3E,
        0x31F4, 0x31FB,
        0x32C9, 0x32CA
    };

    [Constructible]
    public PlantBowl() : base(0x15FD)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1060834; // a plant bowl

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            return;
        }

        from.Target = new InternalTarget(this);
        from.SendLocalizedMessage(1061897); // Choose a patch of dirt to scoop up.
    }

    public static bool IsDirtPatch(object obj)
    {
        int tileID;
        int[] tiles;

        if (obj is Static staticObj && !staticObj.Movable)
        {
            tileID = staticObj.ItemID;
            tiles = _dirtPatchStaticTiles;
        }
        else if (obj is StaticTarget staticTarget)
        {
            tileID = staticTarget.ItemID;
            tiles = _dirtPatchStaticTiles;
        }
        else if (obj is LandTarget landTarget)
        {
            tileID = landTarget.TileID;
            tiles = _dirtPatchLandTiles;
        }
        else
        {
            return false;
        }

        var contains = false;

        for (var i = 0; !contains && i < tiles.Length; i += 2)
        {
            contains = tileID >= tiles[i] && tileID <= tiles[i + 1];
        }

        return contains;
    }

    private class InternalTarget : Target
    {
        private readonly PlantBowl _plantBowl;

        public InternalTarget(PlantBowl plantBowl) : base(3, true, TargetFlags.None) => _plantBowl = plantBowl;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_plantBowl.Deleted)
            {
                return;
            }

            if (!_plantBowl.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                return;
            }

            if (targeted is FertileDirt dirt)
            {
                var _dirtNeeded = Core.ML ? 20 : 40;

                if (!dirt.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                }
                else if (dirt.Amount < _dirtNeeded)
                {
                    // You need more dirt to fill a plant bowl!
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061896);
                }
                else
                {
                    var fullBowl = new PlantItem(true);

                    if (from.PlaceInBackpack(fullBowl))
                    {
                        dirt.Consume(_dirtNeeded);
                        _plantBowl.Delete();

                        // You fill the bowl with fresh dirt.
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061895);
                    }
                    else
                    {
                        fullBowl.Delete();

                        // There is no room in your backpack for a bowl full of dirt!
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061894);
                    }
                }
            }
            else if (IsDirtPatch(targeted))
            {
                var fullBowl = new PlantItem();

                if (from.PlaceInBackpack(fullBowl))
                {
                    _plantBowl.Delete();

                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061895); // You fill the bowl with fresh dirt.
                }
                else
                {
                    fullBowl.Delete();

                    // There is no room in your backpack for a bowl full of dirt!
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061894);
                }
            }
            else
            {
                // You'll want to gather fresh dirt in order to raise a healthy plant!
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061893);
            }
        }

        protected override void OnTargetOutOfRange(Mobile from, object targeted)
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502825); // That location is too far away
        }
    }
}
