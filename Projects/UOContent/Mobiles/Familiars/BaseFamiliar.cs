using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public abstract partial class BaseFamiliar : BaseCreature
{
    // Open-ground distance beyond which the familiar snaps to the caster.
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

    // Joins the caster's fights; false never fights.
    public virtual bool AssistsMaster => true;

    // FamiliarAI decides whether it fights, not the ML stand-down rule.
    public override bool StandsDownOnCommand => false;

    // A wounded familiar still keeps up.
    public override bool ReduceSpeedWithDamage => false;

    // The one choke point for "never fights" / "not while the caster is hidden":
    // BaseCreature.AggressiveAction assigns Combatant unconditionally. GetCPA does not inherit.
    [CommandProperty(AccessLevel.GameMaster)]
    public override Mobile Combatant
    {
        get => base.Combatant;
        set
        {
            if (value != null && (!AssistsMaster || ControlMaster?.Hidden == true))
            {
                return;
            }

            base.Combatant = value;
        }
    }

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

        // Compare our own state: Mobile.OnMove reveals a stepping NPC.
        if (Hidden != master.Hidden)
        {
            Hidden = master.Hidden;

            if (Hidden)
            {
                Warmode = false; // nulls Combatant
            }
        }
    }

    // Nothing reveals a hidden caster's familiar.
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
