using Server.Spells.Third;

namespace Server.Items
{
    public class FireballWand : BaseWand
    {
        [Constructible]
        public FireballWand() : base(WandEffect.Fireball, 5, Core.ML ? 109 : 15)
        {
        }

        public FireballWand(Serial serial) : base(serial)
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
            Cast(new FireballSpell(from, this));
        }
    }
}
