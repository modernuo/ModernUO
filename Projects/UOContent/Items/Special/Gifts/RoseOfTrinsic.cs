using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    [Flippable(0x234C, 0x234D)]
    public class RoseOfTrinsic : Item, ISecurable
    {
        private static readonly TimeSpan m_SpawnTime = TimeSpan.FromHours(4.0);
        private DateTime m_NextSpawnTime;

        private int m_Petals;
        private SpawnTimer m_SpawnTimer;

        [Constructible]
        public RoseOfTrinsic() : base(0x234D)
        {
            Weight = 1.0;
            LootType = LootType.Blessed;

            m_Petals = 0;
            StartSpawnTimer(TimeSpan.FromMinutes(1.0));
        }

        public RoseOfTrinsic(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1062913; // Rose of Trinsic

        [CommandProperty(AccessLevel.GameMaster)]
        public int Petals
        {
            get => m_Petals;
            set
            {
                if (value >= 10)
                {
                    m_Petals = 10;

                    StopSpawnTimer();
                }
                else
                {
                    if (value <= 0)
                    {
                        m_Petals = 0;
                    }
                    else
                    {
                        m_Petals = value;
                    }

                    StartSpawnTimer(m_SpawnTime);
                }

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1062925, Petals); // Petals:  ~1_COUNT~
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            SetSecureLevelEntry.AddTo(from, this, list);
        }

        private void StartSpawnTimer(TimeSpan delay)
        {
            StopSpawnTimer();

            m_SpawnTimer = new SpawnTimer(this, delay);
            m_SpawnTimer.Start();

            m_NextSpawnTime = Core.Now + delay;
        }

        private void StopSpawnTimer()
        {
            if (m_SpawnTimer != null)
            {
                m_SpawnTimer.Stop();
                m_SpawnTimer = null;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (Petals > 0)
            {
                from.AddToBackpack(new RoseOfTrinsicPetal(Petals));
                Petals = 0;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(m_Petals);
            writer.WriteDeltaTime(m_NextSpawnTime);
            writer.WriteEncodedInt((int)Level);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Petals = reader.ReadEncodedInt();
            m_NextSpawnTime = reader.ReadDeltaTime();
            Level = (SecureLevel)reader.ReadEncodedInt();

            if (m_Petals < 10)
            {
                StartSpawnTimer(m_NextSpawnTime - Core.Now);
            }
        }

        private class SpawnTimer : Timer
        {
            private readonly RoseOfTrinsic m_Rose;

            public SpawnTimer(RoseOfTrinsic rose, TimeSpan delay) : base(delay)
            {
                m_Rose = rose;
            }

            protected override void OnTick()
            {
                if (m_Rose.Deleted)
                {
                    return;
                }

                m_Rose.m_SpawnTimer = null;
                m_Rose.Petals++;
            }
        }
    }

    public class RoseOfTrinsicPetal : Item
    {
        [Constructible]
        public RoseOfTrinsicPetal(int amount = 1) : base(0x1021)
        {
            Stackable = true;
            Amount = amount;

            Weight = 1.0;
            Hue = 0xE;
        }

        public RoseOfTrinsicPetal(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1062926; // Petal of the Rose of Trinsic

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
            }
            else if (from.GetStatMod("RoseOfTrinsicPetal") != null)
            {
                from.SendLocalizedMessage(
                    1062927
                ); // You have eaten one of these recently and eating another would provide no benefit.
            }
            else
            {
                from.PlaySound(0x1EE);
                from.AddStatMod(new StatMod(StatType.Str, "RoseOfTrinsicPetal", 5, TimeSpan.FromMinutes(5.0)));

                Consume();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
