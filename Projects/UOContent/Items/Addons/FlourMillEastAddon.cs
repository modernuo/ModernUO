using System;
using ModernUO.Serialization;
using Server.Network;

namespace Server.Items
{
    public interface IFlourMill
    {
        int MaxFlour { get; }
        int CurFlour { get; set; }
    }

    public enum FlourMillStage
    {
        Empty,
        Filled,
        Working
    }

    [SerializationGenerator(0, false)]
    public partial class FlourMillEastAddon : BaseAddon, IFlourMill
    {
        private static readonly int[][] m_StageTable =
        {
            new[] { 0x1920, 0x1921, 0x1925 },
            new[] { 0x1922, 0x1923, 0x1926 },
            new[] { 0x1924, 0x1924, 0x1928 }
        };

        [Constructible]
        public FlourMillEastAddon()
        {
            AddComponent(new AddonComponent(0x1920), -1, 0, 0);
            AddComponent(new AddonComponent(0x1922), 0, 0, 0);
            AddComponent(new AddonComponent(0x1924), 1, 0, 0);
        }

        public override BaseAddonDeed Deed => new FlourMillEastDeed();

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasFlour => _curFlour > 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull => _curFlour >= MaxFlour;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsWorking { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxFlour => 2;

        [SerializableProperty(0)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurFlour
        {
            get => _curFlour;
            set
            {
                _curFlour = Math.Clamp(value, 0, MaxFlour);
                UpdateStage();
                this.MarkDirty();
            }
        }

        public void StartWorking(Mobile from)
        {
            if (IsWorking)
            {
                return;
            }

            Timer.StartTimer(TimeSpan.FromSeconds(5.0), () => FinishWorking_Callback(from));
            IsWorking = true;
            UpdateStage();
        }

        private void FinishWorking_Callback(Mobile from)
        {
            IsWorking = false;

            if (from?.Deleted == false && !Deleted && IsFull)
            {
                var flour = new SackFlour { ItemID = Utility.RandomBool() ? 4153 : 4165 };

                if (from.PlaceInBackpack(flour))
                {
                    _curFlour = 0;
                }
                else
                {
                    flour.Delete();
                    from.SendLocalizedMessage(500998); // There is not enough room in your backpack!  You stop grinding.
                }
            }

            UpdateStage();
        }

        private int[] FindItemTable(int itemID)
        {
            for (var i = 0; i < m_StageTable.Length; ++i)
            {
                var itemTable = m_StageTable[i];

                for (var j = 0; j < itemTable.Length; ++j)
                {
                    if (itemTable[j] == itemID)
                    {
                        return itemTable;
                    }
                }
            }

            return null;
        }

        public void UpdateStage()
        {
            if (IsWorking)
            {
                UpdateStage(FlourMillStage.Working);
            }
            else if (HasFlour)
            {
                UpdateStage(FlourMillStage.Filled);
            }
            else
            {
                UpdateStage(FlourMillStage.Empty);
            }
        }

        public void UpdateStage(FlourMillStage stage)
        {
            var components = Components;

            for (var i = 0; i < components.Count; ++i)
            {
                var component = components[i];

                var itemTable = FindItemTable(component.ItemID);

                if (itemTable != null)
                {
                    component.ItemID = itemTable[(int)stage];
                }
            }
        }

        public override void OnComponentUsed(AddonComponent c, Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 4) || !from.InLOS(this))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (!IsFull)
            {
                from.SendLocalizedMessage(500997); // You need more wheat to make a sack of flour.
            }
            else
            {
                StartWorking(from);
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            UpdateStage();
        }
    }

    [SerializationGenerator(0, false)]
    public partial class FlourMillEastDeed : BaseAddonDeed
    {
        [Constructible]
        public FlourMillEastDeed()
        {
        }

        public override BaseAddon Addon => new FlourMillEastAddon();
        public override int LabelNumber => 1044347; // flour mill (east)
    }
}
