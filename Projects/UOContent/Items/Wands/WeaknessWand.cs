using Server.Spells.First;

namespace Server.Items
{
    public class WeaknessWand : BaseWand
    {
        [Constructible]
        public WeaknessWand() : base(WandEffect.Weakness, 5, 30)
        {
        }

        public WeaknessWand(Serial serial) : base(serial)
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
            Cast(new WeakenSpell(from, this));
        }
    }
}
