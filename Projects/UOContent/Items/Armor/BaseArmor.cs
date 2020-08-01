using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Ethics;
using Server.Factions;
using Server.Network;
using Server.Utilities;
using AMA = Server.Items.ArmorMeditationAllowance;
using AMT = Server.Items.ArmorMaterialType;

namespace Server.Items
{
  public abstract class BaseArmor : Item, IScissorable, IFactionItem, ICraftable, IWearableDurability
  {
    // Overridable values. These values are provided to override the defaults which get defined in the individual armor scripts.
    private int m_ArmorBase = -1;
    private Mobile m_Crafter;
    private ArmorDurabilityLevel m_Durability;
    private int m_HitPoints;
    private bool m_Identified;

    /* Armor internals work differently now (Jun 19 2003)
     *
     * The attributes defined below default to -1.
     * If the value is -1, the corresponding virtual 'Aos/Old' property is used.
     * If not, the attribute value itself is used. Here's the list:
     *  - ArmorBase
     *  - StrBonus
     *  - DexBonus
     *  - IntBonus
     *  - StrReq
     *  - DexReq
     *  - IntReq
     *  - MeditationAllowance
     */

    // Instance values. These values must are unique to each armor piece.
    private int m_MaxHitPoints;
    private AMA m_Meditate = (AMA)(-1);
    private int m_PhysicalBonus, m_FireBonus, m_ColdBonus, m_PoisonBonus, m_EnergyBonus;
    private ArmorProtectionLevel m_Protection;
    private ArmorQuality m_Quality;
    private CraftResource m_Resource;
    private int m_StrBonus = -1, m_DexBonus = -1, m_IntBonus = -1;
    private int m_StrReq = -1, m_DexReq = -1, m_IntReq = -1;

    public BaseArmor(Serial serial) : base(serial)
    {
    }

    public BaseArmor(int itemID) : base(itemID)
    {
      m_Quality = ArmorQuality.Regular;
      m_Durability = ArmorDurabilityLevel.Regular;
      m_Crafter = null;

      m_Resource = DefaultResource;
      Hue = CraftResources.GetHue(m_Resource);

      m_HitPoints = m_MaxHitPoints = Utility.RandomMinMax(InitMinHits, InitMaxHits);

      Layer = (Layer)ItemData.Quality;

      Attributes = new AosAttributes(this);
      ArmorAttributes = new AosArmorAttributes(this);
      SkillBonuses = new AosSkillBonuses(this);
    }

    public virtual bool AllowMaleWearer => true;
    public virtual bool AllowFemaleWearer => true;

    public abstract AMT MaterialType { get; }

    public virtual int RevertArmorBase => ArmorBase;
    public virtual int ArmorBase => 0;

    public virtual AMA DefMedAllowance => AMA.None;
    public virtual AMA AosMedAllowance => DefMedAllowance;
    public virtual AMA OldMedAllowance => DefMedAllowance;

    public virtual int AosStrBonus => 0;
    public virtual int AosDexBonus => 0;
    public virtual int AosIntBonus => 0;
    public virtual int AosStrReq => 0;
    public virtual int AosDexReq => 0;
    public virtual int AosIntReq => 0;

    public virtual int OldStrBonus => 0;
    public virtual int OldDexBonus => 0;
    public virtual int OldIntBonus => 0;
    public virtual int OldStrReq => 0;
    public virtual int OldDexReq => 0;
    public virtual int OldIntReq => 0;

    [CommandProperty(AccessLevel.GameMaster)]
    public AMA MeditationAllowance
    {
      get => m_Meditate == (AMA)(-1) ? Core.AOS ? AosMedAllowance : OldMedAllowance : m_Meditate;
      set => m_Meditate = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int BaseArmorRating
    {
      get
      {
        if (m_ArmorBase == -1)
          return ArmorBase;
        return m_ArmorBase;
      }
      set
      {
        m_ArmorBase = value;
        Invalidate();
      }
    }

    public double BaseArmorRatingScaled => BaseArmorRating * ArmorScalar;

    public virtual double ArmorRating
    {
      get
      {
        int ar = BaseArmorRating;

        if (m_Protection != ArmorProtectionLevel.Regular)
          ar += 10 + 5 * (int)m_Protection;

        switch (m_Resource)
        {
          case CraftResource.DullCopper:
            ar += 2;
            break;
          case CraftResource.ShadowIron:
            ar += 4;
            break;
          case CraftResource.Copper:
            ar += 6;
            break;
          case CraftResource.Bronze:
            ar += 8;
            break;
          case CraftResource.Gold:
            ar += 10;
            break;
          case CraftResource.Agapite:
            ar += 12;
            break;
          case CraftResource.Verite:
            ar += 14;
            break;
          case CraftResource.Valorite:
            ar += 16;
            break;
          case CraftResource.SpinedLeather:
            ar += 10;
            break;
          case CraftResource.HornedLeather:
            ar += 13;
            break;
          case CraftResource.BarbedLeather:
            ar += 16;
            break;
        }

        ar += -8 + 8 * (int)m_Quality;
        return ScaleArmorByDurability(ar);
      }
    }

    public double ArmorRatingScaled => ArmorRating * ArmorScalar;

    [CommandProperty(AccessLevel.GameMaster)]
    public int StrBonus
    {
      get => m_StrBonus == -1 ? Core.AOS ? AosStrBonus : OldStrBonus : m_StrBonus;
      set
      {
        m_StrBonus = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int DexBonus
    {
      get => m_DexBonus == -1 ? Core.AOS ? AosDexBonus : OldDexBonus : m_DexBonus;
      set
      {
        m_DexBonus = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int IntBonus
    {
      get => m_IntBonus == -1 ? Core.AOS ? AosIntBonus : OldIntBonus : m_IntBonus;
      set
      {
        m_IntBonus = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int StrRequirement
    {
      get => m_StrReq == -1 ? Core.AOS ? AosStrReq : OldStrReq : m_StrReq;
      set
      {
        m_StrReq = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int DexRequirement
    {
      get => m_DexReq == -1 ? Core.AOS ? AosDexReq : OldDexReq : m_DexReq;
      set
      {
        m_DexReq = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int IntRequirement
    {
      get => m_IntReq == -1 ? Core.AOS ? AosIntReq : OldIntReq : m_IntReq;
      set
      {
        m_IntReq = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Identified
    {
      get => m_Identified;
      set
      {
        m_Identified = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool PlayerConstructed { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public CraftResource Resource
    {
      get => m_Resource;
      set
      {
        if (m_Resource != value)
        {
          UnscaleDurability();

          m_Resource = value;

          if (CraftItem.RetainsColor(GetType())) Hue = CraftResources.GetHue(m_Resource);

          Invalidate();
          InvalidateProperties();

          (Parent as Mobile)?.UpdateResistances();

          ScaleDurability();
        }
      }
    }

    public virtual double ArmorScalar
    {
      get
      {
        int pos = (int)BodyPosition;

        if (pos >= 0 && pos < ArmorScalars.Length)
          return ArmorScalars[pos];

        return 1.0;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile Crafter
    {
      get => m_Crafter;
      set
      {
        m_Crafter = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public ArmorQuality Quality
    {
      get => m_Quality;
      set
      {
        UnscaleDurability();
        m_Quality = value;
        Invalidate();
        InvalidateProperties();
        ScaleDurability();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public ArmorDurabilityLevel Durability
    {
      get => m_Durability;
      set
      {
        UnscaleDurability();
        m_Durability = value;
        ScaleDurability();
        InvalidateProperties();
      }
    }

    public virtual int ArtifactRarity => 0;

    [CommandProperty(AccessLevel.GameMaster)]
    public ArmorProtectionLevel ProtectionLevel
    {
      get => m_Protection;
      set
      {
        if (m_Protection != value)
        {
          m_Protection = value;

          Invalidate();
          InvalidateProperties();

          (Parent as Mobile)?.UpdateResistances();
        }
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public AosAttributes Attributes { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public AosArmorAttributes ArmorAttributes { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public AosSkillBonuses SkillBonuses { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int PhysicalBonus
    {
      get => m_PhysicalBonus;
      set
      {
        m_PhysicalBonus = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int FireBonus
    {
      get => m_FireBonus;
      set
      {
        m_FireBonus = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ColdBonus
    {
      get => m_ColdBonus;
      set
      {
        m_ColdBonus = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int PoisonBonus
    {
      get => m_PoisonBonus;
      set
      {
        m_PoisonBonus = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int EnergyBonus
    {
      get => m_EnergyBonus;
      set
      {
        m_EnergyBonus = value;
        InvalidateProperties();
      }
    }

    public virtual int BasePhysicalResistance => 0;
    public virtual int BaseFireResistance => 0;
    public virtual int BaseColdResistance => 0;
    public virtual int BasePoisonResistance => 0;
    public virtual int BaseEnergyResistance => 0;

    public override int PhysicalResistance => BasePhysicalResistance + GetProtOffset() +
                                              GetResourceAttrs().ArmorPhysicalResist + m_PhysicalBonus;

    public override int FireResistance =>
      BaseFireResistance + GetProtOffset() + GetResourceAttrs().ArmorFireResist + m_FireBonus;

    public override int ColdResistance =>
      BaseColdResistance + GetProtOffset() + GetResourceAttrs().ArmorColdResist + m_ColdBonus;

    public override int PoisonResistance =>
      BasePoisonResistance + GetProtOffset() + GetResourceAttrs().ArmorPoisonResist + m_PoisonBonus;

    public override int EnergyResistance =>
      BaseEnergyResistance + GetProtOffset() + GetResourceAttrs().ArmorEnergyResist + m_EnergyBonus;

    [CommandProperty(AccessLevel.GameMaster)]
    public ArmorBodyType BodyPosition
    {
      get
      {
        return Layer switch
        {
          Layer.Neck => ArmorBodyType.Gorget,
          Layer.TwoHanded => ArmorBodyType.Shield,
          Layer.Gloves => ArmorBodyType.Gloves,
          Layer.Helm => ArmorBodyType.Helmet,
          Layer.Arms => ArmorBodyType.Arms,
          Layer.InnerLegs => ArmorBodyType.Legs,
          Layer.OuterLegs => ArmorBodyType.Legs,
          Layer.Pants => ArmorBodyType.Legs,
          Layer.InnerTorso => ArmorBodyType.Chest,
          Layer.OuterTorso => ArmorBodyType.Chest,
          Layer.Shirt => ArmorBodyType.Chest,
          _ => ArmorBodyType.Gorget
        };
      }
    }

    public static double[] ArmorScalars { get; set; } = { 0.07, 0.07, 0.14, 0.15, 0.22, 0.35 };

    public virtual CraftResource DefaultResource => CraftResource.Iron;

    public virtual Race RequiredRace => null;

    [Hue]
    [CommandProperty(AccessLevel.GameMaster)]
    public override int Hue
    {
      get => base.Hue;
      set
      {
        base.Hue = value;
        InvalidateProperties();
      }
    }

    public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
      CraftItem craftItem, int resHue)
    {
      Quality = (ArmorQuality)quality;

      if (makersMark)
        Crafter = from;

      Type resourceType = typeRes ?? craftItem.Resources[0].ItemType;

      Resource = CraftResources.GetFromType(resourceType);
      PlayerConstructed = true;

      CraftContext context = craftSystem.GetContext(from);

      if (context?.DoNotColor == true)
        Hue = 0;

      if (Quality == ArmorQuality.Exceptional)
      {
        if (!(Core.ML && this is BaseShield)) // Guessed Core.ML removed exceptional resist bonuses from crafted shields
          DistributeBonuses(tool is BaseRunicTool ? 6 :
            Core.SE ? 15 : 14); // Not sure since when, but right now 15 points are added, not 14.

        if (Core.ML && !(this is BaseShield))
        {
          int bonus = (int)(from.Skills.ArmsLore.Value / 20);

          for (int i = 0; i < bonus; i++)
            switch (Utility.Random(5))
            {
              case 0:
                m_PhysicalBonus++;
                break;
              case 1:
                m_FireBonus++;
                break;
              case 2:
                m_ColdBonus++;
                break;
              case 3:
                m_EnergyBonus++;
                break;
              case 4:
                m_PoisonBonus++;
                break;
            }

          from.CheckSkill(SkillName.ArmsLore, 0, 100);
        }
      }

      if (Core.AOS)
        (tool as BaseRunicTool)?.ApplyAttributesTo(this);

      return quality;
    }

    public bool Scissor(Mobile from, Scissors scissors)
    {
      if (!IsChildOf(from.Backpack))
      {
        from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack.
        return false;
      }

      if (Ethic.IsImbued(this))
      {
        from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
        return false;
      }

      CraftSystem system = DefTailoring.CraftSystem;

      CraftItem item = system.CraftItems.SearchFor(GetType());

      if (item?.Resources.Count == 1 && item.Resources[0].Amount >= 2)
        try
        {
          Item res = (Item)ActivatorUtil.CreateInstance(CraftResources.GetInfo(m_Resource).ResourceTypes[0]);

          ScissorHelper(from, res, PlayerConstructed ? item.Resources[0].Amount / 2 : 1);
          return true;
        }
        catch
        {
          // ignored
        }

      from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
      return false;
    }

    public virtual bool CanFortify => true;

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxHitPoints
    {
      get => m_MaxHitPoints;
      set
      {
        m_MaxHitPoints = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int HitPoints
    {
      get => m_HitPoints;
      set
      {
        if (value != m_HitPoints && MaxHitPoints > 0)
        {
          m_HitPoints = value;

          if (m_HitPoints < 0)
            Delete();
          else if (m_HitPoints > MaxHitPoints)
            m_HitPoints = MaxHitPoints;

          InvalidateProperties();
        }
      }
    }

    public virtual int InitMinHits => 0;
    public virtual int InitMaxHits => 0;

    public void UnscaleDurability()
    {
      int scale = 100 + GetDurabilityBonus();

      m_HitPoints = (m_HitPoints * 100 + (scale - 1)) / scale;
      m_MaxHitPoints = (m_MaxHitPoints * 100 + (scale - 1)) / scale;
      InvalidateProperties();
    }

    public void ScaleDurability()
    {
      int scale = 100 + GetDurabilityBonus();

      m_HitPoints = (m_HitPoints * scale + 99) / 100;
      m_MaxHitPoints = (m_MaxHitPoints * scale + 99) / 100;
      InvalidateProperties();
    }

    public virtual int OnHit(BaseWeapon weapon, int damageTaken)
    {
      double halfar = ArmorRating / 2.0;
      int absorbed = (int)(halfar + halfar * Utility.RandomDouble());

      // Don't go below zero
      damageTaken = Math.Min(absorbed, damageTaken);

      if (absorbed < 2)
        absorbed = 2;

      if (Utility.Random(100) < 25) // 25% chance to lower durability
      {
        if (Core.AOS && ArmorAttributes.SelfRepair > Utility.Random(10))
        {
          HitPoints += 2;
        }
        else
        {
          int wear;

          if (weapon.Type == WeaponType.Bashing)
            wear = absorbed / 2;
          else
            wear = Utility.Random(2);

          if (wear > 0 && m_MaxHitPoints > 0)
          {
            if (m_HitPoints >= wear)
            {
              HitPoints -= wear;
              wear = 0;
            }
            else
            {
              wear -= HitPoints;
              HitPoints = 0;
            }

            if (wear > 0)
            {
              if (m_MaxHitPoints > wear)
              {
                MaxHitPoints -= wear;

                if (Parent is Mobile mobile)
                  mobile.LocalOverheadMessage(MessageType.Regular, 0x3B2,
                    1061121); // Your equipment is severely damaged.
              }
              else
              {
                Delete();
              }
            }
          }
        }
      }

      return damageTaken;
    }

    public override void OnAfterDuped(Item newItem)
    {
      if (!(newItem is BaseArmor armor))
        return;

      armor.Attributes = new AosAttributes(newItem, Attributes);
      armor.ArmorAttributes = new AosArmorAttributes(newItem, ArmorAttributes);
      armor.SkillBonuses = new AosSkillBonuses(newItem, SkillBonuses);
    }

    public int ComputeStatReq(StatType type)
    {
      int v;

      if (type == StatType.Str)
        v = StrRequirement;
      else if (type == StatType.Dex)
        v = DexRequirement;
      else
        v = IntRequirement;

      return AOS.Scale(v, 100 - GetLowerStatReq());
    }

    public int ComputeStatBonus(StatType type)
    {
      if (type == StatType.Str)
        return StrBonus + Attributes.BonusStr;
      if (type == StatType.Dex)
        return DexBonus + Attributes.BonusDex;
      return IntBonus + Attributes.BonusInt;
    }

    public void DistributeBonuses(int amount)
    {
      for (int i = 0; i < amount; ++i)
        switch (Utility.Random(5))
        {
          case 0:
            ++m_PhysicalBonus;
            break;
          case 1:
            ++m_FireBonus;
            break;
          case 2:
            ++m_ColdBonus;
            break;
          case 3:
            ++m_PoisonBonus;
            break;
          case 4:
            ++m_EnergyBonus;
            break;
        }

      InvalidateProperties();
    }

    public CraftAttributeInfo GetResourceAttrs()
    {
      CraftResourceInfo info = CraftResources.GetInfo(m_Resource);

      if (info == null)
        return CraftAttributeInfo.Blank;

      return info.AttributeInfo;
    }

    public int GetProtOffset()
    {
      return m_Protection switch
      {
        ArmorProtectionLevel.Guarding => 1,
        ArmorProtectionLevel.Hardening => 2,
        ArmorProtectionLevel.Fortification => 3,
        ArmorProtectionLevel.Invulnerability => 4,
        _ => 0
      };
    }

    public int GetDurabilityBonus()
    {
      int bonus = 0;

      if (m_Quality == ArmorQuality.Exceptional)
        bonus += 20;

      switch (m_Durability)
      {
        case ArmorDurabilityLevel.Durable:
          bonus += 20;
          break;
        case ArmorDurabilityLevel.Substantial:
          bonus += 50;
          break;
        case ArmorDurabilityLevel.Massive:
          bonus += 70;
          break;
        case ArmorDurabilityLevel.Fortified:
          bonus += 100;
          break;
        case ArmorDurabilityLevel.Indestructible:
          bonus += 120;
          break;
      }

      if (Core.AOS)
      {
        bonus += ArmorAttributes.DurabilityBonus;

        CraftResourceInfo resInfo = CraftResources.GetInfo(m_Resource);
        CraftAttributeInfo attrInfo = null;

        if (resInfo != null)
          attrInfo = resInfo.AttributeInfo;

        if (attrInfo != null)
          bonus += attrInfo.ArmorDurability;
      }

      return bonus;
    }

    public static void ValidateMobile(Mobile m)
    {
      for (int i = m.Items.Count - 1; i >= 0; --i)
      {
        if (i >= m.Items.Count)
          continue;

        Item item = m.Items[i];

        if (item is BaseArmor armor)
        {
          if (armor.RequiredRace != null && m.Race != armor.RequiredRace)
          {
            if (armor.RequiredRace == Race.Elf)
              m.SendLocalizedMessage(1072203); // Only Elves may use this.
            else
              m.SendMessage("Only {0} may use this.", armor.RequiredRace.PluralName);

            m.AddToBackpack(armor);
          }
          else if (!armor.AllowMaleWearer && !m.Female && m.AccessLevel < AccessLevel.GameMaster)
          {
            if (armor.AllowFemaleWearer)
              m.SendLocalizedMessage(1010388); // Only females can wear this.
            else
              m.SendMessage("You may not wear this.");

            m.AddToBackpack(armor);
          }
          else if (!armor.AllowFemaleWearer && m.Female && m.AccessLevel < AccessLevel.GameMaster)
          {
            if (armor.AllowMaleWearer)
              m.SendLocalizedMessage(1063343); // Only males can wear this.
            else
              m.SendMessage("You may not wear this.");

            m.AddToBackpack(armor);
          }
        }
      }
    }

    public int GetLowerStatReq()
    {
      if (!Core.AOS)
        return 0;

      int v = ArmorAttributes.LowerStatReq;

      CraftResourceInfo info = CraftResources.GetInfo(m_Resource);

      CraftAttributeInfo attrInfo = info?.AttributeInfo;

      if (attrInfo != null)
        v += attrInfo.ArmorLowerRequirements;

      if (v > 100)
        v = 100;

      return v;
    }

    public override void OnAdded(IEntity parent)
    {
      if (parent is Mobile from)
      {
        if (Core.AOS)
          SkillBonuses.AddTo(from);

        from.Delta(MobileDelta.Armor); // Tell them armor rating has changed
      }
    }

    public virtual double ScaleArmorByDurability(double armor)
    {
      int scale = 100;

      if (m_MaxHitPoints > 0 && m_HitPoints < m_MaxHitPoints)
        scale = 50 + 50 * m_HitPoints / m_MaxHitPoints;

      return armor * scale / 100;
    }

    protected void Invalidate()
    {
      (Parent as Mobile)?.Delta(MobileDelta.Armor); // Tell them armor rating has changed
    }

    private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
    {
      if (setIf)
        flags |= toSet;
    }

    private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet) => (flags & toGet) != 0;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(7); // version

      SaveFlag flags = SaveFlag.None;

      SetSaveFlag(ref flags, SaveFlag.Attributes, !Attributes.IsEmpty);
      SetSaveFlag(ref flags, SaveFlag.ArmorAttributes, !ArmorAttributes.IsEmpty);
      SetSaveFlag(ref flags, SaveFlag.PhysicalBonus, m_PhysicalBonus != 0);
      SetSaveFlag(ref flags, SaveFlag.FireBonus, m_FireBonus != 0);
      SetSaveFlag(ref flags, SaveFlag.ColdBonus, m_ColdBonus != 0);
      SetSaveFlag(ref flags, SaveFlag.PoisonBonus, m_PoisonBonus != 0);
      SetSaveFlag(ref flags, SaveFlag.EnergyBonus, m_EnergyBonus != 0);
      SetSaveFlag(ref flags, SaveFlag.Identified, m_Identified);
      SetSaveFlag(ref flags, SaveFlag.MaxHitPoints, m_MaxHitPoints != 0);
      SetSaveFlag(ref flags, SaveFlag.HitPoints, m_HitPoints != 0);
      SetSaveFlag(ref flags, SaveFlag.Crafter, m_Crafter != null);
      SetSaveFlag(ref flags, SaveFlag.Quality, m_Quality != ArmorQuality.Regular);
      SetSaveFlag(ref flags, SaveFlag.Durability, m_Durability != ArmorDurabilityLevel.Regular);
      SetSaveFlag(ref flags, SaveFlag.Protection, m_Protection != ArmorProtectionLevel.Regular);
      SetSaveFlag(ref flags, SaveFlag.Resource, m_Resource != DefaultResource);
      SetSaveFlag(ref flags, SaveFlag.BaseArmor, m_ArmorBase != -1);
      SetSaveFlag(ref flags, SaveFlag.StrBonus, m_StrBonus != -1);
      SetSaveFlag(ref flags, SaveFlag.DexBonus, m_DexBonus != -1);
      SetSaveFlag(ref flags, SaveFlag.IntBonus, m_IntBonus != -1);
      SetSaveFlag(ref flags, SaveFlag.StrReq, m_StrReq != -1);
      SetSaveFlag(ref flags, SaveFlag.DexReq, m_DexReq != -1);
      SetSaveFlag(ref flags, SaveFlag.IntReq, m_IntReq != -1);
      SetSaveFlag(ref flags, SaveFlag.MedAllowance, m_Meditate != (AMA)(-1));
      SetSaveFlag(ref flags, SaveFlag.SkillBonuses, !SkillBonuses.IsEmpty);
      SetSaveFlag(ref flags, SaveFlag.PlayerConstructed, PlayerConstructed);

      writer.WriteEncodedInt((int)flags);

      if (GetSaveFlag(flags, SaveFlag.Attributes))
        Attributes.Serialize(writer);

      if (GetSaveFlag(flags, SaveFlag.ArmorAttributes))
        ArmorAttributes.Serialize(writer);

      if (GetSaveFlag(flags, SaveFlag.PhysicalBonus))
        writer.WriteEncodedInt(m_PhysicalBonus);

      if (GetSaveFlag(flags, SaveFlag.FireBonus))
        writer.WriteEncodedInt(m_FireBonus);

      if (GetSaveFlag(flags, SaveFlag.ColdBonus))
        writer.WriteEncodedInt(m_ColdBonus);

      if (GetSaveFlag(flags, SaveFlag.PoisonBonus))
        writer.WriteEncodedInt(m_PoisonBonus);

      if (GetSaveFlag(flags, SaveFlag.EnergyBonus))
        writer.WriteEncodedInt(m_EnergyBonus);

      if (GetSaveFlag(flags, SaveFlag.MaxHitPoints))
        writer.WriteEncodedInt(m_MaxHitPoints);

      if (GetSaveFlag(flags, SaveFlag.HitPoints))
        writer.WriteEncodedInt(m_HitPoints);

      if (GetSaveFlag(flags, SaveFlag.Crafter))
        writer.Write(m_Crafter);

      if (GetSaveFlag(flags, SaveFlag.Quality))
        writer.WriteEncodedInt((int)m_Quality);

      if (GetSaveFlag(flags, SaveFlag.Durability))
        writer.WriteEncodedInt((int)m_Durability);

      if (GetSaveFlag(flags, SaveFlag.Protection))
        writer.WriteEncodedInt((int)m_Protection);

      if (GetSaveFlag(flags, SaveFlag.Resource))
        writer.WriteEncodedInt((int)m_Resource);

      if (GetSaveFlag(flags, SaveFlag.BaseArmor))
        writer.WriteEncodedInt(m_ArmorBase);

      if (GetSaveFlag(flags, SaveFlag.StrBonus))
        writer.WriteEncodedInt(m_StrBonus);

      if (GetSaveFlag(flags, SaveFlag.DexBonus))
        writer.WriteEncodedInt(m_DexBonus);

      if (GetSaveFlag(flags, SaveFlag.IntBonus))
        writer.WriteEncodedInt(m_IntBonus);

      if (GetSaveFlag(flags, SaveFlag.StrReq))
        writer.WriteEncodedInt(m_StrReq);

      if (GetSaveFlag(flags, SaveFlag.DexReq))
        writer.WriteEncodedInt(m_DexReq);

      if (GetSaveFlag(flags, SaveFlag.IntReq))
        writer.WriteEncodedInt(m_IntReq);

      if (GetSaveFlag(flags, SaveFlag.MedAllowance))
        writer.WriteEncodedInt((int)m_Meditate);

      if (GetSaveFlag(flags, SaveFlag.SkillBonuses))
        SkillBonuses.Serialize(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 7:
        case 6:
        case 5:
          {
            SaveFlag flags = (SaveFlag)reader.ReadEncodedInt();

            if (GetSaveFlag(flags, SaveFlag.Attributes))
              Attributes = new AosAttributes(this, reader);
            else
              Attributes = new AosAttributes(this);

            if (GetSaveFlag(flags, SaveFlag.ArmorAttributes))
              ArmorAttributes = new AosArmorAttributes(this, reader);
            else
              ArmorAttributes = new AosArmorAttributes(this);

            if (GetSaveFlag(flags, SaveFlag.PhysicalBonus))
              m_PhysicalBonus = reader.ReadEncodedInt();

            if (GetSaveFlag(flags, SaveFlag.FireBonus))
              m_FireBonus = reader.ReadEncodedInt();

            if (GetSaveFlag(flags, SaveFlag.ColdBonus))
              m_ColdBonus = reader.ReadEncodedInt();

            if (GetSaveFlag(flags, SaveFlag.PoisonBonus))
              m_PoisonBonus = reader.ReadEncodedInt();

            if (GetSaveFlag(flags, SaveFlag.EnergyBonus))
              m_EnergyBonus = reader.ReadEncodedInt();

            if (GetSaveFlag(flags, SaveFlag.Identified))
              m_Identified = version >= 7 || reader.ReadBool();

            if (GetSaveFlag(flags, SaveFlag.MaxHitPoints))
              m_MaxHitPoints = reader.ReadEncodedInt();

            if (GetSaveFlag(flags, SaveFlag.HitPoints))
              m_HitPoints = reader.ReadEncodedInt();

            if (GetSaveFlag(flags, SaveFlag.Crafter))
              m_Crafter = reader.ReadMobile();

            if (GetSaveFlag(flags, SaveFlag.Quality))
              m_Quality = (ArmorQuality)reader.ReadEncodedInt();
            else
              m_Quality = ArmorQuality.Regular;

            if (version == 5 && m_Quality == ArmorQuality.Low)
              m_Quality = ArmorQuality.Regular;

            if (GetSaveFlag(flags, SaveFlag.Durability))
            {
              m_Durability = (ArmorDurabilityLevel)reader.ReadEncodedInt();

              if (m_Durability > ArmorDurabilityLevel.Indestructible)
                m_Durability = ArmorDurabilityLevel.Durable;
            }

            if (GetSaveFlag(flags, SaveFlag.Protection))
            {
              m_Protection = (ArmorProtectionLevel)reader.ReadEncodedInt();

              if (m_Protection > ArmorProtectionLevel.Invulnerability)
                m_Protection = ArmorProtectionLevel.Defense;
            }

            if (GetSaveFlag(flags, SaveFlag.Resource))
              m_Resource = (CraftResource)reader.ReadEncodedInt();
            else
              m_Resource = DefaultResource;

            if (m_Resource == CraftResource.None)
              m_Resource = DefaultResource;

            if (GetSaveFlag(flags, SaveFlag.BaseArmor))
              m_ArmorBase = reader.ReadEncodedInt();
            else
              m_ArmorBase = -1;

            if (GetSaveFlag(flags, SaveFlag.StrBonus))
              m_StrBonus = reader.ReadEncodedInt();
            else
              m_StrBonus = -1;

            if (GetSaveFlag(flags, SaveFlag.DexBonus))
              m_DexBonus = reader.ReadEncodedInt();
            else
              m_DexBonus = -1;

            if (GetSaveFlag(flags, SaveFlag.IntBonus))
              m_IntBonus = reader.ReadEncodedInt();
            else
              m_IntBonus = -1;

            if (GetSaveFlag(flags, SaveFlag.StrReq))
              m_StrReq = reader.ReadEncodedInt();
            else
              m_StrReq = -1;

            if (GetSaveFlag(flags, SaveFlag.DexReq))
              m_DexReq = reader.ReadEncodedInt();
            else
              m_DexReq = -1;

            if (GetSaveFlag(flags, SaveFlag.IntReq))
              m_IntReq = reader.ReadEncodedInt();
            else
              m_IntReq = -1;

            if (GetSaveFlag(flags, SaveFlag.MedAllowance))
              m_Meditate = (AMA)reader.ReadEncodedInt();
            else
              m_Meditate = (AMA)(-1);

            if (GetSaveFlag(flags, SaveFlag.SkillBonuses))
              SkillBonuses = new AosSkillBonuses(this, reader);

            if (GetSaveFlag(flags, SaveFlag.PlayerConstructed))
              PlayerConstructed = true;

            break;
          }
        case 4:
          {
            Attributes = new AosAttributes(this, reader);
            ArmorAttributes = new AosArmorAttributes(this, reader);
            goto case 3;
          }
        case 3:
          {
            m_PhysicalBonus = reader.ReadInt();
            m_FireBonus = reader.ReadInt();
            m_ColdBonus = reader.ReadInt();
            m_PoisonBonus = reader.ReadInt();
            m_EnergyBonus = reader.ReadInt();
            goto case 2;
          }
        case 2:
        case 1:
          {
            m_Identified = reader.ReadBool();
            goto case 0;
          }
        case 0:
          {
            m_ArmorBase = reader.ReadInt();
            m_MaxHitPoints = reader.ReadInt();
            m_HitPoints = reader.ReadInt();
            m_Crafter = reader.ReadMobile();
            m_Quality = (ArmorQuality)reader.ReadInt();
            m_Durability = (ArmorDurabilityLevel)reader.ReadInt();
            m_Protection = (ArmorProtectionLevel)reader.ReadInt();

            AMT mat = (AMT)reader.ReadInt();

            if (m_ArmorBase == RevertArmorBase)
              m_ArmorBase = -1;

            /*m_BodyPos = (ArmorBodyType)*/
            reader.ReadInt();

            if (version < 4)
            {
              Attributes = new AosAttributes(this);
              ArmorAttributes = new AosArmorAttributes(this);
            }

            if (version < 3 && m_Quality == ArmorQuality.Exceptional)
              DistributeBonuses(6);

            if (version >= 2)
            {
              m_Resource = (CraftResource)reader.ReadInt();
            }
            else
            {
              var info = reader.ReadInt() switch
              {
                0 => OreInfo.Iron,
                1 => OreInfo.DullCopper,
                2 => OreInfo.ShadowIron,
                3 => OreInfo.Copper,
                4 => OreInfo.Bronze,
                5 => OreInfo.Gold,
                6 => OreInfo.Agapite,
                7 => OreInfo.Verite,
                8 => OreInfo.Valorite,
                _ => OreInfo.Iron
              };

              m_Resource = CraftResources.GetFromOreInfo(info, mat);
            }

            m_StrBonus = reader.ReadInt();
            m_DexBonus = reader.ReadInt();
            m_IntBonus = reader.ReadInt();
            m_StrReq = reader.ReadInt();
            m_DexReq = reader.ReadInt();
            m_IntReq = reader.ReadInt();

            if (m_StrBonus == OldStrBonus)
              m_StrBonus = -1;

            if (m_DexBonus == OldDexBonus)
              m_DexBonus = -1;

            if (m_IntBonus == OldIntBonus)
              m_IntBonus = -1;

            if (m_StrReq == OldStrReq)
              m_StrReq = -1;

            if (m_DexReq == OldDexReq)
              m_DexReq = -1;

            if (m_IntReq == OldIntReq)
              m_IntReq = -1;

            m_Meditate = (AMA)reader.ReadInt();

            if (m_Meditate == OldMedAllowance)
              m_Meditate = (AMA)(-1);

            if (m_Resource == CraftResource.None)
            {
              if (mat == ArmorMaterialType.Studded || mat == ArmorMaterialType.Leather)
                m_Resource = CraftResource.RegularLeather;
              else if (mat == ArmorMaterialType.Spined)
                m_Resource = CraftResource.SpinedLeather;
              else if (mat == ArmorMaterialType.Horned)
                m_Resource = CraftResource.HornedLeather;
              else if (mat == ArmorMaterialType.Barbed)
                m_Resource = CraftResource.BarbedLeather;
              else
                m_Resource = CraftResource.Iron;
            }

            if (m_MaxHitPoints == 0 && m_HitPoints == 0)
              m_HitPoints = m_MaxHitPoints = Utility.RandomMinMax(InitMinHits, InitMaxHits);

            break;
          }
      }

      SkillBonuses ??= new AosSkillBonuses(this);

      Mobile m = Parent as Mobile;

      if (Core.AOS && m != null)
        SkillBonuses.AddTo(m);

      int strBonus = ComputeStatBonus(StatType.Str);
      int dexBonus = ComputeStatBonus(StatType.Dex);
      int intBonus = ComputeStatBonus(StatType.Int);

      if (m != null && (strBonus != 0 || dexBonus != 0 || intBonus != 0))
      {
        string modName = Serial.ToString();

        if (strBonus != 0)
          m.AddStatMod(new StatMod(StatType.Str, $"{modName}Str", strBonus, TimeSpan.Zero));

        if (dexBonus != 0)
          m.AddStatMod(new StatMod(StatType.Dex, $"{modName}Dex", dexBonus, TimeSpan.Zero));

        if (intBonus != 0)
          m.AddStatMod(new StatMod(StatType.Int, $"{modName}Int", intBonus, TimeSpan.Zero));
      }

      m?.CheckStatTimers();

      if (version < 7)
        PlayerConstructed = true; // we don't know, so, assume it's crafted
    }

    public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
    {
      if (!Ethic.CheckTrade(from, to, newOwner, this))
        return false;

      return base.AllowSecureTrade(from, to, newOwner, accepted);
    }

    public override bool CanEquip(Mobile from)
    {
      if (!Ethic.CheckEquip(from, this))
        return false;

      if (from.AccessLevel < AccessLevel.GameMaster)
      {
        if (RequiredRace != null && from.Race != RequiredRace)
        {
          if (RequiredRace == Race.Elf)
            from.SendLocalizedMessage(1072203); // Only Elves may use this.
          else
            from.SendMessage("Only {0} may use this.", RequiredRace.PluralName);

          return false;
        }

        if (!AllowMaleWearer && !from.Female)
        {
          if (AllowFemaleWearer)
            from.SendLocalizedMessage(1010388); // Only females can wear this.
          else
            from.SendMessage("You may not wear this.");

          return false;
        }

        if (!AllowFemaleWearer && from.Female)
        {
          if (AllowMaleWearer)
            from.SendLocalizedMessage(1063343); // Only males can wear this.
          else
            from.SendMessage("You may not wear this.");

          return false;
        }

        int strBonus = ComputeStatBonus(StatType.Str), strReq = ComputeStatReq(StatType.Str);
        int dexBonus = ComputeStatBonus(StatType.Dex), dexReq = ComputeStatReq(StatType.Dex);
        int intBonus = ComputeStatBonus(StatType.Int), intReq = ComputeStatReq(StatType.Int);

        if (from.Dex < dexReq || from.Dex + dexBonus < 1)
        {
          from.SendLocalizedMessage(502077); // You do not have enough dexterity to equip this item.
          return false;
        }

        if (from.Str < strReq || from.Str + strBonus < 1)
        {
          from.SendLocalizedMessage(500213); // You are not strong enough to equip that.
          return false;
        }

        if (from.Int < intReq || from.Int + intBonus < 1)
        {
          from.SendMessage("You are not smart enough to equip that.");
          return false;
        }
      }

      return base.CanEquip(from);
    }

    public override bool CheckPropertyConflict(Mobile m)
    {
      if (base.CheckPropertyConflict(m))
        return true;

      if (Layer == Layer.Pants)
        return m.FindItemOnLayer(Layer.InnerLegs) != null;

      if (Layer == Layer.Shirt)
        return m.FindItemOnLayer(Layer.InnerTorso) != null;

      return false;
    }

    public override bool OnEquip(Mobile from)
    {
      from.CheckStatTimers();

      int strBonus = ComputeStatBonus(StatType.Str);
      int dexBonus = ComputeStatBonus(StatType.Dex);
      int intBonus = ComputeStatBonus(StatType.Int);

      if (strBonus != 0 || dexBonus != 0 || intBonus != 0)
      {
        string modName = Serial.ToString();

        if (strBonus != 0)
          from.AddStatMod(new StatMod(StatType.Str, $"{modName}Str", strBonus, TimeSpan.Zero));

        if (dexBonus != 0)
          from.AddStatMod(new StatMod(StatType.Dex, $"{modName}Dex", dexBonus, TimeSpan.Zero));

        if (intBonus != 0)
          from.AddStatMod(new StatMod(StatType.Int, $"{modName}Int", intBonus, TimeSpan.Zero));
      }

      return base.OnEquip(from);
    }

    public override void OnRemoved(IEntity parent)
    {
      if (parent is Mobile m)
      {
        string modName = Serial.ToString();

        m.RemoveStatMod($"{modName}Str");
        m.RemoveStatMod($"{modName}Dex");
        m.RemoveStatMod($"{modName}Int");

        if (Core.AOS)
          SkillBonuses.Remove();

        m.Delta(MobileDelta.Armor); // Tell them armor rating has changed
        m.CheckStatTimers();
      }

      base.OnRemoved(parent);
    }

    private string GetNameString() => Name ?? $"#{LabelNumber}";

    public override void AddNameProperty(ObjectPropertyList list)
    {
      var oreType = m_Resource switch
      {
        CraftResource.DullCopper => 1053108,
        CraftResource.ShadowIron => 1053107,
        CraftResource.Copper => 1053106,
        CraftResource.Bronze => 1053105,
        CraftResource.Gold => 1053104,
        CraftResource.Agapite => 1053103,
        CraftResource.Verite => 1053102,
        CraftResource.Valorite => 1053101,
        CraftResource.SpinedLeather => 1061118,
        CraftResource.HornedLeather => 1061117,
        CraftResource.BarbedLeather => 1061116,
        CraftResource.RedScales => 1060814,
        CraftResource.YellowScales => 1060818,
        CraftResource.BlackScales => 1060820,
        CraftResource.GreenScales => 1060819,
        CraftResource.WhiteScales => 1060821,
        CraftResource.BlueScales => 1060815,
        _ => 0
      };

      if (m_Quality == ArmorQuality.Exceptional)
      {
        if (oreType != 0)
          list.Add(1053100, "#{0}\t{1}", oreType, GetNameString()); // exceptional ~1_oretype~ ~2_armortype~
        else
          list.Add(1050040, GetNameString()); // exceptional ~1_ITEMNAME~
      }
      else
      {
        if (oreType != 0)
          list.Add(1053099, "#{0}\t{1}", oreType, GetNameString()); // ~1_oretype~ ~2_armortype~
        else if (Name == null)
          list.Add(LabelNumber);
        else
          list.Add(Name);
      }
    }

    public override bool AllowEquippedCast(Mobile from)
    {
      if (base.AllowEquippedCast(from))
        return true;

      return Attributes.SpellChanneling != 0;
    }

    public virtual int GetLuckBonus()
    {
      CraftResourceInfo resInfo = CraftResources.GetInfo(m_Resource);

      CraftAttributeInfo attrInfo = resInfo?.AttributeInfo;

      if (attrInfo == null)
        return 0;

      return attrInfo.ArmorLuck;
    }

    public override void GetProperties(ObjectPropertyList list)
    {
      base.GetProperties(list);

      if (m_Crafter != null)
        list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~

      if (m_FactionState != null)
        list.Add(1041350); // faction item

      if (RequiredRace == Race.Elf)
        list.Add(1075086); // Elves Only

      SkillBonuses.GetProperties(list);

      int prop;

      if ((prop = ArtifactRarity) > 0)
        list.Add(1061078, prop.ToString()); // artifact rarity ~1_val~

      if ((prop = Attributes.WeaponDamage) != 0)
        list.Add(1060401, prop.ToString()); // damage increase ~1_val~%

      if ((prop = Attributes.DefendChance) != 0)
        list.Add(1060408, prop.ToString()); // defense chance increase ~1_val~%

      if ((prop = Attributes.BonusDex) != 0)
        list.Add(1060409, prop.ToString()); // dexterity bonus ~1_val~

      if ((prop = Attributes.EnhancePotions) != 0)
        list.Add(1060411, prop.ToString()); // enhance potions ~1_val~%

      if ((prop = Attributes.CastRecovery) != 0)
        list.Add(1060412, prop.ToString()); // faster cast recovery ~1_val~

      if ((prop = Attributes.CastSpeed) != 0)
        list.Add(1060413, prop.ToString()); // faster casting ~1_val~

      if ((prop = Attributes.AttackChance) != 0)
        list.Add(1060415, prop.ToString()); // hit chance increase ~1_val~%

      if ((prop = Attributes.BonusHits) != 0)
        list.Add(1060431, prop.ToString()); // hit point increase ~1_val~

      if ((prop = Attributes.BonusInt) != 0)
        list.Add(1060432, prop.ToString()); // intelligence bonus ~1_val~

      if ((prop = Attributes.LowerManaCost) != 0)
        list.Add(1060433, prop.ToString()); // lower mana cost ~1_val~%

      if ((prop = Attributes.LowerRegCost) != 0)
        list.Add(1060434, prop.ToString()); // lower reagent cost ~1_val~%

      if ((prop = GetLowerStatReq()) != 0)
        list.Add(1060435, prop.ToString()); // lower requirements ~1_val~%

      if ((prop = GetLuckBonus() + Attributes.Luck) != 0)
        list.Add(1060436, prop.ToString()); // luck ~1_val~

      if (ArmorAttributes.MageArmor != 0)
        list.Add(1060437); // mage armor

      if ((prop = Attributes.BonusMana) != 0)
        list.Add(1060439, prop.ToString()); // mana increase ~1_val~

      if ((prop = Attributes.RegenMana) != 0)
        list.Add(1060440, prop.ToString()); // mana regeneration ~1_val~

      if (Attributes.NightSight != 0)
        list.Add(1060441); // night sight

      if ((prop = Attributes.ReflectPhysical) != 0)
        list.Add(1060442, prop.ToString()); // reflect physical damage ~1_val~%

      if ((prop = Attributes.RegenStam) != 0)
        list.Add(1060443, prop.ToString()); // stamina regeneration ~1_val~

      if ((prop = Attributes.RegenHits) != 0)
        list.Add(1060444, prop.ToString()); // hit point regeneration ~1_val~

      if ((prop = ArmorAttributes.SelfRepair) != 0)
        list.Add(1060450, prop.ToString()); // self repair ~1_val~

      if (Attributes.SpellChanneling != 0)
        list.Add(1060482); // spell channeling

      if ((prop = Attributes.SpellDamage) != 0)
        list.Add(1060483, prop.ToString()); // spell damage increase ~1_val~%

      if ((prop = Attributes.BonusStam) != 0)
        list.Add(1060484, prop.ToString()); // stamina increase ~1_val~

      if ((prop = Attributes.BonusStr) != 0)
        list.Add(1060485, prop.ToString()); // strength bonus ~1_val~

      if ((prop = Attributes.WeaponSpeed) != 0)
        list.Add(1060486, prop.ToString()); // swing speed increase ~1_val~%

      if (Core.ML && (prop = Attributes.IncreasedKarmaLoss) != 0)
        list.Add(1075210, prop.ToString()); // Increased Karma Loss ~1val~%

      AddResistanceProperties(list);

      if ((prop = GetDurabilityBonus()) > 0)
        list.Add(1060410, prop.ToString()); // durability ~1_val~%

      if ((prop = ComputeStatReq(StatType.Str)) > 0)
        list.Add(1061170, prop.ToString()); // strength requirement ~1_val~

      if (m_HitPoints >= 0 && m_MaxHitPoints > 0)
        list.Add(1060639, "{0}\t{1}", m_HitPoints, m_MaxHitPoints); // durability ~1_val~ / ~2_val~
    }

    public override void OnSingleClick(Mobile from)
    {
      List<EquipInfoAttribute> attrs = new List<EquipInfoAttribute>();

      if (DisplayLootType)
      {
        if (LootType == LootType.Blessed)
          attrs.Add(new EquipInfoAttribute(1038021)); // blessed
        else if (LootType == LootType.Cursed)
          attrs.Add(new EquipInfoAttribute(1049643)); // cursed
      }

      if (m_FactionState != null)
        attrs.Add(new EquipInfoAttribute(1041350)); // faction item

      if (m_Quality == ArmorQuality.Exceptional)
        attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));

      if (m_Identified || from.AccessLevel >= AccessLevel.GameMaster)
      {
        if (m_Durability != ArmorDurabilityLevel.Regular)
          attrs.Add(new EquipInfoAttribute(1038000 + (int)m_Durability));

        if (m_Protection > ArmorProtectionLevel.Regular && m_Protection <= ArmorProtectionLevel.Invulnerability)
          attrs.Add(new EquipInfoAttribute(1038005 + (int)m_Protection));
      }
      else if (m_Durability != ArmorDurabilityLevel.Regular || (m_Protection > ArmorProtectionLevel.Regular &&
               m_Protection <= ArmorProtectionLevel.Invulnerability))
      {
        attrs.Add(new EquipInfoAttribute(1038000)); // Unidentified
      }

      int number;

      if (Name == null)
      {
        number = LabelNumber;
      }
      else
      {
        LabelTo(from, Name);
        number = 1041000;
      }

      if (attrs.Count == 0 && Crafter == null && Name != null)
        return;

      EquipmentInfo eqInfo = new EquipmentInfo(number, m_Crafter, false, attrs.ToArray());

      from.Send(new DisplayEquipmentInfo(this, eqInfo));
    }

    [Flags]
    private enum SaveFlag
    {
      None = 0x00000000,
      Attributes = 0x00000001,
      ArmorAttributes = 0x00000002,
      PhysicalBonus = 0x00000004,
      FireBonus = 0x00000008,
      ColdBonus = 0x00000010,
      PoisonBonus = 0x00000020,
      EnergyBonus = 0x00000040,
      Identified = 0x00000080,
      MaxHitPoints = 0x00000100,
      HitPoints = 0x00000200,
      Crafter = 0x00000400,
      Quality = 0x00000800,
      Durability = 0x00001000,
      Protection = 0x00002000,
      Resource = 0x00004000,
      BaseArmor = 0x00008000,
      StrBonus = 0x00010000,
      DexBonus = 0x00020000,
      IntBonus = 0x00040000,
      StrReq = 0x00080000,
      DexReq = 0x00100000,
      IntReq = 0x00200000,
      MedAllowance = 0x00400000,
      SkillBonuses = 0x00800000,
      PlayerConstructed = 0x01000000
    }

    private FactionItem m_FactionState;

    public FactionItem FactionItemState
    {
      get => m_FactionState;
      set
      {
        m_FactionState = value;

        if (m_FactionState == null)
          Hue = CraftResources.GetHue(Resource);

        LootType = m_FactionState == null ? LootType.Regular : LootType.Blessed;
      }
    }
  }
}
