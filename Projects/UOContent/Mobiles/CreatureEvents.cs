using ModernUO.CodeGeneratedEvents;

namespace Server.Mobiles;

// Hosts BaseCreature's generated events: two generators cannot both emit [GeneratedCode] on one type (CS0579).
public static partial class CreatureEvents
{
    [GeneratedEvent(nameof(CreatureDeathEvent))]
    public static partial void CreatureDeathEvent(BaseCreature bc);

    [GeneratedEvent(nameof(CreatureDeletedEvent))]
    public static partial void CreatureDeletedEvent(BaseCreature bc);
}
