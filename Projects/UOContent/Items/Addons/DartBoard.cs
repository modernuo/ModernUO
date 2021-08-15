using Server.Network;

namespace Server.Items
{
    [Serializable(0)]
    public partial class DartBoard : AddonComponent
    {
        [Constructible]
        public DartBoard(bool east = true) : base(east ? 0x1E2F : 0x1E2E)
        {
        }

        public override bool NeedsWall => true;
        public override Point3D WallPosition => East ? new Point3D(-1, 0, 0) : new Point3D(0, -1, 0);

        public bool East => ItemID == 0x1E2F;

        public override void OnDoubleClick(Mobile from)
        {
            Direction dir;
            if (from.Location != Location)
            {
                dir = from.GetDirectionTo(this);
            }
            else if (East)
            {
                dir = Direction.West;
            }
            else
            {
                dir = Direction.North;
            }

            from.Direction = dir;

            bool canThrow;

            if (!from.InRange(this, 4) || !from.InLOS(this))
            {
                canThrow = false;
            }
            else if (East)
            {
                canThrow = dir == Direction.Left || dir == Direction.West || dir == Direction.Up;
            }
            else
            {
                canThrow = dir == Direction.Up || dir == Direction.North || dir == Direction.Right;
            }

            if (canThrow)
            {
                Throw(from);
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public void Throw(Mobile from)
        {
            if (!(from.Weapon is BaseKnife knife))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500751); // Try holding a knife...
                return;
            }

            from.Animate(from.Mounted ? 26 : 9, 7, 1, true, false, 0);
            from.MovingEffect(this, knife.ItemID, 7, 1, false, false);
            from.PlaySound(0x238);

            var rand = Utility.RandomDouble();

            int message = rand switch
            {
                < 0.05 => 500752,
                < 0.20 => 500753,
                < 0.45 => 500754,
                < 0.70 => 500755,
                < 0.85 => 500756,
                _      => 500757
            };

            PublicOverheadMessage(MessageType.Regular, 0x3B2, message);
        }
    }

    [Serializable(0)]
    public partial class DartBoardEastAddon : BaseAddon
    {
        public DartBoardEastAddon()
        {
            AddComponent(new DartBoard(), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new DartBoardEastDeed();
    }

    [Serializable(0)]
    public partial class DartBoardEastDeed : BaseAddonDeed
    {
        [Constructible]
        public DartBoardEastDeed()
        {
        }

        public override BaseAddon Addon => new DartBoardEastAddon();

        public override int LabelNumber => 1044326; // dartboard (east)
    }

    [Serializable(0)]
    public partial class DartBoardSouthAddon : BaseAddon
    {
        public DartBoardSouthAddon()
        {
            AddComponent(new DartBoard(false), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new DartBoardSouthDeed();
    }

    [Serializable(0)]
    public partial class DartBoardSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public DartBoardSouthDeed()
        {
        }

        public override BaseAddon Addon => new DartBoardSouthAddon();

        public override int LabelNumber => 1044325; // dartboard (south)
    }
}
