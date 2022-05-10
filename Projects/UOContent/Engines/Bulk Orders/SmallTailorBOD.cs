using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Engines.BulkOrders
{
    [SerializationGenerator(0, false)]
    public partial class SmallTailorBOD : SmallBOD
    {
        public static double[] m_TailoringMaterialChances =
        {
            0.857421875, // None
            0.125000000, // Spined
            0.015625000, // Horned
            0.001953125  // Barbed
        };

        private SmallTailorBOD(SmallBulkEntry entry, BulkMaterialType mat, int amountMax, bool reqExceptional)
            : base(0x483, 0, amountMax, entry.Type, entry.Number, entry.Graphic, reqExceptional, mat)
        {
        }

        [Constructible]
        public SmallTailorBOD()
        {
            var useMaterials = Utility.RandomBool();
            var entries = useMaterials ? SmallBulkEntry.TailorLeather : SmallBulkEntry.TailorCloth;

            if (entries.Length <= 0)
            {
                return;
            }

            var amountMax = Utility.RandomList(10, 15, 20);

            var material = useMaterials
                ? GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances)
                : BulkMaterialType.None;

            var reqExceptional = Utility.RandomBool() || material == BulkMaterialType.None;
            var entry = entries.RandomElement();

            Hue = 0x483;
            AmountMax = amountMax;
            Type = entry.Type;
            Number = entry.Number;
            Graphic = entry.Graphic;
            RequireExceptional = reqExceptional;
            Material = material;
        }

        public SmallTailorBOD(
            int amountCur, int amountMax, Type type, int number, int graphic, bool reqExceptional,
            BulkMaterialType mat
        ) : base(0x483, amountCur, amountMax, type, number, graphic, reqExceptional, mat)
        {
        }

        public override int ComputeFame() => TailorRewardCalculator.Instance.ComputeFame(this);

        public override int ComputeGold() => TailorRewardCalculator.Instance.ComputeGold(this);

        public override RewardGroup GetRewardGroup() =>
            TailorRewardCalculator.Instance.LookupRewards(TailorRewardCalculator.Instance.ComputePoints(this));

        public static SmallTailorBOD CreateRandomFor(Mobile m)
        {
            SmallBulkEntry[] entries;
            var useMaterials = Utility.RandomBool();

            var theirSkill = m.Skills.Tailoring.Base;

            // Ugly, but the easiest leather BOD is Leather Cap which requires at least 6.2 skill.
            if (useMaterials && theirSkill >= 6.2)
            {
                entries = SmallBulkEntry.TailorLeather;
            }
            else
            {
                entries = SmallBulkEntry.TailorCloth;
            }

            if (entries.Length > 0)
            {
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
                        var check = GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances);

                        var skillReq = check switch
                        {
                            BulkMaterialType.DullCopper => 65.0,
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

                var excChance = 0.0;

                if (theirSkill >= 70.1)
                {
                    excChance = (theirSkill + 80.0) / 200.0;
                }

                var reqExceptional = excChance > Utility.RandomDouble();

                var system = DefTailoring.CraftSystem;

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

                if (validEntries.Count > 0)
                {
                    var entry = validEntries.RandomElement();
                    return new SmallTailorBOD(entry, material, amountMax, reqExceptional);
                }
            }

            return null;
        }
    }
}
