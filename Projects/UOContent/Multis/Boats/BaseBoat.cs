using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Engines.Spawners;
using Server.Items;
using Server.Multis.Boats;

namespace Server.Multis
{
    public enum BoatOrder
    {
        Move,
        Course,
        Single
    }

    public abstract class BaseBoat : BaseMulti
    {
        public enum DryDockResult
        {
            Valid,
            Dead,
            NoKey,
            NotAnchored,
            Mobiles,
            Items,
            Hold,
            Decaying
        }

        private static Rectangle2D[] m_BritWrap =
            { new(16, 16, 5120 - 32, 4096 - 32), new(5136, 2320, 992, 1760) };

        private static Rectangle2D[] m_IlshWrap = { new(16, 16, 2304 - 32, 1600 - 32) };
        private static Rectangle2D[] m_TokunoWrap = { new(16, 16, 1448 - 32, 1448 - 32) };

        private static TimeSpan BoatDecayDelay = TimeSpan.FromDays(9.0);

        private static TimeSpan SlowInterval = TimeSpan.FromSeconds(NewBoatMovement ? 0.50 : 0.75);
        private static TimeSpan FastInterval = TimeSpan.FromSeconds(NewBoatMovement ? 0.25 : 0.75);

        private const int SlowSpeed = 1;
        private static int FastSpeed = NewBoatMovement ? 1 : 3;

        private static TimeSpan SlowDriftInterval = TimeSpan.FromSeconds(NewBoatMovement ? 0.50 : 1.50);
        private static TimeSpan FastDriftInterval = TimeSpan.FromSeconds(NewBoatMovement ? 0.25 : 0.75);

        private const int SlowDriftSpeed = 1;
        private const int FastDriftSpeed = 1;

        private const Direction Forward = Direction.North;
        private const Direction ForwardLeft = Direction.Up;
        private const Direction ForwardRight = Direction.Right;
        private const Direction Backward = Direction.South;
        private const Direction BackwardLeft = Direction.Left;
        private const Direction BackwardRight = Direction.Down;
        private const Direction Left = Direction.West;
        private const Direction Right = Direction.East;
        private const Direction Port = Left;
        private const Direction Starboard = Right;

        private int m_ClientSpeed;

        private bool m_Decaying;

        private DateTime m_DecayTime;

        private Direction m_Facing;

        private string m_ShipName;


        private TurnTimer _turnTimer;
        private MoveTimer _moveTimer;

        public BaseBoat() : base(0x0)
        {
            m_DecayTime = Core.Now + BoatDecayDelay;

            TillerMan = new TillerMan(this);
            Hold = new Hold(this);

            PPlank = new Plank(this, PlankSide.Port, 0);
            SPlank = new Plank(this, PlankSide.Starboard, 0);

            PPlank.MoveToWorld(new Point3D(X + PortOffset.X, Y + PortOffset.Y, Z), Map);
            SPlank.MoveToWorld(new Point3D(X + StarboardOffset.X, Y + StarboardOffset.Y, Z), Map);

            Facing = Direction.North;

            NextNavPoint = -1;

            Movable = false;

            Boats.Add(this);
        }

        public BaseBoat(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Hold Hold { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TillerMan TillerMan { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Plank PPlank { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Plank SPlank { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Facing
        {
            get => m_Facing;
            set => SetFacing(value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Moving { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsMoving => _moveTimer?.Running == true;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Speed { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Anchored { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ShipName
        {
            get => m_ShipName;
            set
            {
                m_ShipName = value;
                TillerMan?.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BoatOrder Order { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public MapItem MapItem { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NextNavPoint { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TimeOfDecay
        {
            get => m_DecayTime;
            set
            {
                m_DecayTime = value;
                TillerMan?.InvalidateProperties();
            }
        }

        public int Status
        {
            get
            {
                var start = Core.Now - TimeOfDecay - BoatDecayDelay;

                if (start < TimeSpan.FromHours(1.0))
                {
                    return 1043010; // This structure is like new.
                }

                if (start < TimeSpan.FromDays(2.0))
                {
                    return 1043011; // This structure is slightly worn.
                }

                if (start < TimeSpan.FromDays(3.0))
                {
                    return 1043012; // This structure is somewhat worn.
                }

                if (start < TimeSpan.FromDays(4.0))
                {
                    return 1043013; // This structure is fairly worn.
                }

                if (start < TimeSpan.FromDays(5.0))
                {
                    return 1043014; // This structure is greatly worn.
                }

                return 1043015; // This structure is in danger of collapsing.
            }
        }

        public virtual int NorthID => 0;
        public virtual int EastID => 0;
        public virtual int SouthID => 0;
        public virtual int WestID => 0;

        public virtual int HoldDistance => 0;
        public virtual int TillerManDistance => 0;
        public virtual Point2D StarboardOffset => Point2D.Zero;
        public virtual Point2D PortOffset => Point2D.Zero;
        public virtual Point3D MarkOffset => Point3D.Zero;

        public virtual BaseDockedBoat DockedBoat => null;

        public static List<BaseBoat> Boats { get; } = new();

        /*
         * Intervals:
         *       drift forward
         * fast | 0.25|   0.25
         * slow | 0.50|   0.50
         *
         * Speed:
         *       drift forward
         * fast |  0x4|    0x4
         * slow |  0x3|    0x3
         *
         * Tiles (per interval):
         *       drift forward
         * fast |    1|      1
         * slow |    1|      1
         *
         * 'walking' in piloting mode has a 1s interval, speed 0x2
         */

        private static bool NewBoatMovement => Core.HS;

        public override bool HandlesOnSpeech => true;

        public override bool AllowsRelativeDrop => true;

        public static BaseBoat FindBoatAt(Point3D loc, Map map)
        {
            var sector = map.GetSector(loc);

            for (var i = 0; i < sector.Multis.Count; i++)
            {
                if (sector.Multis[i] is BaseBoat boat && boat.Contains(loc.X, loc.Y))
                {
                    return boat;
                }
            }

            return null;
        }

        public Point3D GetRotatedLocation(int x, int y)
        {
            var p = new Point3D(X + x, Y + y, Z);

            return Rotate(p, (int)m_Facing / 2);
        }

        public void UpdateComponents()
        {
            if (PPlank != null)
            {
                PPlank.MoveToWorld(GetRotatedLocation(PortOffset.X, PortOffset.Y), Map);
                PPlank.SetFacing(m_Facing);
            }

            if (SPlank != null)
            {
                SPlank.MoveToWorld(GetRotatedLocation(StarboardOffset.X, StarboardOffset.Y), Map);
                SPlank.SetFacing(m_Facing);
            }

            int xOffset = 0, yOffset = 0;
            Movement.Movement.Offset(m_Facing, ref xOffset, ref yOffset);

            if (TillerMan != null)
            {
                TillerMan.Location = new Point3D(
                    X + xOffset * TillerManDistance + (m_Facing == Direction.North ? 1 : 0),
                    Y + yOffset * TillerManDistance,
                    TillerMan.Z
                );
                TillerMan.SetFacing(m_Facing);
                TillerMan.InvalidateProperties();
            }

            if (Hold != null)
            {
                Hold.Location = new Point3D(X + xOffset * HoldDistance, Y + yOffset * HoldDistance, Hold.Z);
                Hold.SetFacing(m_Facing);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3);

            writer.Write(MapItem);
            writer.Write(NextNavPoint);

            writer.Write((int)m_Facing);

            writer.WriteDeltaTime(m_DecayTime);

            writer.Write(Owner);
            writer.Write(PPlank);
            writer.Write(SPlank);
            writer.Write(TillerMan);
            writer.Write(Hold);
            writer.Write(Anchored);
            writer.Write(m_ShipName);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        MapItem = (MapItem)reader.ReadEntity<Item>();
                        NextNavPoint = reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Facing = (Direction)reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_DecayTime = reader.ReadDeltaTime();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 3)
                        {
                            NextNavPoint = -1;
                        }

                        if (version < 2)
                        {
                            if (ItemID == NorthID)
                            {
                                m_Facing = Direction.North;
                            }
                            else if (ItemID == SouthID)
                            {
                                m_Facing = Direction.South;
                            }
                            else if (ItemID == EastID)
                            {
                                m_Facing = Direction.East;
                            }
                            else if (ItemID == WestID)
                            {
                                m_Facing = Direction.West;
                            }
                        }

                        Owner = reader.ReadEntity<Mobile>();
                        PPlank = reader.ReadEntity<Plank>();
                        SPlank = reader.ReadEntity<Plank>();
                        TillerMan = reader.ReadEntity<TillerMan>();
                        Hold = reader.ReadEntity<Hold>();
                        Anchored = reader.ReadBool();
                        m_ShipName = reader.ReadString();

                        if (version < 1)
                        {
                            Refresh();
                        }

                        break;
                    }
            }

            Boats.Add(this);

            Timer.DelayCall(() => CheckDecay());
        }

        public void RemoveKeys(Mobile m)
        {
            uint keyValue = 0;

            if (PPlank != null)
            {
                keyValue = PPlank.KeyValue;
            }

            if (keyValue == 0 && SPlank != null)
            {
                keyValue = SPlank.KeyValue;
            }

            Key.RemoveKeys(m, keyValue);
        }

        public uint CreateKeys(Mobile m)
        {
            var value = Key.RandomValue();

            var packKey = new Key(KeyType.Gold, value, this);
            var bankKey = new Key(KeyType.Gold, value, this);

            packKey.MaxRange = 10;
            bankKey.MaxRange = 10;

            packKey.Name = "a ship key";
            bankKey.Name = "a ship key";

            var box = m.BankBox;

            if (!box.TryDropItem(m, bankKey, false))
            {
                bankKey.Delete();
            }
            else
            {
                m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502484); // A ship's key is now in my safety deposit box.
            }

            if (m.AddToBackpack(packKey))
            {
                m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502485); // A ship's key is now in my backpack.
            }
            else
            {
                m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502483); // A ship's key is now at my feet.
            }

            return value;
        }

        public override void OnAfterDelete()
        {
            TillerMan?.Delete();
            Hold?.Delete();
            PPlank?.Delete();
            SPlank?.Delete();
            _turnTimer?.Stop();
            _moveTimer?.Stop();

            Boats.Remove(this);
        }

        public override void OnLocationChange(Point3D old)
        {
            if (TillerMan != null)
            {
                TillerMan.Location = new Point3D(
                    X + (TillerMan.X - old.X),
                    Y + (TillerMan.Y - old.Y),
                    Z + (TillerMan.Z - old.Z)
                );
            }

            if (Hold != null)
            {
                Hold.Location = new Point3D(X + (Hold.X - old.X), Y + (Hold.Y - old.Y), Z + (Hold.Z - old.Z));
            }

            if (PPlank != null)
            {
                PPlank.Location = new Point3D(X + (PPlank.X - old.X), Y + (PPlank.Y - old.Y), Z + (PPlank.Z - old.Z));
            }

            if (SPlank != null)
            {
                SPlank.Location = new Point3D(X + (SPlank.X - old.X), Y + (SPlank.Y - old.Y), Z + (SPlank.Z - old.Z));
            }
        }

        public override void OnMapChange()
        {
            if (TillerMan != null)
            {
                TillerMan.Map = Map;
            }

            if (Hold != null)
            {
                Hold.Map = Map;
            }

            if (PPlank != null)
            {
                PPlank.Map = Map;
            }

            if (SPlank != null)
            {
                SPlank.Map = Map;
            }
        }

        public bool CanCommand(Mobile m) => true;

        public Point3D GetMarkedLocation()
        {
            var p = new Point3D(X + MarkOffset.X, Y + MarkOffset.Y, Z + MarkOffset.Z);

            return Rotate(p, (int)m_Facing / 2);
        }

        public bool CheckKey(uint keyValue) => SPlank?.KeyValue == keyValue || PPlank?.KeyValue == keyValue;

        public void Refresh()
        {
            m_DecayTime = Core.Now + BoatDecayDelay;

            TillerMan?.InvalidateProperties();
        }

        private bool ShouldDecay => !IsMoving && Core.Now >= m_DecayTime;

        public bool CheckDecay()
        {
            if (m_Decaying)
            {
                return true;
            }

            if (ShouldDecay)
            {
                new DecayTimer(this).Start();
                m_Decaying = true;
                return true;
            }

            return false;
        }

        public bool LowerAnchor(bool message)
        {
            if (CheckDecay())
            {
                return false;
            }

            if (Anchored)
            {
                if (message)
                {
                    TillerMan?.Say(501445); // Ar, the anchor was already dropped sir.
                }

                return false;
            }

            StopMove(false);

            Anchored = true;

            if (message)
            {
                TillerMan?.Say(501444); // Ar, anchor dropped sir.
            }

            return true;
        }

        public bool RaiseAnchor(bool message)
        {
            if (CheckDecay())
            {
                return false;
            }

            if (!Anchored)
            {
                if (message)
                {
                    TillerMan?.Say(501447); // Ar, the anchor has not been dropped sir.
                }

                return false;
            }

            Anchored = false;

            if (message)
            {
                TillerMan?.Say(501446); // Ar, anchor raised sir.
            }

            return true;
        }

        public bool StartMove(Direction dir, bool fast)
        {
            if (CheckDecay())
            {
                return false;
            }

            var drift = dir != Forward && dir != ForwardLeft && dir != ForwardRight;
            var interval = fast ? drift ? FastDriftInterval : FastInterval :
                drift ? SlowDriftInterval : SlowInterval;
            var speed = fast ? drift ? FastDriftSpeed : FastSpeed :
                drift ? SlowDriftSpeed : SlowSpeed;
            var clientSpeed = fast ? 0x4 : 0x3;

            if (StartMove(dir, speed, clientSpeed, interval, false, true))
            {
                TillerMan?.Say(501429); // Aye aye sir.

                return true;
            }

            return false;
        }

        public bool OneMove(Direction dir)
        {
            if (CheckDecay())
            {
                return false;
            }

            var drift = dir != Forward;
            var interval = drift ? FastDriftInterval : FastInterval;
            var speed = drift ? FastDriftSpeed : FastSpeed;

            if (StartMove(dir, speed, 0x1, interval, true, true))
            {
                TillerMan?.Say(501429); // Aye aye sir.

                return true;
            }

            return false;
        }

        public void BeginRename(Mobile from)
        {
            if (CheckDecay())
            {
                return;
            }

            if (from.AccessLevel < AccessLevel.GameMaster && from != Owner)
            {
                TillerMan?.Say(
                    Utility.Random(
                        1042876,
                        4
                    )
                ); // Arr, don't do that! | Arr, leave me alone! | Arr, watch what thour'rt doing, matey! | Arr! Do that again and I’ll throw ye overhead!

                return;
            }

            TillerMan?.Say(502580); // What dost thou wish to name thy ship?

            from.Prompt = new RenameBoatPrompt(this);
        }

        public void EndRename(Mobile from, string newName)
        {
            if (Deleted || CheckDecay())
            {
                return;
            }

            if (from.AccessLevel < AccessLevel.GameMaster && from != Owner)
            {
                TillerMan?.Say(1042880); // Arr! Only the owner of the ship may change its name!

                return;
            }

            if (!from.Alive)
            {
                TillerMan?.Say(502582); // You appear to be dead.

                return;
            }

            Rename(newName.Trim().DefaultIfNullOrEmpty(null));
        }

        public DryDockResult CheckDryDock(Mobile from)
        {
            if (CheckDecay())
            {
                return DryDockResult.Decaying;
            }

            if (!from.Alive)
            {
                return DryDockResult.Dead;
            }

            var pack = from.Backpack;
            if ((SPlank == null || !Key.ContainsKey(pack, SPlank.KeyValue)) &&
                (PPlank == null || !Key.ContainsKey(pack, PPlank.KeyValue)))
            {
                return DryDockResult.NoKey;
            }

            if (!Anchored)
            {
                return DryDockResult.NotAnchored;
            }

            if (Hold?.Items.Count > 0)
            {
                return DryDockResult.Hold;
            }

            var map = Map;

            if (map == null || map == Map.Internal)
            {
                return DryDockResult.Items;
            }

            using var ents = GetMovingEntities();
            var enumerator = ents.GetEnumerator();

            if (enumerator.MoveNext())
            {
                return enumerator.Current is Mobile ? DryDockResult.Mobiles : DryDockResult.Items;
            }

            return DryDockResult.Valid;
        }

        public void BeginDryDock(Mobile from)
        {
            if (CheckDecay())
            {
                return;
            }

            var result = CheckDryDock(from);

            if (result == DryDockResult.Dead)
            {
                from.SendLocalizedMessage(502493); // You appear to be dead.
            }
            else if (result == DryDockResult.NoKey)
            {
                from.SendLocalizedMessage(502494); // You must have a key to the ship to dock the boat.
            }
            else if (result == DryDockResult.NotAnchored)
            {
                from.SendLocalizedMessage(1010570); // You must lower the anchor to dock the boat.
            }
            else if (result == DryDockResult.Mobiles)
            {
                from.SendLocalizedMessage(502495); // You cannot dock the ship with beings on board!
            }
            else if (result == DryDockResult.Items)
            {
                from.SendLocalizedMessage(502496); // You cannot dock the ship with a cluttered deck.
            }
            else if (result == DryDockResult.Hold)
            {
                from.SendLocalizedMessage(502497); // Make sure your hold is empty, and try again!
            }
            else if (result == DryDockResult.Valid)
            {
                from.SendGump(new ConfirmDryDockGump(from, this));
            }
        }

        public void EndDryDock(Mobile from)
        {
            if (Deleted || CheckDecay())
            {
                return;
            }

            var result = CheckDryDock(from);

            if (result == DryDockResult.Dead)
            {
                from.SendLocalizedMessage(502493); // You appear to be dead.
            }
            else if (result == DryDockResult.NoKey)
            {
                from.SendLocalizedMessage(502494); // You must have a key to the ship to dock the boat.
            }
            else if (result == DryDockResult.NotAnchored)
            {
                from.SendLocalizedMessage(1010570); // You must lower the anchor to dock the boat.
            }
            else if (result == DryDockResult.Mobiles)
            {
                from.SendLocalizedMessage(502495); // You cannot dock the ship with beings on board!
            }
            else if (result == DryDockResult.Items)
            {
                from.SendLocalizedMessage(502496); // You cannot dock the ship with a cluttered deck.
            }
            else if (result == DryDockResult.Hold)
            {
                from.SendLocalizedMessage(502497); // Make sure your hold is empty, and try again!
            }

            if (result != DryDockResult.Valid)
            {
                return;
            }

            var boat = DockedBoat;

            if (boat == null)
            {
                return;
            }

            RemoveKeys(from);

            from.AddToBackpack(boat);
            Delete();
        }

        public void SetName(SpeechEventArgs e)
        {
            if (CheckDecay())
            {
                return;
            }

            if (e.Mobile.AccessLevel < AccessLevel.GameMaster && e.Mobile != Owner)
            {
                TillerMan?.Say(1042880); // Arr! Only the owner of the ship may change its name!

                return;
            }

            if (!e.Mobile.Alive)
            {
                TillerMan?.Say(502582); // You appear to be dead.

                return;
            }

            if (e.Speech.Length > 8)
            {
                Rename(e.Speech[8..].Trim().DefaultIfNullOrEmpty(null));
            }
        }

        public void Rename(string newName)
        {
            if (CheckDecay())
            {
                return;
            }

            if (newName?.Length > 40)
            {
                newName = newName[..40];
            }

            if (m_ShipName == newName)
            {
                TillerMan?.Say(502531); // Yes, sir.

                return;
            }

            ShipName = newName;

            if (TillerMan != null && m_ShipName != null)
            {
                TillerMan.Say(1042885, m_ShipName); // This ship is now called the ~1_NEW_SHIP_NAME~.
            }
            else
            {
                TillerMan?.Say(502534); // This ship now has no name.
            }
        }

        public void RemoveName(Mobile m)
        {
            if (CheckDecay())
            {
                return;
            }

            if (m.AccessLevel < AccessLevel.GameMaster && m != Owner)
            {
                TillerMan?.Say(1042880); // Arr! Only the owner of the ship may change its name!

                return;
            }

            if (!m.Alive)
            {
                TillerMan?.Say(502582); // You appear to be dead.

                return;
            }

            if (m_ShipName == null)
            {
                TillerMan?.Say(502526); // Ar, this ship has no name.

                return;
            }

            ShipName = null;

            TillerMan?.Say(502534); // This ship now has no name.
        }

        public void GiveName(Mobile m)
        {
            if (TillerMan == null || CheckDecay())
            {
                return;
            }

            if (m_ShipName == null)
            {
                TillerMan.Say(502526); // Ar, this ship has no name.
            }
            else
            {
                TillerMan.Say(1042881, m_ShipName); // This is the ~1_BOAT_NAME~.
            }
        }

        public void GiveNavPoint()
        {
            if (TillerMan == null || CheckDecay())
            {
                return;
            }

            if (NextNavPoint < 0)
            {
                TillerMan.Say(1042882); // I have no current nav point.
            }
            else
            {
                TillerMan.Say(
                    1042883,
                    (NextNavPoint + 1).ToString()
                ); // My current destination navpoint is nav ~1_NAV_POINT_NUM~.
            }
        }

        public void AssociateMap(MapItem map)
        {
            if (CheckDecay())
            {
                return;
            }

            if (map is BlankMap)
            {
                TillerMan?.Say(502575); // Ar, that is not a map, tis but a blank piece of paper!
            }
            else if (map.Pins.Count == 0)
            {
                TillerMan?.Say(502576); // Arrrr, this map has no course on it!
            }
            else
            {
                StopMove(false);

                MapItem = map;
                NextNavPoint = -1;

                TillerMan?.Say(502577); // A map!
            }
        }

        public bool StartCourse(string navPoint, bool single, bool message)
        {
            var number = -1;

            var start = -1;
            for (var i = 0; i < navPoint.Length; i++)
            {
                if (char.IsDigit(navPoint[i]))
                {
                    start = i;
                    break;
                }
            }

            if (start != -1)
            {
                var sNumber = navPoint[start..];

                if (!int.TryParse(sNumber, out number))
                {
                    number = -1;
                }

                if (number != -1)
                {
                    number--;

                    if (MapItem == null || number < 0 || number >= MapItem.Pins.Count)
                    {
                        number = -1;
                    }
                }
            }

            if (number == -1)
            {
                if (message)
                {
                    TillerMan?.Say(1042551); // I don't see that navpoint, sir.
                }

                return false;
            }

            NextNavPoint = number;
            return StartCourse(single, message);
        }

        public bool StartCourse(bool single, bool message)
        {
            if (CheckDecay())
            {
                return false;
            }

            if (Anchored)
            {
                if (message)
                {
                    TillerMan?.Say(501419); // Ar, the anchor is down sir!
                }

                return false;
            }

            if (MapItem?.Deleted != false)
            {
                if (message)
                {
                    TillerMan?.Say(502513); // I have seen no map, sir.
                }

                return false;
            }

            if (Map != MapItem.Map || !Contains(MapItem.GetWorldLocation()))
            {
                if (message)
                {
                    TillerMan?.Say(502514); // The map is too far away from me, sir.
                }

                return false;
            }

            if (Map != Map.Trammel && Map != Map.Felucca || NextNavPoint < 0 || NextNavPoint >= MapItem.Pins.Count)
            {
                if (message)
                {
                    TillerMan?.Say(1042551); // I don't see that navpoint, sir.
                }

                return false;
            }

            Speed = FastSpeed;
            Order = single ? BoatOrder.Single : BoatOrder.Course;

            if (!SafelyStartMoveTimer(FastInterval, single))
            {
                return false;
            }

            if (message)
            {
                TillerMan?.Say(501429); // Aye aye sir.
            }

            return true;
        }

        private void StopBoat()
        {
            if (!DoMovement(true))
            {
                StopMove(false);
            }
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (CheckDecay())
            {
                return;
            }

            var from = e.Mobile;

            if (CanCommand(from) && Contains(from))
            {
                for (var i = 0; i < e.Keywords.Length; ++i)
                {
                    var keyword = e.Keywords[i];

                    if (keyword >= 0x42 && keyword <= 0x6B)
                    {
                        switch (keyword)
                        {
                            case 0x42:
                                SetName(e);
                                break;
                            case 0x43:
                                RemoveName(e.Mobile);
                                break;
                            case 0x44:
                                GiveName(e.Mobile);
                                break;
                            case 0x45:
                                StartMove(Forward, true);
                                break;
                            case 0x46:
                                StartMove(Backward, true);
                                break;
                            case 0x47:
                                StartMove(Left, true);
                                break;
                            case 0x48:
                                StartMove(Right, true);
                                break;
                            case 0x4B:
                                StartMove(ForwardLeft, true);
                                break;
                            case 0x4C:
                                StartMove(ForwardRight, true);
                                break;
                            case 0x4D:
                                StartMove(BackwardLeft, true);
                                break;
                            case 0x4E:
                                StartMove(BackwardRight, true);
                                break;
                            case 0x4F:
                                StopMove(true);
                                break;
                            case 0x50:
                                StartMove(Left, false);
                                break;
                            case 0x51:
                                StartMove(Right, false);
                                break;
                            case 0x52:
                                StartMove(Forward, false);
                                break;
                            case 0x53:
                                StartMove(Backward, false);
                                break;
                            case 0x54:
                                StartMove(ForwardLeft, false);
                                break;
                            case 0x55:
                                StartMove(ForwardRight, false);
                                break;
                            case 0x56:
                                StartMove(BackwardRight, false);
                                break;
                            case 0x57:
                                StartMove(BackwardLeft, false);
                                break;
                            case 0x58:
                                OneMove(Left);
                                break;
                            case 0x59:
                                OneMove(Right);
                                break;
                            case 0x5A:
                                OneMove(Forward);
                                break;
                            case 0x5B:
                                OneMove(Backward);
                                break;
                            case 0x5C:
                                OneMove(ForwardLeft);
                                break;
                            case 0x5D:
                                OneMove(ForwardRight);
                                break;
                            case 0x5E:
                                OneMove(BackwardRight);
                                break;
                            case 0x5F:
                                OneMove(BackwardLeft);
                                break;
                            case 0x49:
                            case 0x65:
                                StartTurn(2, true);
                                break; // turn right
                            case 0x4A:
                            case 0x66:
                                StartTurn(-2, true);
                                break; // turn left
                            case 0x67:
                                StartTurn(-4, true);
                                break; // turn around, come about
                            case 0x68:
                                StartMove(Forward, true);
                                break;
                            case 0x69:
                                StopMove(true);
                                break;
                            case 0x6A:
                                LowerAnchor(true);
                                break;
                            case 0x6B:
                                RaiseAnchor(true);
                                break;
                            case 0x60:
                                GiveNavPoint();
                                break; // nav
                            case 0x61:
                                NextNavPoint = 0;
                                StartCourse(false, true);
                                break; // start
                            case 0x62:
                                StartCourse(false, true);
                                break; // continue
                            case 0x63:
                                StartCourse(e.Speech, false, true);
                                break; // goto*
                            case 0x64:
                                StartCourse(e.Speech, true, true);
                                break; // single*
                        }

                        break;
                    }
                }
            }
        }

        public bool StartTurn(int offset, bool message)
        {
            if (CheckDecay())
            {
                return false;
            }

            if (Anchored)
            {
                if (message)
                {
                    TillerMan.Say(501419); // Ar, the anchor is down sir!
                }

                return false;
            }

            if (Order != BoatOrder.Move)
            {
                _turnTimer?.Stop();
            }

            _turnTimer ??= new TurnTimer(offset, this, TimeSpan.FromMilliseconds(500), message);

            if (_turnTimer.Running && _turnTimer._turn == offset)
            {
                // Dont let them spin around in circles like crazy
                return true;
            }

            // Have they issued a new command too soon? Ignore it if so
            var turnTimerNext = (_turnTimer.Next - Core.Now).TotalMilliseconds;
            if (turnTimerNext is < 0 and > -250)
            {
                return true;
            }

            _turnTimer._turn = offset;
            _turnTimer.Stop();
            _turnTimer.Start();

            if (message)
            {
                TillerMan?.Say(501429); // Aye aye sir.
            }

            return true;
        }

        public bool Turn(int offset, bool message = true)
        {
            _turnTimer?.Stop();

            if (CheckDecay())
            {
                return false;
            }

            if (Anchored)
            {
                if (message)
                {
                    TillerMan.Say(501419); // Ar, the anchor is down sir!
                }

                return false;
            }

            if (SetFacing((Direction)(((int)m_Facing + offset) & 0x7)))
            {
                return true;
            }

            if (message)
            {
                TillerMan.Say(501423); // Ar, can't turn sir.
            }

            return false;
        }

        public bool StartMove(Direction dir, int speed, int clientSpeed, TimeSpan interval, bool single, bool message)
        {
            if (CheckDecay())
            {
                return false;
            }

            if (Anchored)
            {
                if (message)
                {
                    TillerMan?.Say(501419); // Ar, the anchor is down sir!
                }

                return false;
            }

            Moving = dir;
            Speed = speed;
            m_ClientSpeed = clientSpeed;
            Order = BoatOrder.Move;

            SafelyStartMoveTimer(interval, single);

            return true;
        }

        private bool SafelyStartMoveTimer(TimeSpan interval, bool single)
        {
            var singleNum = single ? 1 : 0;

            if (_moveTimer != null)
            {
                // Do not allow them to travel faster simply by respamming the command
                if (_moveTimer.Running && _moveTimer.Interval == interval && _moveTimer.Count == singleNum)
                {
                    return false;
                }

                // Have they issued a new command too soon? Ignore it if so
                var moverTimerNext = (_moveTimer.Next - Core.Now).TotalMilliseconds;
                if (moverTimerNext is < 0 and > -500)
                {
                    return false;
                }

                // Changing the interval or flipping between commands: "Forward" and "Forward One"
                TimeSpan delay = TimeSpan.Zero;
                if (interval.TotalMilliseconds > Math.Abs(moverTimerNext))
                {
                    var addedDelay = (int)(interval.TotalMilliseconds - Math.Abs(moverTimerNext));
                    delay = TimeSpan.FromMilliseconds(addedDelay);
                }

                _moveTimer.Stop();

                // We can reuse the timer if it's not a single count
                if (_moveTimer.Count == 0 && singleNum == 0)
                {
                    _moveTimer.Delay = delay;
                    _moveTimer.Interval = interval;
                }
                else
                {
                    _moveTimer = new MoveTimer(this, delay, interval, singleNum);
                }
            }
            else
            {
                _moveTimer = new MoveTimer(this, interval, interval, singleNum);
            }

            _moveTimer.Start();
            return true;
        }

        public bool StopMove(bool message)
        {
            if (CheckDecay())
            {
                return false;
            }

            if (_moveTimer?.Running == false)
            {
                if (message)
                {
                    TillerMan?.Say(501443); // Er, the ship is not moving sir.
                }

                return false;
            }

            Moving = Direction.North;
            Speed = 0;
            m_ClientSpeed = 0;
            _moveTimer?.Stop();

            if (message)
            {
                TillerMan?.Say(501429); // Aye aye sir.
            }

            return true;
        }

        public bool CanFit(Point3D p, Map map, int itemID)
        {
            if (map == null || map == Map.Internal || Deleted || CheckDecay())
            {
                return false;
            }

            var newComponents = MultiData.GetComponents(itemID);

            for (var x = 0; x < newComponents.Width; ++x)
            {
                for (var y = 0; y < newComponents.Height; ++y)
                {
                    var tx = p.X + newComponents.Min.X + x;
                    var ty = p.Y + newComponents.Min.Y + y;

                    if (newComponents.Tiles[x][y].Length == 0 || Contains(tx, ty))
                    {
                        continue;
                    }

                    var landTile = map.Tiles.GetLandTile(tx, ty);
                    var tiles = map.Tiles.GetStaticTiles(tx, ty, true);

                    var hasWater = landTile.Z == p.Z &&
                                   (landTile.ID >= 168 && landTile.ID <= 171 || landTile.ID >= 310 && landTile.ID <= 311);

                    // int z = p.Z;

                    // int landZ = 0, landAvg = 0, landTop = 0;

                    // map.GetAverageZ( tx, ty, ref landZ, ref landAvg, ref landTop );

                    // if (!landTile.Ignored && top > landZ && landTop > z)
                    // return false;

                    for (var i = 0; i < tiles.Length; ++i)
                    {
                        var tile = tiles[i];
                        var isWater = tile.ID >= 0x1796 && tile.ID <= 0x17B2;

                        if (tile.Z == p.Z && isWater)
                        {
                            hasWater = true;
                        }
                        else if (tile.Z >= p.Z && !isWater)
                        {
                            return false;
                        }
                    }

                    if (!hasWater)
                    {
                        return false;
                    }
                }
            }

            var bounds = new Rectangle2D(
                p.X + newComponents.Min.X,
                p.Y + newComponents.Min.Y,
                newComponents.Width,
                newComponents.Height
            );

            foreach (var item in map.GetItemsInBounds(bounds))
            {
                if (item is BaseMulti || item.ItemID > TileData.MaxItemValue || item.Z < p.Z || !item.Visible)
                {
                    continue;
                }

                var x = item.X - p.X + newComponents.Min.X;
                var y = item.Y - p.Y + newComponents.Min.Y;

                // Out of bounds, return false - cannot fit
                if ((x < 0 || x >= newComponents.Width || y < 0 || y >= newComponents.Height ||
                     newComponents.Tiles[x][y].Length != 0) && !Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        public Point3D Rotate(Point3D p, int count)
        {
            var rx = p.X - Location.X;
            var ry = p.Y - Location.Y;

            for (var i = 0; i < count; i++)
            {
                var temp = rx;
                rx = -ry;
                ry = temp;
            }

            return new Point3D(Location.X + rx, Location.Y + ry, p.Z);
        }

        public override bool Contains(int x, int y) =>
            base.Contains(x, y) ||
            TillerMan?.X == x && y == TillerMan.Y ||
            Hold?.X == x && Hold.Y == y ||
            PPlank?.X == x && PPlank.Y == y ||
            SPlank?.X == x && SPlank.Y == y;

        public static bool IsValidLocation(Point3D p, Map map)
        {
            var wrap = GetWrapFor(map);

            for (var i = 0; i < wrap.Length; ++i)
            {
                if (wrap[i].Contains(p))
                {
                    return true;
                }
            }

            return false;
        }

        public static Rectangle2D[] GetWrapFor(Map m) => m == Map.Ilshenar ? m_IlshWrap :
            m == Map.Tokuno ? m_TokunoWrap : m_BritWrap;

        public Direction GetMovementFor(int x, int y, out int maxSpeed)
        {
            var dx = x - X;
            var dy = y - Y;

            var adx = dx.Abs();
            var ady = dy.Abs();

            var dir = Utility.GetDirection(Location.X, Location.Y, x, y);
            var iDir = (int)dir;

            // Compute the maximum distance we can travel without going too far away
            if (iDir % 2 == 0) // North, East, South and West
            {
                maxSpeed = (adx - ady).Abs();
            }
            else // Right, Down, Left and Up
            {
                maxSpeed = Math.Min(adx, ady);
            }

            return (Direction)((iDir - (int)Facing) & 0x7);
        }

        public bool DoMovement(bool message)
        {
            Direction dir;
            int speed, clientSpeed;

            if (Order == BoatOrder.Move)
            {
                dir = Moving;
                speed = Speed;
                clientSpeed = m_ClientSpeed;
            }
            else if (MapItem?.Deleted != false)
            {
                if (message)
                {
                    TillerMan?.Say(502513); // I have seen no map, sir.
                }

                return false;
            }
            else if (Map != MapItem.Map || !Contains(MapItem.GetWorldLocation()))
            {
                if (message)
                {
                    TillerMan?.Say(502514); // The map is too far away from me, sir.
                }

                return false;
            }
            else if (Map != Map.Trammel && Map != Map.Felucca || NextNavPoint < 0 || NextNavPoint >= MapItem.Pins.Count)
            {
                if (message)
                {
                    TillerMan?.Say(1042551); // I don't see that navpoint, sir.
                }

                return false;
            }
            else
            {
                var dest = MapItem.Pins[NextNavPoint];

                MapItem.ConvertToWorld(dest.X, dest.Y, out var x, out var y);

                dir = GetMovementFor(x, y, out var maxSpeed);

                if (maxSpeed == 0)
                {
                    if (message && Order == BoatOrder.Single)
                    {
                        TillerMan?.Say(
                            1042874,
                            (NextNavPoint + 1).ToString()
                        ); // We have arrived at nav point ~1_POINT_NUM~ , sir.
                    }

                    if (NextNavPoint + 1 < MapItem.Pins.Count)
                    {
                        NextNavPoint++;

                        if (Order == BoatOrder.Course)
                        {
                            if (message)
                            {
                                TillerMan?.Say(
                                    1042875,
                                    (NextNavPoint + 1).ToString()
                                ); // Heading to nav point ~1_POINT_NUM~, sir.
                            }

                            return true;
                        }

                        return false;
                    }

                    NextNavPoint = -1;

                    if (message && Order == BoatOrder.Course)
                    {
                        TillerMan?.Say(502515); // The course is completed, sir.
                    }

                    return false;
                }

                if (dir == Left || dir == BackwardLeft || dir == Backward)
                {
                    return Turn(-2);
                }

                if (dir == Right || dir == BackwardRight)
                {
                    return Turn(2);
                }

                speed = Math.Min(Speed, maxSpeed);
                clientSpeed = 0x4;
            }

            return Move(dir, speed, clientSpeed, true);
        }

        public bool Move(Direction dir, int speed, int clientSpeed, bool message)
        {
            var map = Map;

            if (map == null || Deleted || CheckDecay())
            {
                return false;
            }

            if (Anchored)
            {
                if (message)
                {
                    TillerMan?.Say(501419); // Ar, the anchor is down sir!
                }

                return false;
            }

            int rx = 0, ry = 0;
            var d = (Direction)(((int)m_Facing + (int)dir) & 0x7);
            Movement.Movement.Offset(d, ref rx, ref ry);

            for (var i = 1; i <= speed; ++i)
            {
                if (!CanFit(new Point3D(X + i * rx, Y + i * ry, Z), Map, ItemID))
                {
                    if (i == 1)
                    {
                        if (message)
                        {
                            TillerMan?.Say(501424); // Ar, we've stopped sir.
                        }

                        return false;
                    }

                    speed = i - 1;
                    break;
                }
            }

            var xOffset = speed * rx;
            var yOffset = speed * ry;

            var newX = X + xOffset;
            var newY = Y + yOffset;

            var wrap = GetWrapFor(map);

            for (var i = 0; i < wrap.Length; ++i)
            {
                var rect = wrap[i];

                if (rect.Contains(new Point2D(X, Y)) && !rect.Contains(new Point2D(newX, newY)))
                {
                    if (newX < rect.X)
                    {
                        newX = rect.X + rect.Width - 1;
                    }
                    else if (newX >= rect.X + rect.Width)
                    {
                        newX = rect.X;
                    }

                    if (newY < rect.Y)
                    {
                        newY = rect.Y + rect.Height - 1;
                    }
                    else if (newY >= rect.Y + rect.Height)
                    {
                        newY = rect.Y;
                    }

                    for (var j = 1; j <= speed; ++j)
                    {
                        if (!CanFit(new Point3D(newX + j * rx, newY + j * ry, Z), Map, ItemID))
                        {
                            if (message)
                            {
                                TillerMan?.Say(501424); // Ar, we've stopped sir.
                            }

                            return false;
                        }
                    }

                    xOffset = newX - X;
                    yOffset = newY - Y;
                }
            }

            if (!NewBoatMovement || xOffset.Abs() > 1 || yOffset.Abs() > 1)
            {
                Teleport(xOffset, yOffset, 0);
            }
            else
            {
                using var eable = GetMovingEntities(true);

                // Packet must be sent before actual locations are changed
                foreach (var ns in Map.GetClientsInRange(Location, GetMaxUpdateRange()))
                {
                    var m = ns.Mobile;

                    if (ns.HighSeas && m.CanSee(this) && m.InRange(Location, GetUpdateRange(m)))
                    {
                        ns.SendMoveBoatHS(m, this, d, clientSpeed, eable, xOffset, yOffset);
                        eable.Reset();
                    }
                }

                foreach (var e in eable)
                {
                    if (e is Item item)
                    {
                        item.NoMoveHS = true;

                        if (item is not (Server.Items.TillerMan or Server.Items.Hold or Plank))
                        {
                            item.Location = new Point3D(item.X + xOffset, item.Y + yOffset, item.Z);
                        }
                    }
                    else if (e is Mobile m)
                    {
                        m.NoMoveHS = true;
                        m.Location = new Point3D(m.X + xOffset, m.Y + yOffset, m.Z);
                    }
                }

                NoMoveHS = true;
                Location = new Point3D(X + xOffset, Y + yOffset, Z);

                eable.Reset();

                foreach (var e in eable)
                {
                    if (e is Item item)
                    {
                        item.NoMoveHS = false;
                    }
                    else if (e is Mobile mobile)
                    {
                        mobile.NoMoveHS = false;
                    }
                }

                NoMoveHS = false;
            }

            return true;
        }

        public void Teleport(int xOffset, int yOffset, int zOffset)
        {
            foreach (var e in GetMovingEntities())
            {
                if (e is Item item)
                {
                    item.Location = new Point3D(item.X + xOffset, item.Y + yOffset, item.Z + zOffset);
                }
                else if (e is Mobile m)
                {
                    m.Location = new Point3D(m.X + xOffset, m.Y + yOffset, m.Z + zOffset);
                }
            }

            Location = new Point3D(X + xOffset, Y + yOffset, Z + zOffset);
        }

        public virtual MovingEntitiesEnumerable GetMovingEntities(bool includeBoat = false)
        {
            var map = Map;

            if (map == null || map == Map.Internal)
            {
                return new MovingEntitiesEnumerable(this, includeBoat, null);
            }

            var mcl = Components;

            var eable = map.GetObjectsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));
            return new MovingEntitiesEnumerable(this, includeBoat, eable);
        }

        public ref struct MovingEntitiesEnumerable
        {
            private readonly IPooledEnumerable<IEntity> _entities;
            private readonly IEnumerator<IEntity> _enumerator;
            private readonly bool _includeBoat;
            private BaseBoat _boat;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public MovingEntitiesEnumerable(BaseBoat boat, bool includeBoat, IPooledEnumerable<IEntity> entities)
            {
                _entities = entities;
                _enumerator = entities?.GetEnumerator();
                _boat = boat;
                _includeBoat = includeBoat;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public MovingEntitiesEnumerator GetEnumerator() => new(_boat, _includeBoat, _enumerator);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _entities?.Dispose();
                _enumerator?.Dispose();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                _enumerator?.Reset();
            }
        }

        public ref struct MovingEntitiesEnumerator
        {
            private readonly IEnumerator<IEntity> _enumerator;
            private IEntity? _current;
            private readonly bool _includeBoat;
            private readonly BaseBoat _boat;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public MovingEntitiesEnumerator(BaseBoat boat, bool includeBoat, IEnumerator<IEntity> enumerator = null)
            {
                _enumerator = enumerator;
                _current = default;
                _includeBoat = includeBoat;
                _boat = boat;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                IEntity current;
                while (_enumerator?.MoveNext() == true)
                {
                    current = _enumerator.Current;

                    // Skip the boat, effects, spawners, or parts of the boat
                    if (current == _boat || current is EffectItem or BaseSpawner ||
                        !_includeBoat && current is Server.Items.TillerMan or Server.Items.Hold or Plank)
                    {
                        continue;
                    }

                    if (current is Item item)
                    {
                        // TODO: Remove visible check and use something better, like check for spawners, or other things in the ocean we shouldn't pick up on accident
                        if (_boat!.Contains(item) && item.Visible && item.Z >= _boat.Z)
                        {
                            _current = current;
                            return true;
                        }
                    }
                    else if (current is Mobile m && _boat!.Contains(m))
                    {
                        _current = current;
                        return true;
                    }
                }

                return false;
            }

            public IEntity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
        }

        public bool SetFacing(Direction facing)
        {
            if (Parent != null || Map == null)
            {
                return false;
            }

            if (CheckDecay())
            {
                return false;
            }

            if (Map != Map.Internal)
            {
                switch (facing)
                {
                    case Direction.North:
                        if (!CanFit(Location, Map, NorthID))
                        {
                            return false;
                        }

                        break;
                    case Direction.East:
                        if (!CanFit(Location, Map, EastID))
                        {
                            return false;
                        }

                        break;
                    case Direction.South:
                        if (!CanFit(Location, Map, SouthID))
                        {
                            return false;
                        }

                        break;
                    case Direction.West:
                        if (!CanFit(Location, Map, WestID))
                        {
                            return false;
                        }

                        break;
                }
            }

            var old = m_Facing;

            m_Facing = facing;

            TillerMan?.SetFacing(facing);
            Hold?.SetFacing(facing);
            PPlank?.SetFacing(facing);
            SPlank?.SetFacing(facing);

            int xOffset = 0, yOffset = 0;
            Movement.Movement.Offset(facing, ref xOffset, ref yOffset);

            var count = ((m_Facing - old) & 0x7) / 2;

            foreach (var e in GetMovingEntities())
            {
                if (e == this)
                {
                    continue;
                }

                if (e is Item item)
                {
                    item.Location = Rotate(item.Location, count);
                }
                else if (e is Mobile m)
                {
                    m.Direction = (m.Direction - old + facing) & Direction.Mask;
                    m.Location = Rotate(m.Location, count);
                }
            }

            if (TillerMan != null)
            {
                TillerMan.Location = new Point3D(
                    X + xOffset * TillerManDistance + (facing == Direction.North ? 1 : 0),
                    Y + yOffset * TillerManDistance,
                    TillerMan.Z
                );
            }

            if (Hold != null)
            {
                Hold.Location = new Point3D(X + xOffset * HoldDistance, Y + yOffset * HoldDistance, Hold.Z);
            }

            if (PPlank != null)
            {
                PPlank.Location = Rotate(PPlank.Location, count);
            }

            if (SPlank != null)
            {
                SPlank.Location = Rotate(SPlank.Location, count);
            }

            ItemID = facing switch
            {
                Direction.North => NorthID,
                Direction.East  => EastID,
                Direction.South => SouthID,
                Direction.West  => WestID,
                _               => ItemID
            };

            return true;
        }

        public static void UpdateAllComponents()
        {
            for (var i = Boats.Count - 1; i >= 0; --i)
            {
                Boats[i].UpdateComponents();
            }
        }

        public static void Initialize()
        {
            EventSink.WorldLoad += UpdateAllComponents;
            EventSink.WorldSave += UpdateAllComponents;
        }

        private class TurnTimer : Timer
        {
            internal int _turn;
            private bool _message;
            private BaseBoat _boat;

            public TurnTimer(int turn, BaseBoat boat, TimeSpan delay, bool message = true) : base(delay)
            {
                _turn = turn;
                _message = message;
                _boat = boat;
            }
            protected override void OnTick()
            {
                _boat?.Turn(_turn, _message);
                Stop();
            }
        }

        private class MoveTimer : Timer
        {
            private readonly BaseBoat _boat;

            public MoveTimer(BaseBoat boat, TimeSpan delay, TimeSpan interval, int single) : base(delay, interval, single) =>
                _boat = boat;

            protected override void OnTick()
            {
                _boat?.StopBoat();
            }
        }

        private class DecayTimer : Timer
        {
            private readonly BaseBoat m_Boat;
            private int m_Count;

            public DecayTimer(BaseBoat boat) : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(5.0))
            {
                m_Boat = boat;
            }

            protected override void OnTick()
            {
                if (m_Count == 5)
                {
                    m_Boat.Delete();
                    Stop();
                }
                else
                {
                    m_Boat.Location = new Point3D(m_Boat.X, m_Boat.Y, m_Boat.Z - 1);

                    m_Boat.TillerMan?.Say(1007168 + m_Count);

                    ++m_Count;
                }
            }
        }

        /*
         * OSI sends the 0xF7 packet instead, holding 0xF3 packets
         * for every entity on the boat. Though, the regular 0xF3
         * packets are still being sent as well as entities come
         * into sight. Do we really need it?
         */
        /*
        protected override Packet GetWorldPacketFor( NetState state )
        {
          if (NewBoatMovement && state.HighSeas)
            return new DisplayBoatHS( state.Mobile, this );
          else
            return base.GetWorldPacketFor( state );
        }
        */
    }
}
