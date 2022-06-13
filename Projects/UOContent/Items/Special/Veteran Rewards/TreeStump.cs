using System;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    public class TreeStump : BaseAddon, IRewardItem
    {
        private bool m_IsRewardItem;
        private int m_Logs;
        private TimerExecutionToken _timerToken;

        [Constructible]
        public TreeStump(int itemID)
        {
            AddComponent(new AddonComponent(itemID), 0, 0, 0);

            Timer.StartTimer(TimeSpan.FromDays(1), TimeSpan.FromDays(1), GiveLogs, out _timerToken);
        }

        public TreeStump(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                var deed = new TreeStumpDeed();
                deed.IsRewardItem = m_IsRewardItem;
                deed.Logs = m_Logs;

                return deed;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Logs
        {
            get => m_Logs;
            set
            {
                m_Logs = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get => m_IsRewardItem;
            set
            {
                m_IsRewardItem = value;
                InvalidateProperties();
            }
        }

        private void GiveLogs()
        {
            m_Logs = Math.Min(100, m_Logs + 10);
        }

        public override void OnAfterDelete()
        {
            _timerToken.Cancel();
        }

        public override void OnComponentUsed(AddonComponent c, Mobile from)
        {
            var house = BaseHouse.FindHouseAt(this);

            /*
             * Unique problems have unique solutions.  OSI does not have a problem with 1000s of mining carts
             * due to the fact that they have only a miniscule fraction of the number of 10 year vets that a
             * typical RunUO shard will have (RunUO's scaled down account aging system makes this a unique problem),
             * and the "freeness" of free accounts. We also dont have mitigating factors like inactive (unpaid)
             * accounts not gaining veteran time.
             *
             * The lack of high end vets and vet rewards on OSI has made testing the *exact* ranging/stacking
             * behavior of these things all but impossible, so either way its just an estimation.
             *
             * If youd like your shard's carts/stumps to work the way they did before, simply replace the check
             * below with this line of code:
             *
             * if (!from.InRange(GetWorldLocation(), 2)
             *
             * However, I am sure these checks are more accurate to OSI than the former version was.
             *
             */

            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this) || !(from.Z - Z > -3 && from.Z - Z < 3))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (house?.HasSecureAccess(from, SecureLevel.Friends) == true)
            {
                if (m_Logs > 0)
                {
                    var logs = Utility.Random(7) switch
                    {
                        0 => new Log(),
                        1 => new AshLog(),
                        2 => new OakLog(),
                        3 => new YewLog(),
                        4 => new HeartwoodLog(),
                        5 => new BloodwoodLog(),
                        _ => new FrostwoodLog()
                    };

                    var amount = Math.Min(10, m_Logs);
                    logs.Amount = amount;

                    if (!from.PlaceInBackpack(logs))
                    {
                        logs.Delete();
                        from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
                    }
                    else
                    {
                        m_Logs -= amount;
                        PublicOverheadMessage(MessageType.Regular, 0, 1094719, m_Logs.ToString()); // Logs: ~1_COUNT~
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1094720); // There are no more logs available.
                }
            }
            else
            {
                from.SendLocalizedMessage(1061637); // You are not allowed to access this.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_IsRewardItem);
            writer.Write(m_Logs);

            if (_timerToken.Running)
            {
                writer.Write(_timerToken.Next);
            }
            else
            {
                writer.Write(Core.Now + TimeSpan.FromDays(1));
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_IsRewardItem = reader.ReadBool();
            m_Logs = reader.ReadInt();

            var next = reader.ReadDateTime();

            if (next < Core.Now)
            {
                next = Core.Now;
            }

            Timer.StartTimer(next - Core.Now, TimeSpan.FromDays(1), GiveLogs, out _timerToken);
        }
    }

    public class TreeStumpDeed : BaseAddonDeed, IRewardItem, IRewardOption
    {
        private bool m_IsRewardItem;

        private int m_ItemID;

        private int m_Logs;

        [Constructible]
        public TreeStumpDeed() => LootType = LootType.Blessed;

        public TreeStumpDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1080406; // a deed for a tree stump decoration

        public override BaseAddon Addon
        {
            get
            {
                var addon = new TreeStump(m_ItemID);
                addon.IsRewardItem = m_IsRewardItem;
                addon.Logs = m_Logs;

                return addon;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Logs
        {
            get => m_Logs;
            set
            {
                m_Logs = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get => m_IsRewardItem;
            set
            {
                m_IsRewardItem = value;
                InvalidateProperties();
            }
        }

        public void GetOptions(RewardOptionList list)
        {
            list.Add(1, 1080403); // Tree Stump with Axe West
            list.Add(2, 1080404); // Tree Stump with Axe North
            list.Add(3, 1080401); // Tree Stump East
            list.Add(4, 1080402); // Tree Stump South
        }

        public void OnOptionSelected(Mobile from, int option)
        {
            m_ItemID = option switch
            {
                1 => 0xE56,
                2 => 0xE58,
                3 => 0xE57,
                4 => 0xE59,
                _ => m_ItemID
            };

            if (!Deleted)
            {
                base.OnDoubleClick(from);
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsRewardItem)
            {
                list.Add(1076223); // 7th Year Veteran Reward
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_IsRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
            {
                return;
            }

            if (IsChildOf(from.Backpack))
            {
                from.CloseGump<RewardOptionGump>();
                from.SendGump(new RewardOptionGump(this));
            }
            else
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_IsRewardItem);
            writer.Write(m_Logs);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_IsRewardItem = reader.ReadBool();
            m_Logs = reader.ReadInt();
        }
    }
}
