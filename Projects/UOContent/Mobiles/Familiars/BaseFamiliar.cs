using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public abstract partial class BaseFamiliar : BaseCreature
{
    // Beyond this many tiles on open ground the familiar snaps to the caster instead of walking.
    public const int KeepUpRange = 10;

    public BaseFamiliar() : base(AIType.AI_Melee)
    {
        SetSpeed(0.1, 0.1);
    }

    protected override BaseAI ForcedAI => new FamiliarAI(this);

    public override bool BardImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override bool Commandable => false;
    public override bool IgnoreMobiles => true;

    public override bool PlayerRangeSensitive => false;

    // Assist aggro: engages the caster's target and anything hostile to the caster or itself.
    public virtual bool AssistsMaster => true;

    // Never muted by the ML stand-down rule; FamiliarAI decides whether it fights.
    public override bool StandsDownOnCommand => false;

    // A wounded familiar still keeps up.
    public override bool ReduceSpeedWithDamage => false;

    public override void OnThink()
    {
        base.OnThink();

        if (Deleted)
        {
            return;
        }

        var master = ControlMaster;

        if (master?.Deleted != false)
        {
            DropPackContents();
            Delete();
            return;
        }

        // Mirror the caster's visibility from our own state, not a cache of theirs: a step can
        // reveal us (Mobile.OnMove) and the mirror must re-assert.
        if (Hidden != master.Hidden)
        {
            Hidden = master.Hidden;
        }
    }

    // Walking, attacking, etc. must not give the caster's position away.
    public override void RevealingAction()
    {
        if (ControlMaster?.Hidden != true)
        {
            base.RevealingAction();
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from.Alive && Controlled && from == ControlMaster && from.InRange(this, 14))
        {
            list.Add(new ReleaseEntry());
        }
    }

    public virtual void BeginRelease(Mobile from)
    {
        if (!Deleted && Controlled && from == ControlMaster && from.CheckAlive())
        {
            EndRelease(from);
        }
    }

    public virtual void EndRelease(Mobile from)
    {
        if (from?.CheckAlive() != false && !Deleted && Controlled && from == ControlMaster)
        {
            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x3728,
                1,
                13,
                2100,
                3,
                5042,
                0
            );
            PlaySound(0x201);
            Delete();
        }
    }

    public virtual void DropPackContents()
    {
        var map = Map;
        var pack = Backpack;

        if (map == null || map == Map.Internal || pack == null)
        {
            return;
        }

        using var queue = pack.EnumerateItems();
        while (queue.Count > 0)
        {
            queue.Dequeue().MoveToWorld(Location, map);
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        DropPackContents();
        Delete();
    }

    private class ReleaseEntry : ContextMenuEntry
    {
        public ReleaseEntry() : base(6118, 14)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from.CheckAlive() && target is BaseFamiliar { Deleted: false, Controlled: true } familiar &&
                from == familiar.ControlMaster)
            {
                familiar.BeginRelease(from);
            }
        }
    }
}
