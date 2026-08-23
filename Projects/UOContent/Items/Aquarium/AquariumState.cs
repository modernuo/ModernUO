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

        [SerializableField(0, allowFieldChange: nameof(AllowStateChange))]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _state;

        private bool AllowStateChange(ref int value)
        {
            value = Math.Clamp(value, 0, 4);
            return true;
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
