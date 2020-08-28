using System;

namespace Server.Items
{
    public enum WaterState
    {
        Dead,
        Dying,
        Unhealthy,
        Healthy,
        Strong
    }

    public enum FoodState
    {
        Dead,
        Starving,
        Hungry,
        Full,
        Overfed
    }

    [PropertyObject]
    public class AquariumState
    {
        private int m_State;

        [CommandProperty(AccessLevel.GameMaster)]
        public int State
        {
            get => m_State;
            set => m_State = Math.Clamp(value, 0, 4);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Maintain { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Improve { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Added { get; set; }

        public override string ToString() => "...";

        public virtual void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(m_State);
            writer.Write(Maintain);
            writer.Write(Improve);
            writer.Write(Added);
        }

        public virtual void Deserialize(IGenericReader reader)
        {
            var version = reader.ReadInt();

            m_State = reader.ReadInt();
            Maintain = reader.ReadInt();
            Improve = reader.ReadInt();
            Added = reader.ReadInt();
        }
    }
}
