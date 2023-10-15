using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Server.Network;

namespace Server;

public delegate TimeSpan SkillUseCallback(Mobile user);

public enum SkillLock : byte
{
    Up = 0,
    Down = 1,
    Locked = 2
}

public enum SkillName
{
    Alchemy = 0,
    Anatomy = 1,
    AnimalLore = 2,
    ItemID = 3,
    ArmsLore = 4,
    Parry = 5,
    Begging = 6,
    Blacksmith = 7,
    Fletching = 8,
    Peacemaking = 9,
    Camping = 10,
    Carpentry = 11,
    Cartography = 12,
    Cooking = 13,
    DetectHidden = 14,
    Discordance = 15,
    EvalInt = 16,
    Healing = 17,
    Fishing = 18,
    Forensics = 19,
    Herding = 20,
    Hiding = 21,
    Provocation = 22,
    Inscribe = 23,
    Lockpicking = 24,
    Magery = 25,
    MagicResist = 26,
    Tactics = 27,
    Snooping = 28,
    Musicianship = 29,
    Poisoning = 30,
    Archery = 31,
    SpiritSpeak = 32,
    Stealing = 33,
    Tailoring = 34,
    AnimalTaming = 35,
    TasteID = 36,
    Tinkering = 37,
    Tracking = 38,
    Veterinary = 39,
    Swords = 40,
    Macing = 41,
    Fencing = 42,
    Wrestling = 43,
    Lumberjacking = 44,
    Mining = 45,
    Meditation = 46,
    Stealth = 47,
    RemoveTrap = 48,
    Necromancy = 49,
    Focus = 50,
    Chivalry = 51,
    Bushido = 52,
    Ninjitsu = 53,
    Spellweaving = 54,
    Mysticism = 55,
    Imbuing = 56,
    Throwing = 57
}

public enum Stat
{
    Str,
    Dex,
    Int
}

[PropertyObject]
public class Skill
{
    private ushort m_Base;
    private ushort m_Cap;

    public Skill(Skills owner, SkillInfo info, IGenericReader reader)
    {
        Owner = owner;
        Info = info;

        int version = reader.ReadByte();

        switch (version)
        {
            case 0:
                {
                    m_Base = reader.ReadUShort();
                    m_Cap = reader.ReadUShort();
                    Lock = (SkillLock)reader.ReadByte();

                    break;
                }
            case 0xFF:
                {
                    m_Base = 0;
                    m_Cap = 1000;
                    Lock = SkillLock.Up;

                    break;
                }
            default:
                {
                    if ((version & 0xC0) == 0x00)
                    {
                        if ((version & 0x1) != 0)
                        {
                            m_Base = reader.ReadUShort();
                        }

                        if ((version & 0x2) != 0)
                        {
                            m_Cap = reader.ReadUShort();
                        }
                        else
                        {
                            m_Cap = 1000;
                        }

                        if ((version & 0x4) != 0)
                        {
                            Lock = (SkillLock)reader.ReadByte();
                        }
                    }

                    break;
                }
        }

        if (Lock > SkillLock.Locked)
        {
            Lock = SkillLock.Up;
        }
    }

    public Skill(Skills owner, SkillInfo info, int baseValue, int cap, SkillLock skillLock)
    {
        Owner = owner;
        Info = info;
        m_Base = (ushort)baseValue;
        m_Cap = (ushort)cap;
        Lock = skillLock;
    }

    public Skills Owner { get; }

    public SkillName SkillName => (SkillName)Info.SkillID;

    public int SkillID => Info.SkillID;

    [CommandProperty(AccessLevel.Counselor)]
    public string Name => Info.Name;

    public SkillInfo Info { get; }

    [CommandProperty(AccessLevel.Counselor)]
    public SkillLock Lock { get; private set; }

    public int BaseFixedPoint
    {
        get => m_Base;
        set
        {
            var sv = (ushort)Math.Clamp(value, 0, 0xFFFF);

            int oldBase = m_Base;

            if (m_Base != sv)
            {
                Owner.Total = Owner.Total - m_Base + sv;

                m_Base = sv;

                Owner.OnSkillChange(this);

                var m = Owner.Owner;

                m?.OnSkillChange(SkillName, (double)oldBase / 10);
            }
        }
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public double Base
    {
        get => m_Base / 10.0;
        set => BaseFixedPoint = (int)(value * 10.0);
    }

    public int CapFixedPoint
    {
        get => m_Cap;
        set
        {
            var sv = (ushort)Math.Clamp(value, 0, 0xFFFF);

            if (m_Cap != sv)
            {
                m_Cap = sv;

                Owner.OnSkillChange(this);
            }
        }
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public double Cap
    {
        get => m_Cap / 10.0;
        set => CapFixedPoint = (int)(value * 10.0);
    }

    public int Fixed => (int)(Value * 10);

    [CommandProperty(AccessLevel.Counselor)]
    public double Value
    {
        get
        {
            // There has to be this distinction between the racial values and not to account for gaining skills and these skills aren't displayed nor Totaled up.
            var value = NonRacialValue;

            var raceBonus = Owner.Owner.RacialSkillBonus;

            if (raceBonus > value)
            {
                value = raceBonus;
            }

            return value;
        }
    }

    [CommandProperty(AccessLevel.Counselor)]
    public double NonRacialValue
    {
        get
        {
            var baseValue = Base;
            var statsOffset = Owner.Owner.RawStr * Info.StrScale +
                              Owner.Owner.RawDex * Info.DexScale +
                              Owner.Owner.RawInt * Info.IntScale;

            var inv = 100.0 - baseValue;

            if (inv <= 0.0)
            {
                statsOffset = 0.0;
            }
            else
            {
                statsOffset *= inv;

                if (Info.StatTotal > 0)
                {
                    var statTotal = Info.StatTotal * inv;

                    if (statsOffset > statTotal)
                    {
                        statsOffset = statTotal;
                    }
                }
            }

            var value = baseValue + statsOffset;

            Owner.Owner.ValidateSkillMods();

            double bonusObey = 0.0, bonusNotObey = 0.0;

            var mods = Owner.Owner.SkillMods;

            if (mods != null)
            {
                foreach (var mod in mods)
                {
                    if (mod.Skill != (SkillName)Info.SkillID)
                    {
                        continue;
                    }

                    if (mod.Relative)
                    {
                        if (mod.ObeyCap)
                        {
                            bonusObey += mod.Value;
                        }
                        else
                        {
                            bonusNotObey += mod.Value;
                        }
                    }
                    else
                    {
                        bonusObey = 0.0;
                        bonusNotObey = 0.0;
                        value = mod.Value;
                    }
                }
            }

            value += bonusNotObey;

            if (value < Cap)
            {
                value += bonusObey;

                if (value > Cap)
                {
                    value = Cap;
                }
            }

            return value;
        }
    }

    public override string ToString() => $"[{Name}: {Base}]";

    public void SetLockNoRelay(SkillLock skillLock)
    {
        if (skillLock > SkillLock.Locked)
        {
            return;
        }

        Lock = skillLock;
    }

    public void Serialize(IGenericWriter writer)
    {
        if (m_Base == 0 && m_Cap == 1000 && Lock == SkillLock.Up)
        {
            writer.Write((byte)0xFF); // default
        }
        else
        {
            var flags = 0x0;

            if (m_Base != 0)
            {
                flags |= 0x1;
            }

            if (m_Cap != 1000)
            {
                flags |= 0x2;
            }

            if (Lock != SkillLock.Up)
            {
                flags |= 0x4;
            }

            writer.Write((byte)flags); // version

            if (m_Base != 0)
            {
                writer.Write((short)m_Base);
            }

            if (m_Cap != 1000)
            {
                writer.Write((short)m_Cap);
            }

            if (Lock != SkillLock.Up)
            {
                writer.Write((byte)Lock);
            }
        }
    }

    public void Update()
    {
        Owner.OnSkillChange(this);
    }
}

public class SkillInfo
{
    [JsonConstructor]
    public SkillInfo(
        int skillID, string name, double strScale, double dexScale, double intScale, string title,
        SkillUseCallback callback, double strGain, double dexGain, double intGain, double gainFactor,
        string professionSkillName, Stat primaryStat, Stat secondaryStat
    )
    {
        Name = name;
        Title = title;
        SkillID = skillID;
        StrScale = strScale / 100.0;
        DexScale = dexScale / 100.0;
        IntScale = intScale / 100.0;
        Callback = callback;
        StrGain = strGain;
        DexGain = dexGain;
        IntGain = intGain;
        GainFactor = gainFactor;
        ProfessionSkillName = professionSkillName ?? Name.RemoveOrdinal(" ");
        StatTotal = strScale + dexScale + intScale;
        PrimaryStat = primaryStat;
        SecondaryStat = secondaryStat;
    }

    public SkillUseCallback Callback { get; set; }

    public int SkillID { get; }

    public string Name { get; set; }

    public string Title { get; set; }

    public double StrScale { get; set; }

    public double DexScale { get; set; }

    public double IntScale { get; set; }

    public double StatTotal { get; set; }

    public double StrGain { get; set; }

    public double DexGain { get; set; }

    public double IntGain { get; set; }

    public double GainFactor { get; set; }

    public string ProfessionSkillName { get; set; }

    public Stat PrimaryStat { get; set; }

    public Stat SecondaryStat { get; set; }

    public static SkillInfo[] Table { get; set; } = Array.Empty<SkillInfo>();
}

[PropertyObject]
public class Skills
{
    private readonly Skill[] m_Skills;
    private Skill m_Highest;

    public Skills(Mobile owner)
    {
        Owner = owner;
        Cap = 7000;

        var info = SkillInfo.Table;

        m_Skills = new Skill[info.Length];
    }

    public Skills(Mobile owner, IGenericReader reader)
    {
        Owner = owner;

        var version = reader.ReadInt();

        switch (version)
        {
            case 3:
            case 2:
                {
                    Cap = reader.ReadInt();

                    goto case 1;
                }
            case 1:
                {
                    if (version < 2)
                    {
                        Cap = 7000;
                    }

                    /*m_Total =*/
                    if (version < 3)
                    {
                        reader.ReadInt();
                    }

                    var info = SkillInfo.Table;

                    m_Skills = new Skill[info.Length];

                    var count = reader.ReadInt();

                    for (var i = 0; i < count; ++i)
                    {
                        if (i < info.Length)
                        {
                            var sk = new Skill(this, info[i], reader);

                            if (sk.BaseFixedPoint != 0 || sk.CapFixedPoint != 1000 || sk.Lock != SkillLock.Up)
                            {
                                m_Skills[i] = sk;
                                Total += sk.BaseFixedPoint;
                            }
                        }
                        else
                        {
                            // Will be discarded
                            _ = new Skill(this, null, reader);
                        }
                    }

                    // for ( int i = count; i < info.Length; ++i )
                    // m_Skills[i] = new Skill( this, info[i], 0, 1000, SkillLock.Up );

                    break;
                }
            case 0:
                {
                    reader.ReadInt();

                    goto case 1;
                }
        }
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Cap { get; set; }

    public int Total { get; set; }

    public Mobile Owner { get; }

    public int Length => m_Skills.Length;

    public Skill this[SkillName name] => this[(int)name];

    public Skill this[int skillID]
    {
        get
        {
            if (skillID < 0 || skillID >= m_Skills.Length)
            {
                return null;
            }

            var sk = m_Skills[skillID];

            if (sk == null)
            {
                m_Skills[skillID] = sk = new Skill(this, SkillInfo.Table[skillID], 0, 1000, SkillLock.Up);
            }

            return sk;
        }
    }

    public Skill Highest
    {
        get
        {
            if (m_Highest == null)
            {
                Skill highest = null;
                var value = int.MinValue;

                for (var i = 0; i < m_Skills.Length; ++i)
                {
                    var sk = m_Skills[i];

                    if (sk?.BaseFixedPoint > value)
                    {
                        value = sk.BaseFixedPoint;
                        highest = sk;
                    }
                }

                m_Highest = highest == null && m_Skills.Length > 0 ? this[0] : highest;
            }

            return m_Highest;
        }
    }

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Alchemy => this[SkillName.Alchemy];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Anatomy => this[SkillName.Anatomy];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill AnimalLore => this[SkillName.AnimalLore];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill ItemID => this[SkillName.ItemID];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill ArmsLore => this[SkillName.ArmsLore];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Parry => this[SkillName.Parry];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Begging => this[SkillName.Begging];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Blacksmith => this[SkillName.Blacksmith];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Fletching => this[SkillName.Fletching];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Peacemaking => this[SkillName.Peacemaking];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Camping => this[SkillName.Camping];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Carpentry => this[SkillName.Carpentry];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Cartography => this[SkillName.Cartography];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Cooking => this[SkillName.Cooking];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill DetectHidden => this[SkillName.DetectHidden];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Discordance => this[SkillName.Discordance];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill EvalInt => this[SkillName.EvalInt];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Healing => this[SkillName.Healing];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Fishing => this[SkillName.Fishing];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Forensics => this[SkillName.Forensics];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Herding => this[SkillName.Herding];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Hiding => this[SkillName.Hiding];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Provocation => this[SkillName.Provocation];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Inscribe => this[SkillName.Inscribe];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Lockpicking => this[SkillName.Lockpicking];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Magery => this[SkillName.Magery];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill MagicResist => this[SkillName.MagicResist];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Tactics => this[SkillName.Tactics];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Snooping => this[SkillName.Snooping];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Musicianship => this[SkillName.Musicianship];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Poisoning => this[SkillName.Poisoning];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Archery => this[SkillName.Archery];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill SpiritSpeak => this[SkillName.SpiritSpeak];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Stealing => this[SkillName.Stealing];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Tailoring => this[SkillName.Tailoring];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill AnimalTaming => this[SkillName.AnimalTaming];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill TasteID => this[SkillName.TasteID];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Tinkering => this[SkillName.Tinkering];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Tracking => this[SkillName.Tracking];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Veterinary => this[SkillName.Veterinary];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Swords => this[SkillName.Swords];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Macing => this[SkillName.Macing];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Fencing => this[SkillName.Fencing];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Wrestling => this[SkillName.Wrestling];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Lumberjacking => this[SkillName.Lumberjacking];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Mining => this[SkillName.Mining];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Meditation => this[SkillName.Meditation];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Stealth => this[SkillName.Stealth];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill RemoveTrap => this[SkillName.RemoveTrap];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Necromancy => this[SkillName.Necromancy];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Focus => this[SkillName.Focus];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Chivalry => this[SkillName.Chivalry];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Bushido => this[SkillName.Bushido];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Ninjitsu => this[SkillName.Ninjitsu];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Spellweaving => this[SkillName.Spellweaving];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Mysticism => this[SkillName.Mysticism];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Imbuing => this[SkillName.Imbuing];

    [CommandProperty(AccessLevel.Counselor, canModify: true)]
    public Skill Throwing => this[SkillName.Throwing];

    public override string ToString() => "...";

    public static bool UseSkill(Mobile from, SkillName name) => UseSkill(from, (int)name);

    public static bool UseSkill(Mobile from, int skillID)
    {
        if (!from.CheckAlive())
        {
            return false;
        }

        if (!from.Region.OnSkillUse(from, skillID))
        {
            return false;
        }

        if (!from.AllowSkillUse((SkillName)skillID))
        {
            return false;
        }

        if (skillID >= 0 && skillID < SkillInfo.Table.Length)
        {
            var info = SkillInfo.Table[skillID];

            if (info.Callback != null)
            {
                if (Core.TickCount - from.NextSkillTime >= 0 && from.Spell == null)
                {
                    from.DisruptiveAction();

                    from.NextSkillTime = Core.TickCount + (int)info.Callback(from).TotalMilliseconds;

                    return true;
                }

                from.SendSkillMessage();
            }
            else
            {
                from.SendLocalizedMessage(500014); // That skill cannot be used directly.
            }
        }

        return false;
    }

    public void Serialize(IGenericWriter writer)
    {
        Total = 0;

        writer.Write(3); // version

        writer.Write(Cap);
        writer.Write(m_Skills.Length);

        for (var i = 0; i < m_Skills.Length; ++i)
        {
            var sk = m_Skills[i];

            if (sk == null)
            {
                writer.Write((byte)0xFF);
            }
            else
            {
                sk.Serialize(writer);
                Total += sk.BaseFixedPoint;
            }
        }
    }

    public void OnSkillChange(Skill skill)
    {
        if (skill == m_Highest) // could be downgrading the skill, force a recalc
        {
            m_Highest = null;
        }
        else if (skill.BaseFixedPoint > m_Highest?.BaseFixedPoint)
        {
            m_Highest = skill;
        }

        Owner.OnSkillInvalidated(skill);
        Owner.NetState.SendSkillChange(skill);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(m_Skills);

    public ref struct Enumerator
    {
        private readonly Skill[] _skills;
        private int _index;
        private Skill _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(Skill[] skills)
        {
            _skills = skills;
            _index = 0;
            _current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            Skill[] localList = _skills;

            while ((uint)_index < (uint)localList.Length)
            {
                _current = localList[_index++];
                if (_current != null)
                {
                    return true;
                }
            }

            return false;
        }

        public Skill Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}
