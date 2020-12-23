using System;

namespace Server.Factions
{
    public class TownState
    {
        private Mobile m_Finance;
        private Mobile m_Sheriff;

        public TownState(Town town) => Town = town;

        public TownState(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 3:
                    {
                        LastIncome = reader.ReadDateTime();

                        goto case 2;
                    }
                case 2:
                    {
                        Tax = reader.ReadEncodedInt();
                        LastTaxChange = reader.ReadDateTime();

                        goto case 1;
                    }
                case 1:
                    {
                        Silver = reader.ReadEncodedInt();

                        goto case 0;
                    }
                case 0:
                    {
                        Town = Town.ReadReference(reader);
                        Owner = Faction.ReadReference(reader);

                        m_Sheriff = reader.ReadEntity<Mobile>();
                        m_Finance = reader.ReadEntity<Mobile>();

                        Town.State = this;

                        break;
                    }
            }
        }

        public Town Town { get; set; }

        public Faction Owner { get; set; }

        public Mobile Sheriff
        {
            get => m_Sheriff;
            set
            {
                if (m_Sheriff != null)
                {
                    var pl = PlayerState.Find(m_Sheriff);

                    if (pl != null)
                    {
                        pl.Sheriff = null;
                    }
                }

                m_Sheriff = value;

                if (m_Sheriff != null)
                {
                    var pl = PlayerState.Find(m_Sheriff);

                    if (pl != null)
                    {
                        pl.Sheriff = Town;
                    }
                }
            }
        }

        public Mobile Finance
        {
            get => m_Finance;
            set
            {
                if (m_Finance != null)
                {
                    var pl = PlayerState.Find(m_Finance);

                    if (pl != null)
                    {
                        pl.Finance = null;
                    }
                }

                m_Finance = value;

                if (m_Finance != null)
                {
                    var pl = PlayerState.Find(m_Finance);

                    if (pl != null)
                    {
                        pl.Finance = Town;
                    }
                }
            }
        }

        public int Silver { get; set; }

        public int Tax { get; set; }

        public DateTime LastTaxChange { get; set; }

        public DateTime LastIncome { get; set; }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(3); // version

            writer.Write(LastIncome);

            writer.WriteEncodedInt(Tax);
            writer.Write(LastTaxChange);

            writer.WriteEncodedInt(Silver);

            Town.WriteReference(writer, Town);
            Faction.WriteReference(writer, Owner);

            writer.Write(m_Sheriff);
            writer.Write(m_Finance);
        }
    }
}
