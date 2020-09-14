using Server.Mobiles;

namespace Server.Engines.Quests.Necro
{
    public class VaultOfSecretsBarrier : Item
    {
        [Constructible]
        public VaultOfSecretsBarrier() : base(0x49E)
        {
            Movable = false;
            Visible = false;
        }

        public VaultOfSecretsBarrier(Serial serial) : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            if (m is PlayerMobile pm && pm.Profession == 4)
            {
                m.SendLocalizedMessage(1060188, "", 0x24); // The wicked may not enter!
                return false;
            }

            return base.OnMoveOver(m);
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
    }
}
