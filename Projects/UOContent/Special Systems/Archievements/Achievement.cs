using Server;
using Server.Mobiles;
using System;


namespace Scripts.Systems.Achievements
{

    class AchieveData
    {
        //public int ID { get; set; }
        public int Progress { get; set; }
        public DateTime CompletedOn { get; set; } = DateTime.MinValue;

        public AchieveData()
        {

        }
        public AchieveData(IGenericReader reader)
        {
            Deserialize(reader);
        }
        public void Serialize(IGenericWriter writer)
        {

            writer.Write(0); // version
            writer.Write(Progress);
            writer.Write(CompletedOn);

        }
        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            Progress = reader.ReadInt();
            CompletedOn = reader.ReadDateTime();

        }

    }
}
