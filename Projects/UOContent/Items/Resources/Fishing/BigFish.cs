using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(1, false)]
    public partial class BigFish : Item, ICarvable
    {
        [InvalidateProperties]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        [SerializableField(0)]
        private Mobile _fisher;

        [Constructible]
        public BigFish() : base(0x09CC)
        {
            // TODO: Find correct formula.  max on OSI currently 200, OSI dev says it's not 200 as max, and ~ 1/1,000,000 chance to get highest
            Weight = Utility.RandomMinMax(3, 200);
            Hue = Utility.RandomBool() ? 0x847 : 0x58C;
        }

        public override int LabelNumber => 1041112; // a big fish

        public void Carve(Mobile from, Item item)
        {
            ScissorHelper(from, new RawFishSteak(), Math.Max(16, (int)Weight) / 4, false);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Weight >= 20)
            {
                if (_fisher != null)
                {
                    list.Add(1070857, _fisher.Name); // Caught by ~1_fisherman~
                }

                list.Add(1070858, $"{(int)Weight}"); // ~1_weight~ stones
            }
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            Weight = Utility.RandomMinMax(3, 200);
        }
    }
}
