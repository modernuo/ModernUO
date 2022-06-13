using ModernUO.Serialization;

namespace Server.Engines.BulkOrders
{
    [SerializationGenerator(0, false)]
    public partial class LargeSmithBOD : LargeBOD
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

        [Constructible]
        public LargeSmithBOD()
        {
            LargeBulkEntry[] entries;
            var useMaterials = true;

            var rand = Utility.Random(8);

            entries = rand switch
            {
                0 => LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargeRing),
                1 => LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargePlate),
                2 => LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargeChain),
                3 => LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargeAxes),
                4 => LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargeFencing),
                5 => LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargeMaces),
                6 => LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargePolearms),
                7 => LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargeSwords),
                _ => LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargeRing)
            };

            if (rand > 2 && rand < 8)
            {
                useMaterials = false;
            }

            var hue = 0x44E;
            var amountMax = Utility.RandomList(10, 15, 20, 20);
            var reqExceptional = Utility.RandomDouble() < 0.825;

            var material = useMaterials
                ? GetRandomMaterial(BulkMaterialType.DullCopper, m_BlacksmithMaterialChances)
                : BulkMaterialType.None;

            Hue = hue;
            AmountMax = amountMax;
            Entries = entries;
            RequireExceptional = reqExceptional;
            Material = material;
        }

        public LargeSmithBOD(int amountMax, bool reqExceptional, BulkMaterialType mat, LargeBulkEntry[] entries)
            : base(0x44E, amountMax, reqExceptional, mat, entries)
        {
        }

        public override int ComputeFame() => SmithRewardCalculator.Instance.ComputeFame(this);

        public override int ComputeGold() => SmithRewardCalculator.Instance.ComputeGold(this);

        public override RewardGroup GetRewardGroup() =>
            SmithRewardCalculator.Instance.LookupRewards(SmithRewardCalculator.Instance.ComputePoints(this));
    }
}
