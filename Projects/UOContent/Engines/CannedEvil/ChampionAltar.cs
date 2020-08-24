using Server.Items;

namespace Server.Engines.CannedEvil
{
    public class ChampionAltar : PentagramAddon
    {
        private ChampionSpawn m_Spawn;

        public ChampionAltar(ChampionSpawn spawn) => m_Spawn = spawn;

        public ChampionAltar(Serial serial) : base(serial)
        {
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_Spawn?.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Spawn);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Spawn = reader.ReadItem() as ChampionSpawn;

                        if (m_Spawn == null)
                            Delete();

                        break;
                    }
            }
        }
    }
}
