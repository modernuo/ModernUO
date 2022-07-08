using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TwilightLantern : Lantern
    {
        [Constructible]
        public TwilightLantern() => Hue = Utility.RandomBool() ? 244 : 997;

        public override string DefaultName => "Twilight Lantern";

        public override bool AllowEquippedCast(Mobile from) => true;

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060482); // Spell Channeling
        }
    }
}
