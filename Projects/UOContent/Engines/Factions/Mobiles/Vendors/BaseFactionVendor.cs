using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Factions
{
    public abstract class BaseFactionVendor : BaseVendor
    {
        private Faction m_Faction;
        private Town m_Town;

        public BaseFactionVendor(Town town, Faction faction, string title) : base(title)
        {
            Frozen = true;
            CantWalk = true;
            Female = false;
            BodyValue = 400;
            Name = NameList.RandomName("male");

            RangeHome = 0;

            m_Town = town;
            m_Faction = faction;
            Register();
        }

        public BaseFactionVendor(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Town Town
        {
            get => m_Town;
            set
            {
                Unregister();
                m_Town = value;
                Register();
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Faction Faction
        {
            get => m_Faction;
            set
            {
                Unregister();
                m_Faction = value;
                Register();
            }
        }

        protected override List<SBInfo> SBInfos { get; } = new();

        public void Register()
        {
            if (m_Town != null && m_Faction != null)
            {
                m_Town.RegisterVendor(this);
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (Core.ML)
            {
                return true;
            }

            return base.OnMoveOver(m);
        }

        public void Unregister()
        {
            m_Town?.UnregisterVendor(this);
        }

        public override void InitSBInfo()
        {
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Unregister();
        }

        public override bool CheckVendorAccess(Mobile from) => true;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            Town.WriteReference(writer, m_Town);
            Faction.WriteReference(writer, m_Faction);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Town = Town.ReadReference(reader);
                        m_Faction = Faction.ReadReference(reader);
                        Register();
                        break;
                    }
            }

            Frozen = true;
        }
    }
}
