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

    public static void Configure()
    {
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
        from.AccessLevel >= AccessLevel.GameMaster || IsDisabled() || from.Kills < 5;

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
        BaseGuard useGuard = null;
        foreach (var m in focus.GetMobilesInRange<BaseGuard>(8))
        {
            if (m.Focus == null)
            {
                useGuard = m;
                break;
            }
        }

        if (useGuard == null)
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
        else
        {
            useGuard.Focus = focus;
        }
    }

    public override void OnEnter(Mobile m)
    {
        if (IsDisabled())
        {
            return;
        }

        if (!AllowReds && (m.Kills >= 5 || m is BaseCreature { AlwaysMurderer: true }))
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

        if (!IsDisabled() && aggressor != aggressed && criminal)
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

                MakeGuard(m);
                timer.Stop();
                m_GuardCandidates.Remove(m);
                m.SendLocalizedMessage(502276); // Guards can no longer be called on you.
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
            if (!IsGuardCandidate(m) || !m.Region.IsPartOf(this) && !m_GuardCandidates.ContainsKey(m))
            {
                continue;
            }

            if (m_GuardCandidates.Remove(m, out var timer))
            {
                timer.Stop();
            }

            queue.Enqueue(m);
            break;
        }

        while (queue.Count > 0)
        {
            var m = queue.Dequeue();
            MakeGuard(m);
            m.SendLocalizedMessage(502276); // Guards can no longer be called on you.
        }
    }

    public bool IsGuardCandidate(Mobile m)
    {
        if (m is BaseGuard || !m.Alive || m.AccessLevel > AccessLevel.Player || m.Blessed)
        {
            return false;
        }

        var bc = m as BaseCreature;

        if (bc?.IsInvulnerable == true)
        {
            return false;
        }

        return !AllowReds && (m.Kills >= 5 || bc?.AlwaysMurderer == true) || m.Criminal;
    }

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
