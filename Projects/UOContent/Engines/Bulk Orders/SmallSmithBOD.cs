using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Engines.BulkOrders
{
    [SerializationGenerator(0, false)]
    public partial class SmallSmithBOD : SmallBOD
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
            0.001953125  // Valorite
        };

        private SmallSmithBOD(SmallBulkEntry entry, BulkMaterialType mat, int amountMax, bool reqExceptional)
            : base(0x44E, 0, amountMax, entry.Type, entry.Number, entry.Graphic, reqExceptional, mat)
        {
        }

        [Constructible]
        public SmallSmithBOD()
        {
            var useMaterials = Utility.RandomBool();

            var entries = useMaterials ? SmallBulkEntry.BlacksmithArmor : SmallBulkEntry.BlacksmithWeapons;

            if (entries.Length <= 0)
            {
                return;
            }

            var hue = 0x44E;
            var amountMax = Utility.RandomList(10, 15, 20);

            var material = useMaterials
                ? GetRandomMaterial(BulkMaterialType.DullCopper, m_BlacksmithMaterialChances)
                : BulkMaterialType.None;

            var reqExceptional = Utility.RandomBool() || material == BulkMaterialType.None;

            var entry = entries.RandomElement();

            Hue = hue;
            AmountMax = amountMax;
            Type = entry.Type;
            Number = entry.Number;
            Graphic = entry.Graphic;
            RequireExceptional = reqExceptional;
            Material = material;
        }

        public SmallSmithBOD(
            int amountCur, int amountMax, Type type, int number, int graphic, bool reqExceptional,
            BulkMaterialType mat
        ) : base(0x44E, amountCur, amountMax, type, number, graphic, reqExceptional, mat)
        {
        }

        public override int ComputeFame() => SmithRewardCalculator.Instance.ComputeFame(this);

        public override int ComputeGold() => SmithRewardCalculator.Instance.ComputeGold(this);

        public override RewardGroup GetRewardGroup() =>
            SmithRewardCalculator.Instance.LookupRewards(SmithRewardCalculator.Instance.ComputePoints(this));

        public static SmallSmithBOD CreateRandomFor(Mobile m)
        {
            var useMaterials = Utility.RandomBool();

            var entries = useMaterials ? SmallBulkEntry.BlacksmithArmor : SmallBulkEntry.BlacksmithWeapons;

            if (entries.Length <= 0)
            {
                return null;
            }

            var theirSkill = m.Skills.Blacksmith.Base;

            int amountMax = theirSkill switch
            {
                >= 70.1 => Utility.RandomList(10, 15, 20, 20),
                >= 50.1 => Utility.RandomList(10, 15, 15, 20),
                _       => Utility.RandomList(10, 10, 15, 20)
            };

            var material = BulkMaterialType.None;

            if (useMaterials && theirSkill >= 70.1)
            {
                for (var i = 0; i < 20; ++i)
                {
                    var check = GetRandomMaterial(BulkMaterialType.DullCopper, m_BlacksmithMaterialChances);

                    var skillReq = check switch
                    {
                        BulkMaterialType.DullCopper => 65.0,
                        BulkMaterialType.ShadowIron => 70.0,
                        BulkMaterialType.Copper     => 75.0,
                        BulkMaterialType.Bronze     => 80.0,
                        BulkMaterialType.Gold       => 85.0,
                        BulkMaterialType.Agapite    => 90.0,
                        BulkMaterialType.Verite     => 95.0,
                        BulkMaterialType.Valorite   => 100.0,
                        BulkMaterialType.Spined     => 65.0,
                        BulkMaterialType.Horned     => 80.0,
                        BulkMaterialType.Barbed     => 99.0,
                        _                           => 0.0
                    };

                    if (theirSkill >= skillReq)
                    {
                        material = check;
                        break;
                    }
                }
            }

            var excChance = theirSkill >= 70.1 ? (theirSkill + 80.0) / 200.0 : 0.0;

            var reqExceptional = excChance > Utility.RandomDouble();

            var system = DefBlacksmithy.CraftSystem;

            var validEntries = new List<SmallBulkEntry>();

            for (var i = 0; i < entries.Length; ++i)
            {
                var item = system.CraftItems.SearchFor(entries[i].Type);

                if (item != null)
                {
                    var chance = item.GetSuccessChance(m, null, system, false, out var allRequiredSkills);

                    if (allRequiredSkills && chance >= 0.0)
                    {
                        if (reqExceptional)
                        {
                            chance = item.GetExceptionalChance(system, chance, m);
                        }

                        if (chance > 0.0)
                        {
                            validEntries.Add(entries[i]);
                        }
                    }
                }
            }

            if (validEntries.Count <= 0)
            {
                return null;
            }

            var entry = validEntries.RandomElement();
            return new SmallSmithBOD(entry, material, amountMax, reqExceptional);
        }
    }
}
