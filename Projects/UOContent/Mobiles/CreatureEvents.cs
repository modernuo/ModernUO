using ModernUO.CodeGeneratedEvents;

namespace Server.Mobiles;

// Hosts BaseCreature's generated events. They cannot live on BaseCreature itself: the
// events generator and the serialization generator each emit a [GeneratedCode] partial for
// the declaring type, and the attribute does not allow duplicates (CS0579).
public static partial class CreatureEvents
{
    [GeneratedEvent(nameof(CreatureDeathEvent))]
    public static partial void CreatureDeathEvent(BaseCreature bc);

    [GeneratedEvent(nameof(CreatureDeletedEvent))]
    public static partial void CreatureDeletedEvent(BaseCreature bc);
}
