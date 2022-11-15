using System;
using ModernUO.Serialization;

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
    [SerializationGenerator(0, false)]
    public partial class AquariumState
    {
        [DirtyTrackingEntity]
        private Aquarium _aquarium;

        public AquariumState(Aquarium parent) => _aquarium = parent;

        [SerializableProperty(0)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = Math.Clamp(value, 0, 4);
                    this.MarkDirty();
                }
            }
        }

        [SerializableField(1)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _maintain;

        [SerializableField(2)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _improve;

        [SerializableField(3)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _added;

        public override string ToString() => "...";
    }
}
