using ModernUO.Serialization;

namespace Server.Engines.BulkOrders
{
    [SerializationGenerator(0, false)]
    public partial class LargeTailorBOD : LargeBOD
    {
        public static double[] m_TailoringMaterialChances =
        {
            0.857421875, // None
            0.125000000, // Spined
            0.015625000, // Horned
            0.001953125  // Barbed
        };

        [Constructible]
        public LargeTailorBOD()
        {
            LargeBulkEntry[] entries;
            var useMaterials = false;

            switch (Utility.Random(14))
            {
                default:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Farmer);
                    break;
                case 1:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.FemaleLeatherSet);
                    useMaterials = true;
                    break;
                case 2:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.FisherGirl);
                    break;
                case 3:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Gypsy);
                    break;
                case 4:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.HatSet);
                    break;
                case 5:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Jester);
                    break;
                case 6:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Lady);
                    break;
                case 7:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.MaleLeatherSet);
                    useMaterials = true;
                    break;
                case 8:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Pirate);
                    break;
                case 9:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.ShoeSet);
                    useMaterials = Core.ML;
                    break;
                case 10:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.StuddedSet);
                    useMaterials = true;
                    break;
                case 11:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.TownCrier);
                    break;
                case 12:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Wizard);
                    break;
                case 13:
                    entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.BoneSet);
                    useMaterials = true;
                    break;
            }

            var hue = 0x483;
            var amountMax = Utility.RandomList(10, 15, 20, 20);
            var reqExceptional = Utility.RandomDouble() < 0.825;

            var material = useMaterials
                ? GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances)
                : BulkMaterialType.None;

            Hue = hue;
            AmountMax = amountMax;
            Entries = entries;
            RequireExceptional = reqExceptional;
            Material = material;
        }

        public LargeTailorBOD(int amountMax, bool reqExceptional, BulkMaterialType mat, LargeBulkEntry[] entries)
            : base(0x483, amountMax, reqExceptional, mat, entries)
        {
        }

        public override int ComputeFame() => TailorRewardCalculator.Instance.ComputeFame(this);

        public override int ComputeGold() => TailorRewardCalculator.Instance.ComputeGold(this);

        public override RewardGroup GetRewardGroup() =>
            TailorRewardCalculator.Instance.LookupRewards(TailorRewardCalculator.Instance.ComputePoints(this));
    }
}
