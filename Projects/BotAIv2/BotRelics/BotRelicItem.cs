using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.BotAI.V2;

/// <summary>
/// A relic in the world: the Crown of Britannia, a guild's banner, the Bell of Minoc (07.10.2026, relics20). An engine item, so the
/// world save keeps it wherever it lies — on a hall's floor, in a carrier's pack, in a lair's chest — and the registry
/// (<see cref="BotRelics"/>, kept by <see cref="BotRelicStore"/>) says whose it is and since when.
///
/// <para>
/// <b>Not movable by a player's hand, blessed, never decaying, never sold.</b> <c>Movable = false</c> is the engine's own word for
/// "cannot be lifted": a player standing in a hall cannot pick the banner off the floor, and a container holding it cannot be looted
/// of it. Blessed keeps it in the carrier's pack when the carrier dies, so a relic is lost on the road only to a thief or a war, the
/// two ways Patrick named. No NPC's buy list names this type, so no counter will take it; the bots' own market prices only kinds it
/// knows. Everything that moves it is code: <see cref="BotRelics"/> moves it between pack, hall and lair.
/// </para>
///
/// <para>
/// <b>Found again by itself after a restart.</b> Each relic read back from the save adds itself to <see cref="Loaded"/>, and the
/// registry links them by key at the module's start; a second item with a key already linked is a stray and is deleted. So no walk
/// over the world's items is ever needed to find them.
/// </para>
/// </summary>
[SerializationGenerator(0)]
public partial class BotRelicItem : Item
{
    /// <summary>Which relic this is: <c>crown</c>, <c>bell</c>, <c>banner:The Hammer</c>. See <see cref="BotRelics"/>.</summary>
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _key;

    /// <summary>The relics read back from the save, waiting to be linked to the registry. Emptied at the module's start.</summary>
    public static readonly List<BotRelicItem> Loaded = [];

    [Constructible]
    public BotRelicItem(string key, int itemId) : base(itemId)
    {
        _key = key;
        Movable = false;
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override bool Decays => false;

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Loaded.Add(this);
    }
}
