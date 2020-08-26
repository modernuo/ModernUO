using Server.Items;
using Server.Mobiles;

namespace Server.Multis
{
    public class BankerCamp : BaseCamp
    {
        [Constructible]
        public BankerCamp() : base(0x1F6)
        {
        }

        public BankerCamp(Serial serial) : base(serial)
        {
        }

        public override void AddComponents()
        {
            BaseDoor west, east;

            AddItem(west = new LightWoodGate(DoorFacing.WestCW), -4, 4, 7);
            AddItem(east = new LightWoodGate(DoorFacing.EastCCW), -3, 4, 7);

            west.Link = east;
            east.Link = west;

            AddItem(new Sign(SignType.Bank, SignFacing.West), -5, 5, -4);

            AddMobile(new Banker(), 4, -4, 3, 7);
            AddMobile(new Banker(), 5, 4, -2, 0);
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
