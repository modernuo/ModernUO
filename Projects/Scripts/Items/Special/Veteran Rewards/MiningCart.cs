using System;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
  public enum MiningCartType
  {
    OreSouth = 100,
    OreEast = 101,
    GemSouth = 102,
    GemEast = 103
  }

  public class MiningCart : BaseAddon, IRewardItem
  {
    private Timer m_Timer;

    [Constructible]
    public MiningCart(MiningCartType type)
    {
      CartType = type;

      switch (type)
      {
        case MiningCartType.OreSouth:
          AddComponent(new AddonComponent(0x1A83), 0, 0, 0);
          AddComponent(new AddonComponent(0x1A82), 0, 1, 0);
          AddComponent(new AddonComponent(0x1A86), 0, -1, 0);
          break;
        case MiningCartType.OreEast:
          AddComponent(new AddonComponent(0x1A88), 0, 0, 0);
          AddComponent(new AddonComponent(0x1A87), 1, 0, 0);
          AddComponent(new AddonComponent(0x1A8B), -1, 0, 0);
          break;
        case MiningCartType.GemSouth:
          AddComponent(new LocalizedAddonComponent(0x1A83, 1080388), 0, 0, 0);
          AddComponent(new LocalizedAddonComponent(0x1A82, 1080388), 0, 1, 0);
          AddComponent(new LocalizedAddonComponent(0x1A86, 1080388), 0, -1, 0);

          AddComponent(new AddonComponent(0xF2C), 0, 0, 6);
          AddComponent(new AddonComponent(0xF1D), 0, 0, 5);
          AddComponent(new AddonComponent(0xF2B), 0, 0, 2);
          AddComponent(new AddonComponent(0xF21), 0, 0, 1);
          AddComponent(new AddonComponent(0xF22), 0, 0, 4);
          AddComponent(new AddonComponent(0xF2F), 0, 0, 5);
          AddComponent(new AddonComponent(0xF26), 0, 0, 6);
          AddComponent(new AddonComponent(0xF27), 0, 0, 3);
          AddComponent(new AddonComponent(0xF29), 0, 0, 0);
          break;
        case MiningCartType.GemEast:
          AddComponent(new LocalizedAddonComponent(0x1A88, 1080388), 0, 0, 0);
          AddComponent(new LocalizedAddonComponent(0x1A87, 1080388), 1, 0, 0);
          AddComponent(new LocalizedAddonComponent(0x1A8B, 1080388), -1, 0, 0);

          AddComponent(new AddonComponent(0xF2E), 0, 0, 6);
          AddComponent(new AddonComponent(0xF12), 0, 0, 3);
          AddComponent(new AddonComponent(0xF29), 0, 0, 1);
          AddComponent(new AddonComponent(0xF24), 0, 0, 5);
          AddComponent(new AddonComponent(0xF21), 0, 0, 1);
          AddComponent(new AddonComponent(0xF2B), 0, 0, 3);
          AddComponent(new AddonComponent(0xF2F), 0, 0, 4);
          AddComponent(new AddonComponent(0xF23), 0, 0, 3);
          AddComponent(new AddonComponent(0xF27), 0, 0, 3);
          break;
      }

      m_Timer = Timer.DelayCall(TimeSpan.FromDays(1), TimeSpan.FromDays(1), GiveResources);
    }

    public MiningCart(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed
    {
      get
      {
        MiningCartDeed deed = new MiningCartDeed();
        deed.IsRewardItem = IsRewardItem;
        deed.Gems = Gems;
        deed.Ore = Ore;

        return deed;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public MiningCartType CartType{ get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Gems{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Ore{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem{ get; set; }

    private void GiveResources()
    {
      switch (CartType)
      {
        case MiningCartType.OreSouth:
        case MiningCartType.OreEast:
          Ore = Math.Min(100, Ore + 10);
          break;
        case MiningCartType.GemSouth:
        case MiningCartType.GemEast:
          Gems = Math.Min(50, Gems + 5);
          break;
      }
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
      BaseHouse house = BaseHouse.FindHouseAt(this);

      /*
       * Unique problems have unique solutions.  OSI does not have a problem with 1000s of mining carts
       * due to the fact that they have only a miniscule fraction of the number of 10 year vets that a
       * typical RunUO shard will have (RunUO's scaled down account aging system makes this a unique problem),
       * and the "freeness" of free accounts. We also dont have mitigating factors like inactive (unpaid)
       * accounts not gaining veteran time.
       *
       * The lack of high end vets and vet rewards on OSI has made testing the *exact* ranging/stacking
       * behavior of these things all but impossible, so either way its just an estimation.
       *
       * If youd like your shard's carts/stumps to work the way they did before, simply replace the check
       * below with this line of code:
       *
       * if (!from.InRange(GetWorldLocation(), 2)
       *
       * However, I am sure these checks are more accurate to OSI than the former version was.
       *
       */

      if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this) || !(from.Z - Z > -3 && from.Z - Z < 3))
        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
      else if (house?.HasSecureAccess(from, SecureLevel.Friends) == true)
        switch (CartType)
        {
          case MiningCartType.OreSouth:
          case MiningCartType.OreEast:
            if (Ore > 0)
            {
              Item ingots = null;

              switch (Utility.Random(9))
              {
                case 0:
                  ingots = new IronIngot();
                  break;
                case 1:
                  ingots = new DullCopperIngot();
                  break;
                case 2:
                  ingots = new ShadowIronIngot();
                  break;
                case 3:
                  ingots = new CopperIngot();
                  break;
                case 4:
                  ingots = new BronzeIngot();
                  break;
                case 5:
                  ingots = new GoldIngot();
                  break;
                case 6:
                  ingots = new AgapiteIngot();
                  break;
                case 7:
                  ingots = new VeriteIngot();
                  break;
                case 8:
                  ingots = new ValoriteIngot();
                  break;
              }

              int amount = Math.Min(10, Ore);
              ingots.Amount = amount;

              if (!from.PlaceInBackpack(ingots))
              {
                ingots.Delete();
                from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
              }
              else
              {
                PublicOverheadMessage(MessageType.Regular, 0, 1094724, amount.ToString()); // Ore: ~1_COUNT~
                Ore -= amount;
              }
            }
            else
            {
              from.SendLocalizedMessage(1094725); // There are no more resources available at this time.
            }

            break;
          case MiningCartType.GemSouth:
          case MiningCartType.GemEast:
            if (Gems > 0)
            {
              Item gems = null;

              switch (Utility.Random(15))
              {
                case 0:
                  gems = new Amber();
                  break;
                case 1:
                  gems = new Amethyst();
                  break;
                case 2:
                  gems = new Citrine();
                  break;
                case 3:
                  gems = new Diamond();
                  break;
                case 4:
                  gems = new Emerald();
                  break;
                case 5:
                  gems = new Ruby();
                  break;
                case 6:
                  gems = new Sapphire();
                  break;
                case 7:
                  gems = new StarSapphire();
                  break;
                case 8:
                  gems = new Tourmaline();
                  break;

                // Mondain's Legacy gems
                case 9:
                  gems = new PerfectEmerald();
                  break;
                case 10:
                  gems = new DarkSapphire();
                  break;
                case 11:
                  gems = new Turquoise();
                  break;
                case 12:
                  gems = new EcruCitrine();
                  break;
                case 13:
                  gems = new FireRuby();
                  break;
                case 14:
                  gems = new BlueDiamond();
                  break;
              }

              int amount = Math.Min(5, Gems);
              gems.Amount = amount;

              if (!from.PlaceInBackpack(gems))
              {
                gems.Delete();
                from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
              }
              else
              {
                PublicOverheadMessage(MessageType.Regular, 0, 1094723, amount.ToString()); // Gems: ~1_COUNT~
                Gems -= amount;
              }
            }
            else
            {
              from.SendLocalizedMessage(1094725); // There are no more resources available at this time.
            }

            break;
        }
      else
        from.SendLocalizedMessage(1061637); // You are not allowed to access this.
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(1); // version

      #region version 1

      writer.Write((int)CartType);

      #endregion

      writer.Write(IsRewardItem);
      writer.Write(Gems);
      writer.Write(Ore);

      if (m_Timer != null)
        writer.Write(m_Timer.Next);
      else
        writer.Write(DateTime.UtcNow + TimeSpan.FromDays(1));
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();

      switch (version)
      {
        case 1:
          CartType = (MiningCartType)reader.ReadInt();
          goto case 0;
        case 0:
          IsRewardItem = reader.ReadBool();
          Gems = reader.ReadInt();
          Ore = reader.ReadInt();

          DateTime next = reader.ReadDateTime();

          if (next < DateTime.UtcNow)
            next = DateTime.UtcNow;

          m_Timer = Timer.DelayCall(next - DateTime.UtcNow, TimeSpan.FromDays(1), GiveResources);
          break;
      }
    }
  }

  public class MiningCartDeed : BaseAddonDeed, IRewardItem, IRewardOption
  {
    private MiningCartType m_CartType;

    private bool m_IsRewardItem;

    [Constructible]
    public MiningCartDeed() => LootType = LootType.Blessed;

    public MiningCartDeed(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1080385; // deed for a mining cart decoration

    public override BaseAddon Addon
    {
      get
      {
        MiningCart addon = new MiningCart(m_CartType);
        addon.IsRewardItem = m_IsRewardItem;
        addon.Gems = Gems;
        addon.Ore = Ore;

        return addon;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Gems{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Ore{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem
    {
      get => m_IsRewardItem;
      set
      {
        m_IsRewardItem = value;
        InvalidateProperties();
      }
    }

    public void GetOptions(RewardOptionList list)
    {
      list.Add((int)MiningCartType.OreSouth, 1080391);
      list.Add((int)MiningCartType.OreEast, 1080390);
      list.Add((int)MiningCartType.GemSouth, 1080500);
      list.Add((int)MiningCartType.GemEast, 1080499);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
      m_CartType = (MiningCartType)choice;

      if (!Deleted)
        base.OnDoubleClick(from);
    }

    public override void GetProperties(ObjectPropertyList list)
    {
      base.GetProperties(list);

      if (m_IsRewardItem)
        list.Add(1080457); // 10th Year Veteran Reward
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (m_IsRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
        return;

      if (IsChildOf(from.Backpack))
      {
        from.CloseGump<RewardOptionGump>();
        from.SendGump(new RewardOptionGump(this));
      }
      else
      {
        from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
      }
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version

      writer.Write(m_IsRewardItem);
      writer.Write(Gems);
      writer.Write(Ore);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();

      m_IsRewardItem = reader.ReadBool();
      Gems = reader.ReadInt();
      Ore = reader.ReadInt();
    }
  }
}
