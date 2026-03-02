using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Engines.ConPVP;

[SerializationGenerator(0, false)]
public partial class TournamentBracketItem : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TournamentController _tournament;

    [Constructible]
    public TournamentBracketItem() : base(3774) => Movable = false;

    public override string DefaultName => "tournament bracket";

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that
        }
        else
        {
            var tourney = Tournament?.Tournament;

            if (tourney != null)
            {
                from.SendGump(new TournamentBracketGump(from, tourney, TourneyBracketGumpType.Index), true);
            }
        }
    }
}
