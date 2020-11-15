using System;
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

    public class FlourMillEastAddon : BaseAddon, IFlourMill
    {
        private static readonly int[][] m_StageTable =
        {
            new[] { 0x1920, 0x1921, 0x1925 },
            new[] { 0x1922, 0x1923, 0x1926 },
            new[] { 0x1924, 0x1924, 0x1928 }
        };

        private int m_Flour;
        private Timer m_Timer;

        [Constructible]
        public FlourMillEastAddon()
        {
            AddComponent(new AddonComponent(0x1920), -1, 0, 0);
            AddComponent(new AddonComponent(0x1922), 0, 0, 0);
            AddComponent(new AddonComponent(0x1924), 1, 0, 0);
        }

        public FlourMillEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new FlourMillEastDeed();

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasFlour => m_Flour > 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull => m_Flour >= MaxFlour;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsWorking => m_Timer != null;

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxFlour => 2;

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurFlour
        {
            get => m_Flour;
            set
            {
                m_Flour = Math.Max(0, Math.Min(value, MaxFlour));
                UpdateStage();
            }
        }

        public void StartWorking(Mobile from)
        {
            if (IsWorking)
            {
                return;
            }

            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), FinishWorking_Callback, from);
            UpdateStage();
        }

        private void FinishWorking_Callback(Mobile from)
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            if (from?.Deleted == false && !Deleted && IsFull)
            {
                var flour = new SackFlour { ItemID = Utility.RandomBool() ? 4153 : 4165 };

                if (from.PlaceInBackpack(flour))
                {
                    m_Flour = 0;
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(m_Flour);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Flour = reader.ReadInt();
                        break;
                    }
            }

            UpdateStage();
        }
    }

    public class FlourMillEastDeed : BaseAddonDeed
    {
        [Constructible]
        public FlourMillEastDeed()
        {
        }

        public FlourMillEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new FlourMillEastAddon();
        public override int LabelNumber => 1044347; // flour mill (east)

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
