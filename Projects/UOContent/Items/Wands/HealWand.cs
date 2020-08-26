using Server.Spells.First;

namespace Server.Items
{
    public class HealWand : BaseWand
    {
        [Constructible]
        public HealWand() : base(WandEffect.Healing, 10, Core.ML ? 109 : 25)
        {
        }

        public HealWand(Serial serial) : base(serial)
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
            Cast(new HealSpell(from, this));
        }
    }
}
