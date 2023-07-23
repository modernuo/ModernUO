using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Text;

namespace Server.Items;

[Flags]
public enum TeleporterFlags
{
    None = 0x00000000,
    Active = 0x00000001,
    Creatures = 0x00000002,
    CombatCheck = 0x00000004,
    CriminalCheck = 0x00000008,
    SourceEffect = 0x00000010,
    DestEffect = 0x00000020
}

[SerializationGenerator(5, false)]
public partial class Teleporter : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TeleporterFlags _flags;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _delay;

    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _soundID;

    [InvalidateProperties]
    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _pointDest;

    [InvalidateProperties]
    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _mapDest;

    [Constructible]
    public Teleporter() : this(new Point3D(0, 0, 0))
    {
    }

    [Constructible]
    public Teleporter(Point3D pointDest, Map mapDest = null, bool creatures = false) : base(0x1BC3)
    {
        Movable = false;
        Visible = false;

        _flags = TeleporterFlags.Active;
        Creatures = creatures;

        _pointDest = pointDest;
        _mapDest = mapDest;
    }

    public bool GetFlag(TeleporterFlags flag) => (Flags & flag) != 0;

    public void SetFlag(TeleporterFlags flag, bool value)
    {
        if (value)
        {
            Flags |= flag;
        }
        else
        {
            Flags &= ~flag;
        }

        InvalidateProperties();
        this.MarkDirty();
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool SourceEffect
    {
        get => GetFlag(TeleporterFlags.SourceEffect);
        set => SetFlag(TeleporterFlags.SourceEffect, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DestEffect
    {
        get => GetFlag(TeleporterFlags.DestEffect);
        set => SetFlag(TeleporterFlags.DestEffect, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active
    {
        get => GetFlag(TeleporterFlags.Active);
        set => SetFlag(TeleporterFlags.Active, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Creatures
    {
        get => GetFlag(TeleporterFlags.Creatures);
        set => SetFlag(TeleporterFlags.Creatures, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool CombatCheck
    {
        get => GetFlag(TeleporterFlags.CombatCheck);
        set => SetFlag(TeleporterFlags.CombatCheck, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool CriminalCheck
    {
        get => GetFlag(TeleporterFlags.CriminalCheck);
        set => SetFlag(TeleporterFlags.CriminalCheck, value);
    }

    public override int LabelNumber => 1026095; // teleporter

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Active)
        {
            list.Add(1060742); // active
        }
        else
        {
            list.Add(1060743); // inactive
        }

        if (_mapDest != null)
        {
            list.Add(1060658, $"{"Map"}\t{_mapDest}");
        }

        if (_pointDest != Point3D.Zero)
        {
            list.Add(1060659, $"{"Coords"}\t{_pointDest}");
        }

        list.Add(1060660, $"{"Creatures"}\t{(Creatures ? "Yes" : "No")}");
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (Active)
        {
            if (_mapDest != null && _pointDest != Point3D.Zero)
            {
                LabelTo(from, $"{_pointDest} [{_mapDest}]");
            }
            else if (_mapDest != null)
            {
                LabelTo(from, $"[{_mapDest}]");
            }
            else if (_pointDest != Point3D.Zero)
            {
                LabelTo(from, _pointDest.ToString());
            }
        }
        else
        {
            LabelTo(from, "(inactive)");
        }
    }

    public virtual bool CanTeleport(Mobile m)
    {
        if (!Creatures && !m.Player)
        {
            return false;
        }

        if (CriminalCheck && m.Criminal)
        {
            m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
            return false;
        }

        if (CombatCheck && SpellHelper.CheckCombat(m))
        {
            m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            return false;
        }

        return true;
    }

    public virtual void StartTeleport(Mobile m)
    {
        if (_delay == TimeSpan.Zero)
        {
            DoTeleport(m);
        }
        else
        {
            Timer.StartTimer(_delay, () => DoTeleport(m));
        }
    }

    public virtual void DoTeleport(Mobile m)
    {
        var map = _mapDest;

        if (map == null || map == Map.Internal)
        {
            map = m.Map;
        }

        var p = _pointDest;

        if (p == Point3D.Zero)
        {
            p = m.Location;
        }

        BaseCreature.TeleportPets(m, p, map);

        var sendEffect = !m.Hidden || m.AccessLevel == AccessLevel.Player;

        if (SourceEffect && sendEffect)
        {
            Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10);
        }

        m.MoveToWorld(p, map);

        if (DestEffect && sendEffect)
        {
            Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10);
        }

        if (_soundID > 0 && sendEffect)
        {
            Effects.PlaySound(m.Location, m.Map, _soundID);
        }
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (Active && CanTeleport(m))
        {
            StartTeleport(m);
            return false;
        }

        return true;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        if (reader.ReadBool())
        {
            _flags |= TeleporterFlags.CriminalCheck;
        }

        if (reader.ReadBool())
        {
            _flags |= TeleporterFlags.CombatCheck;
        }

        if (reader.ReadBool())
        {
            _flags |= TeleporterFlags.SourceEffect;
        }

        if (reader.ReadBool())
        {
            _flags |= TeleporterFlags.DestEffect;
        }

        _delay = reader.ReadTimeSpan();
        _soundID = reader.ReadEncodedInt();

        if (reader.ReadBool())
        {
            _flags |= TeleporterFlags.Creatures;
        }

        if (reader.ReadBool())
        {
            _flags |= TeleporterFlags.Active;
        }

        _pointDest = reader.ReadPoint3D();
        _mapDest = reader.ReadMap();
    }
}

[SerializationGenerator(1, false)]
public partial class SkillTeleporter : Teleporter
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SkillName _skill;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private double _required;

    [CanBeNull]
    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TextDefinition _message;

    [Constructible]
    public SkillTeleporter()
    {
    }

    public override bool CanTeleport(Mobile m)
    {
        if (!base.CanTeleport(m))
        {
            return false;
        }

        var sk = m.Skills[_skill];

        if (sk?.Base >= _required)
        {
            return true;
        }

        if (!m.BeginAction(this))
        {
            return false;
        }

        if (_message?.Number > 0)
        {
            m.NetState.SendMessageLocalized(
                Serial,
                ItemID,
                MessageType.Regular,
                0x3B2,
                3,
                _message.Number,
                null
            );
        }
        else if (!string.IsNullOrWhiteSpace(_message?.String))
        {
            m.NetState.SendMessage(
                Serial,
                ItemID,
                MessageType.Regular,
                0x3B2,
                3,
                false,
                "ENU",
                null,
                _message.String
            );
        }

        Timer.StartTimer(TimeSpan.FromSeconds(5.0), () => m.EndAction(this));

        return false;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var skillIndex = (int)_skill;
        string skillName;

        if (skillIndex >= 0 && skillIndex < SkillInfo.Table.Length)
        {
            skillName = SkillInfo.Table[skillIndex].Name;
        }
        else
        {
            skillName = "(Invalid)";
        }

        list.Add(1060661, $"{skillName}\t{_required:F1}");

        if (_message?.Number > 0)
        {
            list.Add(1060662, $"{"Message"}\t{_message.Number:#}");
        }
        else if (!string.IsNullOrWhiteSpace(_message?.String))
        {
            list.Add(1060662, $"{"Message"}\t{_message.String}");
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _skill = (SkillName)reader.ReadInt();
        _required = reader.ReadDouble();
        _message = reader.ReadString();

        var localNumber = reader.ReadInt();
        if (localNumber > 0)
        {
            _message = localNumber;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class KeywordTeleporter : Teleporter
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _substring;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _keyword;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _range;

    [Constructible]
    public KeywordTeleporter()
    {
        _keyword = -1;
        _substring = null;
    }

    public override bool HandlesOnSpeech => true;

    public override void OnSpeech(SpeechEventArgs e)
    {
        if (!e.Handled && Active)
        {
            var m = e.Mobile;

            if (!m.InRange(GetWorldLocation(), _range))
            {
                return;
            }

            var isMatch = false;

            if (_keyword >= 0 && e.HasKeyword(_keyword))
            {
                isMatch = true;
            }
            else if (_substring != null && e.Speech.InsensitiveContains(_substring))
            {
                isMatch = true;
            }

            if (!isMatch || !CanTeleport(m))
            {
                return;
            }

            e.Handled = true;
            StartTeleport(m);
        }
    }

    public override void DoTeleport(Mobile m)
    {
        if (!m.InRange(GetWorldLocation(), _range) || m.Map != Map)
        {
            return;
        }

        base.DoTeleport(m);
    }

    public override bool OnMoveOver(Mobile m) => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060661, $"{"Range"}\t{_range}");

        if (_keyword >= 0)
        {
            list.Add(1060662, $"{"Keyword"}\t{_keyword}");
        }

        if (_substring != null)
        {
            list.Add(1060663, $"{"Substring"}\t{_substring}");
        }
    }
}

[SerializationGenerator(1, false)]
public partial class WaitTeleporter : KeywordTeleporter
{
    private static Dictionary<Mobile, TeleportingInfo> _table = new();

    [CanBeNull]
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TextDefinition _startMessage;

    [CanBeNull]
    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TextDefinition _progressMessage;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _showTimeRemaining;

    [Constructible]
    public WaitTeleporter()
    {
    }

    public static void Initialize()
    {
        EventSink.Logout += EventSink_Logout;
    }

    public static void EventSink_Logout(Mobile from)
    {
        if (from != null && _table.Remove(from, out var info))
        {
            info.TimerToken.Cancel();
        }
    }

    public static string FormatTime(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            var h = (int)Math.Round(ts.TotalHours);
            return $"{h} hour{(h == 1 ? "" : "s")}";
        }

        if (ts.TotalMinutes >= 1)
        {
            var m = (int)Math.Round(ts.TotalMinutes);
            return $"{m} minute{(m == 1 ? "" : "s")}";
        }

        var s = Math.Max((int)Math.Round(ts.TotalSeconds), 0);
        return $"{s} second{(s == 1 ? "" : "s")}";
    }

    public override void StartTeleport(Mobile m)
    {
        if (_table.TryGetValue(m, out var info))
        {
            if (info.Teleporter == this)
            {
                if (!m.BeginAction(this))
                {
                    return;
                }

                _progressMessage.SendMessageTo(m);

                if (_showTimeRemaining)
                {
                    m.SendMessage($"Time remaining: {FormatTime(info.TimerToken.Next - Core.Now)}");
                }

                Timer.StartTimer(TimeSpan.FromSeconds(5), () => m.EndAction(this));

                return;
            }

            info.TimerToken.Cancel();
        }

        _startMessage.SendMessageTo(m);

        if (Delay == TimeSpan.Zero)
        {
            DoTeleport(m);
        }
        else
        {
            Timer.StartTimer(Delay, () => DoTeleport(m), out var timerToken);
            _table[m] = new TeleportingInfo(this, timerToken);
        }
    }

    public override void DoTeleport(Mobile m)
    {
        _table.Remove(m);

        base.DoTeleport(m);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        var number = reader.ReadInt();
        var message = reader.ReadString();

        _startMessage = number > 0 ? number : message;

        number = reader.ReadInt();
        message = reader.ReadString();

        _progressMessage = number > 0 ? number : message;

        _showTimeRemaining = reader.ReadBool();
    }

    private class TeleportingInfo
    {
        public TeleportingInfo(WaitTeleporter tele, TimerExecutionToken token)
        {
            Teleporter = tele;
            TimerToken = token;
        }

        public WaitTeleporter Teleporter { get; }

        public TimerExecutionToken TimerToken { get; }
    }
}

[SerializationGenerator(1, false)]
public partial class TimeoutTeleporter : Teleporter
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _timeoutDelay;

    private Dictionary<Mobile, TimerExecutionToken> _teleporting;

    [Constructible]
    public TimeoutTeleporter() : this(new Point3D(0, 0, 0))
    {
    }

    [Constructible]
    public TimeoutTeleporter(Point3D pointDest, Map mapDest = null, bool creatures = false)
        : base(pointDest, mapDest, creatures) => _teleporting = new Dictionary<Mobile, TimerExecutionToken>();

    public void StartTimer(Mobile m)
    {
        StartTimer(m, _timeoutDelay);
    }

    private void StartTimer(Mobile m, TimeSpan delay)
    {
        StopTimer(m);
        Timer.StartTimer(delay, () => StartTeleport(m), out var timerToken);
        _teleporting[m] = timerToken;
    }

    public void StopTimer(Mobile m)
    {
        if (_teleporting.Remove(m, out var t))
        {
            t.Cancel();
        }
    }

    public override void DoTeleport(Mobile m)
    {
        _teleporting.Remove(m);

        base.DoTeleport(m);
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (Active)
        {
            if (!CanTeleport(m))
            {
                return false;
            }

            StartTimer(m);
        }

        return true;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _timeoutDelay = reader.ReadTimeSpan();
        _teleporting = new Dictionary<Mobile, TimerExecutionToken>();

        var count = reader.ReadInt();

        for (var i = 0; i < count; ++i)
        {
            reader.ReadEntity<Mobile>();
            reader.ReadDateTime();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class TimeoutGoal : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeoutTeleporter _teleporter;

    [Constructible]
    public TimeoutGoal() : base(0x1822)
    {
        Movable = false;
        Visible = false;

        Hue = 1154;
    }

    public override string DefaultName => "timeout teleporter goal";

    public override bool OnMoveOver(Mobile m)
    {
        Teleporter?.StopTimer(m);
        return true;
    }
}

[SerializationGenerator(1, false)]
public partial class ConditionTeleporter : Teleporter
{
    [SerializableField(0, getter: "protected", setter: "protected")]
    private ConditionFlag _flags;

    [Constructible]
    public ConditionTeleporter()
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DenyMounted
    {
        get => GetFlag(ConditionFlag.DenyMounted);
        set
        {
            SetFlag(ConditionFlag.DenyMounted, value);
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DenyFollowers
    {
        get => GetFlag(ConditionFlag.DenyFollowers);
        set
        {
            SetFlag(ConditionFlag.DenyFollowers, value);
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DenyPackContents
    {
        get => GetFlag(ConditionFlag.DenyPackContents);
        set
        {
            SetFlag(ConditionFlag.DenyPackContents, value);
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DenyHolding
    {
        get => GetFlag(ConditionFlag.DenyHolding);
        set
        {
            SetFlag(ConditionFlag.DenyHolding, value);
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DenyEquipment
    {
        get => GetFlag(ConditionFlag.DenyEquipment);
        set
        {
            SetFlag(ConditionFlag.DenyEquipment, value);
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DenyTransformed
    {
        get => GetFlag(ConditionFlag.DenyTransformed);
        set
        {
            SetFlag(ConditionFlag.DenyTransformed, value);
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool StaffOnly
    {
        get => GetFlag(ConditionFlag.StaffOnly);
        set
        {
            SetFlag(ConditionFlag.StaffOnly, value);
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DenyPackEthereals
    {
        get => GetFlag(ConditionFlag.DenyPackEthereals);
        set
        {
            SetFlag(ConditionFlag.DenyPackEthereals, value);
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DeadOnly
    {
        get => GetFlag(ConditionFlag.DeadOnly);
        set
        {
            SetFlag(ConditionFlag.DeadOnly, value);
            InvalidateProperties();
        }
    }

    public override bool CanTeleport(Mobile m)
    {
        if (!base.CanTeleport(m))
        {
            return false;
        }

        if (GetFlag(ConditionFlag.StaffOnly) && m.AccessLevel < AccessLevel.Counselor)
        {
            return false;
        }

        if (GetFlag(ConditionFlag.DenyMounted) && m.Mounted)
        {
            m.SendLocalizedMessage(1077252); // You must dismount before proceeding.
            return false;
        }

        if (GetFlag(ConditionFlag.DenyFollowers) &&
            (m.Followers != 0 || m is PlayerMobile mobile && mobile.AutoStabled?.Count != 0))
        {
            m.SendLocalizedMessage(1077250); // No pets permitted beyond this point.
            return false;
        }

        var pack = m.Backpack;

        if (pack != null)
        {
            if (GetFlag(ConditionFlag.DenyPackContents) && pack.TotalItems != 0)
            {
                m.SendMessage("You must empty your backpack before proceeding.");
                return false;
            }

            if (GetFlag(ConditionFlag.DenyPackEthereals) &&
                pack.FindItemByType(new[] { typeof(EtherealMount), typeof(BaseImprisonedMobile) }) != null)
            {
                m.SendMessage("You must empty your backpack of ethereal mounts before proceeding.");
                return false;
            }
        }

        if (GetFlag(ConditionFlag.DenyHolding) && m.Holding != null)
        {
            m.SendMessage("You must let go of what you are holding before proceeding.");
            return false;
        }

        if (GetFlag(ConditionFlag.DenyEquipment))
        {
            foreach (var item in m.Items)
            {
                switch (item.Layer)
                {
                    case Layer.Hair:
                    case Layer.FacialHair:
                    case Layer.Backpack:
                    case Layer.Mount:
                    case Layer.Bank:
                        {
                            continue; // ignore
                        }
                    default:
                        {
                            m.SendMessage("You must remove all equipment before proceeding.");
                            return false;
                        }
                }
            }
        }

        if (GetFlag(ConditionFlag.DenyTransformed) && m.IsBodyMod)
        {
            m.SendMessage("You cannot go there in this form.");
            return false;
        }

        if (GetFlag(ConditionFlag.DeadOnly) && m.Alive)
        {
            m.SendLocalizedMessage(1060014); // Only the dead may pass.
            return false;
        }

        return true;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        using var props = ValueStringBuilder.Create(128);

        if (GetFlag(ConditionFlag.DenyMounted))
        {
            props.Append("<BR>Deny Mounted");
        }

        if (GetFlag(ConditionFlag.DenyFollowers))
        {
            props.Append("<BR>Deny Followers");
        }

        if (GetFlag(ConditionFlag.DenyPackContents))
        {
            props.Append("<BR>Deny Pack Contents");
        }

        if (GetFlag(ConditionFlag.DenyPackEthereals))
        {
            props.Append("<BR>Deny Pack Ethereals");
        }

        if (GetFlag(ConditionFlag.DenyHolding))
        {
            props.Append("<BR>Deny Holding");
        }

        if (GetFlag(ConditionFlag.DenyEquipment))
        {
            props.Append("<BR>Deny Equipment");
        }

        if (GetFlag(ConditionFlag.DenyTransformed))
        {
            props.Append("<BR>Deny Transformed");
        }

        if (GetFlag(ConditionFlag.StaffOnly))
        {
            props.Append("<BR>Staff Only");
        }

        if (GetFlag(ConditionFlag.DeadOnly))
        {
            props.Append("<BR>Dead Only");
        }

        if (props.Length != 0)
        {
            props.Remove(0, 4);
            list.Add(props.ToString());
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _flags = (ConditionFlag)reader.ReadInt();
    }

    protected bool GetFlag(ConditionFlag flag) => (_flags & flag) != 0;

    protected void SetFlag(ConditionFlag flag, bool value)
    {
        if (value)
        {
            _flags |= flag;
        }
        else
        {
            _flags &= ~flag;
        }
    }

    [Flags]
    protected enum ConditionFlag
    {
        None = 0x000,
        DenyMounted = 0x001,
        DenyFollowers = 0x002,
        DenyPackContents = 0x004,
        DenyHolding = 0x008,
        DenyEquipment = 0x010,
        DenyTransformed = 0x020,
        StaffOnly = 0x040,
        DenyPackEthereals = 0x080,
        DeadOnly = 0x100
    }
}
