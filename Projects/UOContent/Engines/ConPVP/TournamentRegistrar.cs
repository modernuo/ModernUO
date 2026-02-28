using System;
using ModernUO.Serialization;
using Server.Factions;
using Server.Mobiles;

namespace Server.Engines.ConPVP;

[SerializationGenerator(0, false)]
public partial class TournamentRegistrar : Banker
{
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [SerializableField(0)]
    private TournamentController _tournament;

    [Constructible]
    public TournamentRegistrar() =>
        Timer.DelayCall(TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(30.0), Announce_Callback);

    private void Announce_Callback()
    {
        var tourney = Tournament?.Tournament;

        if (tourney?.Stage == TournamentStage.Signup)
        {
            PublicOverheadMessage(
                MessageType.Regular,
                0x35,
                false,
                "Come one, come all! Do you aspire to be a fighter of great renown? Join this tournament and show the world your abilities."
            );
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        var tourney = Tournament?.Tournament;

        if (InRange(m, 4) && !InRange(oldLocation, 4) && tourney?.Stage == TournamentStage.Signup && m.CanBeginAction(this))
        {
            var ladder = Ladder.Instance;

            var entry = ladder?.Find(m);

            if (entry != null && Ladder.GetLevel(entry.Experience) < tourney.LevelRequirement)
            {
                return;
            }

            if (tourney.IsFactionRestricted && Faction.Find(m) == null)
            {
                return;
            }

            if (tourney.HasParticipant(m))
            {
                return;
            }

            PrivateOverheadMessage(
                MessageType.Regular,
                0x35,
                false,
                $"Hello m'{(m.Female ? "Lady" : "Lord")}. Dost thou wish to enter this tournament? You need only to write your name in this book.",
                m.NetState
            );
            m.BeginAction(this);
            Timer.StartTimer(TimeSpan.FromSeconds(10.0), () => ReleaseLock_Callback(m));
        }
    }

    public void ReleaseLock_Callback(Mobile m)
    {
        m.EndAction(this);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.DelayCall(TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(30.0), Announce_Callback);
    }
}
