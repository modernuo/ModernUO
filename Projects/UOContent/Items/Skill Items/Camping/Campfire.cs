using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

public enum CampfireStatus
{
    Burning,
    Extinguishing,
    Off
}

[SerializationGenerator(0, false)]
public partial class Campfire : Item
{
    public const int SecureRange = 7;

    private static readonly Dictionary<Mobile, CampfireEntry> _table = [];

    private readonly List<CampfireEntry> _entries;

    private TimerExecutionToken _timerToken;

    public Campfire() : base(0xDE3)
    {
        Movable = false;
        Light = LightType.Circle300;

        _entries = [];
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), OnTick, out _timerToken);
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

    public static CampfireEntry GetEntry(Mobile player) => _table.GetValueOrDefault(player);

    public static void RemoveEntry(CampfireEntry entry)
    {
        _table.Remove(entry.Player);
        entry.Fire._entries.Remove(entry);
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

        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];

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

        foreach (var state in GetClientsInRange(SecureRange))
        {
            if (state.Mobile is PlayerMobile pm && GetEntry(pm) == null)
            {
                var entry = new CampfireEntry(pm, this);

                _table[pm] = entry;
                _entries.Add(entry);

                pm.SendLocalizedMessage(500620); // You feel it would take a few moments to secure your camp.
            }
        }
    }

    private void ClearEntries()
    {
        if (_entries == null)
        {
            return;
        }

        foreach (var entry in _entries)
        {
            _table.Remove(entry.Player);
        }

        _entries.Clear();
        _entries.TrimExcess();
    }

    public override void OnAfterDelete()
    {
        _timerToken.Cancel();
        ClearEntries();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Delete();
    }
}

public class CampfireEntry
{
    private bool _safe;

    public CampfireEntry(PlayerMobile player, Campfire fire)
    {
        Player = player;
        Fire = fire;
        Start = Core.Now;
        _safe = false;
    }

    public PlayerMobile Player { get; }
    public Campfire Fire { get; }
    public DateTime Start { get; }

    public bool Valid => !Fire.Deleted && Fire.Status != CampfireStatus.Off && Player.Map == Fire.Map &&
                         Player.InRange(Fire, Campfire.SecureRange);

    public bool Safe
    {
        get => Valid && _safe;
        set => _safe = value;
    }
}
