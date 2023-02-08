using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class SerpentPillar : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _active;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _word;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Rectangle2D _destination;

    [Constructible]
    public SerpentPillar() : this(null, new Rectangle2D(), false)
    {
    }

    public SerpentPillar(string word, Rectangle2D destination, bool active = true) : base(0x233F)
    {
        Movable = false;
        _active = active;
        _word = word;
        _destination = destination;
    }

    public override bool HandlesOnSpeech => true;

    public override void OnSpeech(SpeechEventArgs e)
    {
        var from = e.Mobile;

        if (!e.Handled && from.InRange(this, 10) && e.Speech.ToLower() == Word)
        {
            var boat = BaseBoat.FindBoatAt(from.Location, from.Map);

            if (boat == null)
            {
                return;
            }

            if (!Active)
            {
                // Ar, Legend has it that these pillars are inactive! No man knows how it might be undone!
                boat.TillerMan?.Say(502507);
                return;
            }

            var map = from.Map;

            for (var i = 0; i < 5; i++) // Try 5 times
            {
                var x = Utility.Random(Destination.X, Destination.Width);
                var y = Utility.Random(Destination.Y, Destination.Height);
                var z = map.GetAverageZ(x, y);

                var dest = new Point3D(x, y, z);

                if (boat.CanFit(dest, map, boat.ItemID))
                {
                    var xOffset = x - boat.X;
                    var yOffset = y - boat.Y;
                    var zOffset = z - boat.Z;

                    boat.Teleport(xOffset, yOffset, zOffset);

                    return;
                }
            }

            boat.TillerMan?.Say(502508); // Ar, I refuse to take that matey through here!
        }
    }
}
