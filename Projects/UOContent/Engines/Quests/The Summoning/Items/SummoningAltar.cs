using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Doom
{
    public class SummoningAltar : AbbatoirAddon
    {
        private BoneDemon m_Daemon;

        [Constructible]
        public SummoningAltar()
        {
        }

        public SummoningAltar(Serial serial) : base(serial)
        {
        }

        public BoneDemon Daemon
        {
            get => m_Daemon;
            set
            {
                m_Daemon = value;
                CheckDaemon();
            }
        }

        public void CheckDaemon()
        {
            if (m_Daemon?.Alive != true)
            {
                m_Daemon = null;
                Hue = 0;
            }
            else
            {
                Hue = 0x66D;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Daemon);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Daemon = reader.ReadEntity<BoneDemon>();

            CheckDaemon();
        }
    }
}
