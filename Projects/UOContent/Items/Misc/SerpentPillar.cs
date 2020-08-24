using Server.Multis;

namespace Server.Items
{
    public class SerpentPillar : Item
    {
        [Constructible]
        public SerpentPillar() : this(null, new Rectangle2D(), false)
        {
        }

        public SerpentPillar(string word, Rectangle2D destination, bool active = true) : base(0x233F)
        {
            Movable = false;

            Active = active;
            Word = word;
            Destination = destination;
        }

        public SerpentPillar(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Word { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Destination { get; set; }

        public override bool HandlesOnSpeech => true;

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!e.Handled && from.InRange(this, 10) && e.Speech.ToLower() == Word)
            {
                BaseBoat boat = BaseBoat.FindBoatAt(from, from.Map);

                if (boat == null)
                    return;

                if (!Active)
                {
                    boat.TillerMan
                        ?.Say(502507); // Ar, Legend has it that these pillars are inactive! No man knows how it might be undone!

                    return;
                }

                Map map = from.Map;

                for (int i = 0; i < 5; i++) // Try 5 times
                {
                    int x = Utility.Random(Destination.X, Destination.Width);
                    int y = Utility.Random(Destination.Y, Destination.Height);
                    int z = map.GetAverageZ(x, y);

                    Point3D dest = new Point3D(x, y, z);

                    if (boat.CanFit(dest, map, boat.ItemID))
                    {
                        int xOffset = x - boat.X;
                        int yOffset = y - boat.Y;
                        int zOffset = z - boat.Z;

                        boat.Teleport(xOffset, yOffset, zOffset);

                        return;
                    }
                }

                boat.TillerMan?.Say(502508); // Ar, I refuse to take that matey through here!
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(Active);
            writer.Write(Word);
            writer.Write(Destination);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            Active = reader.ReadBool();
            Word = reader.ReadString();
            Destination = reader.ReadRect2D();
        }
    }
}
