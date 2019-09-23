using System;
using Server.Buffers;
using Server.Mobiles;
using Server.Network;

namespace Server
{
  public class BuffInfo
  {
    public static bool Enabled => Core.ML;

    public static void Initialize()
    {
      if (Enabled)
        EventSink.ClientVersionReceived += delegate(ClientVersionReceivedArgs args)
        {
          if (args.State.Mobile is PlayerMobile pm)
            Timer.DelayCall(TimeSpan.Zero, pm.ResendBuffs);
        };
    }

    public BuffIcon ID{ get; }

    public int Title{ get; }

    public TextDefinition Description{ get; }

    public TimeSpan TimeLength{ get; }

    public DateTime TimeStart{ get; }

    public Timer Timer{ get; }

    public bool RetainThroughDeath { get; }

    public BuffInfo(BuffIcon iconId, int title, TimeSpan time = default, Mobile m = null) :
      this(iconId, title, title + 1, time, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, string args, TimeSpan time = default, Mobile m = null) :
      this(iconId, title, title + 1, args, time, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, int desc, TimeSpan time = default, Mobile m = null) :
      this(iconId, title, desc, null, time, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, string args, bool retain, Mobile m = null) :
      this(iconId, title, title + 1, args, DateTime.UtcNow, default, retain, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, int desc, string args, TimeSpan time, Mobile m = null) :
      this(iconId, title, new TextDefinition(desc, args), DateTime.UtcNow, time, false, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, string args, TimeSpan time, bool retain = false, Mobile m = null) :
      this(iconId, title, title + 1, args, DateTime.UtcNow, time, retain, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, DateTime start, TimeSpan time = default, bool retain = false,
      Mobile m = null) : this(iconId, title, title + 1, start, time, retain, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, string args, DateTime start, TimeSpan time = default,  bool retain = false, Mobile m = null) :
      this(iconId, title, title + 1, args, start, time, retain, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, int desc, TimeSpan time = default, bool retain = false,
      Mobile m = null) : this(iconId, title, desc, null, time, retain, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, int desc, string args, TimeSpan time = default, bool retain = false,
      Mobile m = null) : this(iconId, title, new TextDefinition(desc, args), DateTime.UtcNow, time, retain, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, int desc, string args, DateTime start, TimeSpan time = default, bool retain = false,
      Mobile m = null) : this(iconId, title, new TextDefinition(desc, args), start, time, retain, m)
    {
    }

    public BuffInfo(BuffIcon iconId, int title, TextDefinition desc, DateTime start = default, TimeSpan time = default, bool retain = false,
      Mobile m = null)
    {
      ID = iconId;
      Title = title;
      Description = desc;
      TimeLength = time;
      TimeStart = start;
      RetainThroughDeath = retain;
      if (m is PlayerMobile pm)
        Timer = Timer.DelayCall(time, playermobile => playermobile.RemoveBuff(this), pm);
    }

    public BuffInfo(BuffIcon iconId, int title, TextDefinition desc, TimeSpan time, DateTime start, bool retain,
      Timer timer)
    {
      ID = iconId;
      Title = title;
      Description = desc;
      TimeLength = time;
      TimeStart = start;
      RetainThroughDeath = retain;
      Timer = timer;
    }

    #region Convenience Methods

    public static void AddBuff(Mobile m, BuffInfo b)
    {
      (m as PlayerMobile)?.AddBuff(b);
    }

    public static void RemoveBuff(Mobile m, BuffInfo b)
    {
      (m as PlayerMobile)?.RemoveBuff(b);
    }

    public static void RemoveBuff(Mobile m, BuffIcon b)
    {
      (m as PlayerMobile)?.RemoveBuff(b);
    }

    #endregion
  }

  public enum BuffIcon : short
  {
    DismountPrevention = 0x3E9,
    NoRearm = 0x3EA,

    //Currently, no 0x3EB or 0x3EC
    NightSight = 0x3ED, //*
    DeathStrike,
    EvilOmen,
    UnknownStandingSwirl, //Which is healing throttle & Stamina throttle?
    UnknownKneelingSword,
    DivineFury, //*
    EnemyOfOne, //*
    HidingAndOrStealth, //*
    ActiveMeditation, //*
    BloodOathCaster, //*
    BloodOathCurse, //*
    CorpseSkin, //*
    Mindrot, //*
    PainSpike, //*
    Strangle,
    GiftOfRenewal, //*
    AttuneWeapon, //*
    Thunderstorm, //*
    EssenceOfWind, //*
    EtherealVoyage, //*
    GiftOfLife, //*
    ArcaneEmpowerment, //*
    MortalStrike,
    ReactiveArmor, //*
    Protection, //*
    ArchProtection,
    MagicReflection, //*
    Incognito, //*
    Disguised,
    AnimalForm,
    Polymorph,
    Invisibility, //*
    Paralyze, //*
    Poison,
    Bleed,
    Clumsy, //*
    FeebleMind, //*
    Weaken, //*
    Curse, //*
    MassCurse,
    Agility, //*
    Cunning, //*
    Strength, //*
    Bless, //*
    Sleep,
    StoneForm,
    SpellPlague,
    SpellTrigger,
    NetherBolt,
    Fly
  }

  public static class BuffPackets
  {
    public static void SendAddBuff(Mobile m, BuffInfo info)
    {
      SendAddBuff(m.NetState, m.Serial, info.ID, info.Title, info.Description,
        info.TimeStart != DateTime.MinValue ? info.TimeStart + info.TimeLength - DateTime.UtcNow : TimeSpan.Zero);
    }
    public static void SendAddBuff(NetState ns, Serial m, BuffIcon iconID, int title, TextDefinition desc, TimeSpan time)
    {
      if (ns == null)
        return;

      string args = desc?.String ?? "";

      int length = 44 + args.Length * 2;

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xDF); // Packet ID
      writer.Write((ushort)length); // Dynamic Length

      writer.Write(m);

      writer.Write((short)iconID); //ID
      writer.Write((short)0x1); //Type 0 for removal. 1 for add 2 for Data

      writer.Position += 4;

      writer.Write((short)iconID); //ID
      writer.Write((short)0x01); //Type 0 for removal. 1 for add 2 for Data

      writer.Position += 4;

      writer.Write((short)Math.Max(time.TotalSeconds, 0)); //Time in seconds

      writer.Position += 3;

      writer.Write(title);
      writer.Write(desc?.Number ?? 0);

      writer.Position += 5;
      writer.Write((byte)0x1); // Start indicator?
      writer.Position += 2;

      writer.WriteLittleUniNull(args);

      ns.Send(writer.Span);
    }

    public static void SendRemoveBuffPacket(NetState ns, Serial m, int icon)
    {
      SpanWriter writer = new SpanWriter(stackalloc byte[13]);
      writer.Write((byte)0xDF); // Packet ID
      writer.Write((ushort)13); // Dynamic Length

      writer.Write(m);

      writer.Write((short)icon); //ID
      writer.Write((short)0x0); //Type 0 for removal. 1 for add 2 for Data

      writer.Position += 4;

      ns.Send(writer.Span);
    }
  }
}
