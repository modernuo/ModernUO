using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.Mind;

/// <summary>
/// The body the debugger looks out of: one figure in a white robe that nobody in the world can see, cannot
/// be hurt, cannot hurt anything, and gets about by appearing somewhere else.
///
/// <para>
/// <b>It is not a <c>BotMobile</c>, and that is the first decision here rather than an implementation
/// detail.</b> Everything the population does to a bot — the clock, the auction, the ladder, the urges, the
/// kit, the revival — is keyed on that type. A debugger derived from it would be raised, outfitted, asked
/// what it wanted to do, counted in every census, and would appear in its own reports as one of the
/// subjects. An observer that shows up in its own measurements is not an observer. So this derives from
/// <see cref="PlayerMobile"/> directly, nothing in BotAIv2 has any way to reach it, and every count it
/// takes is a count of the population and not of the population plus itself.
/// </para>
///
/// <para>
/// <b>Invisible to everybody but an administrator, and it is the engine's own rule doing it.</b>
/// <c>Mobile.CanSee(Mobile)</c> lets a hidden mobile through only for a viewer whose access level is above
/// Player <em>and</em> at least the hidden one's own. Setting this body to
/// <see cref="AccessLevel.Administrator"/> therefore hides it from every player and from every counsellor,
/// game master and seer as well; the owner's own character sees it because Owner outranks Administrator.
/// Nothing here filters packets or overrides visibility, which matters: a hand-written rule would be a
/// second opinion about who sees what, and the two would disagree the first time the engine changed.
/// </para>
///
/// <para>
/// <b>And the same two flags are what keep it from disturbing the very thing it is watching.</b>
/// <c>CanMoveOver</c> in the fork's own movement implementation passes a mobile that is hidden and above
/// Player, so a bot never has to path around this one and can walk straight through the tile it is standing
/// on. That is not a nicety — an observer that blocks a doorway would manufacture the stalls it is here to
/// find, and every finding after that would be about itself. <see cref="PlayerMobile.OnAccessLevelChanged"/>
/// sets <c>IgnoreMobiles</c> from the same access level, which closes the other direction.
/// </para>
///
/// <para>
/// <b>Blessed, so it is outside every fight in both directions.</b> <c>Mobile.CanBeHarmful</c> refuses on
/// either party being blessed, so nothing can attack it and it cannot attack anything even by accident.
/// There is no combat code in this folder at all; the flag is the belt to that brace, and it is what makes
/// "it is not aggressive" a property of the engine rather than a promise made by this assembly.
/// </para>
/// </summary>
public class BotDebugger : PlayerMobile
{
    /// <summary>
    /// The robe's colour. Bright white, and it is the one thing about this body meant to be looked at:
    /// the only person who can see it at all is the one who went looking for it.
    /// </summary>
    public static int RobeHue { get; set; } = 0x481;

    /// <summary>Who may see it. Anything above this rank sees it too — see the note on the class.</summary>
    public static AccessLevel SeenBy { get; set; } = AccessLevel.Administrator;

    /// <summary>How many times it has moved itself across the world.</summary>
    public long Hops { get; private set; }

    public BotDebugger(Serial serial) : base(serial)
    {
    }

    public BotDebugger()
    {
    }

    /// <summary>
    /// Makes this body what it is. Separate from the constructor because a deserialised one must not be
    /// dressed again — it is about to be deleted, not used. See <see cref="BotVigil"/>.
    /// </summary>
    public void Awaken(string name) => Awaken(name, RobeHue);

    /// <summary>The same, in a robe of this hue: the squad tells its members apart by colour.</summary>
    public void Awaken(string name, int hue)
    {
        Name = name;

        // A player as far as the engine is concerned, for the same reason the bots are: a mobile without the
        // flag is deleted outright by the death path and reads as alive while it is a ghost. This one is
        // blessed and will never die, so the flag is precaution rather than cure — but the cure cost a whole
        // evening once, and precaution here costs a line.
        Player = true;

        Body = 0x190;
        Female = false;

        // The whole figure white rather than a skin tone: what is being dressed is not a person, and the one
        // pair of eyes that can see it should be able to tell at a glance which of the shapes in a crowd is
        // the one that is not part of the population.
        Hue = hue;
        HairItemID = 0x203C;
        HairHue = hue;

        Blessed = true;
        Hidden = true;
        AccessLevel = SeenBy;

        // Nothing regenerates, nothing starves, nothing poisons it. The timers would run for the life of the
        // shard and settle nothing.
        Hits = HitsMax;
        Stam = StamMax;
        Mana = ManaMax;

        AddItem(new Backpack { Movable = false });

        Dress(new Robe(hue));
        Dress(new Sandals(hue));
    }

    private void Dress(Item item)
    {
        if (item == null)
        {
            return;
        }

        item.Movable = false;
        item.LootType = LootType.Blessed;

        if (!EquipItem(item))
        {
            item.Delete();
        }
    }

    /// <summary>
    /// Appears somewhere else. The only way this body ever moves, and it takes no time and no path.
    ///
    /// <para>
    /// <b>A teleport rather than walking, and the reason is measurement rather than convenience.</b> An
    /// observer that walked would spend its life in the same roads, doors and stalls it is watching for, and
    /// would be subject to them: the one bot on the shard whose job is to notice that nobody can get out of
    /// a yard must not be able to get stuck in one. It also has to be able to be beside a bot on the far side
    /// of the map within the same second, because what is worth watching changes faster than anything can
    /// walk.
    /// </para>
    /// </summary>
    public bool Hover(Map map, Point3D where)
    {
        if (Deleted || map == null || map == Map.Internal)
        {
            return false;
        }

        if (Map == map && Location == where)
        {
            return false;
        }

        MoveToWorld(where, map);

        // Re-asserted rather than assumed: nothing in this folder ever reveals it, but a shard is a large
        // place and hiding is a state a great deal of content likes to change.
        Hidden = true;
        Hops++;

        return true;
    }

    /// <summary>Nothing about this body is worth a save; see the note in <see cref="BotVigil.Purge"/>.</summary>
    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        reader.ReadInt();
    }

    public override bool ShouldCheckStatTimers => false;

    public override string ToString() => $"{Name} the debugger";
}
