using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Server.Collections;
using Server.Json;
using Server.Mobiles;

namespace Server.Misc;

public static class AntiMacroSystem
{
    // *** NOTE ***: Modifying these values will not change an already created antimacro.json file!
    private static readonly bool[] _antiMacroSkillDefaults =
    {
        false, // Alchemy = 0,
        true,  // Anatomy = 1,
        true,  // AnimalLore = 2,
        true,  // ItemID = 3,
        true,  // ArmsLore = 4,
        false, // Parry = 5,
        true,  // Begging = 6,
        false, // Blacksmith = 7,
        false, // Fletching = 8,
        true,  // Peacemaking = 9,
        true,  // Camping = 10,
        false, // Carpentry = 11,
        false, // Cartography = 12,
        false, // Cooking = 13,
        true,  // DetectHidden = 14,
        true,  // Discordance = 15,
        true,  // EvalInt = 16,
        true,  // Healing = 17,
        true,  // Fishing = 18,
        true,  // Forensics = 19,
        true,  // Herding = 20,
        true,  // Hiding = 21,
        true,  // Provocation = 22,
        false, // Inscribe = 23,
        true,  // Lockpicking = 24,
        true,  // Magery = 25,
        true,  // MagicResist = 26,
        false, // Tactics = 27,
        true,  // Snooping = 28,
        true,  // Musicianship = 29,
        true,  // Poisoning = 30,
        false, // Archery = 31,
        true,  // SpiritSpeak = 32,
        true,  // Stealing = 33,
        false, // Tailoring = 34,
        true,  // AnimalTaming = 35,
        true,  // TasteID = 36,
        false, // Tinkering = 37,
        true,  // Tracking = 38,
        true,  // Veterinary = 39,
        false, // Swords = 40,
        false, // Macing = 41,
        false, // Fencing = 42,
        false, // Wrestling = 43,
        true,  // Lumberjacking = 44,
        true,  // Mining = 45,
        true,  // Meditation = 46,
        true,  // Stealth = 47,
        true,  // RemoveTrap = 48,
        true,  // Necromancy = 49,
        false, // Focus = 50,
        true,  // Chivalry = 51
        true,  // Bushido = 52
        true,  // Ninjitsu = 53
        true,  // Spellweaving
        true,  // Mysticism = 55
        true,  // Imbuing = 56
        false, // Throwing = 57
    };

    private static Dictionary<Mobile, PlayerAntiMacro> _antiMacroTable;
    private static Dictionary<Mobile, Timer> _logoutCleanup;

    private const string _antiMacroPath = "Configuration/antimacro.json";
    public static AntiMacroSettings Settings { get; private set; }

    public static void Configure()
    {
        var path = Path.Combine(Core.BaseDirectory, _antiMacroPath);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<AntiMacroSettings>(path);
        }
        else
        {
            Settings = new AntiMacroSettings
            {
                Enabled = false,
                Allowance = 3,
                LocationSize = 5,
                Expire = TimeSpan.FromMinutes(5.0),
                SkillsThatUseAntiMacro = new BitArray(_antiMacroSkillDefaults)
            };

            JsonConfig.Serialize(Path.Join(Core.BaseDirectory, _antiMacroPath), Settings);
        }
    }

    public static void Initialize()
    {
        EventSink.WorldSave += OnWorldSave;
        EventSink.Logout += OnLogout;
        EventSink.Login += OnLogin;
    }

    private static void OnWorldSave()
    {
        if (_antiMacroTable == null)
        {
            return;
        }

        var now = Core.Now;

        using var toRemove = PooledRefQueue<Mobile>.Create();
        foreach (var (m, antiMacro) in _antiMacroTable)
        {
            if (antiMacro._lastExpiration <= now)
            {
                toRemove.Enqueue(m);
            }
            else
            {
                antiMacro.CleanExpired();
            }
        }

        while (toRemove.Count > 0)
        {
            _antiMacroTable.Remove(toRemove.Dequeue());
        }
    }

    private static void OnLogin(Mobile m)
    {
        // Stop the clear out timer
        if (_logoutCleanup?.Remove(m, out var timer) == true)
        {
            timer.Stop();
        }
    }

    private static void OnLogout(Mobile m)
    {
        if (_antiMacroTable?.TryGetValue(m, out var antiMacro) != true)
        {
            return;
        }

        if (antiMacro._lastExpiration < Core.Now)
        {
            _antiMacroTable.Remove(m);
            return;
        }

        _logoutCleanup ??= new Dictionary<Mobile, Timer>();
        if (_logoutCleanup.TryGetValue(m, out var timer))
        {
            timer.Stop();
        }
        else
        {
            _logoutCleanup[m] = timer = Timer.DelayCall(Settings.Expire, CleanupPlayer, m);
        }

        timer.Start();
    }

    public static void CleanupPlayer(Mobile pm)
    {
        if (_antiMacroTable?.Remove(pm, out var antiMacro) == true)
        {
            // Hint to GC that we don't want this
            antiMacro._antiMacroTracking.Clear();
            antiMacro._antiMacroTracking = null;
        }

        if (_logoutCleanup?.Remove(pm, out var timer) == true)
        {
            timer.Stop();
        }
    }

    public static bool UseAntiMacro(int skillId) =>
        skillId >= 0 && skillId < Settings.SkillsThatUseAntiMacro.Length && Settings.SkillsThatUseAntiMacro[skillId];

    public static bool AntiMacroCheck(PlayerMobile pm, Skill skill, object obj)
    {
        if (!Settings.Enabled || obj == null || pm.AccessLevel != AccessLevel.Player || !UseAntiMacro(skill.Info.SkillID))
        {
            return true;
        }

        _antiMacroTable ??= new Dictionary<Mobile, PlayerAntiMacro>();

        // Hot path so use optimized code
        ref PlayerAntiMacro antiMacro = ref CollectionsMarshal.GetValueRefOrAddDefault(_antiMacroTable, pm, out var exists);
        if (!exists)
        {
            antiMacro = new PlayerAntiMacro();
        }

        return antiMacro.AntiMacroCheck(skill, obj);
    }

    public record AntiMacroSettings
    {
        // How many times may we use the same location/target for gain
        public int Allowance { get; init; }

        // The size of each location, make this smaller so players dont have to move as far
        public int LocationSize { get; init; }

        public bool Enabled { get; init; }

        // How long do we remember targets/locations?
        public TimeSpan Expire { get; init; }

        [JsonConverter(typeof(BitArrayEnumIndexConverter<SkillName>))]
        public BitArray SkillsThatUseAntiMacro { get; init; }
    }

    private class PlayerAntiMacro
    {
        // This can get quite large. If a player is logged in for a while, this can be promoted to Gen 2 and
        // become a memory leak.
        public Dictionary<(Skill, object), CountAndTimeStamp> _antiMacroTracking = new();
        public DateTime _lastExpiration;

        public bool AntiMacroCheck(Skill skill, object obj)
        {
            var now = Core.Now;

            // Potential hot path, so use optimized code
            ref CountAndTimeStamp _countTimeStamp =
                ref CollectionsMarshal.GetValueRefOrAddDefault(_antiMacroTracking, (skill, obj), out bool exists);

            _countTimeStamp._count++;

            if (!exists || _countTimeStamp._expiration <= now || _countTimeStamp._count < Settings.Allowance)
            {
                _countTimeStamp._expiration = _lastExpiration = now + Settings.Expire;
                return true;
            }

            return false;
        }

        public void CleanExpired()
        {
            var now = Core.Now;

            using var toRemove = PooledRefQueue<(Skill, object)>.Create();

            foreach (var (key, countAndTimeStamp) in _antiMacroTracking)
            {
                if (countAndTimeStamp._count <= 0 || countAndTimeStamp._expiration <= now)
                {
                    toRemove.Enqueue(key);
                }
            }

            while (toRemove.Count > 0)
            {
                _antiMacroTracking.Remove(toRemove.Dequeue());
            }
        }
    }

    private struct CountAndTimeStamp
    {
        public int _count;
        public DateTime _expiration;
    }
}
