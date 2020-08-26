using Server.Spells.Fourth;

namespace Server.Items
{
    public class GreaterHealWand : BaseWand
    {
        [Constructible]
        public GreaterHealWand() : base(WandEffect.GreaterHealing, 1, Core.ML ? 109 : 5)
        {
        }

        public GreaterHealWand(Serial serial) : base(serial)
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
            Cast(new GreaterHealSpell(from, this));
        }
    }
}
