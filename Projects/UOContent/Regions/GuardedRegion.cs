using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Server.Collections;
using Server.Mobiles;

namespace Server.Regions;

public class GuardedRegion : BaseRegion
{
    private static readonly object[] m_GuardParams = new object[1];

    private readonly Dictionary<Mobile, GuardTimer> m_GuardCandidates = new();

    [JsonConstructor] // Don't include parent, since it is special
    public GuardedRegion(string name, Map map, int priority, params Rectangle3D[] area) :
        base(name, map, priority, area) => GuardType = DefaultGuardType;

    public GuardedRegion(string name, Map map, int priority, params Rectangle2D[] area) :
        base(name, map, priority, area) => GuardType = DefaultGuardType;

    public GuardedRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area) =>
        GuardType = DefaultGuardType;

    public GuardedRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area) => GuardType = DefaultGuardType;

    public GuardedRegion(string name, Map map, Region parent, int priority, Type guardType, params Rectangle3D[] area)
        : base(name, map, parent, priority, area) => GuardType = guardType ?? DefaultGuardType;

    public Type GuardType { get; set; }

    public bool GuardsDisabled { get; set; }

    public virtual bool AllowReds => Core.AOS;

    public virtual Type DefaultGuardType
    {
        get
        {
            if (Map == Map.Ilshenar || Map == Map.Malas)
            {
                return typeof(ArcherGuard);
            }

            return typeof(WarriorGuard);
        }
    }

    public virtual bool IsDisabled() => GuardsDisabled;

    /// <summary>
    /// Shard rule (08.10.2026): the guards answer a hostile creature that fights a player inside the town, whoever struck
    /// first. See <see cref="IsMenace"/>. Setting <c>guards.hostileCreatures</c>, on by default.
    /// </summary>
    public static bool GuardsHostileCreatures { get; set; } = true;

    /// <summary>
    /// Whether the rule also covers a player with a client (or that player's pet). Off by default: a player who lures or
    /// fights a monster in town keeps the stock rule and the kill. Setting <c>guards.hostileCreaturesForClients</c>.
    /// </summary>
    public static bool GuardsHostileCreaturesForClients { get; set; }

    public static void Configure()
    {
        GuardsHostileCreatures = ServerConfiguration.GetSetting("guards.hostileCreatures", true);
        GuardsHostileCreaturesForClients = ServerConfiguration.GetSetting("guards.hostileCreaturesForClients", false);

        CommandSystem.Register("CheckGuarded", AccessLevel.GameMaster, CheckGuarded_OnCommand);
        CommandSystem.Register("SetGuarded", AccessLevel.Administrator, SetGuarded_OnCommand);
        CommandSystem.Register("ToggleGuarded", AccessLevel.Administrator, ToggleGuarded_OnCommand);
    }

    [Usage("CheckGuarded"), Description("Returns a value indicating if the current region is guarded or not.")]
    private static void CheckGuarded_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var reg = from.Region.GetRegion<GuardedRegion>();

        if (reg == null)
        {
            from.SendMessage("You are not in a guardable region.");
        }
        else if (reg.GuardsDisabled)
        {
            from.SendMessage("The guards in this region have been disabled.");
        }
        else
        {
            from.SendMessage("This region is actively guarded.");
        }
    }

    [Usage("SetGuarded <true|false>"), Description("Enables or disables guards for the current region.")]
    private static void SetGuarded_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length == 1)
        {
            var reg = from.Region.GetRegion<GuardedRegion>();

            if (reg == null)
            {
                from.SendMessage("You are not in a guardable region.");
            }
            else
            {
                reg.GuardsDisabled = !e.GetBoolean(0);

                from.SendMessage(
                    reg.GuardsDisabled
                        ? "The guards in this region have been disabled."
                        : "The guards in this region have been enabled."
                );
            }
        }
        else
        {
            from.SendMessage("Format: SetGuarded <true|false>");
        }
    }

    [Usage("ToggleGuarded"), Description("Toggles the state of guards for the current region.")]
    private static void ToggleGuarded_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var reg = from.Region.GetRegion<GuardedRegion>();

        if (reg == null)
        {
            from.SendMessage("You are not in a guardable region.");
        }
        else
        {
            reg.GuardsDisabled = !reg.GuardsDisabled;

            from.SendMessage(
                reg.GuardsDisabled
                    ? "The guards in this region have been disabled."
                    : "The guards in this region have been enabled."
            );
        }
    }

    public static GuardedRegion Disable(GuardedRegion reg)
    {
        reg.GuardsDisabled = true;
        return reg;
    }

    public virtual bool CheckVendorAccess(BaseVendor vendor, Mobile from) =>
        from.AccessLevel >= AccessLevel.GameMaster || IsDisabled() || !from.Murderer;

    public override bool OnBeginSpellCast(Mobile m, ISpell s)
    {
        if (!IsDisabled() && !s.OnCastInTown(this))
        {
            m.SendLocalizedMessage(500946); // You cannot cast this in town!
            return false;
        }

        return base.OnBeginSpellCast(m, s);
    }

    public override bool AllowHousing(Mobile from, Point3D p) => false;

    public override void MakeGuard(Mobile focus)
    {
        m_GuardParams[0] = focus;

        try
        {
            GuardType.CreateInstance<object>(m_GuardParams);
        }
        catch
        {
            // ignored
        }
    }

    public override void OnEnter(Mobile m)
    {
        if (IsDisabled())
        {
            return;
        }

        if (!AllowReds && m.Murderer || IsMenace(m))
        {
            CheckGuardCandidate(m);
        }
    }

    public override void OnExit(Mobile m)
    {
    }

    public override void OnSpeech(SpeechEventArgs args)
    {
        base.OnSpeech(args);

        if (IsDisabled())
        {
            return;
        }

        if (args.Mobile.Alive && args.HasKeyword(0x0007)) // *guards*
        {
            CallGuards(args.Mobile.Location);
        }
    }

    public override void OnAggressed(Mobile aggressor, Mobile aggressed, bool criminal)
    {
        base.OnAggressed(aggressor, aggressed, criminal);

        if (!IsDisabled() && aggressor != aggressed && (criminal || IsMenace(aggressor) && IsShielded(aggressed)))
        {
            CheckGuardCandidate(aggressor);
        }
    }

    public override void OnGotBeneficialAction(Mobile helper, Mobile helped)
    {
        base.OnGotBeneficialAction(helper, helped);

        if (IsDisabled())
        {
            return;
        }

        var noto = Notoriety.Compute(helper, helped);

        if (helper != helped && noto is Notoriety.Criminal or Notoriety.Murderer)
        {
            CheckGuardCandidate(helper);
        }
    }

    public override void OnCriminalAction(Mobile m, bool message)
    {
        base.OnCriminalAction(m, message);

        if (!IsDisabled())
        {
            CheckGuardCandidate(m);
        }
    }

    public void CheckGuardCandidate(Mobile m)
    {
        if (!IsGuardCandidate(m))
        {
            return;
        }

        if (!m_GuardCandidates.TryGetValue(m, out var timer))
        {
            timer = new GuardTimer(m, m_GuardCandidates);
            timer.Start();

            m_GuardCandidates[m] = timer;
            m.SendLocalizedMessage(502275); // Guards can now be called on you!

            var map = m.Map;

            if (map == null)
            {
                return;
            }

            Mobile fakeCall = null;
            var prio = 0.0;

            foreach (var v in m.GetMobilesInRange(8))
            {
                if (v.Player || v == m || IsGuardCandidate(v) ||
                    !((v as BaseCreature)?.IsHumanInTown() ?? v.Body.IsHuman && v.Region.IsPartOf(this)))
                {
                    continue;
                }

                var dist = m.GetDistanceToSqrt(v);

                if (fakeCall == null || dist < prio)
                {
                    fakeCall = v;
                    prio = dist;
                }
            }

            if (fakeCall != null)
            {
                fakeCall.Say(Utility.Random(1013037, 16));

                BaseGuard.Spawn(fakeCall, m);
                timer.Stop();
                m_GuardCandidates.Remove(m);

                if (!IsAlwaysGuardCandidate(m))
                {
                    m.SendLocalizedMessage(502276); // Guards can no longer be called on you.
                }
            }
        }
        else
        {
            timer.Stop();
            timer.Start();
        }
    }

    public void CallGuards(Point3D p)
    {
        using var queue = PooledRefQueue<Mobile>.Create();

        foreach (var m in Map.GetMobilesInRange(p, 14))
        {
            if (!IsGuardCandidate(m))
            {
                continue;
            }

            // Dictionary members can be targeted regardless of region (guards reach outside).
            // Non-dictionary permanent candidates (reds, and hostile creatures fighting a player) must be in the region.
            if (m_GuardCandidates.ContainsKey(m) || m.Region.IsPartOf(this) && (IsAlwaysGuardCandidate(m) || IsMenace(m)))
            {
                if (m_GuardCandidates.Remove(m, out var timer))
                {
                    timer.Stop();
                }

                queue.Enqueue(m);
            }
        }

        while (queue.Count > 0)
        {
            var m = queue.Dequeue();
            BaseGuard.Spawn(this, m);

            if (!IsAlwaysGuardCandidate(m))
            {
                m.SendLocalizedMessage(502276); // Guards can no longer be called on you.
            }
        }
    }

    private bool IsAlwaysGuardCandidate(Mobile m) => !AllowReds && m.Murderer;

    public bool IsGuardCandidate(Mobile m)
    {
        if (m is BaseGuard || !m.Alive || m.AccessLevel > AccessLevel.Player || m.Blessed)
        {
            return false;
        }

        return (m as BaseCreature)?.IsInvulnerable != true && (IsAlwaysGuardCandidate(m) || m.Criminal || IsMenace(m));
    }

    /// <summary>Whether the guards could be called on <paramref name="m"/> here now: on the region's list, or a standing candidate inside it.</summary>
    public bool IsCallable(Mobile m) =>
        IsGuardCandidate(m) &&
        (m_GuardCandidates.ContainsKey(m) || m.Region.IsPartOf(this) && (IsAlwaysGuardCandidate(m) || IsMenace(m)));

    /// <summary>
    /// <b>A hostile creature fighting a player is a guard candidate (shard rule, 08.10.2026).</b> In this era the guards come
    /// only for a criminal or a murderer, and a creature is criminal only for harming someone innocent to it. A player stops
    /// being innocent to a creature the moment it strikes it, and while the creature keeps biting, every bite refreshes the
    /// player's own entry in the creature's aggressors (<c>Mobile.AggressiveAction</c>), so the player never becomes innocent
    /// to it again. A traveller who swung once on the road and ran to town was therefore never protected there. The rule:
    /// a creature that attacks on sight (fight mode Closest, Strongest or Weakest), wild (not tamed, not summoned by a
    /// player), not invulnerable, with a player or a player's pet for its combatant, may be called on like a criminal.
    /// Creatures that only fight back (Aggressor, Evil, None), such as bears and deer, are left to the old rule. Unless
    /// <see cref="GuardsHostileCreaturesForClients"/> is set, only for a player with no client (the shard's bots), so that
    /// a player at the keyboard keeps the stock rule. See <see cref="IsShielded"/>.
    /// </summary>
    public static bool IsMenace(Mobile m) =>
        GuardsHostileCreatures && IsHostileCreature(m) && m.Combatant is { Deleted: false, Alive: true } foe && IsShielded(foe);

    /// <summary>
    /// Whether the rule of <see cref="IsMenace"/> protects <paramref name="m"/>: a player or a player's pet, and, unless
    /// <see cref="GuardsHostileCreaturesForClients"/>, one whose player has no client.
    /// </summary>
    public static bool IsShielded(Mobile m)
    {
        if (!IsPlayerSide(m))
        {
            return false;
        }

        if (GuardsHostileCreaturesForClients)
        {
            return true;
        }

        var owner = m.Player ? m : m is BaseCreature bc ? bc.Controlled ? bc.ControlMaster : bc.SummonMaster : null;

        return owner?.NetState == null;
    }

    /// <summary>A wild creature that attacks on sight. See <see cref="IsMenace"/>.</summary>
    public static bool IsHostileCreature(Mobile m) =>
        m is BaseCreature { Deleted: false, Controlled: false, IsInvulnerable: false, InitialInnocent: false } bc &&
        bc.FightMode is FightMode.Closest or FightMode.Strongest or FightMode.Weakest &&
        (!bc.Summoned || bc.SummonMaster?.Player != true);

    /// <summary>A player, or a creature tamed by or summoned for one.</summary>
    public static bool IsPlayerSide(Mobile m) =>
        m.Player || m is BaseCreature bc && (bc.Controlled && bc.ControlMaster?.Player == true || bc.Summoned && bc.SummonMaster?.Player == true);

    private class GuardTimer : Timer
    {
        private readonly Mobile m_Mobile;
        private readonly Dictionary<Mobile, GuardTimer> m_Table;

        public GuardTimer(Mobile m, Dictionary<Mobile, GuardTimer> table) : base(TimeSpan.FromSeconds(15.0))
        {

            m_Mobile = m;
            m_Table = table;
        }

        protected override void OnTick()
        {
            if (m_Table.Remove(m_Mobile))
            {
                m_Mobile.SendLocalizedMessage(502276); // Guards can no longer be called on you.
            }
        }
    }
}
