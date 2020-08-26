using System;
using Server.Mobiles;
using Server.Network;

namespace Server
{
  public class BuffInfo
  {
    public static bool Enabled { get; private set; }

    public static void Initialize()
    {
      Enabled = ServerConfiguration.GetOrUpdateSetting("buffIcons.enable", Core.ML);

      if (Enabled)
        EventSink.ClientVersionReceived += ResendBuffsOnClientVersionReceived;
    }

    public static void ResendBuffsOnClientVersionReceived(NetState ns, ClientVersion cv)
    {
      if (ns.Mobile is PlayerMobile pm)
        Timer.DelayCall(pm.ResendBuffs);
    }

    public BuffIcon ID { get; }

    public int TitleCliloc { get; }

    public int SecondaryCliloc { get; }

    public TimeSpan TimeLength { get; }

    public DateTime TimeStart { get; }

    public Timer Timer { get; }

    public bool RetainThroughDeath { get; }

    public TextDefinition Args { get; }

    public BuffInfo(BuffIcon iconID, int titleCliloc)
      : this(iconID, titleCliloc, titleCliloc + 1)
    {
    }

    public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc)
    {
      ID = iconID;
      TitleCliloc = titleCliloc;
      SecondaryCliloc = secondaryCliloc;
    }

    public BuffInfo(BuffIcon iconID, int titleCliloc, TimeSpan length, Mobile m)
      : this(iconID, titleCliloc, titleCliloc + 1, length, m)
    {
    }

    // Only the timed one needs to Mobile to know when to automagically remove it.
    public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, TimeSpan length, Mobile m)
      : this(iconID, titleCliloc, secondaryCliloc)
    {
      TimeLength = length;
      TimeStart = DateTime.UtcNow;

      Timer = Timer.DelayCall(length, RemoveBuff, m, this);
    }

    public BuffInfo(BuffIcon iconID, int titleCliloc, TextDefinition args)
      : this(iconID, titleCliloc, titleCliloc + 1, args)
    {
    }

    public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, TextDefinition args)
      : this(iconID, titleCliloc, secondaryCliloc) =>
      Args = args;

    public BuffInfo(BuffIcon iconID, int titleCliloc, bool retainThroughDeath)
      : this(iconID, titleCliloc, titleCliloc + 1, retainThroughDeath)
    {
    }

    public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, bool retainThroughDeath)
      : this(iconID, titleCliloc, secondaryCliloc) =>
      RetainThroughDeath = retainThroughDeath;

    public BuffInfo(BuffIcon iconID, int titleCliloc, TextDefinition args, bool retainThroughDeath)
      : this(iconID, titleCliloc, titleCliloc + 1, args, retainThroughDeath)
    {
    }

    public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, TextDefinition args, bool retainThroughDeath)
      : this(iconID, titleCliloc, secondaryCliloc, args) =>
      RetainThroughDeath = retainThroughDeath;

    public BuffInfo(BuffIcon iconID, int titleCliloc, TimeSpan length, Mobile m, TextDefinition args)
      : this(iconID, titleCliloc, titleCliloc + 1, length, m, args)
    {
    }

    public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, TimeSpan length, Mobile m,
      TextDefinition args)
      : this(iconID, titleCliloc, secondaryCliloc, length, m) =>
      Args = args;

    public BuffInfo(BuffIcon iconID, int titleCliloc, TimeSpan length, Mobile m, TextDefinition args,
      bool retainThroughDeath)
      : this(iconID, titleCliloc, titleCliloc + 1, length, m, args, retainThroughDeath)
    {
    }

    public BuffInfo(BuffIcon iconID, int titleCliloc, int secondaryCliloc, TimeSpan length, Mobile m,
      TextDefinition args, bool retainThroughDeath)
      : this(iconID, titleCliloc, secondaryCliloc, length, m)
    {
      Args = args;
      RetainThroughDeath = retainThroughDeath;
    }

    public static void AddBuff(Mobile m, BuffInfo b)
    {
      if (m is PlayerMobile pm)
        pm.AddBuff(b);
    }

    public static void RemoveBuff(Mobile m, BuffInfo b)
    {
      if (m is PlayerMobile pm)
        pm.RemoveBuff(b);
    }

    public static void RemoveBuff(Mobile m, BuffIcon b)
    {
      if (m is PlayerMobile pm)
        pm.RemoveBuff(b);
    }
  }

  public enum BuffIcon : short
  {
    DismountPrevention = 0x3E9,
    NoRearm = 0x3EA,

    // Currently, no 0x3EB or 0x3EC
    NightSight = 0x3ED, // *
    DeathStrike,
    EvilOmen,
    UnknownStandingSwirl, // Which is healing throttle & Stamina throttle?
    UnknownKneelingSword,
    DivineFury, // *
    EnemyOfOne, // *
    HidingAndOrStealth, // *
    ActiveMeditation, // *
    BloodOathCaster, // *
    BloodOathCurse, // *
    CorpseSkin, // *
    Mindrot, // *
    PainSpike, // *
    Strangle,
    GiftOfRenewal, // *
    AttuneWeapon, // *
    Thunderstorm, // *
    EssenceOfWind, // *
    EtherealVoyage, // *
    GiftOfLife, // *
    ArcaneEmpowerment, // *
    MortalStrike,
    ReactiveArmor, // *
    Protection, // *
    ArchProtection,
    MagicReflection, // *
    Incognito, // *
    Disguised,
    AnimalForm,
    Polymorph,
    Invisibility, // *
    Paralyze, // *
    Poison,
    Bleed,
    Clumsy, // *
    FeebleMind, // *
    Weaken, // *
    Curse, // *
    MassCurse,
    Agility, // *
    Cunning, // *
    Strength, // *
    Bless, // *
    Sleep,
    StoneForm,
    SpellPlague,
    SpellTrigger,
    NetherBolt,
    Fly
  }

  public sealed class AddBuffPacket : Packet
  {
    public AddBuffPacket(Mobile m, BuffInfo info)
      : this(m, info.ID, info.TitleCliloc, info.SecondaryCliloc, info.Args,
        info.TimeStart != DateTime.MinValue ? info.TimeStart + info.TimeLength - DateTime.UtcNow : TimeSpan.Zero)
    {
    }

    public AddBuffPacket(Mobile mob, BuffIcon iconID, int titleCliloc, int secondaryCliloc, TextDefinition args,
      TimeSpan length)
      : base(0xDF)
    {
      bool hasArgs = args != null;

      EnsureCapacity(hasArgs ? 48 + args.ToString().Length * 2 : 44);
      Stream.Write(mob.Serial);

      Stream.Write((short)iconID); // ID
      Stream.Write((short)0x1); // Type 0 for removal. 1 for add 2 for Data

      Stream.Fill(4);

      Stream.Write((short)iconID); // ID
      Stream.Write((short)0x01); // Type 0 for removal. 1 for add 2 for Data

      Stream.Fill(4);

      if (length < TimeSpan.Zero)
        length = TimeSpan.Zero;

      Stream.Write((short)length.TotalSeconds); // Time in seconds

      Stream.Fill(3);
      Stream.Write(titleCliloc);
      Stream.Write(secondaryCliloc);

      if (!hasArgs)
      {
        // m_Stream.Fill( 2 );
        Stream.Fill(10);
      }
      else
      {
        Stream.Fill(4);
        Stream.Write((short)0x1); // Unknown -> Possibly something saying 'hey, I have more data!'?
        Stream.Fill(2);

        // m_Stream.WriteLittleUniNull( "\t#1018280" );
        Stream.WriteLittleUniNull($"\t{args}");

        Stream.Write((short)0x1); // Even more Unknown -> Possibly something saying 'hey, I have more data!'?
        Stream.Fill(2);
      }
    }
  }

  public sealed class RemoveBuffPacket : Packet
  {
    public RemoveBuffPacket(Mobile mob, BuffInfo info)
      : this(mob, info.ID)
    {
    }

    public RemoveBuffPacket(Mobile mob, BuffIcon iconID)
      : base(0xDF)
    {
      EnsureCapacity(13);
      Stream.Write(mob.Serial);

      Stream.Write((short)iconID); // ID
      Stream.Write((short)0x0); // Type 0 for removal. 1 for add 2 for Data

      Stream.Fill(4);
    }
  }
}
