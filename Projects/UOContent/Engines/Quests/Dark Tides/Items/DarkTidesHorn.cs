using Server.Mobiles;

namespace Server.Engines.Quests.Necro
{
    public class DarkTidesHorn : HornOfRetreat
    {
        [Constructible]
        public DarkTidesHorn()
        {
            DestLoc = new Point3D(2103, 1319, -68);
            DestMap = Map.Malas;
        }

        public DarkTidesHorn(Serial serial) : base(serial)
        {
        }

        public override bool ValidateUse(Mobile from) => from is PlayerMobile pm && pm.Quest is DarkTidesQuest;

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
