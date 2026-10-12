using ModernUO.Serialization;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The door a guild puts into the doorway of the town building it settles in: the engine's own dark wooden door, which
/// opens for the guild's members and for nobody else.
///
/// <para>
/// <b>Keyed to the guild by name rather than locked, and the difference is what lets the members in at all.</b> A locked
/// door in this era wants its key in the pack (<c>BaseDoor.Use</c>), and a key is lost with the pack that held it at the next
/// wipe — the reason <c>BotEstate.Unlock</c> opens every hall's doors for ever. It is also a wall to the planner:
/// <c>BotStep.BlockedByItems</c> counts a locked shut door as one for every bot, members included, so a locked guild house is
/// a house its own guild cannot walk home into. So the door is left unlocked in the engine's terms and refuses at the
/// handle instead: <see cref="Use"/> opens it for a member, for staff and for anybody already standing inside (nobody is
/// shut in), and says "That is locked" to everybody else, as the engine's own locked door does.
/// </para>
///
/// <para>
/// <b>What this does not stop, said plainly:</b> a door a member has opened stays open for the engine's twenty seconds, and
/// anybody may walk through it while it does; and the planner, which cannot know who is asking, still draws a stranger's
/// path through it — the stranger is refused at the handle and its walk gives up there. A building with one doorway is a
/// dead end, so no through-road runs across one.
/// </para>
///
/// <para>
/// The art, sounds and facings are exactly <see cref="DarkWoodDoor"/>'s, which is what the engine's own door generator puts
/// into every doorway it fills (<c>DoorGenerator.AddDoor</c>); the facing is chosen the way that generator chooses it. See
/// <see cref="BotGuildHouses"/>.
/// </para>
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotGuildDoor : BaseDoor
{
    /// <summary>The guild whose door this is, by name: the name outlives the guild object a world reload replaces.</summary>
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _guild;

    [Constructible]
    public BotGuildDoor(DoorFacing facing, string guild) : base(
        0x6A5 + 2 * (int)facing,
        0x6A6 + 2 * (int)facing,
        0xEA,
        0xF1,
        GetOffset(facing)
    ) => _guild = guild;

    public override void Use(Mobile from)
    {
        if (from != null && !Open && !BotGuildHouses.MayOpen(this, from))
        {
            // The engine's own words for a locked door, said the engine's way.
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502503); // That is locked.
            BotGuildHouses.Refused(from);

            return;
        }

        base.Use(from);
    }
}
