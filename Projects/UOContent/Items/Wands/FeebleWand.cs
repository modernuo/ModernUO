using Server.Spells.First;

namespace Server.Items
{
    public class FeebleWand : BaseWand
    {
        [Constructible]
        public FeebleWand() : base(WandEffect.Feeblemindedness, 5, 30)
        {
        }

        public FeebleWand(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public override void OnWandUse(Mobile from)
        {
            Cast(new FeeblemindSpell(from, this));
        }
    }
}
