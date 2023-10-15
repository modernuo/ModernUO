using System;
using Server.Factions;
using Server.Multis;
using Server.Spells;

namespace Server.Items
{
    public enum PlankSide
    {
        Port,
        Starboard
    }

    public class Plank : Item, ILockable
    {
        private Timer m_CloseTimer;

        public Plank(BaseBoat boat, PlankSide side, uint keyValue) : base(0x3EB1 + (int)side)
        {
            Boat = boat;
            Side = side;
            KeyValue = keyValue;
            Locked = true;

            Movable = false;
        }

        public Plank(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseBoat Boat { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlankSide Side { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsOpen => ItemID is 0x3ED5 or 0x3ED4 or 0x3E84 or 0x3E89;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Starboard => Side == PlankSide.Starboard;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Locked { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public uint KeyValue { get; set; }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Boat);
            writer.Write((int)Side);
            writer.Write(Locked);
            writer.Write(KeyValue);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Boat = reader.ReadEntity<BaseBoat>();
                        Side = (PlankSide)reader.ReadInt();
                        Locked = reader.ReadBool();
                        KeyValue = reader.ReadUInt();

                        if (Boat == null)
                        {
                            Delete();
                        }

                        break;
                    }
            }

            if (IsOpen)
            {
                m_CloseTimer = new CloseTimer(this);
                m_CloseTimer.Start();
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

            m_CloseTimer?.Stop();

            m_CloseTimer = new CloseTimer(this);
            m_CloseTimer.Start();

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

            var eable = GetObjectsInRange(0);

            foreach (var obj in eable)
            {
                if (obj == this)
                {
                    return true;
                }
            }

            return false;
        }

        public void Close()
        {
            if (!IsOpen || !CanClose() || Deleted)
            {
                return;
            }

            m_CloseTimer?.Stop();

            m_CloseTimer = null;

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
            private readonly Plank m_Plank;

            public CloseTimer(Plank plank) : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
            {
                m_Plank = plank;
            }

            protected override void OnTick()
            {
                m_Plank.Close();
            }
        }
    }
}
