using System;
using Server.HuePickers;

namespace Server.Network
{
    public sealed class DragEffect : Packet
    {
        public DragEffect(IEntity src, IEntity trg, int itemID, int hue, int amount) : base(0x23, 26)
        {
            Stream.Write((short)itemID);
            Stream.Write((byte)0);
            Stream.Write((short)hue);
            Stream.Write((short)amount);
            Stream.Write(src.Serial);
            Stream.Write((short)src.X);
            Stream.Write((short)src.Y);
            Stream.Write((sbyte)src.Z);
            Stream.Write(trg.Serial);
            Stream.Write((short)trg.X);
            Stream.Write((short)trg.Y);
            Stream.Write((sbyte)trg.Z);
        }
    }

    public sealed class SeasonChange : Packet
    {
        private static readonly SeasonChange[][] m_Cache =
        {
            new SeasonChange[2],
            new SeasonChange[2],
            new SeasonChange[2],
            new SeasonChange[2],
            new SeasonChange[2]
        };

        public SeasonChange(int season, bool playSound = true) : base(0xBC, 3)
        {
            Stream.Write((byte)season);
            Stream.Write(playSound);
        }

        public static SeasonChange Instantiate(int season) => Instantiate(season, true);

        public static SeasonChange Instantiate(int season, bool playSound)
        {
            if (season >= 0 && season < m_Cache.Length)
            {
                var idx = playSound ? 1 : 0;

                var p = m_Cache[season][idx];

                if (p == null)
                {
                    m_Cache[season][idx] = p = new SeasonChange(season, playSound);
                    p.SetStatic();
                }

                return p;
            }

            return new SeasonChange(season, playSound);
        }
    }

    public sealed class DisplayPaperdoll : Packet
    {
        public DisplayPaperdoll(Serial m, string title, bool warmode, bool canLift) : base(0x88, 66)
        {
            byte flags = 0x00;

            if (warmode)
            {
                flags |= 0x01;
            }

            if (canLift)
            {
                flags |= 0x02;
            }

            Stream.Write(m);
            Stream.WriteAsciiFixed(title, 60);
            Stream.Write(flags);
        }
    }

    public sealed class PlayMusic : Packet
    {
        public static readonly Packet InvalidInstance = SetStatic(new PlayMusic(MusicName.Invalid));

        private static readonly Packet[] m_Instances = new Packet[60];

        public PlayMusic(MusicName name) : base(0x6D, 3)
        {
            Stream.Write((short)name);
        }

        public static Packet GetInstance(MusicName name)
        {
            if (name == MusicName.Invalid)
            {
                return InvalidInstance;
            }

            var v = (int)name;
            Packet p;

            if (v >= 0 && v < m_Instances.Length)
            {
                p = m_Instances[v];

                if (p == null)
                {
                    m_Instances[v] = p = SetStatic(new PlayMusic(name));
                }
            }
            else
            {
                p = new PlayMusic(name);
            }

            return p;
        }
    }

    public sealed class ScrollMessage : Packet
    {
        public ScrollMessage(int type, int tip, string text) : base(0xA6)
        {
            text ??= "";

            EnsureCapacity(10 + text.Length);

            Stream.Write((byte)type);
            Stream.Write(tip);
            Stream.Write((ushort)text.Length);
            Stream.WriteAsciiFixed(text, text.Length);
        }
    }

    public sealed class CurrentTime : Packet
    {
        public CurrentTime() : this(DateTime.Now)
        {
        }

        public CurrentTime(DateTime date) : base(0x5B, 4)
        {
            Stream.Write((byte)date.Hour);
            Stream.Write((byte)date.Minute);
            Stream.Write((byte)date.Second);
        }
    }

    public sealed class PathfindMessage : Packet
    {
        public PathfindMessage(Point3D p) : base(0x38, 7)
        {
            Stream.Write((short)p.X);
            Stream.Write((short)p.Y);
            Stream.Write((short)p.Z);
        }
    }

    public sealed class PingAck : Packet
    {
        private static readonly PingAck[] m_Cache = new PingAck[0x100];

        public PingAck(byte ping) : base(0x73, 2)
        {
            Stream.Write(ping);
        }

        public static PingAck Instantiate(byte ping)
        {
            var p = m_Cache[ping];

            if (p == null)
            {
                m_Cache[ping] = p = new PingAck(ping);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class ClearWeaponAbility : Packet
    {
        public static readonly Packet Instance = SetStatic(new ClearWeaponAbility());

        public ClearWeaponAbility() : base(0xBF)
        {
            EnsureCapacity(5);

            Stream.Write((short)0x21);
        }
    }

    public sealed class DisplayHuePicker : Packet
    {
        public DisplayHuePicker(HuePicker huePicker) : base(0x95, 9)
        {
            Stream.Write(huePicker.Serial);
            Stream.Write((short)0);
            Stream.Write((short)huePicker.ItemID);
        }
    }
}
