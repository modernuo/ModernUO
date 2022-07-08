using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseFish : Item
    {
        private static readonly TimeSpan DeathDelay = TimeSpan.FromMinutes(5);

        private TimerExecutionToken _timerToken;

        [Constructible]
        public BaseFish(int itemID) : base(itemID)
        {
            StartTimer();
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Dead => ItemID == 0x3B0C;

        public virtual void StartTimer()
        {
            _timerToken.Cancel();
            Timer.StartTimer(DeathDelay, Kill, out _timerToken);

            InvalidateProperties();
        }

        public virtual void StopTimer()
        {
            _timerToken.Cancel();
            InvalidateProperties();
        }

        public override void OnDelete()
        {
            StopTimer();
        }

        public virtual void Kill()
        {
            ItemID = 0x3B0C;
            StopTimer();
        }

        public int GetDescription()
        {
            // TODO: This will never return "very unusual dead aquarium creature" due to the way it is killed
            if (ItemID > 0x3B0F)
            {
                return Dead ? 1074424 : 1074422; // A very unusual [dead/live] aquarium creature
            }

            if (Hue != 0)
            {
                return Dead ? 1074425 : 1074423; // A [dead/live] aquarium creature of unusual color
            }

            return Dead ? 1073623 : 1073622; // A [dead/live] aquarium creature
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(GetDescription());

            if (!Dead && _timerToken.Running)
            {
                list.Add(1074507); // Gasping for air
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (Parent is not Aquarium && Parent is not FishBowl)
            {
                StartTimer();
            }
        }
    }
}
