using System;

namespace Server.Engines.Harvest
{
    public class HarvestBank
    {
        private readonly int m_Maximum;
        private int m_Current;
        private DateTime m_NextRespawn;
        private HarvestVein m_Vein, m_DefaultVein;

        public HarvestBank(HarvestDefinition def, HarvestVein defaultVein)
        {
            m_Maximum = Utility.RandomMinMax(def.MinTotal, def.MaxTotal);
            m_Current = m_Maximum;
            m_DefaultVein = defaultVein;
            m_Vein = m_DefaultVein;

            Definition = def;
        }

        public HarvestDefinition Definition { get; }

        public int Current
        {
            get
            {
                CheckRespawn();
                return m_Current;
            }
        }

        public HarvestVein Vein
        {
            get
            {
                CheckRespawn();
                return m_Vein;
            }
            set => m_Vein = value;
        }

        public HarvestVein DefaultVein
        {
            get
            {
                CheckRespawn();
                return m_DefaultVein;
            }
        }

        public void CheckRespawn()
        {
            if (m_Current == m_Maximum || m_NextRespawn > Core.Now)
            {
                return;
            }

            m_Current = m_Maximum;

            if (Definition.RandomizeVeins)
            {
                m_DefaultVein = Definition.GetVeinFrom((uint)Utility.Random(Definition.VeinWeights));
            }

            m_Vein = m_DefaultVein;
        }

        public void Consume(int amount, Mobile from)
        {
            CheckRespawn();

            if (m_Current == m_Maximum)
            {
                var min = Definition.MinRespawn.TotalMinutes;
                var max = Definition.MaxRespawn.TotalMinutes;
                var rnd = Utility.RandomDouble();

                m_Current = m_Maximum - amount;

                var minutes = min + rnd * (max - min);
                if (Definition.RaceBonus && from.Race == Race.Elf) // def.RaceBonus = Core.ML
                {
                    minutes *= .75; // 25% off the time.
                }

                m_NextRespawn = Core.Now + TimeSpan.FromMinutes(minutes);
            }
            else
            {
                m_Current -= amount;
            }

            if (m_Current < 0)
            {
                m_Current = 0;
            }
        }
    }
}
