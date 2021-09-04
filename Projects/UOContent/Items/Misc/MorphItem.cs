namespace Server.Items
{
    public class MorphItem : Item
    {
        private int m_InsideRange;
        private int m_OutsideRange;

        [Constructible]
        public MorphItem(int inactiveItemID, int activeItemID, int range) : this(inactiveItemID, activeItemID, range, range)
        {
        }

        [Constructible]
        public MorphItem(int inactiveItemID, int activeItemID, int inRange, int outRange) : base(inactiveItemID)
        {
            Movable = false;

            InactiveItemID = inactiveItemID;
            ActiveItemID = activeItemID;
            InsideRange = inRange;
            OutsideRange = outRange;
        }

        public MorphItem(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int InactiveItemID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ActiveItemID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int InsideRange
        {
            get => m_InsideRange;
            set => m_InsideRange = value > 18 ? 18 :
                value < 0 ? 0 : value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OutsideRange
        {
            get => m_OutsideRange;
            set => m_OutsideRange = value > 18 ? 18 :
                value < 0 ? 0 : value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentRange => ItemID == InactiveItemID ? InsideRange : OutsideRange;

        public override bool HandlesOnMovement => true;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Utility.InRange(m.Location, Location, CurrentRange) || Utility.InRange(oldLocation, Location, CurrentRange))
            {
                Refresh();
            }
        }

        public override void OnMapChange()
        {
            if (!Deleted)
            {
                Refresh();
            }
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            if (!Deleted)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            var found = false;
            var eable = GetMobilesInRange(CurrentRange);
            foreach (var mob in eable)
            {
                if (!mob.Hidden || mob.AccessLevel <= AccessLevel.Player)
                {
                    found = true;
                    break;
                }
            }

            eable.Free();

            ItemID = found ? ActiveItemID : InactiveItemID;

            Visible = ItemID != 0x1;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(m_OutsideRange);

            writer.Write(InactiveItemID);
            writer.Write(ActiveItemID);
            writer.Write(m_InsideRange);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_OutsideRange = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        InactiveItemID = reader.ReadInt();
                        ActiveItemID = reader.ReadInt();
                        m_InsideRange = reader.ReadInt();

                        if (version < 1)
                        {
                            m_OutsideRange = m_InsideRange;
                        }

                        break;
                    }
            }

            Timer.StartTimer(Refresh);
        }
    }
}
