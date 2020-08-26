using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
    public class UzeraanTurmoilHorn : HornOfRetreat
    {
        [Constructible]
        public UzeraanTurmoilHorn()
        {
            DestLoc = new Point3D(3597, 2582, 0);
            DestMap = Map.Trammel;
        }

        public UzeraanTurmoilHorn(Serial serial) : base(serial)
        {
        }

        public override bool ValidateUse(Mobile from) => from is PlayerMobile pm && pm.Quest is UzeraanTurmoilQuest;

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
