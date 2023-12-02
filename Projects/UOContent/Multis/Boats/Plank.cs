using System;
using ModernUO.Serialization;
using Server.Factions;
using Server.Multis;
using Server.Spells;

namespace Server.Items;

public enum PlankSide
{
    Port,
    Starboard
}

[SerializationGenerator(1, false)]
public partial class Plank : Item, ILockable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseBoat _boat;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PlankSide _side;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _locked;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private uint _keyValue;

    private Timer _closeTimer;

    public Plank(BaseBoat boat, PlankSide side, uint keyValue) : base(0x3EB1 + (int)side)
    {
        Boat = boat;
        Side = side;
        KeyValue = keyValue;
        Locked = true;

        Movable = false;
    }

    public Plank(bool locked) => Locked = locked;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsOpen => ItemID is 0x3ED5 or 0x3ED4 or 0x3E84 or 0x3E89;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Starboard => Side == PlankSide.Starboard;

    private void Deserialize(IGenericReader reader, int version)
    {
        Boat = reader.ReadEntity<BaseBoat>();
        Side = (PlankSide)reader.ReadInt();
        Locked = reader.ReadBool();
        KeyValue = reader.ReadUInt();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Boat == null)
        {
            Timer.DelayCall(Delete);
            return;
        }

        if (IsOpen)
        {
            _closeTimer = new CloseTimer(this);
            _closeTimer.Start();
        }
    }

    public void SetFacing(Direction dir)
    {
        if (IsOpen)
        {
            ItemID = dir switch
            {
                Direction.North => Starboard ? 0x3ED4 : 0x3ED5,
                Direction.East  => Starboard ? 0x3E84 : 0x3E89,
                Direction.South => Starboard ? 0x3ED5 : 0x3ED4,
                Direction.West  => Starboard ? 0x3E89 : 0x3E84,
                _               => ItemID
            };
        }
        else
        {
            ItemID = dir switch
            {
                Direction.North => Starboard ? 0x3EB2 : 0x3EB1,
                Direction.East  => Starboard ? 0x3E85 : 0x3E8A,
                Direction.South => Starboard ? 0x3EB1 : 0x3EB2,
                Direction.West  => Starboard ? 0x3E8A : 0x3E85,
                _               => ItemID
            };
        }
    }

    public void Open()
    {
        if (IsOpen || Deleted)
        {
            return;
        }

        _closeTimer?.Stop();

        _closeTimer = new CloseTimer(this);
        _closeTimer.Start();

        ItemID = ItemID switch
        {
            0x3EB1 => 0x3ED5,
            0x3E8A => 0x3E89,
            0x3EB2 => 0x3ED4,
            0x3E85 => 0x3E84,
            _      => ItemID
        };

        Boat?.Refresh();
    }

    public override bool OnMoveOver(Mobile from)
    {
        if (IsOpen)
        {
            if (from is BaseFactionGuard)
            {
                return false;
            }

            if ((from.Direction & Direction.Running) != 0 || Boat?.Contains(from) == false)
            {
                return true;
            }

            var map = Map;

            if (map == null)
            {
                return false;
            }

            int rx = 0, ry = 0;

            if (ItemID == 0x3ED4)
            {
                rx = 1;
            }
            else if (ItemID == 0x3ED5)
            {
                rx = -1;
            }
            else if (ItemID == 0x3E84)
            {
                ry = 1;
            }
            else if (ItemID == 0x3E89)
            {
                ry = -1;
            }

            for (var i = 1; i <= 6; ++i)
            {
                var x = X + i * rx;
                var y = Y + i * ry;
                int z;

                for (var j = -8; j <= 8; ++j)
                {
                    z = from.Z + j;

                    if (map.CanFit(x, y, z, 16, false, false) && !SpellHelper.CheckMulti(new Point3D(x, y, z), map) &&
                        !Region.Find(new Point3D(x, y, z), map).IsPartOf<StrongholdRegion>())
                    {
                        if (i == 1 && j >= -2 && j <= 2)
                        {
                            return true;
                        }

                        from.Location = new Point3D(x, y, z);
                        return false;
                    }
                }

                z = map.GetAverageZ(x, y);

                if (map.CanFit(x, y, z, 16, false, false) && !SpellHelper.CheckMulti(new Point3D(x, y, z), map) &&
                    !Region.Find(new Point3D(x, y, z), map).IsPartOf<StrongholdRegion>())
                {
                    if (i == 1)
                    {
                        return true;
                    }

                    from.Location = new Point3D(x, y, z);
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public bool CanClose()
    {
        if (Map == null || Deleted)
        {
            return false;
        }

        foreach (var item in GetItemsAt())
        {
            if (item != this)
            {
                return false;
            }
        }

        foreach (var m in GetMobilesAt())
        {
            return false;
        }

        return true;
    }

    public void Close()
    {
        if (!IsOpen || !CanClose() || Deleted)
        {
            return;
        }

        _closeTimer?.Stop();

        _closeTimer = null;

        ItemID = ItemID switch
        {
            0x3ED5 => 0x3EB1,
            0x3E89 => 0x3E8A,
            0x3ED4 => 0x3EB2,
            0x3E84 => 0x3E85,
            _      => ItemID
        };

        Boat?.Refresh();
    }

    public override void OnDoubleClickDead(Mobile from)
    {
        OnDoubleClick(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Boat == null)
        {
            return;
        }

        if (from.InRange(GetWorldLocation(), 8))
        {
            if (Boat.Contains(from))
            {
                if (IsOpen)
                {
                    Close();
                }
                else
                {
                    Open();
                }
            }
            else
            {
                if (!IsOpen)
                {
                    if (!Locked)
                    {
                        Open();
                    }
                    else if (from.AccessLevel >= AccessLevel.GameMaster)
                    {
                        from.LocalOverheadMessage(
                            MessageType.Regular,
                            0x00,
                            502502
                        ); // That is locked but your godly powers allow access
                        Open();
                    }
                    else
                    {
                        from.LocalOverheadMessage(MessageType.Regular, 0x00, 502503); // That is locked.
                    }
                }
                else if (!Locked)
                {
                    from.Location = new Point3D(X, Y, Z + 3);
                }
                else if (from.AccessLevel >= AccessLevel.GameMaster)
                {
                    from.LocalOverheadMessage(
                        MessageType.Regular,
                        0x00,
                        502502
                    ); // That is locked but your godly powers allow access
                    from.Location = new Point3D(X, Y, Z + 3);
                }
                else
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x00, 502503); // That is locked.
                }
            }
        }
    }

    private class CloseTimer : Timer
    {
        private readonly Plank _plank;

        public CloseTimer(Plank plank) : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0)) => _plank = plank;

        protected override void OnTick() => _plank.Close();
    }
}
