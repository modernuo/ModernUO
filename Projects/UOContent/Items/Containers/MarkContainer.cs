using System;

namespace Server.Items
{
    public class MarkContainer : LockableContainer
    {
        private bool m_AutoLock;
        private InternalTimer m_RelockTimer;

        [Constructible]
        public MarkContainer(bool bone = false, bool locked = false) : base(bone ? 0xECA : 0xE79)
        {
            Movable = false;

            if (bone)
            {
                Hue = 1102;
            }

            m_AutoLock = locked;
            Locked = locked;

            if (locked)
            {
                LockLevel = -255;
            }
        }

        public MarkContainer(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AutoLock
        {
            get => m_AutoLock;
            set
            {
                m_AutoLock = value;

                if (!m_AutoLock)
                {
                    StopTimer();
                }
                else if (!Locked && m_RelockTimer == null)
                {
                    m_RelockTimer = new InternalTimer(this);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TargetMap { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Target { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Bone
        {
            get => ItemID == 0xECA;
            set
            {
                ItemID = value ? 0xECA : 0xE79;
                Hue = value ? 1102 : 0;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Description { get; set; }

        public override bool IsDecoContainer => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public override bool Locked
        {
            get => base.Locked;
            set
            {
                base.Locked = value;

                if (m_AutoLock)
                {
                    StopTimer();

                    if (!Locked)
                    {
                        m_RelockTimer = new InternalTimer(this);
                    }
                }
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("SecretLocGen", AccessLevel.Administrator, SecretLocGen_OnCommand);
        }

        [Usage("SecretLocGen"), Description("Generates mark containers to Malas secret locations.")]
        public static void SecretLocGen_OnCommand(CommandEventArgs e)
        {
            CreateMalasPassage(951, 546, -70, 1006, 994, -70, false, false);
            CreateMalasPassage(914, 192, -79, 1019, 1062, -70, false, false);
            CreateMalasPassage(1614, 143, -90, 1214, 1313, -90, false, false);
            CreateMalasPassage(2176, 324, -90, 1554, 172, -90, false, false);
            CreateMalasPassage(864, 812, -90, 1061, 1161, -70, false, false);
            CreateMalasPassage(1051, 1434, -85, 1076, 1244, -70, false, true);
            CreateMalasPassage(1326, 523, -87, 1201, 1554, -70, false, false);
            CreateMalasPassage(424, 189, -1, 2333, 1501, -90, true, false);
            CreateMalasPassage(1313, 1115, -85, 1183, 462, -45, false, false);

            e.Mobile.SendMessage("Secret mark containers have been created.");
        }

        private static bool FindMarkContainer(Point3D p, Map map)
        {
            var eable = map.GetItemsInRange<MarkContainer>(p, 0);

            foreach (var item in eable)
            {
                if (item.Z == p.Z)
                {
                    eable.Free();
                    return true;
                    break;
                }
            }

            eable.Free();
            return false;
        }

        private static void CreateMalasPassage(
            int x, int y, int z, int xTarget, int yTarget, int zTarget, bool bone,
            bool locked
        )
        {
            var location = new Point3D(x, y, z);

            if (FindMarkContainer(location, Map.Malas))
            {
                return;
            }

            var cont = new MarkContainer(bone, locked)
            {
                TargetMap = Map.Malas,
                Target = new Point3D(xTarget, yTarget, zTarget),
                Description = "strange location"
            };

            cont.MoveToWorld(location, Map.Malas);
        }

        public void StopTimer()
        {
            m_RelockTimer?.Stop();
            m_RelockTimer = null;
        }

        public void Mark(RecallRune rune)
        {
            if (TargetMap != null)
            {
                rune.Marked = true;
                rune.TargetMap = TargetMap;
                rune.Target = Target;
                rune.Description = Description;
                rune.House = null;
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is RecallRune rune && base.OnDragDrop(from, dropped))
            {
                Mark(rune);
                return true;
            }

            return false;
        }

        public override bool OnDragDropInto(Mobile from, Item dropped, Point3D p)
        {
            if (dropped is RecallRune rune && base.OnDragDropInto(from, dropped, p))
            {
                Mark(rune);
                return true;
            }

            return false;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_AutoLock);

            if (!Locked && m_AutoLock)
            {
                writer.WriteDeltaTime(m_RelockTimer.RelockTime);
            }

            writer.Write(TargetMap);
            writer.Write(Target);
            writer.Write(Description);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_AutoLock = reader.ReadBool();

            if (!Locked && m_AutoLock)
            {
                m_RelockTimer = new InternalTimer(this, reader.ReadDeltaTime() - Core.Now);
            }

            TargetMap = reader.ReadMap();
            Target = reader.ReadPoint3D();
            Description = reader.ReadString();
        }

        private class InternalTimer : Timer
        {
            public InternalTimer(MarkContainer container) : this(container, TimeSpan.FromMinutes(5.0))
            {
            }

            public InternalTimer(MarkContainer container, TimeSpan delay) : base(delay)
            {
                Container = container;
                RelockTime = Core.Now + delay;

                Start();
            }

            public MarkContainer Container { get; }

            public DateTime RelockTime { get; }

            protected override void OnTick()
            {
                Container.Locked = true;
                Container.LockLevel = -255;
            }
        }
    }
}
