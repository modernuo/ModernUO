using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.ConPVP;
using Server.Engines.Harvest;
using Server.Network;

namespace Server.Items
{
  public interface IAxe
  {
    bool Axe(Mobile from, BaseAxe axe);
  }

  public abstract class BaseAxe : BaseMeleeWeapon
  {
    private bool m_ShowUsesRemaining;

    private int m_UsesRemaining;

    public BaseAxe(int itemID) : base(itemID) => m_UsesRemaining = 150;

    public BaseAxe(Serial serial) : base(serial)
    {
    }

    public override int DefHitSound => 0x232;
    public override int DefMissSound => 0x23A;

    public override SkillName DefSkill => SkillName.Swords;
    public override WeaponType DefType => WeaponType.Axe;
    public override WeaponAnimation DefAnimation => WeaponAnimation.Slash2H;

    public virtual HarvestSystem HarvestSystem => Lumberjacking.System;

    [CommandProperty(AccessLevel.GameMaster)]
    public int UsesRemaining
    {
      get => m_UsesRemaining;
      set
      {
        m_UsesRemaining = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ShowUsesRemaining
    {
      get => m_ShowUsesRemaining;
      set
      {
        m_ShowUsesRemaining = value;
        InvalidateProperties();
      }
    }

    public virtual int GetUsesScalar()
    {
      if (Quality == WeaponQuality.Exceptional)
        return 200;

      return 100;
    }

    public override void UnscaleDurability()
    {
      base.UnscaleDurability();

      int scale = GetUsesScalar();

      m_UsesRemaining = (m_UsesRemaining * 100 + (scale - 1)) / scale;
      InvalidateProperties();
    }

    public override void ScaleDurability()
    {
      base.ScaleDurability();

      int scale = GetUsesScalar();

      m_UsesRemaining = (m_UsesRemaining * scale + 99) / 100;
      InvalidateProperties();
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (HarvestSystem == null || Deleted)
        return;

      Point3D loc = GetWorldLocation();

      if (!from.InLOS(loc) || !from.InRange(loc, 2))
      {
        from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1019045); // I can't reach that
        return;
      }

      if (!IsAccessibleTo(from))
      {
        PublicOverheadMessage(MessageType.Regular, 0x3E9, 1061637); // You are not allowed to access this.
        return;
      }

      if (!(HarvestSystem is Mining))
        from.SendLocalizedMessage(1010018); // What do you want to use this item on?

      HarvestSystem.BeginHarvesting(from, this);
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
      base.GetContextMenuEntries(from, list);

      if (HarvestSystem != null)
        BaseHarvestTool.AddContextMenuEntries(from, this, list, HarvestSystem);
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(2); // version

      writer.Write(m_ShowUsesRemaining);

      writer.Write(m_UsesRemaining);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 2:
          {
            m_ShowUsesRemaining = reader.ReadBool();
            goto case 1;
          }
        case 1:
          {
            m_UsesRemaining = reader.ReadInt();
            goto case 0;
          }
        case 0:
          {
            if (m_UsesRemaining < 1)
              m_UsesRemaining = 150;

            break;
          }
      }
    }

    public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
    {
      base.OnHit(attacker, defender, damageBonus);

      if (!Core.AOS && (attacker.Player || attacker.Body.IsHuman) && Layer == Layer.TwoHanded &&
          attacker.Skills.Anatomy.Value >= 80 &&
          attacker.Skills.Anatomy.Value / 400.0 >= Utility.RandomDouble() &&
          DuelContext.AllowSpecialAbility(attacker, "Concussion Blow", false))
      {
        StatMod mod = defender.GetStatMod("Concussion");

        if (mod == null)
        {
          defender.SendMessage("You receive a concussion blow!");
          defender.AddStatMod(new StatMod(StatType.Int, "Concussion", -(defender.RawInt / 2),
            TimeSpan.FromSeconds(30.0)));

          attacker.SendMessage("You deliver a concussion blow!");
          attacker.PlaySound(0x308);
        }
      }
    }
  }
}