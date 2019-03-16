using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Mat = Server.Engines.BulkOrders.BulkMaterialType;

namespace Server.Engines.BulkOrders
{
  public class SmallSmithBOD : SmallBOD
  {
    public static double[] m_BlacksmithMaterialChances =
    {
      0.501953125, // None
      0.250000000, // Dull Copper
      0.125000000, // Shadow Iron
      0.062500000, // Copper
      0.031250000, // Bronze
      0.015625000, // Gold
      0.007812500, // Agapite
      0.003906250, // Verite
      0.001953125 // Valorite
    };

    private SmallSmithBOD(SmallBulkEntry entry, BulkMaterialType mat, int amountMax, bool reqExceptional)
      : base(0x44E, 0, amountMax, entry.Type, entry.Number, entry.Graphic, reqExceptional, mat)
    {
    }

    [Constructible]
    public SmallSmithBOD()
    {
      bool useMaterials = Utility.RandomBool();

      SmallBulkEntry[] entries = useMaterials ? SmallBulkEntry.BlacksmithArmor :
        SmallBulkEntry.BlacksmithWeapons;

      if (entries.Length <= 0)
        return;

      int hue = 0x44E;
      int amountMax = Utility.RandomList(10, 15, 20);

      BulkMaterialType material = useMaterials ? GetRandomMaterial(BulkMaterialType.DullCopper, m_BlacksmithMaterialChances)
        : BulkMaterialType.None;

      bool reqExceptional = Utility.RandomBool() || material == BulkMaterialType.None;

      SmallBulkEntry entry = entries[Utility.Random(entries.Length)];

      Hue = hue;
      AmountMax = amountMax;
      Type = entry.Type;
      Number = entry.Number;
      Graphic = entry.Graphic;
      RequireExceptional = reqExceptional;
      Material = material;
    }

    public SmallSmithBOD(int amountCur, int amountMax, Type type, int number, int graphic, bool reqExceptional,
      BulkMaterialType mat) : base(0x44E, amountCur, amountMax, type, number, graphic, reqExceptional, mat)
    {
    }

    public SmallSmithBOD(Serial serial) : base(serial)
    {
    }

    public override int ComputeFame() => SmithRewardCalculator.Instance.ComputeFame(this);

    public override int ComputeGold() => SmithRewardCalculator.Instance.ComputeGold(this);

    public override RewardGroup GetRewardGroup() =>
      SmithRewardCalculator.Instance.LookupRewards(SmithRewardCalculator.Instance.ComputePoints(this));

    public static SmallSmithBOD CreateRandomFor(Mobile m)
    {
      bool useMaterials = Utility.RandomBool();

      SmallBulkEntry[] entries = useMaterials ? SmallBulkEntry.BlacksmithArmor :
        SmallBulkEntry.BlacksmithWeapons;

      if (entries.Length <= 0)
        return null;

      int theirSkill = m.Skills.Blacksmith.BaseFixedPoint;
      int amountMax;

      if (theirSkill >= 701)
        amountMax = Utility.RandomList(10, 15, 20, 20);
      else if (theirSkill >= 501)
        amountMax = Utility.RandomList(10, 15, 15, 20);
      else
        amountMax = Utility.RandomList(10, 10, 15, 20);

      BulkMaterialType material = BulkMaterialType.None;

      if (useMaterials && theirSkill >= 701)
        for (int i = 0; i < 20; ++i)
        {
          BulkMaterialType check = GetRandomMaterial(BulkMaterialType.DullCopper, m_BlacksmithMaterialChances);
          int skillReq = 0;

          switch (check)
          {
            case BulkMaterialType.DullCopper:
              skillReq = 650;
              break;
            case BulkMaterialType.ShadowIron:
              skillReq = 700;
              break;
            case BulkMaterialType.Copper:
              skillReq = 750;
              break;
            case BulkMaterialType.Bronze:
              skillReq = 800;
              break;
            case BulkMaterialType.Gold:
              skillReq = 850;
              break;
            case BulkMaterialType.Agapite:
              skillReq = 900;
              break;
            case BulkMaterialType.Verite:
              skillReq = 950;
              break;
            case BulkMaterialType.Valorite:
              skillReq = 1000;
              break;
            case BulkMaterialType.Spined:
              skillReq = 650;
              break;
            case BulkMaterialType.Horned:
              skillReq = 800;
              break;
            case BulkMaterialType.Barbed:
              skillReq = 990;
              break;
          }

          if (theirSkill >= skillReq)
          {
            material = check;
            break;
          }
        }

      double excChance = theirSkill >= 701 ? (theirSkill + 800) / 2000.0 : 0.0;

      bool reqExceptional = excChance > Utility.RandomDouble();

      CraftSystem system = DefBlacksmithy.CraftSystem;

      List<SmallBulkEntry> validEntries = new List<SmallBulkEntry>();

      for (int i = 0; i < entries.Length; ++i)
      {
        CraftItem item = system.CraftItems.SearchFor(entries[i].Type);

        if (item != null)
        {
          bool allRequiredSkills = true;
          double chance = item.GetSuccessChance(m, null, system, false, ref allRequiredSkills);

          if (allRequiredSkills && chance >= 0.0)
          {
            if (reqExceptional)
              chance = item.GetExceptionalChance(system, chance, m);

            if (chance > 0.0)
              validEntries.Add(entries[i]);
          }
        }
      }

      if (validEntries.Count <= 0)
        return null;

      SmallBulkEntry entry = validEntries[Utility.Random(validEntries.Count)];
      return new SmallSmithBOD(entry, material, amountMax, reqExceptional);

    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
