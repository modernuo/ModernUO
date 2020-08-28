using Server.Spells.Fourth;

namespace Server.Items
{
    public class ManaDrainWand : BaseWand
    {
        [Constructible]
        public ManaDrainWand() : base(WandEffect.ManaDraining, 5, 30)
        {
        }

        public ManaDrainWand(Serial serial) : base(serial)
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
            Cast(new ManaDrainSpell(from, this));
        }
    }
}
