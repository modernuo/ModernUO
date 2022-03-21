using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items
{
    public enum CampfireStatus
    {
        Burning,
        Extinguishing,
        Off
    }

    public class Campfire : Item
    {
        public static readonly int SecureRange = 7;

        private static readonly Dictionary<Mobile, CampfireEntry> m_Table = new();

        private readonly List<CampfireEntry> m_Entries;

        private TimerExecutionToken _timerToken;

        public Campfire() : base(0xDE3)
        {
            Movable = false;
            Light = LightType.Circle300;

            m_Entries = new List<CampfireEntry>();

            Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), OnTick, out _timerToken);
        }

        public Campfire(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CampfireStatus Status
        {
            get
            {
                return ItemID switch
                {
                    0xDE3 => CampfireStatus.Burning,
                    0xDE9 => CampfireStatus.Extinguishing,
                    _     => CampfireStatus.Off
                };
            }
            set
            {
                if (Status == value)
                {
                    return;
                }

                switch (value)
                {
                    case CampfireStatus.Burning:
                        {
                            ItemID = 0xDE3;
                            Light = LightType.Circle300;
                            break;
                        }

                    case CampfireStatus.Extinguishing:
                        {
                            ItemID = 0xDE9;
                            Light = LightType.Circle150;
                            break;
                        }

                    default:
                        {
                            ItemID = 0xDEA;
                            Light = LightType.ArchedWindowEast;
                            ClearEntries();
                            break;
                        }
                }
            }
        }

        public static CampfireEntry GetEntry(Mobile player)
        {
            m_Table.TryGetValue(player, out var value);
            return value;
        }

        public static void RemoveEntry(CampfireEntry entry)
        {
            m_Table.Remove(entry.Player);
            entry.Fire.m_Entries.Remove(entry);
        }

        private void OnTick()
        {
            var now = Core.Now;
            var age = now - Created;

            if (age >= TimeSpan.FromSeconds(100.0))
            {
                Delete();
            }
            else if (age >= TimeSpan.FromSeconds(90.0))
            {
                Status = CampfireStatus.Off;
            }
            else if (age >= TimeSpan.FromSeconds(60.0))
            {
                Status = CampfireStatus.Extinguishing;
            }

            if (Status == CampfireStatus.Off || Deleted)
            {
                return;
            }

            for (var i = m_Entries.Count - 1; i >= 0; i--)
            {
                var entry = m_Entries[i];

                if (!entry.Valid || entry.Player.NetState == null)
                {
                    RemoveEntry(entry);
                }
                else if (!entry.Safe && now - entry.Start >= TimeSpan.FromSeconds(30.0))
                {
                    entry.Safe = true;
                    entry.Player.SendLocalizedMessage(500621); // The camp is now secure.
                }
            }

            var eable = GetClientsInRange(SecureRange);

            foreach (var state in eable)
            {
                if (state.Mobile is PlayerMobile pm && GetEntry(pm) == null)
                {
                    var entry = new CampfireEntry(pm, this);

                    m_Table[pm] = entry;
                    m_Entries.Add(entry);

                    pm.SendLocalizedMessage(500620); // You feel it would take a few moments to secure your camp.
                }
            }

            eable.Free();
        }

        private void ClearEntries()
        {
            if (m_Entries == null)
            {
                return;
            }

            foreach (var entry in m_Entries)
            {
                m_Table.Remove(entry.Player);
            }

            m_Entries.Clear();
            m_Entries.TrimExcess();
        }

        public override void OnAfterDelete()
        {
            _timerToken.Cancel();

            ClearEntries();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Delete();
        }
    }

    public class CampfireEntry
    {
        private bool m_Safe;

        public CampfireEntry(PlayerMobile player, Campfire fire)
        {
            Player = player;
            Fire = fire;
            Start = Core.Now;
            m_Safe = false;
        }

        public PlayerMobile Player { get; }

        public Campfire Fire { get; }

        public DateTime Start { get; }

        public bool Valid => !Fire.Deleted && Fire.Status != CampfireStatus.Off && Player.Map == Fire.Map &&
                             Player.InRange(Fire, Campfire.SecureRange);

        public bool Safe
        {
            get => Valid && m_Safe;
            set => m_Safe = value;
        }
    }
}
