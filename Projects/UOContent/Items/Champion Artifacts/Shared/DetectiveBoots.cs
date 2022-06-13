using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class DetectiveBoots : Boots
    {
        private int m_Level;

        [Constructible]
        public DetectiveBoots()
        {
            Hue = 0x455;
            Level = Utility.RandomMinMax(0, 2);
        }

        public override int LabelNumber => 1094894 + m_Level; // [Quality] Detective of the Royal Guard [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get => m_Level;
            set
            {
                m_Level = Math.Clamp(value, 0, 2);
                Attributes.BonusInt = 2 + m_Level;
                InvalidateProperties();
            }
        }

        [AfterDeserialization]
        private void AfterDeserialize()
        {
            Level = Attributes.BonusInt - 2;
        }
    }
}
