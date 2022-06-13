using System;
using System.Collections.Generic;
using Server.Buffers;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
    public class Teleporter : Item
    {
        private bool m_Active, m_Creatures, m_CombatCheck, m_CriminalCheck;
        private TimeSpan m_Delay;
        private bool m_DestEffect;
        private Map m_MapDest;
        private Point3D m_PointDest;
        private int m_SoundID;
        private bool m_SourceEffect;

        [Constructible]
        public Teleporter() : this(new Point3D(0, 0, 0))
        {
        }

        [Constructible]
        public Teleporter(Point3D pointDest, Map mapDest = null, bool creatures = false) : base(0x1BC3)
        {
            Movable = false;
            Visible = false;

            m_Active = true;
            m_PointDest = pointDest;
            m_MapDest = mapDest;
            m_Creatures = creatures;

            m_CombatCheck = false;
            m_CriminalCheck = false;
        }

        public Teleporter(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get => m_SourceEffect;
            set
            {
                m_SourceEffect = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get => m_DestEffect;
            set
            {
                m_DestEffect = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get => m_SoundID;
            set
            {
                m_SoundID = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get => m_Delay;
            set
            {
                m_Delay = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
            set
            {
                m_Active = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest
        {
            get => m_PointDest;
            set
            {
                m_PointDest = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map MapDest
        {
            get => m_MapDest;
            set
            {
                m_MapDest = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Creatures
        {
            get => m_Creatures;
            set
            {
                m_Creatures = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CombatCheck
        {
            get => m_CombatCheck;
            set
            {
                m_CombatCheck = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CriminalCheck
        {
            get => m_CriminalCheck;
            set
            {
                m_CriminalCheck = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => 1026095; // teleporter

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
            {
                list.Add(1060742); // active
            }
            else
            {
                list.Add(1060743); // inactive
            }

            if (m_MapDest != null)
            {
                list.Add(1060658, $"{"Map"}\t{m_MapDest}");
            }

            if (m_PointDest != Point3D.Zero)
            {
                list.Add(1060659, $"{"Coords"}\t{m_PointDest}");
            }

            list.Add(1060660, $"{"Creatures"}\t{(m_Creatures ? "Yes" : "No")}");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
            {
                if (m_MapDest != null && m_PointDest != Point3D.Zero)
                {
                    LabelTo(from, "{0} [{1}]", m_PointDest, m_MapDest);
                }
                else if (m_MapDest != null)
                {
                    LabelTo(from, "[{0}]", m_MapDest);
                }
                else if (m_PointDest != Point3D.Zero)
                {
                    LabelTo(from, m_PointDest.ToString());
                }
            }
            else
            {
                LabelTo(from, "(inactive)");
            }
        }

        public virtual bool CanTeleport(Mobile m)
        {
            if (!m_Creatures && !m.Player)
            {
                return false;
            }

            if (m_CriminalCheck && m.Criminal)
            {
                m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }

            if (m_CombatCheck && SpellHelper.CheckCombat(m))
            {
                m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return false;
            }

            return true;
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
            {
                DoTeleport(m);
            }
            else
            {
                Timer.StartTimer(m_Delay, () => DoTeleport(m));
            }
        }

        public virtual void DoTeleport(Mobile m)
        {
            var map = m_MapDest;

            if (map == null || map == Map.Internal)
            {
                map = m.Map;
            }

            var p = m_PointDest;

            if (p == Point3D.Zero)
            {
                p = m.Location;
            }

            BaseCreature.TeleportPets(m, p, map);

            var sendEffect = !m.Hidden || m.AccessLevel == AccessLevel.Player;

            if (m_SourceEffect && sendEffect)
            {
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10);
            }

            m.MoveToWorld(p, map);

            if (m_DestEffect && sendEffect)
            {
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10);
            }

            if (m_SoundID > 0 && sendEffect)
            {
                Effects.PlaySound(m.Location, m.Map, m_SoundID);
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m_Active && CanTeleport(m))
            {
                StartTeleport(m);
                return false;
            }

            return true;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(4); // version

            writer.Write(m_CriminalCheck);
            writer.Write(m_CombatCheck);

            writer.Write(m_SourceEffect);
            writer.Write(m_DestEffect);
            writer.Write(m_Delay);
            writer.WriteEncodedInt(m_SoundID);

            writer.Write(m_Creatures);

            writer.Write(m_Active);
            writer.Write(m_PointDest);
            writer.Write(m_MapDest);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        m_CriminalCheck = reader.ReadBool();
                        goto case 3;
                    }
                case 3:
                    {
                        m_CombatCheck = reader.ReadBool();
                        goto case 2;
                    }
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        m_PointDest = reader.ReadPoint3D();
                        m_MapDest = reader.ReadMap();

                        break;
                    }
            }
        }
    }

    public class SkillTeleporter : Teleporter
    {
        private int m_MessageNumber;
        private string m_MessageString;
        private double m_Required;
        private SkillName m_Skill;

        [Constructible]
        public SkillTeleporter()
        {
        }

        public SkillTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill
        {
            get => m_Skill;
            set
            {
                m_Skill = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Required
        {
            get => m_Required;
            set
            {
                m_Required = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string MessageString
        {
            get => m_MessageString;
            set
            {
                m_MessageString = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageNumber
        {
            get => m_MessageNumber;
            set
            {
                m_MessageNumber = value;
                InvalidateProperties();
            }
        }

        public override bool CanTeleport(Mobile m)
        {
            if (!base.CanTeleport(m))
            {
                return false;
            }

            var sk = m.Skills[m_Skill];

            if (sk == null || sk.Base < m_Required)
            {
                if (m.BeginAction(this))
                {
                    if (m_MessageString != null)
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
                            m_MessageString
                        );
                    }
                    else if (m_MessageNumber != 0)
                    {
                        m.NetState.SendMessageLocalized(
                            Serial,
                            ItemID,
                            MessageType.Regular,
                            0x3B2,
                            3,
                            m_MessageNumber,
                            null
                        );
                    }

                    Timer.StartTimer(TimeSpan.FromSeconds(5.0), () => m.EndAction(this));
                }

                return false;
            }

            return true;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            var skillIndex = (int)m_Skill;
            string skillName;

            if (skillIndex >= 0 && skillIndex < SkillInfo.Table.Length)
            {
                skillName = SkillInfo.Table[skillIndex].Name;
            }
            else
            {
                skillName = "(Invalid)";
            }

            list.Add(1060661, $"{skillName}\t{m_Required:F1}");

            if (m_MessageString != null)
            {
                list.Add(1060662, $"{"Message"}\t{m_MessageString}");
            }
            else if (m_MessageNumber != 0)
            {
                list.Add(1060662, $"{"Message"}\t{m_MessageNumber:#}");
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write((int)m_Skill);
            writer.Write(m_Required);
            writer.Write(m_MessageString);
            writer.Write(m_MessageNumber);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Skill = (SkillName)reader.ReadInt();
                        m_Required = reader.ReadDouble();
                        m_MessageString = reader.ReadString();
                        m_MessageNumber = reader.ReadInt();

                        break;
                    }
            }
        }
    }

    public class KeywordTeleporter : Teleporter
    {
        private int m_Keyword;
        private int m_Range;
        private string m_Substring;

        [Constructible]
        public KeywordTeleporter()
        {
            m_Keyword = -1;
            m_Substring = null;
        }

        public KeywordTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Substring
        {
            get => m_Substring;
            set
            {
                m_Substring = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Keyword
        {
            get => m_Keyword;
            set
            {
                m_Keyword = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get => m_Range;
            set
            {
                m_Range = value;
                InvalidateProperties();
            }
        }

        public override bool HandlesOnSpeech => true;

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && Active)
            {
                var m = e.Mobile;

                if (!m.InRange(GetWorldLocation(), m_Range))
                {
                    return;
                }

                var isMatch = false;

                if (m_Keyword >= 0 && e.HasKeyword(m_Keyword))
                {
                    isMatch = true;
                }
                else if (m_Substring != null && e.Speech.InsensitiveContains(m_Substring))
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
            if (!m.InRange(GetWorldLocation(), m_Range) || m.Map != Map)
            {
                return;
            }

            base.DoTeleport(m);
        }

        public override bool OnMoveOver(Mobile m) => true;

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060661, $"{"Range"}\t{m_Range}");

            if (m_Keyword >= 0)
            {
                list.Add(1060662, $"{"Keyword"}\t{m_Keyword}");
            }

            if (m_Substring != null)
            {
                list.Add(1060663, $"{"Substring"}\t{m_Substring}");
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Substring);
            writer.Write(m_Keyword);
            writer.Write(m_Range);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Substring = reader.ReadString();
                        m_Keyword = reader.ReadInt();
                        m_Range = reader.ReadInt();

                        break;
                    }
            }
        }
    }

    public class WaitTeleporter : KeywordTeleporter
    {
        private static Dictionary<Mobile, TeleportingInfo> m_Table = new Dictionary<Mobile, TeleportingInfo>();

        [Constructible]
        public WaitTeleporter()
        {
        }

        public WaitTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StartNumber { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string StartMessage { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ProgressNumber { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ProgressMessage { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowTimeRemaining { get; set; }

        public static void Initialize()
        {
            EventSink.Logout += EventSink_Logout;
        }

        public static void EventSink_Logout(Mobile from)
        {
            if (from != null && m_Table.Remove(from, out var info))
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
            if (m_Table.TryGetValue(m, out var info))
            {
                if (info.Teleporter == this)
                {
                    if (m.BeginAction(this))
                    {
                        if (ProgressMessage != null)
                        {
                            m.SendMessage(ProgressMessage);
                        }
                        else if (ProgressNumber != 0)
                        {
                            m.SendLocalizedMessage(ProgressNumber);
                        }

                        if (ShowTimeRemaining)
                        {
                            m.SendMessage("Time remaining: {0}", FormatTime(info.TimerToken.Next - Core.Now));
                        }

                        Timer.StartTimer(TimeSpan.FromSeconds(5), () => m.EndAction(this));
                    }

                    return;
                }

                info.TimerToken.Cancel();
            }

            if (StartMessage != null)
            {
                m.SendMessage(StartMessage);
            }
            else if (StartNumber != 0)
            {
                m.SendLocalizedMessage(StartNumber);
            }

            if (Delay == TimeSpan.Zero)
            {
                DoTeleport(m);
            }
            else
            {
                Timer.StartTimer(Delay, () => DoTeleport(m), out var timerToken);
                m_Table[m] = new TeleportingInfo(this, timerToken);
            }
        }

        public override void DoTeleport(Mobile m)
        {
            m_Table.Remove(m);

            base.DoTeleport(m);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(StartNumber);
            writer.Write(StartMessage);
            writer.Write(ProgressNumber);
            writer.Write(ProgressMessage);
            writer.Write(ShowTimeRemaining);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            StartNumber = reader.ReadInt();
            StartMessage = reader.ReadString();
            ProgressNumber = reader.ReadInt();
            ProgressMessage = reader.ReadString();
            ShowTimeRemaining = reader.ReadBool();
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

    public class TimeoutTeleporter : Teleporter
    {
        private Dictionary<Mobile, TimerExecutionToken> m_Teleporting;

        [Constructible]
        public TimeoutTeleporter() : this(new Point3D(0, 0, 0))
        {
        }

        [Constructible]
        public TimeoutTeleporter(Point3D pointDest, Map mapDest = null, bool creatures = false)
            : base(pointDest, mapDest, creatures) =>
            m_Teleporting = new Dictionary<Mobile, TimerExecutionToken>();

        public TimeoutTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan TimeoutDelay { get; set; }

        public void StartTimer(Mobile m)
        {
            StartTimer(m, TimeoutDelay);
        }

        private void StartTimer(Mobile m, TimeSpan delay)
        {
            StopTimer(m);
            Timer.StartTimer(delay, () => StartTeleport(m), out var timerToken);
            m_Teleporting[m] = timerToken;
        }

        public void StopTimer(Mobile m)
        {
            if (m_Teleporting.Remove(m, out var t))
            {
                t.Cancel();
            }
        }

        public override void DoTeleport(Mobile m)
        {
            m_Teleporting.Remove(m);

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(TimeoutDelay);
            writer.Write(m_Teleporting.Count);

            foreach (var kvp in m_Teleporting)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Next);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            TimeoutDelay = reader.ReadTimeSpan();
            m_Teleporting = new Dictionary<Mobile, TimerExecutionToken>();

            var count = reader.ReadInt();

            for (var i = 0; i < count; ++i)
            {
                var m = reader.ReadEntity<Mobile>();
                var end = reader.ReadDateTime();

                StartTimer(m, end - Core.Now);
            }
        }
    }

    public class TimeoutGoal : Item
    {
        [Constructible]
        public TimeoutGoal()
            : base(0x1822)
        {
            Movable = false;
            Visible = false;

            Hue = 1154;
        }

        public TimeoutGoal(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeoutTeleporter Teleporter { get; set; }

        public override string DefaultName => "timeout teleporter goal";

        public override bool OnMoveOver(Mobile m)
        {
            Teleporter?.StopTimer(m);

            return true;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Teleporter);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Teleporter = reader.ReadEntity<TimeoutTeleporter>();
        }
    }

    public class ConditionTeleporter : Teleporter
    {
        private ConditionFlag m_Flags;

        [Constructible]
        public ConditionTeleporter()
        {
        }

        public ConditionTeleporter(Serial serial)
            : base(serial)
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
                (m.Followers != 0 || m is PlayerMobile mobile && mobile.AutoStabled.Count != 0))
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
                                m.SendMessage("You must remove all of your equipment before proceeding.");
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

            using var props = new ValueStringBuilder(stackalloc char[128]);

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write((int)m_Flags);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Flags = (ConditionFlag)reader.ReadInt();
        }

        protected bool GetFlag(ConditionFlag flag) => (m_Flags & flag) != 0;

        protected void SetFlag(ConditionFlag flag, bool value)
        {
            if (value)
            {
                m_Flags |= flag;
            }
            else
            {
                m_Flags &= ~flag;
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
}
