using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class FireRubyBracelet : GoldBracelet
    {
        [Constructible]
        public FireRubyBracelet()
            : base()
        {
            Weight = 1.0;

            BaseRunicTool.ApplyAttributesTo(this, true, 0, Utility.RandomMinMax(1, 4), 0, 100);

            if (Utility.Random(100) < 10)
                Attributes.RegenHits += 2;
            else
                Resistances.Fire += 10;
        }

        public override int LabelNumber => 1073454;// fire ruby bracelet
    }
}
