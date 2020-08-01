using System;
using System.Collections.Generic;
using System.Linq;
using Server.Accounting;
using Server.ContextMenus;
using Server.Engines.BulkOrders;
using Server.Engines.CannedEvil;
using Server.Engines.ConPVP;
using Server.Engines.Craft;
using Server.Engines.Help;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Gumps;
using Server.Engines.PartySystem;
using Server.Engines.Quests;
using Server.Ethics;
using Server.Factions;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Movement;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Fifth;
using Server.Spells.Fourth;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Spellweaving;
using Server.Targeting;
using Server.Utilities;
using BaseQuestGump = Server.Engines.MLQuests.Gumps.BaseQuestGump;
using QuestOfferGump = Server.Engines.MLQuests.Gumps.QuestOfferGump;
using RankDefinition = Server.Guilds.RankDefinition;

namespace Server.Mobiles
{
  [Flags]
  public enum PlayerFlag // First 16 bits are reserved for default-distro use, start custom flags at 0x00010000
  {
    None = 0x00000000,
    Glassblowing = 0x00000001,
    Masonry = 0x00000002,
    SandMining = 0x00000004,
    StoneMining = 0x00000008,
    ToggleMiningStone = 0x00000010,
    KarmaLocked = 0x00000020,
    AutoRenewInsurance = 0x00000040,
    UseOwnFilter = 0x00000080,
    PublicMyRunUO = 0x00000100,
    PagingSquelched = 0x00000200,
    Young = 0x00000400,
    AcceptGuildInvites = 0x00000800,
    DisplayChampionTitle = 0x00001000,
    HasStatReward = 0x00002000,
    RefuseTrades = 0x00004000
  }

  public enum NpcGuild
  {
    None,
    MagesGuild,
    WarriorsGuild,
    ThievesGuild,
    RangersGuild,
    HealersGuild,
    MinersGuild,
    MerchantsGuild,
    TinkersGuild,
    TailorsGuild,
    FishermensGuild,
    BardsGuild,
    BlacksmithsGuild
  }

  public enum SolenFriendship
  {
    None,
    Red,
    Black
  }

  public enum BlockMountType
  {
    None = -1,
    Dazed = 1040024,
    BolaRecovery = 1062910,
    DismountRecovery = 1070859
  }

  public class PlayerMobile : Mobile, IHonorTarget
  {
    private static bool m_NoRecursion;

    private List<Mobile> m_AllFollowers;

    private readonly Dictionary<Skill, Dictionary<object, CountAndTimeStamp>> m_AntiMacroTable;
    private TimeSpan m_GameTime;

    /*
     * a value of zero means, that the mobile is not executing the spell. Otherwise,
     * the value should match the BaseMana required
    */

    private RankDefinition m_GuildRank;

    private bool m_IgnoreMobiles; // IgnoreMobiles should be moved to Server.Mobiles

    private Mobile m_InsuranceAward;
    private int m_InsuranceBonus;

    private int m_LastGlobalLight = -1, m_LastPersonalLight = -1;

    private bool m_LastProtectedMessage;
    private TimeSpan m_LongTermElapse;

    private MountBlock m_MountBlock;
    private int m_NextProtectionCheck = 10;
    private DateTime m_NextSmithBulkOrder;
    private DateTime m_NextTailorBulkOrder;

    private bool m_NoDeltaRecursion;

    private int
      m_NonAutoreinsuredItems; // number of items that could not be automatically reinsured because gold in bank was not enough

    private DateTime m_SavagePaintExpiration;
    private TimeSpan m_ShortTermElapse;

    public PlayerMobile()
    {
      AutoStabled = new List<Mobile>();

      VisibilityList = new List<Mobile>();
      PermaFlags = new List<Mobile>();
      m_AntiMacroTable = new Dictionary<Skill, Dictionary<object, CountAndTimeStamp>>();
      RecentlyReported = new List<Mobile>();

      BOBFilter = new BOBFilter();

      m_GameTime = TimeSpan.Zero;
      m_ShortTermElapse = TimeSpan.FromHours(8.0);
      m_LongTermElapse = TimeSpan.FromHours(40.0);

      JusticeProtectors = new List<Mobile>();
      m_GuildRank = RankDefinition.Lowest;

      ChampionTitles = new ChampionTitleInfo();
    }

    public PlayerMobile(Serial s) : base(s)
    {
      VisibilityList = new List<Mobile>();
      m_AntiMacroTable = new Dictionary<Skill, Dictionary<object, CountAndTimeStamp>>();
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime AnkhNextUse { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan DisguiseTimeLeft => DisguiseTimers.TimeRemaining(this);

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime PeacedUntil { get; set; }

    public DesignContext DesignContext { get; set; }

    public BlockMountType MountBlockReason => CheckBlock(m_MountBlock) ? m_MountBlock.m_Type : BlockMountType.None;

    public override int MaxWeight => (Core.ML && Race == Race.Human ? 100 : 40) + (int)(3.5 * Str);

    public override double ArmorRating
    {
      get
      {
        // BaseArmor ar;
        double rating = 0.0;

        AddArmorRating(ref rating, NeckArmor);
        AddArmorRating(ref rating, HandArmor);
        AddArmorRating(ref rating, HeadArmor);
        AddArmorRating(ref rating, ArmsArmor);
        AddArmorRating(ref rating, LegsArmor);
        AddArmorRating(ref rating, ChestArmor);
        AddArmorRating(ref rating, ShieldArmor);

        return VirtualArmor + VirtualArmorMod + rating;
      }
    }

    public SkillName[] AnimalFormRestrictedSkills { get; } =
    {
      SkillName.ArmsLore, SkillName.Begging, SkillName.Discordance, SkillName.Forensics,
      SkillName.Inscribe, SkillName.ItemID, SkillName.Meditation, SkillName.Peacemaking,
      SkillName.Provocation, SkillName.RemoveTrap, SkillName.SpiritSpeak, SkillName.Stealing,
      SkillName.TasteID
    };

    public override double RacialSkillBonus
    {
      get
      {
        if (Core.ML && Race == Race.Human)
          return 20.0;

        return 0;
      }
    }

    public List<Item> EquipSnapshot { get; private set; }

    public SkillName Learning { get; set; } = (SkillName)(-1);

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan SavagePaintExpiration
    {
      get
      {
        TimeSpan ts = m_SavagePaintExpiration - DateTime.UtcNow;

        if (ts < TimeSpan.Zero)
          ts = TimeSpan.Zero;

        return ts;
      }
      set => m_SavagePaintExpiration = DateTime.UtcNow + value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan NextSmithBulkOrder
    {
      get
      {
        TimeSpan ts = m_NextSmithBulkOrder - DateTime.UtcNow;

        if (ts < TimeSpan.Zero)
          ts = TimeSpan.Zero;

        return ts;
      }
      set
      {
        try
        {
          m_NextSmithBulkOrder = DateTime.UtcNow + value;
        }
        catch
        {
          // ignored
        }
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan NextTailorBulkOrder
    {
      get
      {
        TimeSpan ts = m_NextTailorBulkOrder - DateTime.UtcNow;

        if (ts < TimeSpan.Zero)
          ts = TimeSpan.Zero;

        return ts;
      }
      set
      {
        try
        {
          m_NextTailorBulkOrder = DateTime.UtcNow + value;
        }
        catch
        {
          // ignored
        }
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime LastEscortTime { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime LastPetBallTime { get; set; }

    public List<Mobile> VisibilityList { get; }

    public List<Mobile> PermaFlags { get; private set; }

    public override int Luck => AosAttributes.GetValue(this, AosAttribute.Luck);

    public BOBFilter BOBFilter { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime SessionStart { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan GameTime
    {
      get
      {
        if (NetState != null)
          return m_GameTime + (DateTime.UtcNow - SessionStart);
        return m_GameTime;
      }
    }

    public override bool NewGuildDisplay => Guilds.Guild.NewGuildSystem;

    public bool BedrollLogout { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public override bool Paralyzed
    {
      get => base.Paralyzed;
      set
      {
        base.Paralyzed = value;

        if (value)
          AddBuff(new BuffInfo(BuffIcon.Paralyze, 1075827)); // Paralyze/You are frozen and can not move
        else
          RemoveBuff(BuffIcon.Paralyze);
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Player EthicPlayer { get; set; }

    public PlayerState FactionPlayerState { get; set; }

    public override void ToggleFlying()
    {
      if (Race != Race.Gargoyle) return;

      if (Flying)
      {
        Freeze(TimeSpan.FromSeconds(1));
        Animate(61, 10, 1, true, false, 0);
        Flying = false;
        BuffInfo.RemoveBuff(this, BuffIcon.Fly);
        SendMessage("You have landed.");

        BaseMount.Dismount(this);
        return;
      }

      BlockMountType type = MountBlockReason;

      if (!Alive)
      {
        SendLocalizedMessage(1113082); // You may not fly while dead.
      }
      else if (IsBodyMod && !(BodyMod == 666 || BodyMod == 667))
      {
        SendLocalizedMessage(1112453); // You can't fly in your current form!
      }
      else if (type != BlockMountType.None)
      {
        switch (type)
        {
          case BlockMountType.Dazed:
            SendLocalizedMessage(1112457);
            break; // You are still too dazed to fly.
          case BlockMountType.BolaRecovery:
            SendLocalizedMessage(1112455);
            break; // You cannot fly while recovering from a bola throw.
          case BlockMountType.DismountRecovery:
            SendLocalizedMessage(1112456);
            break; // You cannot fly while recovering from a dismount maneuver.
        }
      }
      else if (Hits < 25) // TODO confirm
      {
        SendLocalizedMessage(1112454); // You must heal before flying.
      }
      else
      {
        if (!Flying)
        {
          // No message?
          if (Spell is FlySpell spell)
            spell.Stop();

          new FlySpell(this).Cast();
        }
        else
        {
          Flying = false;
          BuffInfo.RemoveBuff(this, BuffIcon.Fly);
        }
      }
    }

    public static Direction GetDirection4(Point3D from, Point3D to)
    {
      int dx = from.X - to.X;
      int dy = from.Y - to.Y;

      int rx = dx - dy;
      int ry = dx + dy;

      Direction ret;

      if (rx >= 0 && ry >= 0)
        ret = Direction.West;
      else if (rx >= 0 && ry < 0)
        ret = Direction.South;
      else if (rx < 0 && ry < 0)
        ret = Direction.East;
      else
        ret = Direction.North;

      return ret;
    }

    public override bool OnDroppedItemToWorld(Item item, Point3D location)
    {
      if (!base.OnDroppedItemToWorld(item, location))
        return false;

      if (Core.AOS)
      {
        IPooledEnumerable<Mobile> mobiles = Map.GetMobilesInRange(location, 0);

        bool found = mobiles.Any(m =>
          m.Z >= location.Z && m.Z < location.Z + 16 && (!m.Hidden || m.AccessLevel == AccessLevel.Player));

        mobiles.Free();

        if (found)
          return false;

        mobiles.Free();
      }

      BounceInfo bi = item.GetBounce();

      if (bi != null)
      {
        Type type = item.GetType();

        if (type.IsDefined(typeof(FurnitureAttribute), true) ||
            type.IsDefined(typeof(DynamicFlipingAttribute), true))
        {
          object[] objs = type.GetCustomAttributes(typeof(FlippableAttribute), true);

          if (objs.Length > 0)
            if (objs[0] is FlippableAttribute fp)
            {
              int[] itemIDs = fp.ItemIDs;

              Point3D oldWorldLoc = bi.WorldLoc;
              Point3D newWorldLoc = location;

              if (oldWorldLoc.X != newWorldLoc.X || oldWorldLoc.Y != newWorldLoc.Y)
              {
                Direction dir = GetDirection4(oldWorldLoc, newWorldLoc);

                if (itemIDs.Length == 2)
                  item.ItemID = dir switch
                  {
                    Direction.North => itemIDs[0],
                    Direction.South => itemIDs[0],
                    Direction.East => itemIDs[1],
                    Direction.West => itemIDs[1],
                    _ => item.ItemID
                  };
                else if (itemIDs.Length == 4)
                  item.ItemID = dir switch
                  {
                    Direction.South => itemIDs[0],
                    Direction.East => itemIDs[1],
                    Direction.North => itemIDs[2],
                    Direction.West => itemIDs[3],
                    _ => item.ItemID
                  };
              }
            }
        }
      }

      return true;
    }

    public override int GetPacketFlags()
    {
      int flags = base.GetPacketFlags();

      if (m_IgnoreMobiles)
        flags |= 0x10;

      return flags;
    }

    public override int GetOldPacketFlags()
    {
      int flags = base.GetOldPacketFlags();

      if (m_IgnoreMobiles)
        flags |= 0x10;

      return flags;
    }

    public bool GetFlag(PlayerFlag flag) => (Flags & flag) != 0;

    public void SetFlag(PlayerFlag flag, bool value)
    {
      if (value)
        Flags |= flag;
      else
        Flags &= ~flag;
    }

    public static void Initialize()
    {
      if (FastwalkPrevention)
        PacketHandlers.RegisterThrottler(0x02, MovementThrottle_Callback);

      EventSink.Login += OnLogin;
      EventSink.Logout += OnLogout;
      EventSink.Connected += EventSink_Connected;
      EventSink.Disconnected += EventSink_Disconnected;

      EventSink.TargetedSkillUse += TargetedSkillUse;
      EventSink.EquipMacro += EquipMacro;
      EventSink.UnequipMacro += UnequipMacro;

      if (Core.SE) Timer.DelayCall(CheckPets);
    }

    private static void TargetedSkillUse(Mobile from, IEntity target, int skillId)
    {
      if (from == null || target == null)
        return;

      from.TargetLocked = true;

      if (skillId == 35)
        AnimalTaming.DisableMessage = true;
      // AnimalTaming.DeferredTarget = false;

      if (from.UseSkill(skillId))
        from.Target?.Invoke(from, target);

      if (skillId == 35)
        // AnimalTaming.DeferredTarget = true;
        AnimalTaming.DisableMessage = false;

      from.TargetLocked = false;
    }

    public static void EquipMacro(Mobile m, List<Serial> list)
    {
      if (m is PlayerMobile pm && pm.Backpack != null && pm.Alive)
      {
        Container pack = pm.Backpack;

        foreach (var serial in list)
        {
          Item item = pack.Items.FirstOrDefault(i => i.Serial == serial);
          if (item == null) continue;

          Item toMove = pm.FindItemOnLayer(item.Layer);

          if (toMove != null)
          {
            // pack.DropItem(toMove);
            toMove.Internalize();

            if (!pm.EquipItem(item))
              pm.EquipItem(toMove);
            else
              pack.DropItem(toMove);
          }
          else
            pm.EquipItem(item);
        }
      }
    }

    public static void UnequipMacro(Mobile m, List<Layer> layers)
    {
      if (m is PlayerMobile pm && pm.Backpack != null && pm.Alive)
      {
        Container pack = pm.Backpack;
        List<Item> eq = m.Items;

        foreach (var item in eq)
          if (layers.Contains(item.Layer))
            pack.TryDropItem(pm, item, false);
      }
    }

    private static void CheckPets()
    {
      foreach (Mobile m in World.Mobiles.Values)
        if (m is PlayerMobile pm)
          if (((!pm.Mounted || pm.Mount is EtherealMount) && pm.AllFollowers.Count > pm.AutoStabled.Count) ||
              (pm.Mounted && pm.AllFollowers.Count > pm.AutoStabled.Count + 1))
            pm.AutoStablePets(); /* autostable checks summons, et al: no need here */
    }

    private static bool CheckBlock(MountBlock block) => block?.m_Timer.Running == true;

    public void SetMountBlock(BlockMountType type, TimeSpan duration, bool dismount)
    {
      if (dismount)
      {
        if (Mount != null)
          Mount.Rider = null;
        else if (AnimalForm.UnderTransformation(this)) AnimalForm.RemoveContext(this, true);
      }

      if (m_MountBlock?.m_Timer.Running != true || m_MountBlock.m_Timer.Next < DateTime.UtcNow + duration) m_MountBlock = new MountBlock(duration, type, this);
    }

    public override void OnSkillInvalidated(Skill skill)
    {
      if (Core.AOS && skill.SkillName == SkillName.MagicResist)
        UpdateResistances();
    }

    public override int GetMaxResistance(ResistanceType type)
    {
      if (AccessLevel > AccessLevel.Player)
        return 100;

      int max = base.GetMaxResistance(type);

      if (type != ResistanceType.Physical && max > 60 && CurseSpell.UnderEffect(this))
        max = 60;

      if (Core.ML && Race == Race.Elf && type == ResistanceType.Energy)
        max += 5; // Intended to go after the 60 max from curse

      return max;
    }

    protected override void OnRaceChange(Race oldRace)
    {
      ValidateEquipment();
      UpdateResistances();
    }

    public override void OnNetStateChanged()
    {
      m_LastGlobalLight = -1;
      m_LastPersonalLight = -1;
    }

    public override void ComputeBaseLightLevels(out int global, out int personal)
    {
      global = LightCycle.ComputeLevelFor(this);

      bool racialNightSight = Core.ML && Race == Race.Elf;

      if (LightLevel < 21 && (AosAttributes.GetValue(this, AosAttribute.NightSight) > 0 || racialNightSight))
        personal = 21;
      else
        personal = LightLevel;
    }

    public override void CheckLightLevels(bool forceResend)
    {
      NetState ns = NetState;

      if (ns == null)
        return;

      ComputeLightLevels(out int global, out int personal);

      if (!forceResend)
        forceResend = global != m_LastGlobalLight || personal != m_LastPersonalLight;

      if (!forceResend)
        return;

      m_LastGlobalLight = global;
      m_LastPersonalLight = personal;

      ns.Send(GlobalLightLevel.Instantiate(global));
      ns.Send(new PersonalLightLevel(Serial, personal));
    }

    public override int GetMinResistance(ResistanceType type)
    {
      int magicResist = (int)(Skills.MagicResist.Value * 10);
      int min = int.MinValue;

      if (magicResist >= 1000)
        min = 40 + (magicResist - 1000) / 50;
      else if (magicResist >= 400)
        min = (magicResist - 400) / 15;

      if (min > MaxPlayerResistance)
        min = MaxPlayerResistance;

      int baseMin = base.GetMinResistance(type);

      if (min < baseMin)
        min = baseMin;

      return min;
    }

    public override void OnManaChange(int oldValue)
    {
      base.OnManaChange(oldValue);
      if (ExecutesLightningStrike > 0)
        if (Mana < ExecutesLightningStrike)
          SpecialMove.ClearCurrentMove(this);
    }

    private static void OnLogin(Mobile from)
    {
      CheckAtrophies(from);

      if (AccountHandler.LockdownLevel > AccessLevel.Player)
      {
        string notice;

        if (!(from.Account is Account acct) || !acct.HasAccess(from.NetState))
        {
          if (from.AccessLevel == AccessLevel.Player)
            notice = "The server is currently under lockdown. No players are allowed to log in at this time.";
          else
            notice =
              "The server is currently under lockdown. You do not have sufficient access level to connect.";

          if (from.NetState != null)
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), from.NetState.Dispose);
        }
        else if (from.AccessLevel >= AccessLevel.Administrator)
        {
          notice =
            "The server is currently under lockdown. As you are an administrator, you may change this from the [Admin gump.";
        }
        else
        {
          notice = "The server is currently under lockdown. You have sufficient access level to connect.";
        }

        from.SendGump(new NoticeGump(1060637, 30720, notice, 0xFFC000, 300, 140));
        return;
      }

      if (from is PlayerMobile mobile)
        mobile.ClaimAutoStabledPets();
    }

    public void ValidateEquipment()
    {
      if (m_NoDeltaRecursion || Map == null || Map == Map.Internal)
        return;

      if (Items == null)
        return;

      m_NoDeltaRecursion = true;
      Timer.DelayCall(ValidateEquipment_Sandbox);
    }

    private void ValidateEquipment_Sandbox()
    {
      try
      {
        if (Map == null || Map == Map.Internal)
          return;

        List<Item> items = Items;

        if (items == null)
          return;

        bool moved = false;

        int str = Str;
        int dex = Dex;
        int intel = Int;

        int factionItemCount = 0;

        Mobile from = this;

        Ethic ethic = Ethic.Find(from);

        for (int i = items.Count - 1; i >= 0; --i)
        {
          if (i >= items.Count)
            continue;

          Item item = items[i];

          if ((item.SavedFlags & 0x100) != 0)
          {
            if (item.Hue != Ethic.Hero.Definition.PrimaryHue)
            {
              item.SavedFlags &= ~0x100;
            }
            else if (ethic != Ethic.Hero)
            {
              from.AddToBackpack(item);
              moved = true;
              continue;
            }
          }
          else if ((item.SavedFlags & 0x200) != 0)
          {
            if (item.Hue != Ethic.Evil.Definition.PrimaryHue)
            {
              item.SavedFlags &= ~0x200;
            }
            else if (ethic != Ethic.Evil)
            {
              from.AddToBackpack(item);
              moved = true;
              continue;
            }
          }

          if (item is BaseWeapon weapon)
          {
            bool drop = false;

            if (dex < weapon.DexRequirement)
              drop = true;
            else if (str < AOS.Scale(weapon.StrRequirement, 100 - weapon.GetLowerStatReq()))
              drop = true;
            else if (intel < weapon.IntRequirement)
              drop = true;
            else if (weapon.RequiredRace != null && weapon.RequiredRace != Race)
              drop = true;

            if (drop)
            {
              from.SendLocalizedMessage(1062001, weapon.Name ?? $"#{weapon.LabelNumber}"); // You can no longer wield your ~1_WEAPON~
              from.AddToBackpack(weapon);
              moved = true;
            }
          }
          else if (item is BaseArmor armor)
          {
            bool drop = false;

            if (!armor.AllowMaleWearer && !from.Female && from.AccessLevel < AccessLevel.GameMaster)
            {
              drop = true;
            }
            else if (!armor.AllowFemaleWearer && from.Female && from.AccessLevel < AccessLevel.GameMaster)
            {
              drop = true;
            }
            else if (armor.RequiredRace != null && armor.RequiredRace != Race)
            {
              drop = true;
            }
            else
            {
              int strBonus = armor.ComputeStatBonus(StatType.Str), strReq = armor.ComputeStatReq(StatType.Str);
              int dexBonus = armor.ComputeStatBonus(StatType.Dex), dexReq = armor.ComputeStatReq(StatType.Dex);
              int intBonus = armor.ComputeStatBonus(StatType.Int), intReq = armor.ComputeStatReq(StatType.Int);

              if (dex < dexReq || dex + dexBonus < 1)
                drop = true;
              else if (str < strReq || str + strBonus < 1)
                drop = true;
              else if (intel < intReq || intel + intBonus < 1)
                drop = true;
            }

            if (drop)
            {
              string name = armor.Name ?? $"#{armor.LabelNumber}";

              if (armor is BaseShield)
                from.SendLocalizedMessage(1062003, name); // You can no longer equip your ~1_SHIELD~
              else
                from.SendLocalizedMessage(1062002, name); // You can no longer wear your ~1_ARMOR~

              from.AddToBackpack(armor);
              moved = true;
            }
          }
          else if (item is BaseClothing clothing)
          {
            bool drop = false;

            if (!clothing.AllowMaleWearer && !from.Female && from.AccessLevel < AccessLevel.GameMaster)
            {
              drop = true;
            }
            else if (!clothing.AllowFemaleWearer && from.Female && from.AccessLevel < AccessLevel.GameMaster)
            {
              drop = true;
            }
            else if (clothing.RequiredRace != null && clothing.RequiredRace != Race)
            {
              drop = true;
            }
            else
            {
              int strBonus = clothing.ComputeStatBonus(StatType.Str);
              int strReq = clothing.ComputeStatReq(StatType.Str);

              if (str < strReq || str + strBonus < 1)
                drop = true;
            }

            if (drop)
            {
              from.SendLocalizedMessage(1062002, clothing.Name ?? $"#{clothing.LabelNumber}"); // You can no longer wear your ~1_ARMOR~

              from.AddToBackpack(clothing);
              moved = true;
            }
          }

          FactionItem factionItem = FactionItem.Find(item);

          if (factionItem != null)
          {
            bool drop = false;

            Faction ourFaction = Faction.Find(this);

            if (ourFaction == null || ourFaction != factionItem.Faction)
              drop = true;
            else if (++factionItemCount > FactionItem.GetMaxWearables(this))
              drop = true;

            if (drop)
            {
              from.AddToBackpack(item);
              moved = true;
            }
          }
        }

        if (moved)
          from.SendLocalizedMessage(500647); // Some equipment has been moved to your backpack.
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }
      finally
      {
        m_NoDeltaRecursion = false;
      }
    }

    public override void Delta(MobileDelta flag)
    {
      base.Delta(flag);

      if ((flag & MobileDelta.Stat) != 0)
        ValidateEquipment();
    }

    private static void OnLogout(Mobile m)
    {
      (m as PlayerMobile)?.AutoStablePets();
    }

    private static void EventSink_Connected(Mobile m)
    {
      if (m is PlayerMobile pm)
      {
        pm.SessionStart = DateTime.UtcNow;

        pm.Quest?.StartTimer();

        pm.BedrollLogout = false;
        pm.LastOnline = DateTime.UtcNow;
      }

      DisguiseTimers.StartTimer(m);

      Timer.DelayCall(SpecialMove.ClearAllMoves, m);
    }

    private static void EventSink_Disconnected(Mobile from)
    {
      DesignContext context = DesignContext.Find(from);

      if (context != null)
      {
        /* Client disconnected
         *  - Remove design context
         *  - Eject all from house
         *  - Restore relocated entities
         */

        // Remove design context
        DesignContext.Remove(from);

        // Eject all from house
        from.RevealingAction();

        foreach (Item item in context.Foundation.GetItems())
          item.Location = context.Foundation.BanLocation;

        foreach (Mobile mobile in context.Foundation.GetMobiles())
          mobile.Location = context.Foundation.BanLocation;

        // Restore relocated entities
        context.Foundation.RestoreRelocatedEntities();
      }

      if (from is PlayerMobile pm)
      {
        pm.m_GameTime += DateTime.UtcNow - pm.SessionStart;

        pm.Quest?.StopTimer();

        pm.SpeechLog = null;
        pm.LastOnline = DateTime.UtcNow;
      }

      DisguiseTimers.StopTimer(from);
    }

    public override void RevealingAction()
    {
      if (DesignContext != null)
        return;

      InvisibilitySpell.RemoveTimer(this);

      base.RevealingAction();

      IsStealthing = false; // IsStealthing should be moved to Server.Mobiles
    }

    public override void OnHiddenChanged()
    {
      base.OnHiddenChanged();

      RemoveBuff(BuffIcon
        .Invisibility); // Always remove, default to the hiding icon EXCEPT in the invis spell where it's explicitly set

      if (!Hidden)
        RemoveBuff(BuffIcon.HidingAndOrStealth);
      else // if (!InvisibilitySpell.HasTimer( this ))
        BuffInfo.AddBuff(this,
          new BuffInfo(BuffIcon.HidingAndOrStealth, 1075655)); // Hidden/Stealthing & You Are Hidden
    }

    public override void OnSubItemAdded(Item item)
    {
      if (AccessLevel < AccessLevel.GameMaster && item.IsChildOf(Backpack))
      {
        int maxWeight = WeightOverloading.GetMaxWeight(this);
        int curWeight = BodyWeight + TotalWeight;

        if (curWeight > maxWeight)
          SendLocalizedMessage(1019035, true, $" : {curWeight} / {maxWeight}");
      }

      base.OnSubItemAdded(item);
    }

    public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
    {
      if (DesignContext != null || (target is PlayerMobile mobile && mobile.DesignContext != null))
        return false;

      if ((target is BaseCreature creature && creature.IsInvulnerable) || target is PlayerVendor || target is TownCrier)
      {
        if (message)
        {
          if (target.Title == null)
            SendMessage("{0} cannot be harmed.", target.Name);
          else
            SendMessage("{0} {1} cannot be harmed.", target.Name, target.Title);
        }

        return false;
      }

      return base.CanBeHarmful(target, message, ignoreOurBlessedness);
    }

    public override bool CanBeBeneficial(Mobile target, bool message, bool allowDead)
    {
      if (DesignContext != null || (target is PlayerMobile mobile && mobile.DesignContext != null))
        return false;

      return base.CanBeBeneficial(target, message, allowDead);
    }

    public override bool CheckContextMenuDisplay(IEntity target) => DesignContext == null;

    public override void OnItemAdded(Item item)
    {
      base.OnItemAdded(item);

      if (item is BaseArmor || item is BaseWeapon)
      {
        Hits = Hits;
        Stam = Stam;
        Mana = Mana;
      }

      if (NetState != null)
        CheckLightLevels(false);
    }

    public override void OnItemRemoved(Item item)
    {
      base.OnItemRemoved(item);

      if (item is BaseArmor || item is BaseWeapon)
      {
        Hits = Hits;
        Stam = Stam;
        Mana = Mana;
      }

      if (NetState != null)
        CheckLightLevels(false);
    }

    private void AddArmorRating(ref double rating, Item armor)
    {
      if (armor is BaseArmor ar && (!Core.AOS || ar.ArmorAttributes.MageArmor == 0))
        rating += ar.ArmorRatingScaled;
    }

    public override bool Move(Direction d)
    {
      NetState ns = NetState;

      if (ns != null)
        if (HasGump<ResurrectGump>())
        {
          if (Alive)
          {
            CloseGump<ResurrectGump>();
          }
          else
          {
            SendLocalizedMessage(500111); // You are frozen and cannot move.
            return false;
          }
        }

      int speed = ComputeMovementSpeed(d);

      bool res;

      if (!Alive)
        MovementImpl.IgnoreMovableImpassables = true;

      res = base.Move(d);

      MovementImpl.IgnoreMovableImpassables = false;

      if (!res)
        return false;

      m_NextMovementTime += speed;

      return true;
    }

    public override bool CheckMovement(Direction d, out int newZ)
    {
      DesignContext context = DesignContext;

      if (context == null)
        return base.CheckMovement(d, out newZ);

      HouseFoundation foundation = context.Foundation;

      newZ = foundation.Z + HouseFoundation.GetLevelZ(context.Level, context.Foundation);

      int newX = X, newY = Y;
      Movement.Movement.Offset(d, ref newX, ref newY);

      int startX = foundation.X + foundation.Components.Min.X + 1;
      int startY = foundation.Y + foundation.Components.Min.Y + 1;
      int endX = startX + foundation.Components.Width - 1;
      int endY = startY + foundation.Components.Height - 2;

      return newX >= startX && newY >= startY && newX < endX && newY < endY && Map == foundation.Map;
    }

    public override bool AllowItemUse(Item item)
    {
      if (DuelContext?.AllowItemUse(this, item) == false)
        return false;

      return DesignContext.Check(this);
    }

    public override bool AllowSkillUse(SkillName skill)
    {
      if (AnimalForm.UnderTransformation(this))
        for (int i = 0; i < AnimalFormRestrictedSkills.Length; i++)
          if (AnimalFormRestrictedSkills[i] == skill)
          {
            SendLocalizedMessage(1070771); // You cannot use that skill in this form.
            return false;
          }

      if (DuelContext?.AllowSkillUse(this, skill) == false)
        return false;

      return DesignContext.Check(this);
    }

    public virtual void RecheckTownProtection()
    {
      m_NextProtectionCheck = 10;

      GuardedRegion reg = Region.GetRegion<GuardedRegion>();
      bool isProtected = reg?.IsDisabled() == false;

      if (isProtected != m_LastProtectedMessage)
      {
        if (isProtected)
          SendLocalizedMessage(500112); // You are now under the protection of the town guards.
        else
          SendLocalizedMessage(500113); // You have left the protection of the town guards.

        m_LastProtectedMessage = isProtected;
      }
    }

    public override void MoveToWorld(Point3D loc, Map map)
    {
      base.MoveToWorld(loc, map);

      RecheckTownProtection();
    }

    public override void SetLocation(Point3D loc, bool isTeleport)
    {
      if (!isTeleport && AccessLevel == AccessLevel.Player)
      {
        // moving, not teleporting
        int zDrop = Location.Z - loc.Z;

        if (zDrop > 20) // we fell more than one story
          Hits -= zDrop / 20 * 10 - 5; // deal some damage; does not kill, disrupt, etc
      }

      base.SetLocation(loc, isTeleport);

      if (isTeleport || --m_NextProtectionCheck == 0)
        RecheckTownProtection();
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
      base.GetContextMenuEntries(from, list);

      if (from == this)
      {
        Quest?.GetContextMenuEntries(list);

        if (Alive)
        {
          if (InsuranceEnabled)
          {
            if (Core.SA)
              list.Add(new CallbackEntry(1114299, OpenItemInsuranceMenu)); // Open Item Insurance Menu

            list.Add(new CallbackEntry(6201, ToggleItemInsurance)); // Toggle Item Insurance

            if (!Core.SA)
            {
              if (AutoRenewInsurance)
                list.Add(new CallbackEntry(6202,
                  CancelRenewInventoryInsurance)); // Cancel Renewing Inventory Insurance
              else
                list.Add(new CallbackEntry(6200,
                  AutoRenewInventoryInsurance)); // Auto Renew Inventory Insurance
            }
          }

          if (MLQuestSystem.Enabled)
            list.Add(new CallbackEntry(6169, ToggleQuestItem)); // Toggle Quest Item
        }

        BaseHouse house = BaseHouse.FindHouseAt(this);

        if (house != null)
        {
          if (Alive && house.InternalizedVendors.Count > 0 && house.IsOwner(this))
            list.Add(new CallbackEntry(6204, GetVendor));

          if (house.IsAosRules && !Region.IsPartOf<SafeZone>()) // Dueling
            list.Add(new CallbackEntry(6207, LeaveHouse));
        }

        if (JusticeProtectors.Count > 0)
          list.Add(new CallbackEntry(6157, CancelProtection));

        if (Alive)
          list.Add(new CallbackEntry(6210, ToggleChampionTitleDisplay));

        if (Core.HS)
        {
          NetState ns = from.NetState;

          if (ns?.ExtendedStatus == true)
            list.Add(new CallbackEntry(RefuseTrades ? 1154112 : 1154113,
              ToggleTrades)); // Allow Trades / Refuse Trades
        }
      }
      else
      {
        if (Core.TOL && from.InRange(this, 2)) list.Add(new CallbackEntry(1077728, () => OpenTrade(from))); // Trade

        if (Alive && Core.Expansion >= Expansion.AOS)
        {
          Party theirParty = from.Party as Party;
          Party ourParty = Party as Party;

          if (theirParty == null && ourParty == null)
          {
            list.Add(new AddToPartyEntry(from, this));
          }
          else if (theirParty != null && theirParty.Leader == from)
          {
            if (ourParty == null)
              list.Add(new AddToPartyEntry(from, this));
            else if (ourParty == theirParty) list.Add(new RemoveFromPartyEntry(from, this));
          }
        }

        BaseHouse curhouse = BaseHouse.FindHouseAt(this);

        if (curhouse != null && Alive && Core.Expansion >= Expansion.AOS && curhouse.IsAosRules && curhouse.IsFriend(from))
          list.Add(new EjectPlayerEntry(from, this));
      }
    }

    private void CancelProtection()
    {
      for (int i = 0; i < JusticeProtectors.Count; ++i)
      {
        Mobile prot = JusticeProtectors[i];

        string args = $"{Name}\t{prot.Name}";

        prot.SendLocalizedMessage(1049371,
          args); // The protective relationship between ~1_PLAYER1~ and ~2_PLAYER2~ has been ended.
        SendLocalizedMessage(1049371,
          args); // The protective relationship between ~1_PLAYER1~ and ~2_PLAYER2~ has been ended.
      }

      JusticeProtectors.Clear();
    }

    private void ToggleTrades()
    {
      RefuseTrades = !RefuseTrades;
    }

    private void GetVendor()
    {
      BaseHouse house = BaseHouse.FindHouseAt(this);

      if (CheckAlive() && house?.IsOwner(this) == true && house.InternalizedVendors.Count > 0)
      {
        CloseGump<ReclaimVendorGump>();
        SendGump(new ReclaimVendorGump(house));
      }
    }

    private void LeaveHouse()
    {
      BaseHouse house = BaseHouse.FindHouseAt(this);

      if (house != null)
        Location = house.BanLocation;
    }

    public override void DisruptiveAction()
    {
      if (Meditating)
        RemoveBuff(BuffIcon.ActiveMeditation);

      base.DisruptiveAction();
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (this == from && !Warmode)
      {
        IMount mount = Mount;

        if (mount != null && !DesignContext.Check(this))
          return;
      }

      base.OnDoubleClick(from);
    }

    public override void DisplayPaperdollTo(Mobile to)
    {
      if (DesignContext.Check(this))
        base.DisplayPaperdollTo(to);
    }

    public override bool CheckEquip(Item item)
    {
      if (!base.CheckEquip(item))
        return false;

      if (DuelContext?.AllowItemEquip(this, item) == false)
        return false;

      FactionItem factionItem = FactionItem.Find(item);

      if (factionItem != null)
      {
        Faction faction = Faction.Find(this);

        if (faction == null)
        {
          SendLocalizedMessage(1010371); // You cannot equip a faction item!
          return false;
        }

        if (faction != factionItem.Faction)
        {
          SendLocalizedMessage(1010372); // You cannot equip an opposing faction's item!
          return false;
        }

        int maxWearables = FactionItem.GetMaxWearables(this);

        for (int i = 0; i < Items.Count; ++i)
        {
          Item equipped = Items[i];

          if (item != equipped && FactionItem.Find(equipped) != null)
            if (--maxWearables == 0)
            {
              SendLocalizedMessage(1010373); // You do not have enough rank to equip more faction items!
              return false;
            }
        }
      }

      if (AccessLevel < AccessLevel.GameMaster && item.Layer != Layer.Mount && HasTrade)
      {
        BounceInfo bounce = item.GetBounce();

        if (bounce != null)
        {
          if (bounce.Parent is Item parent)
          {
            if (parent == Backpack || parent.IsChildOf(Backpack))
              return true;
          }
          else if (bounce.Parent == this)
          {
            return true;
          }
        }

        SendLocalizedMessage(
          1004042); // You can only equip what you are already carrying while you have a trade pending.
        return false;
      }

      return true;
    }

    public override bool CheckTrade(Mobile to, Item item, SecureTradeContainer cont, bool message, bool checkItems,
      int plusItems, int plusWeight)
    {
      int msgNum = 0;

      if (cont == null)
      {
        if (to.Holding != null)
          msgNum = 1062727; // You cannot trade with someone who is dragging something.
        else if (HasTrade)
          msgNum = 1062781; // You are already trading with someone else!
        else if (to.HasTrade)
          msgNum = 1062779; // That person is already involved in a trade
        else if (to is PlayerMobile mobile && mobile.RefuseTrades)
          msgNum = 1154111; // ~1_NAME~ is refusing all trades.
      }

      if (msgNum == 0 && item != null)
      {
        if (cont != null)
        {
          plusItems += cont.TotalItems;
          plusWeight += cont.TotalWeight;
        }

        if (Backpack?.CheckHold(this, item, false, checkItems, plusItems, plusWeight) != true)
          msgNum = 1004040; // You would not be able to hold this if the trade failed.
        else if (to.Backpack?.CheckHold(to, item, false, checkItems, plusItems, plusWeight) != true)
          msgNum = 1004039; // The recipient of this trade would not be able to carry this.
        else
          msgNum = CheckContentForTrade(item);
      }

      if (msgNum != 0)
      {
        if (message)
        {
          if (msgNum == 1154111)
            SendLocalizedMessage(msgNum, to.Name);
          else
            SendLocalizedMessage(msgNum);
        }

        return false;
      }

      return true;
    }

    private static int CheckContentForTrade(Item item)
    {
      if (item is TrappableContainer container && container.TrapType != TrapType.None)
        return 1004044; // You may not trade trapped items.

      if (StolenItem.IsStolen(item))
        return 1004043; // You may not trade recently stolen items.

      if (item is Container)
        foreach (Item subItem in item.Items)
        {
          int msg = CheckContentForTrade(subItem);

          if (msg != 0)
            return msg;
        }

      return 0;
    }

    public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
    {
      if (!base.CheckNonlocalDrop(from, item, target))
        return false;

      if (from.AccessLevel >= AccessLevel.GameMaster)
        return true;

      Container pack = Backpack;
      if (from == this && HasTrade && (target == pack || target.IsChildOf(pack)))
      {
        BounceInfo bounce = item.GetBounce();

        if (bounce?.Parent is Item parent && (parent == pack || parent.IsChildOf(pack)))
          return true;

        SendLocalizedMessage(1004041); // You can't do that while you have a trade pending.
        return false;
      }

      return true;
    }

    protected override void OnLocationChange(Point3D oldLocation)
    {
      CheckLightLevels(false);

      DuelContext?.OnLocationChanged(this);

      DesignContext context = DesignContext;

      if (context == null || m_NoRecursion)
        return;

      m_NoRecursion = true;

      HouseFoundation foundation = context.Foundation;

      int newX = X, newY = Y;
      int newZ = foundation.Z + HouseFoundation.GetLevelZ(context.Level, context.Foundation);

      int startX = foundation.X + foundation.Components.Min.X + 1;
      int startY = foundation.Y + foundation.Components.Min.Y + 1;
      int endX = startX + foundation.Components.Width - 1;
      int endY = startY + foundation.Components.Height - 2;

      if (newX >= startX && newY >= startY && newX < endX && newY < endY && Map == foundation.Map)
      {
        if (Z != newZ)
          Location = new Point3D(X, Y, newZ);

        m_NoRecursion = false;
        return;
      }

      Location = new Point3D(foundation.X, foundation.Y, newZ);
      Map = foundation.Map;

      m_NoRecursion = false;
    }

    public override bool OnMoveOver(Mobile m)
    {
      if (m is BaseCreature creature && !creature.Controlled)
        return !Alive || !creature.Alive || IsDeadBondedPet || creature.IsDeadBondedPet ||
               (Hidden && AccessLevel > AccessLevel.Player);

      if (Region.IsPartOf<SafeZone>() && m is PlayerMobile pm)
        if (pm.DuelContext == null || pm.DuelPlayer == null || !pm.DuelContext.Started || pm.DuelContext.Finished ||
            pm.DuelPlayer.Eliminated)
          return true;

      return base.OnMoveOver(m);
    }

    public override bool CheckShove(Mobile shoved)
    {
      if (m_IgnoreMobiles || TransformationSpellHelper.UnderTransformation(shoved, typeof(WraithFormSpell)))
        return true;

      return base.CheckShove(shoved);
    }

    protected override void OnMapChange(Map oldMap)
    {
      if ((Map != Faction.Facet && oldMap == Faction.Facet) || (Map == Faction.Facet && oldMap != Faction.Facet))
        InvalidateProperties();

      DuelContext?.OnMapChanged(this);

      DesignContext context = DesignContext;

      if (context == null || m_NoRecursion)
        return;

      m_NoRecursion = true;

      HouseFoundation foundation = context.Foundation;

      if (Map != foundation.Map)
        Map = foundation.Map;

      m_NoRecursion = false;
    }

    public override void OnBeneficialAction(Mobile target, bool isCriminal)
    {
      SentHonorContext?.OnSourceBeneficialAction(target);

      base.OnBeneficialAction(target, isCriminal);
    }

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
      int disruptThreshold;

      if (!Core.AOS)
        disruptThreshold = 0;
      else if (from?.Player == true)
        disruptThreshold = 18;
      else
        disruptThreshold = 25;

      if (amount > disruptThreshold)
      {
        BandageContext c = BandageContext.GetContext(this);

        c?.Slip();
      }

      if (Confidence.IsRegenerating(this))
        Confidence.StopRegenerating(this);

      WeightOverloading.FatigueOnDamage(this, amount);

      ReceivedHonorContext?.OnTargetDamaged(from, amount);
      SentHonorContext?.OnSourceDamaged(from, amount);

      if (willKill && from is PlayerMobile mobile)
        Timer.DelayCall(TimeSpan.FromSeconds(10), mobile.RecoverAmmo);

      base.OnDamage(amount, from, willKill);
    }

    public override void Resurrect()
    {
      bool wasAlive = Alive;

      base.Resurrect();

      if (Alive && !wasAlive)
      {
        Item deathRobe = new DeathRobe();

        if (!EquipItem(deathRobe))
          deathRobe.Delete();
      }
    }

    public override void OnWarmodeChanged()
    {
      if (!Warmode)
        Timer.DelayCall(TimeSpan.FromSeconds(10), RecoverAmmo);
    }

    private bool FindItems_Callback(Item item) =>
      !item.Deleted && (item.LootType == LootType.Blessed || item.Insured) &&
      Backpack != item.Parent;

    public override bool OnBeforeDeath()
    {
      NetState state = NetState;

      state?.CancelAllTrades();

      DropHolding();

      if (Core.AOS && Backpack?.Deleted == false) Backpack.FindItemsByType<Item>(FindItems_Callback).ForEach(item => Backpack.AddItem(item));

      EquipSnapshot = new List<Item>(Items);

      m_NonAutoreinsuredItems = 0;
      m_InsuranceAward = FindMostRecentDamager(false);

      if (m_InsuranceAward is BaseCreature creature)
      {
        Mobile master = creature.GetMaster();

        if (master != null)
          m_InsuranceAward = master;
      }

      if (m_InsuranceAward != null && (!m_InsuranceAward.Player || m_InsuranceAward == this))
        m_InsuranceAward = null;

      if (m_InsuranceAward is PlayerMobile mobile)
        mobile.m_InsuranceBonus = 0;

      ReceivedHonorContext?.OnTargetKilled();
      SentHonorContext?.OnSourceKilled();

      RecoverAmmo();

      return base.OnBeforeDeath();
    }

    private bool CheckInsuranceOnDeath(Item item)
    {
      if (!InsuranceEnabled || !item.Insured)
        return false;

      if (DuelContext?.Registered == true && DuelContext.Started &&
          m_DuelPlayer?.Eliminated != true)
        return true;

      if (AutoRenewInsurance)
      {
        int cost = GetInsuranceCost(item);

        if (m_InsuranceAward != null)
          cost /= 2;

        if (Banker.Withdraw(this, cost))
        {
          item.PaidInsurance = true;
          SendLocalizedMessage(1060398,
            cost.ToString()); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
        }
        else
        {
          SendLocalizedMessage(1061079, "", 0x23); // You lack the funds to purchase the insurance
          item.PaidInsurance = false;
          item.Insured = false;
          m_NonAutoreinsuredItems++;
        }
      }
      else
      {
        item.PaidInsurance = false;
        item.Insured = false;
      }

      if (m_InsuranceAward != null && Banker.Deposit(m_InsuranceAward, 300) && m_InsuranceAward is PlayerMobile pm)
        pm.m_InsuranceBonus += 300;

      return true;
    }

    public override DeathMoveResult GetParentMoveResultFor(Item item)
    {
      // It seems all items are unmarked on death, even blessed/insured ones
      if (item.QuestItem)
        item.QuestItem = false;

      if (CheckInsuranceOnDeath(item))
        return DeathMoveResult.MoveToBackpack;

      DeathMoveResult res = base.GetParentMoveResultFor(item);

      if (res == DeathMoveResult.MoveToCorpse && item.Movable && Young)
        res = DeathMoveResult.MoveToBackpack;

      return res;
    }

    public override DeathMoveResult GetInventoryMoveResultFor(Item item)
    {
      // It seems all items are unmarked on death, even blessed/insured ones
      if (item.QuestItem)
        item.QuestItem = false;

      if (CheckInsuranceOnDeath(item))
        return DeathMoveResult.MoveToBackpack;

      DeathMoveResult res = base.GetInventoryMoveResultFor(item);

      if (res == DeathMoveResult.MoveToCorpse && item.Movable && Young)
        res = DeathMoveResult.MoveToBackpack;

      return res;
    }

    public override void OnDeath(Container c)
    {
      if (m_NonAutoreinsuredItems > 0) SendLocalizedMessage(1061115);

      base.OnDeath(c);

      EquipSnapshot = null;

      HueMod = -1;
      NameMod = null;
      SavagePaintExpiration = TimeSpan.Zero;

      SetHairMods(-1, -1);

      PolymorphSpell.StopTimer(this);
      IncognitoSpell.StopTimer(this);
      DisguiseTimers.RemoveTimer(this);

      EndAction<PolymorphSpell>();
      EndAction<IncognitoSpell>();

      MeerMage.StopEffect(this, false);

      if (Flying)
      {
        Flying = false;
        BuffInfo.RemoveBuff(this, BuffIcon.Fly);
      }

      StolenItem.ReturnOnDeath(this, c);

      if (PermaFlags.Count > 0)
      {
        PermaFlags.Clear();

        if (c is Corpse corpse)
          corpse.Criminal = true;

        if (Stealing.ClassicMode)
          Criminal = true;
      }

      if (Kills >= 5 && DateTime.UtcNow >= m_NextJustAward)
      {
        Mobile m = FindMostRecentDamager(false);

        if (m is BaseCreature bc)
          m = bc.GetMaster();

        if (m != this && m is PlayerMobile)
        {
          bool gainedPath = false;

          int pointsToGain = 0;

          pointsToGain += (int)Math.Sqrt(GameTime.TotalSeconds * 4);
          pointsToGain *= 5;
          pointsToGain += (int)Math.Pow(Skills.Total / 250.0, 2);

          if (VirtueHelper.Award(m, VirtueName.Justice, pointsToGain, ref gainedPath))
          {
            if (gainedPath)
              m.SendLocalizedMessage(1049367); // You have gained a path in Justice!
            else
              m.SendLocalizedMessage(1049363); // You have gained in Justice.

            m.FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist);
            m.PlaySound(0x1F7);

            m_NextJustAward = DateTime.UtcNow + TimeSpan.FromMinutes(pointsToGain / 3.0);
          }
        }
      }

      if (m_InsuranceAward is PlayerMobile pm)
        if (pm.m_InsuranceBonus > 0)
          pm.SendLocalizedMessage(1060397,
            pm.m_InsuranceBonus.ToString()); // ~1_AMOUNT~ gold has been deposited into your bank box.

      Mobile killer = FindMostRecentDamager(true);

      if (killer is BaseCreature bcKiller)
      {
        Mobile master = bcKiller.GetMaster();
        if (master != null)
          killer = master;
      }

      if (Young && DuelContext == null)
        if (YoungDeathTeleport())
          Timer.DelayCall(TimeSpan.FromSeconds(2.5), SendYoungDeathNotice);

      if (DuelContext?.Registered != true || !DuelContext.Started || m_DuelPlayer?.Eliminated != false)
        Faction.HandleDeath(this, killer);

      Guilds.Guild.HandleDeath(this, killer);

      MLQuestSystem.HandleDeath(this);

      DuelContext?.OnDeath(this, c);

      if (m_BuffTable != null)
      {
        List<BuffInfo> list = new List<BuffInfo>();

        foreach (BuffInfo buff in m_BuffTable.Values)
          if (!buff.RetainThroughDeath)
            list.Add(buff);

        for (int i = 0; i < list.Count; i++)
          RemoveBuff(list[i]);
      }
    }

    public override bool MutateSpeech(List<Mobile> hears, ref string text, ref object context)
    {
      if (Alive)
        return false;

      if (Core.ML && Skills.SpiritSpeak.Value >= 100.0)
        return false;

      if (Core.AOS)
        for (int i = 0; i < hears.Count; ++i)
        {
          Mobile m = hears[i];

          if (m != this && m.Skills.SpiritSpeak.Value >= 100.0)
            return false;
        }

      return base.MutateSpeech(hears, ref text, ref context);
    }

    public override void DoSpeech(string text, int[] keywords, MessageType type, int hue)
    {
      if (Guilds.Guild.NewGuildSystem && (type == MessageType.Guild || type == MessageType.Alliance))
      {
        if (!(Guild is Guild g))
        {
          SendLocalizedMessage(1063142); // You are not in a guild!
        }
        else if (type == MessageType.Alliance)
        {
          if (g.Alliance?.IsMember(g) == true)
          {
            // g.Alliance.AllianceTextMessage( hue, "[Alliance][{0}]: {1}", this.Name, text );
            g.Alliance.AllianceChat(this, text);
            SendToStaffMessage(this, "[Alliance]: {0}", text);

            AllianceMessageHue = hue;
          }
          else
          {
            SendLocalizedMessage(1071020); // You are not in an alliance!
          }
        }
        else // Type == MessageType.Guild
        {
          GuildMessageHue = hue;

          g.GuildChat(this, text);
          SendToStaffMessage(this, "[Guild]: {0}", text);
        }
      }
      else
      {
        base.DoSpeech(text, keywords, type, hue);
      }
    }

    private static void SendToStaffMessage(Mobile from, string text)
    {
      Packet p = null;

      foreach (NetState ns in from.GetClientsInRange(8))
      {
        Mobile mob = ns.Mobile;

        if (mob?.AccessLevel >= AccessLevel.GameMaster && mob.AccessLevel > from.AccessLevel)
        {
          p ??= Packet.Acquire(new UnicodeMessage(from.Serial, from.Body, MessageType.Regular, from.SpeechHue, 3, from.Language, from.Name, text));

          ns.Send(p);
        }
      }

      Packet.Release(p);
    }

    private static void SendToStaffMessage(Mobile from, string format, params object[] args)
    {
      SendToStaffMessage(from, string.Format(format, args));
    }

    public override void Damage(int amount, Mobile from)
    {
      if (EvilOmenSpell.TryEndEffect(this))
        amount = (int)(amount * 1.25);

      Mobile oath = BloodOathSpell.GetBloodOath(from);

      /* Per EA's UO Herald Pub48 (ML):
       * ((resist spellsx10)/20 + 10=percentage of damage resisted)
       */

      if (oath == this)
      {
        amount = (int)(amount * 1.1);

        if (amount > 35 && from is PlayerMobile) /* capped @ 35, seems no expansion */ amount = 35;

        if (Core.ML)
          from.Damage((int)(amount * (1 - (from.Skills.MagicResist.Value * .5 + 10) / 100)), this);
        else
          from.Damage(amount, this);
      }

      if (from != null && Talisman is BaseTalisman talisman)
        if (talisman.Protection != null && talisman.Protection.Type != null)
        {
          Type type = talisman.Protection.Type;

          if (type.IsInstanceOfType(from))
            amount = (int)(amount * (1 - (double)talisman.Protection.Amount / 100));
        }

      base.Damage(amount, from);
    }

    public override bool IsHarmfulCriminal(Mobile target)
    {
      if (Stealing.ClassicMode && target is PlayerMobile mobile && mobile.PermaFlags.Count > 0)
      {
        if (Notoriety.Compute(this, mobile) == Notoriety.Innocent)
          mobile.Delta(MobileDelta.Noto);

        return false;
      }

      BaseCreature bc = target as BaseCreature;

      if (bc?.InitialInnocent == true && !bc.Controlled)
        return false;

      if (Core.ML && bc?.Controlled == true && this == bc.ControlMaster)
        return false;

      return base.IsHarmfulCriminal(target);
    }

    public bool AntiMacroCheck(Skill skill, object obj)
    {
      if (obj == null || m_AntiMacroTable == null || AccessLevel != AccessLevel.Player)
        return true;

      if (!m_AntiMacroTable.TryGetValue(skill, out Dictionary<object, CountAndTimeStamp> tbl))
        m_AntiMacroTable[skill] = tbl = new Dictionary<object, CountAndTimeStamp>();

      if (tbl.TryGetValue(obj, out CountAndTimeStamp count))
      {
        if (count.TimeStamp + SkillCheck.AntiMacroExpire <= DateTime.UtcNow)
        {
          count.Count = 1;
          return true;
        }

        ++count.Count;
        return count.Count <= SkillCheck.Allowance;
      }

      tbl[obj] = count = new CountAndTimeStamp();
      count.Count = 1;

      return true;
    }

    private void RevertHair()
    {
      SetHairMods(-1, -1);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();

      switch (version)
      {
        case 29:
          {
            if (reader.ReadBool())
            {
              m_StuckMenuUses = new DateTime[reader.ReadInt()];

              for (int i = 0; i < m_StuckMenuUses.Length; ++i) m_StuckMenuUses[i] = reader.ReadDateTime();
            }
            else
            {
              m_StuckMenuUses = null;
            }

            goto case 28;
          }
        case 28:
          {
            PeacedUntil = reader.ReadDateTime();

            goto case 27;
          }
        case 27:
          {
            AnkhNextUse = reader.ReadDateTime();

            goto case 26;
          }
        case 26:
          {
            AutoStabled = reader.ReadStrongMobileList();

            goto case 25;
          }
        case 25:
          {
            int recipeCount = reader.ReadInt();

            if (recipeCount > 0)
            {
              m_AcquiredRecipes = new Dictionary<int, bool>();

              for (int i = 0; i < recipeCount; i++)
              {
                int r = reader.ReadInt();
                if (reader.ReadBool()) // Don't add in recipes which we haven't gotten or have been removed
                  m_AcquiredRecipes.Add(r, true);
              }
            }

            goto case 24;
          }
        case 24:
          {
            LastHonorLoss = reader.ReadDeltaTime();
            goto case 23;
          }
        case 23:
          {
            ChampionTitles = new ChampionTitleInfo(reader);
            goto case 22;
          }
        case 22:
          {
            LastValorLoss = reader.ReadDateTime();
            goto case 21;
          }
        case 21:
          {
            ToTItemsTurnedIn = reader.ReadEncodedInt();
            ToTTotalMonsterFame = reader.ReadInt();
            goto case 20;
          }
        case 20:
          {
            AllianceMessageHue = reader.ReadEncodedInt();
            GuildMessageHue = reader.ReadEncodedInt();

            goto case 19;
          }
        case 19:
          {
            int rank = reader.ReadEncodedInt();
            int maxRank = RankDefinition.Ranks.Length - 1;
            if (rank > maxRank)
              rank = maxRank;

            m_GuildRank = RankDefinition.Ranks[rank];
            LastOnline = reader.ReadDateTime();
            goto case 18;
          }
        case 18:
          {
            SolenFriendship = (SolenFriendship)reader.ReadEncodedInt();

            goto case 17;
          }
        case 17: // changed how DoneQuests is serialized
        case 16:
          {
            Quest = QuestSerializer.DeserializeQuest(reader);

            if (Quest != null)
              Quest.From = this;

            int count = reader.ReadEncodedInt();

            if (count > 0)
            {
              DoneQuests = new List<QuestRestartInfo>();

              for (int i = 0; i < count; ++i)
              {
                Type questType = QuestSerializer.ReadType(QuestSystem.QuestTypes, reader);
                DateTime restartTime;

                if (version < 17)
                  restartTime = DateTime.MaxValue;
                else
                  restartTime = reader.ReadDateTime();

                DoneQuests.Add(new QuestRestartInfo(questType, restartTime));
              }
            }

            Profession = reader.ReadEncodedInt();
            goto case 15;
          }
        case 15:
          {
            LastCompassionLoss = reader.ReadDeltaTime();
            goto case 14;
          }
        case 14:
          {
            CompassionGains = reader.ReadEncodedInt();

            if (CompassionGains > 0)
              NextCompassionDay = reader.ReadDeltaTime();

            goto case 13;
          }
        case 13: // just removed m_PaidInsurance list
        case 12:
          {
            BOBFilter = new BOBFilter(reader);
            goto case 11;
          }
        case 11:
          {
            if (version < 13)
            {
              List<Item> paid = reader.ReadStrongItemList();

              for (int i = 0; i < paid.Count; ++i)
                paid[i].PaidInsurance = true;
            }

            goto case 10;
          }
        case 10:
          {
            if (reader.ReadBool())
            {
              m_HairModID = reader.ReadInt();
              m_HairModHue = reader.ReadInt();
              m_BeardModID = reader.ReadInt();
              m_BeardModHue = reader.ReadInt();
            }

            goto case 9;
          }
        case 9:
          {
            SavagePaintExpiration = reader.ReadTimeSpan();

            if (SavagePaintExpiration > TimeSpan.Zero)
            {
              BodyMod = Female ? 184 : 183;
              HueMod = 0;
            }

            goto case 8;
          }
        case 8:
          {
            NpcGuild = (NpcGuild)reader.ReadInt();
            NpcGuildJoinTime = reader.ReadDateTime();
            NpcGuildGameTime = reader.ReadTimeSpan();
            goto case 7;
          }
        case 7:
          {
            PermaFlags = reader.ReadStrongMobileList();
            goto case 6;
          }
        case 6:
          {
            NextTailorBulkOrder = reader.ReadTimeSpan();
            goto case 5;
          }
        case 5:
          {
            NextSmithBulkOrder = reader.ReadTimeSpan();
            goto case 4;
          }
        case 4:
          {
            LastJusticeLoss = reader.ReadDeltaTime();
            JusticeProtectors = reader.ReadStrongMobileList();
            goto case 3;
          }
        case 3:
          {
            LastSacrificeGain = reader.ReadDeltaTime();
            LastSacrificeLoss = reader.ReadDeltaTime();
            AvailableResurrects = reader.ReadInt();
            goto case 2;
          }
        case 2:
          {
            Flags = (PlayerFlag)reader.ReadInt();
            goto case 1;
          }
        case 1:
          {
            m_LongTermElapse = reader.ReadTimeSpan();
            m_ShortTermElapse = reader.ReadTimeSpan();
            m_GameTime = reader.ReadTimeSpan();
            goto case 0;
          }
        case 0:
          {
            if (version < 26)
              AutoStabled = new List<Mobile>();
            break;
          }
      }

      RecentlyReported ??= new List<Mobile>();

      // Professions weren't verified on 1.0 RC0
      if (!CharacterCreation.VerifyProfession(Profession))
        Profession = 0;

      PermaFlags ??= new List<Mobile>();
      JusticeProtectors ??= new List<Mobile>();
      BOBFilter ??= new BOBFilter();

      // Default to member if going from older version to new version (only time it should be null)
      m_GuildRank ??= RankDefinition.Member;

      if (LastOnline == DateTime.MinValue && Account != null)
        LastOnline = ((Account)Account).LastLogin;

      ChampionTitles ??= new ChampionTitleInfo();

      if (AccessLevel > AccessLevel.Player)
        m_IgnoreMobiles = true;

      List<Mobile> list = Stabled;

      for (int i = 0; i < list.Count; ++i)
        if (list[i] is BaseCreature bc)
        {
          bc.IsStabled = true;
          bc.StabledBy = this;
        }

      CheckAtrophies(this);

      if (Hidden) // Hiding is the only buff where it has an effect that's serialized.
        AddBuff(new BuffInfo(BuffIcon.HidingAndOrStealth, 1075655));
    }

    public override void Serialize(IGenericWriter writer)
    {
      // cleanup our anti-macro table
      foreach (Dictionary<object, CountAndTimeStamp> t in m_AntiMacroTable.Values)
      {
        List<object> toRemove = t.Where(kvp => kvp.Value.TimeStamp + SkillCheck.AntiMacroExpire <= DateTime.UtcNow)
          .Select(kvp => kvp.Key).ToList();

        foreach (object key in toRemove)
          t.Remove(key);
      }

      CheckKillDecay();

      CheckAtrophies(this);

      base.Serialize(writer);

      writer.Write(29); // version

      if (m_StuckMenuUses != null)
      {
        writer.Write(true);

        writer.Write(m_StuckMenuUses.Length);

        for (int i = 0; i < m_StuckMenuUses.Length; ++i)
          writer.Write(m_StuckMenuUses[i]);
      }
      else
      {
        writer.Write(false);
      }

      writer.Write(PeacedUntil);
      writer.Write(AnkhNextUse);
      writer.Write(AutoStabled, true);

      if (m_AcquiredRecipes == null)
      {
        writer.Write(0);
      }
      else
      {
        writer.Write(m_AcquiredRecipes.Count);

        foreach (KeyValuePair<int, bool> kvp in m_AcquiredRecipes)
        {
          writer.Write(kvp.Key);
          writer.Write(kvp.Value);
        }
      }

      writer.WriteDeltaTime(LastHonorLoss);

      ChampionTitleInfo.Serialize(writer, ChampionTitles);

      writer.Write(LastValorLoss);
      writer.WriteEncodedInt(ToTItemsTurnedIn);
      writer.Write(ToTTotalMonsterFame); // This ain't going to be a small #.

      writer.WriteEncodedInt(AllianceMessageHue);
      writer.WriteEncodedInt(GuildMessageHue);

      writer.WriteEncodedInt(m_GuildRank.Rank);
      writer.Write(LastOnline);

      writer.WriteEncodedInt((int)SolenFriendship);

      QuestSerializer.Serialize(Quest, writer);

      if (DoneQuests == null)
      {
        writer.WriteEncodedInt(0);
      }
      else
      {
        writer.WriteEncodedInt(DoneQuests.Count);

        for (int i = 0; i < DoneQuests.Count; ++i)
        {
          QuestRestartInfo restartInfo = DoneQuests[i];

          QuestSerializer.Write(restartInfo.QuestType, QuestSystem.QuestTypes, writer);
          writer.Write(restartInfo.RestartTime);
        }
      }

      writer.WriteEncodedInt(Profession);

      writer.WriteDeltaTime(LastCompassionLoss);

      writer.WriteEncodedInt(CompassionGains);

      if (CompassionGains > 0)
        writer.WriteDeltaTime(NextCompassionDay);

      BOBFilter.Serialize(writer);

      bool useMods = m_HairModID != -1 || m_BeardModID != -1;

      writer.Write(useMods);

      if (useMods)
      {
        writer.Write(m_HairModID);
        writer.Write(m_HairModHue);
        writer.Write(m_BeardModID);
        writer.Write(m_BeardModHue);
      }

      writer.Write(SavagePaintExpiration);

      writer.Write((int)NpcGuild);
      writer.Write(NpcGuildJoinTime);
      writer.Write(NpcGuildGameTime);

      writer.Write(PermaFlags, true);

      writer.Write(NextTailorBulkOrder);

      writer.Write(NextSmithBulkOrder);

      writer.WriteDeltaTime(LastJusticeLoss);
      writer.Write(JusticeProtectors, true);

      writer.WriteDeltaTime(LastSacrificeGain);
      writer.WriteDeltaTime(LastSacrificeLoss);
      writer.Write(AvailableResurrects);

      writer.Write((int)Flags);

      writer.Write(m_LongTermElapse);
      writer.Write(m_ShortTermElapse);
      writer.Write(GameTime);
    }

    public static void CheckAtrophies(Mobile m)
    {
      SacrificeVirtue.CheckAtrophy(m);
      JusticeVirtue.CheckAtrophy(m);
      CompassionVirtue.CheckAtrophy(m);
      ValorVirtue.CheckAtrophy(m);

      if (m is PlayerMobile mobile)
        ChampionTitleInfo.CheckAtrophy(mobile);
    }

    public void CheckKillDecay()
    {
      if (m_ShortTermElapse < GameTime)
      {
        m_ShortTermElapse += TimeSpan.FromHours(8);
        if (ShortTermMurders > 0)
          --ShortTermMurders;
      }

      if (m_LongTermElapse < GameTime)
      {
        m_LongTermElapse += TimeSpan.FromHours(40);
        if (Kills > 0)
          --Kills;
      }
    }

    public void ResetKillTime()
    {
      m_ShortTermElapse = GameTime + TimeSpan.FromHours(8);
      m_LongTermElapse = GameTime + TimeSpan.FromHours(40);
    }

    public override bool CanSee(Mobile m)
    {
      if (m is CharacterStatue statue)
        statue.OnRequestedAnimation(this);

      if (m is PlayerMobile mobile && mobile.VisibilityList.Contains(this))
        return true;

      if (DuelContext?.Finished == false && DuelContext.m_Tournament != null && m_DuelPlayer?.Eliminated == false)
      {
        Mobile owner = m;

        if (owner is BaseCreature bc)
        {
          Mobile master = bc.GetMaster();

          if (master != null)
            owner = master;
        }

        if (m.AccessLevel == AccessLevel.Player && owner is PlayerMobile pm && pm.DuelContext != DuelContext)
          return false;
      }

      return base.CanSee(m);
    }

    public virtual void CheckedAnimate(int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
    {
      if (!Mounted)
        Animate(action, frameCount, repeatCount, forward, repeat, delay);
    }

    public override bool CanSee(Item item) => DesignContext?.Foundation.IsHiddenToCustomizer(item) != true && base.CanSee(item);

    public override void OnAfterDelete()
    {
      base.OnAfterDelete();

      Faction faction = Faction.Find(this);

      faction?.RemoveMember(this);

      MLQuestSystem.HandleDeletion(this);

      BaseHouse.HandleDeletion(this);

      DisguiseTimers.RemoveTimer(this);
    }

    public override void GetProperties(ObjectPropertyList list)
    {
      base.GetProperties(list);

      if (Map == Faction.Facet)
      {
        PlayerState pl = PlayerState.Find(this);

        if (pl != null)
        {
          Faction faction = pl.Faction;

          if (faction.Commander == this)
            list.Add(1042733, faction.Definition.PropName); // Commanding Lord of the ~1_FACTION_NAME~
          else if (pl.Sheriff != null)
            list.Add(1042734, "{0}\t{1}", pl.Sheriff.Definition.FriendlyName,
              faction.Definition.PropName); // The Sheriff of  ~1_CITY~, ~2_FACTION_NAME~
          else if (pl.Finance != null)
            list.Add(1042735, "{0}\t{1}", pl.Finance.Definition.FriendlyName,
              faction.Definition.PropName); // The Finance Minister of ~1_CITY~, ~2_FACTION_NAME~
          else if (pl.MerchantTitle != MerchantTitle.None)
            list.Add(1060776, "{0}\t{1}", MerchantTitles.GetInfo(pl.MerchantTitle).Title,
              faction.Definition.PropName); // ~1_val~, ~2_val~
          else
            list.Add(1060776, "{0}\t{1}", pl.Rank.Title, faction.Definition.PropName); // ~1_val~, ~2_val~
        }
      }

      if (Core.ML)
        for (int i = AllFollowers.Count - 1; i >= 0; i--)
          if (AllFollowers[i] is BaseCreature c && c.ControlOrder == OrderType.Guard)
          {
            list.Add(501129); // guarded
            break;
          }
    }

    public override void OnSingleClick(Mobile from)
    {
      if (Map == Faction.Facet)
      {
        PlayerState pl = PlayerState.Find(this);

        if (pl != null)
        {
          string text;
          bool ascii = false;

          Faction faction = pl.Faction;

          if (faction.Commander == this)
          {
            text = $"{(Female ? "(Commanding Lady of the " : "(Commanding Lord of the ")}{faction.Definition.FriendlyName})";
          }
          else if (pl.Sheriff != null)
          {
            text = $"(The Sheriff of {pl.Sheriff.Definition.FriendlyName}, {faction.Definition.FriendlyName})";
          }
          else if (pl.Finance != null)
          {
            text = $"(The Finance Minister of {pl.Finance.Definition.FriendlyName}, {faction.Definition.FriendlyName})";
          }
          else
          {
            ascii = true;

            if (pl.MerchantTitle != MerchantTitle.None)
              text = $"({MerchantTitles.GetInfo(pl.MerchantTitle).Title.String}, {faction.Definition.FriendlyName})";
            else
              text = $"({pl.Rank.Title.String}, {faction.Definition.FriendlyName})";
          }

          int hue = Faction.Find(from) == faction ? 98 : 38;

          PrivateOverheadMessage(MessageType.Label, hue, ascii, text, from.NetState);
        }
      }

      base.OnSingleClick(from);
    }

    protected override bool OnMove(Direction d)
    {
      if (!Core.SE)
        return base.OnMove(d);

      if (AccessLevel != AccessLevel.Player)
        return true;

      if (Hidden && DesignContext.Find(this) == null) // Hidden & NOT customizing a house
      {
        if (!Mounted && Skills.Stealth.Value >= 25.0)
        {
          bool running = (d & Direction.Running) != 0;

          if (running)
          {
            if ((AllowedStealthSteps -= 2) <= 0)
              RevealingAction();
          }
          else if (AllowedStealthSteps-- <= 0)
          {
            Stealth.OnUse(this);
          }
        }
        else
        {
          RevealingAction();
        }
      }

      return true;
    }

    public void AutoStablePets()
    {
      if (Core.SE && AllFollowers.Count > 0)
        for (int i = m_AllFollowers.Count - 1; i >= 0; --i)
        {
          if (!(AllFollowers[i] is BaseCreature pet) || pet.ControlMaster == null)
            continue;

          if (pet.Summoned)
          {
            if (pet.Map != Map)
            {
              pet.PlaySound(pet.GetAngerSound());
              Timer.DelayCall(pet.Delete);
            }

            continue;
          }

          if ((pet as IMount)?.Rider != null)
            continue;

          if ((pet is PackLlama || pet is PackHorse || pet is Beetle) && pet.Backpack?.Items.Count > 0)
            continue;

          if (pet is BaseEscortable)
            continue;

          pet.ControlTarget = null;
          pet.ControlOrder = OrderType.Stay;
          pet.Internalize();

          pet.SetControlMaster(null);
          pet.SummonMaster = null;

          pet.IsStabled = true;
          pet.StabledBy = this;

          pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully happy

          Stabled.Add(pet);
          AutoStabled.Add(pet);
        }
    }

    public void ClaimAutoStabledPets()
    {
      if (!Core.SE || AutoStabled.Count <= 0)
        return;

      if (!Alive)
      {
        SendLocalizedMessage(
          1076251); // Your pet was unable to join you while you are a ghost.  Please re-login once you have ressurected to claim your pets.
        return;
      }

      for (int i = AutoStabled.Count - 1; i >= 0; --i)
      {
        if (!(AutoStabled[i] is BaseCreature pet))
          continue;

        if (pet.Deleted)
        {
          pet.IsStabled = false;
          pet.StabledBy = null;

          if (Stabled.Contains(pet))
            Stabled.Remove(pet);

          continue;
        }

        if (Followers + pet.ControlSlots <= FollowersMax)
        {
          pet.SetControlMaster(this);

          if (pet.Summoned)
            pet.SummonMaster = this;

          pet.ControlTarget = this;
          pet.ControlOrder = OrderType.Follow;

          pet.MoveToWorld(Location, Map);

          pet.IsStabled = false;
          pet.StabledBy = null;

          pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully Happy

          if (Stabled.Contains(pet))
            Stabled.Remove(pet);
        }
        else
        {
          SendLocalizedMessage(1049612,
            pet.Name); // ~1_NAME~ remained in the stables because you have too many followers.
        }
      }

      AutoStabled.Clear();
    }

    private class CountAndTimeStamp
    {
      private int m_Count;

      public DateTime TimeStamp { get; private set; }

      public int Count
      {
        get => m_Count;
        set
        {
          m_Count = value;
          TimeStamp = DateTime.UtcNow;
        }
      }
    }

    private class MountBlock
    {
      public readonly Timer m_Timer;
      public readonly BlockMountType m_Type;

      public MountBlock(TimeSpan duration, BlockMountType type, Mobile mobile)
      {
        m_Type = type;

        m_Timer = Timer.DelayCall(duration, RemoveBlock, mobile);
      }

      private void RemoveBlock(Mobile mobile)
      {
        if (mobile is PlayerMobile pm)
          pm.m_MountBlock = null;
      }
    }

    private delegate void ContextCallback();

    private class CallbackEntry : ContextMenuEntry
    {
      private readonly ContextCallback m_Callback;

      public CallbackEntry(int number, ContextCallback callback) : this(number, -1, callback)
      {
      }

      public CallbackEntry(int number, int range, ContextCallback callback) : base(number, range) => m_Callback = callback;

      public override void OnClick()
      {
        m_Callback?.Invoke();
      }
    }

    public List<Mobile> RecentlyReported { get; set; }

    public List<Mobile> AutoStabled { get; private set; }

    public bool NinjaWepCooldown { get; set; }

    public List<Mobile> AllFollowers => m_AllFollowers ?? (m_AllFollowers = new List<Mobile>());

    public RankDefinition GuildRank
    {
      get => AccessLevel >= AccessLevel.GameMaster ? RankDefinition.Leader : m_GuildRank;
      set => m_GuildRank = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int GuildMessageHue { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int AllianceMessageHue { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Profession { get; set; }

    public int StepsTaken { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsStealthing // IsStealthing should be moved to Server.Mobiles
    {
      get;
      set;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IgnoreMobiles // IgnoreMobiles should be moved to Server.Mobiles
    {
      get => m_IgnoreMobiles;
      set
      {
        if (m_IgnoreMobiles != value)
        {
          m_IgnoreMobiles = value;
          Delta(MobileDelta.Flags);
        }
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public NpcGuild NpcGuild { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NpcGuildJoinTime { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NextBODTurnInTime { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime LastOnline { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public long LastMoved => LastMoveTime;

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan NpcGuildGameTime { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ToTItemsTurnedIn { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ToTTotalMonsterFame { get; set; }

    public int ExecutesLightningStrike { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ToothAche
    {
      get => CandyCane.GetToothAche(this);
      set => CandyCane.SetToothAche(this, value);
    }

    public PlayerFlag Flags { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool PagingSquelched
    {
      get => GetFlag(PlayerFlag.PagingSquelched);
      set => SetFlag(PlayerFlag.PagingSquelched, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Glassblowing
    {
      get => GetFlag(PlayerFlag.Glassblowing);
      set => SetFlag(PlayerFlag.Glassblowing, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Masonry
    {
      get => GetFlag(PlayerFlag.Masonry);
      set => SetFlag(PlayerFlag.Masonry, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool SandMining
    {
      get => GetFlag(PlayerFlag.SandMining);
      set => SetFlag(PlayerFlag.SandMining, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool StoneMining
    {
      get => GetFlag(PlayerFlag.StoneMining);
      set => SetFlag(PlayerFlag.StoneMining, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ToggleMiningStone
    {
      get => GetFlag(PlayerFlag.ToggleMiningStone);
      set => SetFlag(PlayerFlag.ToggleMiningStone, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool KarmaLocked
    {
      get => GetFlag(PlayerFlag.KarmaLocked);
      set => SetFlag(PlayerFlag.KarmaLocked, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool AutoRenewInsurance
    {
      get => GetFlag(PlayerFlag.AutoRenewInsurance);
      set => SetFlag(PlayerFlag.AutoRenewInsurance, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool UseOwnFilter
    {
      get => GetFlag(PlayerFlag.UseOwnFilter);
      set => SetFlag(PlayerFlag.UseOwnFilter, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool PublicMyRunUO
    {
      get => GetFlag(PlayerFlag.PublicMyRunUO);
      set => SetFlag(PlayerFlag.PublicMyRunUO, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool AcceptGuildInvites
    {
      get => GetFlag(PlayerFlag.AcceptGuildInvites);
      set => SetFlag(PlayerFlag.AcceptGuildInvites, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool HasStatReward
    {
      get => GetFlag(PlayerFlag.HasStatReward);
      set => SetFlag(PlayerFlag.HasStatReward, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool RefuseTrades
    {
      get => GetFlag(PlayerFlag.RefuseTrades);
      set => SetFlag(PlayerFlag.RefuseTrades, value);
    }

    public Dictionary<Type, int> RecoverableAmmo { get; } = new Dictionary<Type, int>();

    public void RecoverAmmo()
    {
      if (!Core.SE || !Alive)
        return;

      foreach (KeyValuePair<Type, int> kvp in RecoverableAmmo)
        if (kvp.Value > 0)
        {
          Item ammo = null;

          try
          {
            ammo = ActivatorUtil.CreateInstance(kvp.Key) as Item;
          }
          catch
          {
            // ignored
          }

          if (ammo == null)
            continue;

          ammo.Amount = kvp.Value;

          string name = ammo.Name ?? ammo switch
          {
            Arrow _ => $"arrow{(ammo.Amount != 1 ? "s" : "")}",
            Bolt _ => $"bolt{(ammo.Amount != 1 ? "s" : "")}",
            _ => $"#{ammo.LabelNumber}"
          };

          PlaceInBackpack(ammo);
          SendLocalizedMessage(1073504, $"{ammo.Amount}\t{name}"); // You recover ~1_NUM~ ~2_AMMO~.
        }

      RecoverableAmmo.Clear();
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime AcceleratedStart { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public SkillName AcceleratedSkill { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public override int HitsMax
    {
      get
      {
        int strBase;
        int strOffs = GetStatOffset(StatType.Str);

        if (Core.AOS)
        {
          strBase = Str; // this.Str already includes GetStatOffset/str
          strOffs = AosAttributes.GetValue(this, AosAttribute.BonusHits);

          if (Core.ML && strOffs > 25 && AccessLevel <= AccessLevel.Player)
            strOffs = 25;

          if (AnimalForm.UnderTransformation(this, typeof(BakeKitsune)) ||
              AnimalForm.UnderTransformation(this, typeof(GreyWolf)))
            strOffs += 20;
        }
        else
        {
          strBase = RawStr;
        }

        return strBase / 2 + 50 + strOffs;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public override int StamMax => base.StamMax + AosAttributes.GetValue(this, AosAttribute.BonusStam);

    [CommandProperty(AccessLevel.GameMaster)]
    public override int ManaMax => base.ManaMax + AosAttributes.GetValue(this, AosAttribute.BonusMana) +
                                   (Core.ML && Race == Race.Elf ? 20 : 0);

    [CommandProperty(AccessLevel.GameMaster)]
    public override int Str
    {
      get
      {
        if (Core.ML && AccessLevel == AccessLevel.Player)
          return Math.Min(base.Str, 150);

        return base.Str;
      }
      set => base.Str = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public override int Int
    {
      get
      {
        if (Core.ML && AccessLevel == AccessLevel.Player)
          return Math.Min(base.Int, 150);

        return base.Int;
      }
      set => base.Int = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public override int Dex
    {
      get
      {
        if (Core.ML && AccessLevel == AccessLevel.Player)
          return Math.Min(base.Dex, 150);

        return base.Dex;
      }
      set => base.Dex = value;
    }

    private static int GetInsuranceCost(Item item) => 600;

    private void ToggleItemInsurance()
    {
      if (!CheckAlive())
        return;

      BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);
      SendLocalizedMessage(1060868); // Target the item you wish to toggle insurance status on <ESC> to cancel
    }

    private bool CanInsure(Item item)
    {
      if ((item is Container && !(item is BaseQuiver)) || item is BagOfSending || item is KeyRing || item is PotionKeg ||
          item is Sigil)
        return false;

      if (item.Stackable)
        return false;

      if (item.LootType == LootType.Cursed)
        return false;

      if (item.ItemID == 0x204E) // death shroud
        return false;

      if (item.Layer == Layer.Mount)
        return false;

      if (item.LootType == LootType.Blessed || item.LootType == LootType.Newbied || item.BlessedFor == this)
        return false;

      return true;
    }

    private void ToggleItemInsurance_Callback(Mobile from, object obj)
    {
      if (!CheckAlive())
        return;

      ToggleItemInsurance_Callback(from, obj as Item, true);
    }

    private void ToggleItemInsurance_Callback(Mobile from, Item item, bool target)
    {
      if (item?.IsChildOf(this) != true)
      {
        if (target)
          BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);

        SendLocalizedMessage(1060871, "",
          0x23); // You can only insure items that you have equipped or that are in your backpack
      }
      else if (item.Insured)
      {
        item.Insured = false;

        SendLocalizedMessage(1060874, "", 0x35); // You cancel the insurance on the item

        if (target)
        {
          BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);
          SendLocalizedMessage(1060868, "",
            0x23); // Target the item you wish to toggle insurance status on <ESC> to cancel
        }
      }
      else if (!CanInsure(item))
      {
        if (target)
          BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);

        SendLocalizedMessage(1060869, "", 0x23); // You cannot insure that
      }
      else
      {
        if (!item.PaidInsurance)
        {
          int cost = GetInsuranceCost(item);

          if (Banker.Withdraw(from, cost))
          {
            SendLocalizedMessage(1060398,
              cost.ToString()); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
            item.PaidInsurance = true;
          }
          else
          {
            SendLocalizedMessage(1061079, "", 0x23); // You lack the funds to purchase the insurance
            return;
          }
        }

        item.Insured = true;

        SendLocalizedMessage(1060873, "", 0x23); // You have insured the item

        if (target)
        {
          BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance_Callback);
          SendLocalizedMessage(1060868, "",
            0x23); // Target the item you wish to toggle insurance status on <ESC> to cancel
        }
      }
    }

    private void AutoRenewInventoryInsurance()
    {
      if (!CheckAlive())
        return;

      SendLocalizedMessage(1060881, "",
        0x23); // You have selected to automatically reinsure all insured items upon death
      AutoRenewInsurance = true;
    }

    private void CancelRenewInventoryInsurance()
    {
      if (!CheckAlive())
        return;

      if (Core.SE)
      {
        if (!HasGump<CancelRenewInventoryInsuranceGump>())
          SendGump(new CancelRenewInventoryInsuranceGump(this, null));
      }
      else
      {
        SendLocalizedMessage(1061075, "",
          0x23); // You have cancelled automatically reinsuring all insured items upon death
        AutoRenewInsurance = false;
      }
    }

    private class CancelRenewInventoryInsuranceGump : Gump
    {
      private readonly ItemInsuranceMenuGump m_InsuranceGump;
      private readonly PlayerMobile m_Player;

      public CancelRenewInventoryInsuranceGump(PlayerMobile player, ItemInsuranceMenuGump insuranceGump) : base(250,
        200)
      {
        m_Player = player;
        m_InsuranceGump = insuranceGump;

        AddBackground(0, 0, 240, 142, 0x13BE);
        AddImageTiled(6, 6, 228, 100, 0xA40);
        AddImageTiled(6, 116, 228, 20, 0xA40);
        AddAlphaRegion(6, 6, 228, 142);

        AddHtmlLocalized(8, 8, 228, 100, 1071021, 0x7FFF); // You are about to disable inventory insurance auto-renewal.

        AddButton(6, 116, 0xFB1, 0xFB2, 0);
        AddHtmlLocalized(40, 118, 450, 20, 1060051, 0x7FFF); // CANCEL

        AddButton(114, 116, 0xFA5, 0xFA7, 1);
        AddHtmlLocalized(148, 118, 450, 20, 1071022, 0x7FFF); // DISABLE IT!
      }

      public override void OnResponse(NetState sender, RelayInfo info)
      {
        if (!m_Player.CheckAlive())
          return;

        if (info.ButtonID == 1)
        {
          m_Player.SendLocalizedMessage(1061075, "",
            0x23); // You have cancelled automatically reinsuring all insured items upon death
          m_Player.AutoRenewInsurance = false;
        }
        else
        {
          m_Player.SendLocalizedMessage(1042021); // Cancelled.
        }

        if (m_InsuranceGump != null)
          m_Player.SendGump(m_InsuranceGump.NewInstance());
      }
    }

    private void OpenItemInsuranceMenu()
    {
      if (!CheckAlive())
        return;

      List<Item> items = new List<Item>();

      foreach (Item item in Items)
        if (DisplayInItemInsuranceGump(item))
          items.Add(item);

      Container pack = Backpack;

      if (pack != null)
        items.AddRange(pack.FindItemsByType<Item>(DisplayInItemInsuranceGump));

      // TODO: Investigate item sorting

      CloseGump<ItemInsuranceMenuGump>();

      if (items.Count == 0)
        SendLocalizedMessage(1114915, "", 0x35); // None of your current items meet the requirements for insurance.
      else
        SendGump(new ItemInsuranceMenuGump(this, items.ToArray()));
    }

    private bool DisplayInItemInsuranceGump(Item item) => (item.Visible || AccessLevel >= AccessLevel.GameMaster) && (item.Insured || CanInsure(item));

    private class ItemInsuranceMenuGump : Gump
    {
      private readonly PlayerMobile m_From;
      private readonly bool[] m_Insure;
      private readonly Item[] m_Items;
      private readonly int m_Page;

      public ItemInsuranceMenuGump(PlayerMobile from, Item[] items, bool[] insure = null, int page = 0)
        : base(25, 50)
      {
        m_From = from;
        m_Items = items;

        if (insure == null)
        {
          insure = new bool[items.Length];

          for (int i = 0; i < items.Length; ++i)
            insure[i] = items[i].Insured;
        }

        m_Insure = insure;
        m_Page = page;

        AddPage(0);

        AddBackground(0, 0, 520, 510, 0x13BE);
        AddImageTiled(10, 10, 500, 30, 0xA40);
        AddImageTiled(10, 50, 500, 355, 0xA40);
        AddImageTiled(10, 415, 500, 80, 0xA40);
        AddAlphaRegion(10, 10, 500, 485);

        AddButton(15, 470, 0xFB1, 0xFB2, 0);
        AddHtmlLocalized(50, 472, 80, 20, 1011012, 0x7FFF); // CANCEL

        if (from.AutoRenewInsurance)
          AddButton(360, 10, 9723, 9724, 1);
        else
          AddButton(360, 10, 9720, 9722, 1);

        AddHtmlLocalized(395, 14, 105, 20, 1114122, 0x7FFF); // AUTO REINSURE

        AddButton(395, 470, 0xFA5, 0xFA6, 2);
        AddHtmlLocalized(430, 472, 50, 20, 1006044, 0x7FFF); // OK

        AddHtmlLocalized(10, 14, 150, 20, 1114121, 0x7FFF); // <CENTER>ITEM INSURANCE MENU</CENTER>

        AddHtmlLocalized(45, 54, 70, 20, 1062214, 0x7FFF); // Item
        AddHtmlLocalized(250, 54, 70, 20, 1061038, 0x7FFF); // Cost
        AddHtmlLocalized(400, 54, 70, 20, 1114311, 0x7FFF); // Insured

        int balance = Banker.GetBalance(from);
        int cost = 0;

        for (int i = 0; i < items.Length; ++i)
          if (insure[i])
            cost += GetInsuranceCost(items[i]);

        AddHtmlLocalized(15, 420, 300, 20, 1114310, 0x7FFF); // GOLD AVAILABLE:
        AddLabel(215, 420, 0x481, balance.ToString());
        AddHtmlLocalized(15, 435, 300, 20, 1114123, 0x7FFF); // TOTAL COST OF INSURANCE:
        AddLabel(215, 435, 0x481, cost.ToString());

        if (cost != 0)
        {
          AddHtmlLocalized(15, 450, 300, 20, 1114125, 0x7FFF); // NUMBER OF DEATHS PAYABLE:
          AddLabel(215, 450, 0x481, (balance / cost).ToString());
        }

        for (int i = page * 4, y = 72; i < (page + 1) * 4 && i < items.Length; ++i, y += 75)
        {
          Item item = items[i];
          Rectangle2D b = ItemBounds.Table[item.ItemID];

          AddImageTiledButton(40, y, 0x918, 0x918, 0, GumpButtonType.Page, 0, item.ItemID, item.Hue,
            40 - b.Width / 2 - b.X, 30 - b.Height / 2 - b.Y);
          AddItemProperty(item.Serial);

          if (insure[i])
          {
            AddButton(400, y, 9723, 9724, 100 + i);
            AddLabel(250, y, 0x481, GetInsuranceCost(item).ToString());
          }
          else
          {
            AddButton(400, y, 9720, 9722, 100 + i);
            AddLabel(250, y, 0x66C, GetInsuranceCost(item).ToString());
          }
        }

        if (page >= 1)
        {
          AddButton(15, 380, 0xFAE, 0xFAF, 3);
          AddHtmlLocalized(50, 380, 450, 20, 1044044, 0x7FFF); // PREV PAGE
        }

        if ((page + 1) * 4 < items.Length)
        {
          AddButton(400, 380, 0xFA5, 0xFA7, 4);
          AddHtmlLocalized(435, 380, 70, 20, 1044045, 0x7FFF); // NEXT PAGE
        }
      }

      public ItemInsuranceMenuGump NewInstance() => new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page);

      public override void OnResponse(NetState sender, RelayInfo info)
      {
        if (info.ButtonID == 0 || !m_From.CheckAlive())
          return;

        switch (info.ButtonID)
        {
          case 1: // Auto Reinsure
            {
              if (m_From.AutoRenewInsurance)
              {
                if (!m_From.HasGump<CancelRenewInventoryInsuranceGump>())
                  m_From.SendGump(new CancelRenewInventoryInsuranceGump(m_From, this));
              }
              else
              {
                m_From.AutoRenewInventoryInsurance();
                m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page));
              }

              break;
            }
          case 2: // OK
            {
              m_From.SendGump(new ItemInsuranceMenuConfirmGump(m_From, m_Items, m_Insure, m_Page));

              break;
            }
          case 3: // Prev
            {
              if (m_Page >= 1)
                m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page - 1));

              break;
            }
          case 4: // Next
            {
              if ((m_Page + 1) * 4 < m_Items.Length)
                m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page + 1));

              break;
            }
          default:
            {
              int idx = info.ButtonID - 100;

              if (idx >= 0 && idx < m_Items.Length)
                m_Insure[idx] = !m_Insure[idx];

              m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page));

              break;
            }
        }
      }
    }

    private class ItemInsuranceMenuConfirmGump : Gump
    {
      private readonly PlayerMobile m_From;
      private readonly bool[] m_Insure;
      private readonly Item[] m_Items;
      private readonly int m_Page;

      public ItemInsuranceMenuConfirmGump(PlayerMobile from, Item[] items, bool[] insure, int page)
        : base(250, 200)
      {
        m_From = from;
        m_Items = items;
        m_Insure = insure;
        m_Page = page;

        AddBackground(0, 0, 240, 142, 0x13BE);
        AddImageTiled(6, 6, 228, 100, 0xA40);
        AddImageTiled(6, 116, 228, 20, 0xA40);
        AddAlphaRegion(6, 6, 228, 142);

        AddHtmlLocalized(8, 8, 228, 100, 1114300, 0x7FFF); // Do you wish to insure all newly selected items?

        AddButton(6, 116, 0xFB1, 0xFB2, 0);
        AddHtmlLocalized(40, 118, 450, 20, 1060051, 0x7FFF); // CANCEL

        AddButton(114, 116, 0xFA5, 0xFA7, 1);
        AddHtmlLocalized(148, 118, 450, 20, 1073996, 0x7FFF); // ACCEPT
      }

      public override void OnResponse(NetState sender, RelayInfo info)
      {
        if (!m_From.CheckAlive())
          return;

        if (info.ButtonID == 1)
        {
          for (int i = 0; i < m_Items.Length; ++i)
          {
            Item item = m_Items[i];

            if (item.Insured != m_Insure[i])
              m_From.ToggleItemInsurance_Callback(m_From, item, false);
          }
        }
        else
        {
          m_From.SendLocalizedMessage(1042021); // Cancelled.
          m_From.SendGump(new ItemInsuranceMenuGump(m_From, m_Items, m_Insure, m_Page));
        }
      }
    }

    private void ToggleQuestItem()
    {
      if (!CheckAlive())
        return;

      ToggleQuestItemTarget();
    }

    private void ToggleQuestItemTarget()
    {
      BaseQuestGump.CloseOtherGumps(this);
      CloseGump<QuestLogDetailedGump>();
      CloseGump<QuestLogGump>();
      CloseGump<QuestOfferGump>();
      // CloseGump( typeof( UnknownGump802 ) );
      // CloseGump( typeof( UnknownGump804 ) );

      BeginTarget(-1, false, TargetFlags.None, ToggleQuestItem_Callback);
      SendLocalizedMessage(1072352); // Target the item you wish to toggle Quest Item status on <ESC> to cancel
    }

    private void ToggleQuestItem_Callback(Mobile from, object obj)
    {
      if (!CheckAlive())
        return;

      if (!(obj is Item item))
        return;

      if (from.Backpack == null || item.Parent != from.Backpack)
      {
        SendLocalizedMessage(
          1074769); // An item must be in your backpack (and not in a container within) to be toggled as a quest item.
      }
      else if (item.QuestItem)
      {
        item.QuestItem = false;
        SendLocalizedMessage(1072354); // You remove Quest Item status from the item
      }
      else if (MLQuestSystem.MarkQuestItem(this, item))
      {
        SendLocalizedMessage(1072353); // You set the item to Quest Item status
      }
      else
      {
        SendLocalizedMessage(1072355, "", 0x23); // That item does not match any of your quest criteria
      }

      ToggleQuestItemTarget();
    }

    private DateTime[] m_StuckMenuUses;

    public bool CanUseStuckMenu()
    {
      if (m_StuckMenuUses == null) return true;

      for (int i = 0; i < m_StuckMenuUses.Length; ++i)
        if (DateTime.UtcNow - m_StuckMenuUses[i] > TimeSpan.FromDays(1.0))
          return true;

      return false;
    }

    public void UsedStuckMenu()
    {
      if (m_StuckMenuUses == null) m_StuckMenuUses = new DateTime[2];

      for (int i = 0; i < m_StuckMenuUses.Length; ++i)
        if (DateTime.UtcNow - m_StuckMenuUses[i] > TimeSpan.FromDays(1.0))
        {
          m_StuckMenuUses[i] = DateTime.UtcNow;
          return;
        }
    }

    public override ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
    {
      if (!Alive)
        return ApplyPoisonResult.Immune;

      if (EvilOmenSpell.TryEndEffect(this))
        poison = PoisonImpl.IncreaseLevel(poison);

      ApplyPoisonResult result = base.ApplyPoison(from, poison);

      if (from != null && result == ApplyPoisonResult.Poisoned && PoisonTimer is PoisonImpl.PoisonTimer timer)
        timer.From = from;

      return result;
    }

    public override bool CheckPoisonImmunity(Mobile from, Poison poison)
    {
      if (Young && (DuelContext?.Started != true || DuelContext.Finished))
        return true;

      return base.CheckPoisonImmunity(from, poison);
    }

    public override void OnPoisonImmunity(Mobile from, Poison poison)
    {
      if (Young && (DuelContext?.Started != true || DuelContext.Finished))
        SendLocalizedMessage(
          502808); // You would have been poisoned, were you not new to the land of Britannia. Be careful in the future.
      else
        base.OnPoisonImmunity(from, poison);
    }

    private DuelPlayer m_DuelPlayer;

    public DuelContext DuelContext { get; private set; }

    public DuelPlayer DuelPlayer
    {
      get => m_DuelPlayer;
      set
      {
        bool wasInTourney = DuelContext?.Finished == false && DuelContext.m_Tournament != null;

        m_DuelPlayer = value;

        DuelContext = m_DuelPlayer?.Participant.Context;

        bool isInTourney = DuelContext?.Finished == false && DuelContext.m_Tournament != null;

        if (wasInTourney != isInTourney)
          SendEverything();
      }
    }

    public QuestSystem Quest { get; set; }

    public List<QuestRestartInfo> DoneQuests { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public SolenFriendship SolenFriendship { get; set; }

    public bool ChangedMyRunUO { get; set; }

    public override void OnKillsChange(int oldValue)
    {
      if (Young && Kills > oldValue) ((Account)Account)?.RemoveYoungStatus(0);
    }

    public override void OnGenderChanged(bool oldFemale)
    {
    }

    public override void OnGuildChange(BaseGuild oldGuild)
    {
    }

    public override void OnGuildTitleChange(string oldTitle)
    {
    }

    public override void OnKarmaChange(int oldValue)
    {
    }

    public override void OnFameChange(int oldValue)
    {
    }

    public override void OnSkillChange(SkillName skill, double oldBase)
    {
      if (Young && SkillsTotal >= 4500)
        ((Account)Account)
          ?.RemoveYoungStatus(
            1019036); // You have successfully obtained a respectable skill level, and have outgrown your status as a young player!

      if (MLQuestSystem.Enabled)
        MLQuestSystem.HandleSkillGain(this, skill);
    }

    public override void OnAccessLevelChanged(AccessLevel oldLevel)
    {
      IgnoreMobiles = AccessLevel != AccessLevel.Player;
    }

    public override void OnRawStatChange(StatType stat, int oldValue)
    {
    }

    public override void OnDelete()
    {
      ReceivedHonorContext?.Cancel();
      SentHonorContext?.Cancel();
    }

    private static readonly bool FastwalkPrevention = true; // Is fastwalk prevention enabled?
    private static readonly int FastwalkThreshold = 400; // Fastwalk prevention will become active after 0.4 seconds

    private long m_NextMovementTime;
    private bool m_HasMoved;

    public virtual bool UsesFastwalkPrevention => AccessLevel < AccessLevel.Counselor;

    public override int ComputeMovementSpeed(Direction dir, bool checkTurning)
    {
      if (checkTurning && (dir & Direction.Mask) != (Direction & Direction.Mask))
        return RunMount; // We are NOT actually moving (just a direction change)

      TransformContext context = TransformationSpellHelper.GetContext(this);

      if (context?.Type == typeof(ReaperFormSpell))
        return WalkFoot;

      bool running = (dir & Direction.Running) != 0;

      bool onHorse = Mount != null;

      AnimalFormContext animalContext = AnimalForm.GetContext(this);

      if (onHorse || animalContext?.SpeedBoost == true)
        return running ? RunMount : WalkMount;

      return running ? RunFoot : WalkFoot;
    }

    public static TimeSpan MovementThrottle_Callback(NetState ns)
    {
      if (!(ns.Mobile is PlayerMobile pm) || !pm.UsesFastwalkPrevention)
        return TimeSpan.Zero;

      if (!pm.m_HasMoved)
      {
        // has not yet moved
        pm.m_NextMovementTime = Core.TickCount;
        pm.m_HasMoved = true;
        return TimeSpan.Zero;
      }

      long ts = pm.m_NextMovementTime - Core.TickCount;

      if (ts < 0)
      {
        // been a while since we've last moved
        pm.m_NextMovementTime = Core.TickCount;
        return TimeSpan.Zero;
      }

      return ts < FastwalkThreshold ? TimeSpan.Zero : TimeSpan.FromTicks(ts);
    }

    private Type m_EnemyOfOneType;

    public Type EnemyOfOneType
    {
      get => m_EnemyOfOneType;
      set
      {
        Type oldType = m_EnemyOfOneType;
        Type newType = value;

        if (oldType == newType)
          return;

        m_EnemyOfOneType = value;

        DeltaEnemies(oldType, newType);
      }
    }

    public bool WaitingForEnemy { get; set; }

    private void DeltaEnemies(Type oldType, Type newType)
    {
      foreach (Mobile m in GetMobilesInRange(18))
      {
        Type t = m.GetType();

        if (t == oldType || t == newType)
        {
          NetState ns = NetState;

          if (ns != null)
          {
            if (ns.StygianAbyss)
              ns.Send(new MobileMoving(m, Notoriety.Compute(this, m)));
            else
              ns.Send(new MobileMovingOld(m, Notoriety.Compute(this, m)));
          }
        }
      }
    }

    private int m_HairModID = -1, m_HairModHue;
    private int m_BeardModID = -1, m_BeardModHue;

    public void SetHairMods(int hairID, int beardID)
    {
      if (hairID == -1)
        InternalRestoreHair(true, ref m_HairModID, ref m_HairModHue);
      else if (hairID != -2)
        InternalChangeHair(true, hairID, ref m_HairModID, ref m_HairModHue);

      if (beardID == -1)
        InternalRestoreHair(false, ref m_BeardModID, ref m_BeardModHue);
      else if (beardID != -2)
        InternalChangeHair(false, beardID, ref m_BeardModID, ref m_BeardModHue);
    }

    private void CreateHair(bool hair, int id, int hue)
    {
      if (hair)
      {
        // TODO Verification?
        HairItemID = id;
        HairHue = hue;
      }
      else
      {
        FacialHairItemID = id;
        FacialHairHue = hue;
      }
    }

    private void InternalRestoreHair(bool hair, ref int id, ref int hue)
    {
      if (id == -1)
        return;

      if (hair)
        HairItemID = 0;
      else
        FacialHairItemID = 0;

      // if (id != 0)
      CreateHair(hair, id, hue);

      id = -1;
      hue = 0;
    }

    private void InternalChangeHair(bool hair, int id, ref int storeID, ref int storeHue)
    {
      if (storeID == -1)
      {
        storeID = hair ? HairItemID : FacialHairItemID;
        storeHue = hair ? HairHue : FacialHairHue;
      }

      CreateHair(hair, id, 0);
    }

    public DateTime LastSacrificeGain { get; set; }

    public DateTime LastSacrificeLoss { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int AvailableResurrects { get; set; }

    private DateTime m_NextJustAward;

    public DateTime LastJusticeLoss { get; set; }

    public List<Mobile> JusticeProtectors { get; set; }

    public DateTime LastCompassionLoss { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NextCompassionDay { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int CompassionGains { get; set; }

    public DateTime LastValorLoss { get; set; }

    public DateTime m_hontime;

    public DateTime LastHonorLoss { get; set; }

    public DateTime LastHonorUse { get; set; }

    public bool HonorActive { get; set; }

    public HonorContext ReceivedHonorContext { get; set; }

    public HonorContext SentHonorContext { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Young
    {
      get => GetFlag(PlayerFlag.Young);
      set
      {
        SetFlag(PlayerFlag.Young, value);
        InvalidateProperties();
      }
    }

    public override string ApplyNameSuffix(string suffix)
    {
      if (Young)
      {
        if (suffix.Length == 0)
          suffix = "(Young)";
        else
          suffix = $"{suffix} (Young)";
      }

      if (EthicPlayer != null)
      {
        if (suffix.Length == 0)
          suffix = EthicPlayer.Ethic.Definition.Adjunct.String;
        else
          suffix = $"{suffix} {EthicPlayer.Ethic.Definition.Adjunct.String}";
      }

      if (Core.ML && Map == Faction.Facet)
      {
        Faction faction = Faction.Find(this);

        if (faction != null)
        {
          string adjunct = $"[{faction.Definition.Abbreviation}]";
          if (suffix.Length == 0)
            suffix = adjunct;
          else
            suffix = $"{suffix} {adjunct}";
        }
      }

      return base.ApplyNameSuffix(suffix);
    }

    public override TimeSpan GetLogoutDelay()
    {
      if (Young || BedrollLogout || TestCenter.Enabled)
        return TimeSpan.Zero;

      return base.GetLogoutDelay();
    }

    private DateTime m_LastYoungMessage = DateTime.MinValue;

    public bool CheckYoungProtection(Mobile from)
    {
      if (!Young)
        return false;

      if (Region is BaseRegion region && !region.YoungProtected)
        return false;

      if (from is BaseCreature creature && creature.IgnoreYoungProtection)
        return false;

      if (Quest?.IgnoreYoungProtection(from) == true)
        return false;

      if (DateTime.UtcNow - m_LastYoungMessage > TimeSpan.FromMinutes(1.0))
      {
        m_LastYoungMessage = DateTime.UtcNow;
        SendLocalizedMessage(
          1019067); // A monster looks at you menacingly but does not attack.  You would be under attack now if not for your status as a new citizen of Britannia.
      }

      return true;
    }

    private DateTime m_LastYoungHeal = DateTime.MinValue;

    public bool CheckYoungHealTime()
    {
      if (DateTime.UtcNow - m_LastYoungHeal > TimeSpan.FromMinutes(5.0))
      {
        m_LastYoungHeal = DateTime.UtcNow;
        return true;
      }

      return false;
    }

    private static readonly Point3D[] m_TrammelDeathDestinations =
    {
      new Point3D(1481, 1612, 20),
      new Point3D(2708, 2153, 0),
      new Point3D(2249, 1230, 0),
      new Point3D(5197, 3994, 37),
      new Point3D(1412, 3793, 0),
      new Point3D(3688, 2232, 20),
      new Point3D(2578, 604, 0),
      new Point3D(4397, 1089, 0),
      new Point3D(5741, 3218, -2),
      new Point3D(2996, 3441, 15),
      new Point3D(624, 2225, 0),
      new Point3D(1916, 2814, 0),
      new Point3D(2929, 854, 0),
      new Point3D(545, 967, 0),
      new Point3D(3665, 2587, 0)
    };

    private static readonly Point3D[] m_IlshenarDeathDestinations =
    {
      new Point3D(1216, 468, -13),
      new Point3D(723, 1367, -60),
      new Point3D(745, 725, -28),
      new Point3D(281, 1017, 0),
      new Point3D(986, 1011, -32),
      new Point3D(1175, 1287, -30),
      new Point3D(1533, 1341, -3),
      new Point3D(529, 217, -44),
      new Point3D(1722, 219, 96)
    };

    private static readonly Point3D[] m_MalasDeathDestinations =
    {
      new Point3D(2079, 1376, -70),
      new Point3D(944, 519, -71)
    };

    private static readonly Point3D[] m_TokunoDeathDestinations =
    {
      new Point3D(1166, 801, 27),
      new Point3D(782, 1228, 25),
      new Point3D(268, 624, 15)
    };

    public bool YoungDeathTeleport()
    {
      if (Region.IsPartOf<JailRegion>()
          || Region.IsPartOf("Samurai start location")
          || Region.IsPartOf("Ninja start location")
          || Region.IsPartOf("Ninja cave"))
        return false;

      Point3D loc;
      Map map;

      DungeonRegion dungeon = Region.GetRegion<DungeonRegion>();
      if (dungeon != null && dungeon.EntranceLocation != Point3D.Zero)
      {
        loc = dungeon.EntranceLocation;
        map = dungeon.EntranceMap;
      }
      else
      {
        loc = Location;
        map = Map;
      }

      Point3D[] list;

      if (map == Map.Trammel)
        list = m_TrammelDeathDestinations;
      else if (map == Map.Ilshenar)
        list = m_IlshenarDeathDestinations;
      else if (map == Map.Malas)
        list = m_MalasDeathDestinations;
      else if (map == Map.Tokuno)
        list = m_TokunoDeathDestinations;
      else
        return false;

      Point3D dest = Point3D.Zero;
      int sqDistance = int.MaxValue;

      for (int i = 0; i < list.Length; i++)
      {
        Point3D curDest = list[i];

        int width = loc.X - curDest.X;
        int height = loc.Y - curDest.Y;
        int curSqDistance = width * width + height * height;

        if (curSqDistance < sqDistance)
        {
          dest = curDest;
          sqDistance = curSqDistance;
        }
      }

      MoveToWorld(dest, map);
      return true;
    }

    private void SendYoungDeathNotice()
    {
      SendGump(new YoungDeathNotice());
    }

    public SpeechLog SpeechLog { get; private set; }

    public override void OnSpeech(SpeechEventArgs e)
    {
      if (SpeechLog.Enabled && NetState != null)
      {
        if (SpeechLog == null)
          SpeechLog = new SpeechLog();

        SpeechLog.Add(e.Mobile, e.Speech);
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool DisplayChampionTitle
    {
      get => GetFlag(PlayerFlag.DisplayChampionTitle);
      set => SetFlag(PlayerFlag.DisplayChampionTitle, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public ChampionTitleInfo ChampionTitles { get; private set; }

    private void ToggleChampionTitleDisplay()
    {
      if (!CheckAlive())
        return;

      if (DisplayChampionTitle)
        SendLocalizedMessage(1062419, "", 0x23); // You have chosen to hide your monster kill title.
      else
        SendLocalizedMessage(1062418, "", 0x23); // You have chosen to display your monster kill title.

      DisplayChampionTitle = !DisplayChampionTitle;
    }

    [PropertyObject]
    public class ChampionTitleInfo
    {
      public const int LossAmount = 90;
      public static TimeSpan LossDelay = TimeSpan.FromDays(1.0);

      private TitleInfo[] m_Values;

      public ChampionTitleInfo()
      {
      }

      public ChampionTitleInfo(IGenericReader reader)
      {
        int version = reader.ReadEncodedInt();

        switch (version)
        {
          case 0:
            {
              Harrower = reader.ReadEncodedInt();

              int length = reader.ReadEncodedInt();
              m_Values = new TitleInfo[length];

              for (int i = 0; i < length; i++) m_Values[i] = new TitleInfo(reader);

              if (m_Values.Length != ChampionSpawnInfo.Table.Length)
              {
                TitleInfo[] oldValues = m_Values;
                m_Values = new TitleInfo[ChampionSpawnInfo.Table.Length];

                for (int i = 0; i < m_Values.Length && i < oldValues.Length; i++) m_Values[i] = oldValues[i];
              }

              break;
            }
        }
      }

      [CommandProperty(AccessLevel.GameMaster)]
      public int Pestilence
      {
        get => GetValue(ChampionSpawnType.Pestilence);
        set => SetValue(ChampionSpawnType.Pestilence, value);
      }

      [CommandProperty(AccessLevel.GameMaster)]
      public int Abyss
      {
        get => GetValue(ChampionSpawnType.Abyss);
        set => SetValue(ChampionSpawnType.Abyss, value);
      }

      [CommandProperty(AccessLevel.GameMaster)]
      public int Arachnid
      {
        get => GetValue(ChampionSpawnType.Arachnid);
        set => SetValue(ChampionSpawnType.Arachnid, value);
      }

      [CommandProperty(AccessLevel.GameMaster)]
      public int ColdBlood
      {
        get => GetValue(ChampionSpawnType.ColdBlood);
        set => SetValue(ChampionSpawnType.ColdBlood, value);
      }

      [CommandProperty(AccessLevel.GameMaster)]
      public int ForestLord
      {
        get => GetValue(ChampionSpawnType.ForestLord);
        set => SetValue(ChampionSpawnType.ForestLord, value);
      }

      [CommandProperty(AccessLevel.GameMaster)]
      public int SleepingDragon
      {
        get => GetValue(ChampionSpawnType.SleepingDragon);
        set => SetValue(ChampionSpawnType.SleepingDragon, value);
      }

      [CommandProperty(AccessLevel.GameMaster)]
      public int UnholyTerror
      {
        get => GetValue(ChampionSpawnType.UnholyTerror);
        set => SetValue(ChampionSpawnType.UnholyTerror, value);
      }

      [CommandProperty(AccessLevel.GameMaster)]
      public int VerminHorde
      {
        get => GetValue(ChampionSpawnType.VerminHorde);
        set => SetValue(ChampionSpawnType.VerminHorde, value);
      }

      [CommandProperty(AccessLevel.GameMaster)]
      public int Harrower { get; set; }

      public int GetValue(ChampionSpawnType type) => GetValue((int)type);

      public void SetValue(ChampionSpawnType type, int value)
      {
        SetValue((int)type, value);
      }

      public void Award(ChampionSpawnType type, int value)
      {
        Award((int)type, value);
      }

      public int GetValue(int index)
      {
        if (m_Values == null || index < 0 || index >= m_Values.Length)
          return 0;

        m_Values[index] ??= new TitleInfo();

        return m_Values[index].Value;
      }

      public DateTime GetLastDecay(int index)
      {
        if (m_Values == null || index < 0 || index >= m_Values.Length)
          return DateTime.MinValue;

        m_Values[index] ??= new TitleInfo();

        return m_Values[index].LastDecay;
      }

      public void SetValue(int index, int value)
      {
        m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

        if (index < 0 || index >= m_Values.Length)
          return;

        m_Values[index] ??= new TitleInfo();

        m_Values[index].Value = Math.Max(value, 0);
      }

      public void Award(int index, int value)
      {
        m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

        if (index < 0 || index >= m_Values.Length || value <= 0)
          return;

        m_Values[index] ??= new TitleInfo();

        m_Values[index].Value += value;
      }

      public void Atrophy(int index, int value)
      {
        m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

        if (index < 0 || index >= m_Values.Length || value <= 0)
          return;

        m_Values[index] ??= new TitleInfo();

        int before = m_Values[index].Value;

        m_Values[index].Value -= Math.Min(value, m_Values[index].Value);

        if (before != m_Values[index].Value)
          m_Values[index].LastDecay = DateTime.UtcNow;
      }

      public override string ToString() => "...";

      public static void Serialize(IGenericWriter writer, ChampionTitleInfo titles)
      {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(titles.Harrower);

        int length = titles.m_Values.Length;
        writer.WriteEncodedInt(length);

        for (int i = 0; i < length; i++)
        {
          titles.m_Values[i] ??= new TitleInfo();

          TitleInfo.Serialize(writer, titles.m_Values[i]);
        }
      }

      public static void CheckAtrophy(PlayerMobile pm)
      {
        ChampionTitleInfo t = pm.ChampionTitles;
        if (t == null)
          return;

        t.m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

        for (int i = 0; i < t.m_Values.Length; i++)
          if (t.GetLastDecay(i) + LossDelay < DateTime.UtcNow)
            t.Atrophy(i, LossAmount);
      }

      public static void
        AwardHarrowerTitle(PlayerMobile pm) // Called when killing a harrower.  Will give a minimum of 1 point.
      {
        ChampionTitleInfo t = pm.ChampionTitles;
        if (t == null)
          return;

        t.m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

        int count = 1 + t.m_Values.Count(t1 => t1.Value > 900);

        t.Harrower = Math.Max(count, t.Harrower); // Harrower titles never decay.
      }

      private class TitleInfo
      {
        public TitleInfo()
        {
        }

        public TitleInfo(IGenericReader reader)
        {
          int version = reader.ReadEncodedInt();

          switch (version)
          {
            case 0:
              {
                Value = reader.ReadEncodedInt();
                LastDecay = reader.ReadDateTime();
                break;
              }
          }
        }

        public int Value { get; set; }

        public DateTime LastDecay { get; set; }

        public static void Serialize(IGenericWriter writer, TitleInfo info)
        {
          writer.WriteEncodedInt(0); // version

          writer.WriteEncodedInt(info.Value);
          writer.Write(info.LastDecay);
        }
      }
    }

    private Dictionary<int, bool> m_AcquiredRecipes;

    public virtual bool HasRecipe(Recipe r) => r != null && HasRecipe(r.ID);

    public virtual bool HasRecipe(int recipeID) => m_AcquiredRecipes.TryGetValue(recipeID, out bool value) && value;

    public virtual void AcquireRecipe(Recipe r)
    {
      if (r != null)
        AcquireRecipe(r.ID);
    }

    public virtual void AcquireRecipe(int recipeID)
    {
      m_AcquiredRecipes ??= new Dictionary<int, bool>();

      m_AcquiredRecipes[recipeID] = true;
    }

    public virtual void ResetRecipes()
    {
      m_AcquiredRecipes = null;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int KnownRecipes => m_AcquiredRecipes?.Count ?? 0;

    public void ResendBuffs()
    {
      if (!BuffInfo.Enabled || m_BuffTable == null)
        return;

      if (NetState?.BuffIcon == true)
        foreach (BuffInfo info in m_BuffTable.Values)
          NetState.Send(new AddBuffPacket(this, info));
    }

    private Dictionary<BuffIcon, BuffInfo> m_BuffTable;

    public void AddBuff(BuffInfo b)
    {
      if (!BuffInfo.Enabled || b == null)
        return;

      RemoveBuff(b); // Check & subsequently remove the old one.

      m_BuffTable ??= new Dictionary<BuffIcon, BuffInfo>();

      m_BuffTable.Add(b.ID, b);

      if (NetState?.BuffIcon == true)
        NetState.Send(new AddBuffPacket(this, b));
    }

    public void RemoveBuff(BuffInfo b)
    {
      if (b == null)
        return;

      RemoveBuff(b.ID);
    }

    public void RemoveBuff(BuffIcon b)
    {
      if (m_BuffTable?.ContainsKey(b) != true)
        return;

      BuffInfo info = m_BuffTable[b];

      if (info.Timer?.Running == true)
        info.Timer.Stop();

      m_BuffTable.Remove(b);

      if (NetState?.BuffIcon == true)
        NetState.Send(new RemoveBuffPacket(this, b));

      if (m_BuffTable.Count <= 0)
        m_BuffTable = null;
    }
  }
}
