using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.Tests.Mobiles.AI;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Timer-wheel driven (the real AITimer thinks and moves) against live Trammel statics.
[Collection("Sequential Pathfinding Tests")]
public class FamiliarAITests : IDisposable
{
    // NPCSpeeds is not configured in the fixture; pin the speeds.
    private sealed class Wolf : DarkWolfFamiliar
    {
        public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.1; }
    }

    private sealed class Bat : VampireBatFamiliar
    {
        public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.1; }
    }

    private sealed class Wisp : ShadowWispFamiliar
    {
        public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.1; }
    }

    private sealed class Adder : DeathAdder
    {
        public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.1; }
    }

    private sealed class Minion : HordeMinionFamiliar
    {
        public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.1; }
    }

    private sealed class EnemyStub : Mobile
    {
        public EnemyStub()
        {
            Body = 0x190;
            Str = 100;
        }
    }

    private static readonly Map _map = Map.Maps[1];
    private readonly List<IEntity> _created = new();

    public FamiliarAITests()
    {
        TileDataRequirement.SkipIfMissing();
        Core._tickCount = 0;
        Timer.Init(0);
    }

    public void Dispose()
    {
        for (var i = _created.Count - 1; i >= 0; i--)
        {
            _created[i].Delete();
        }
    }

    private static Point3D At(int x, int y)
    {
        _map.GetAverageZ(x, y, out _, out var z, out _);
        return new Point3D(x, y, (sbyte)z);
    }

    private PlayerMobile Master(int x, int y)
    {
        var p = new PlayerMobile(World.NewMobile);
        p.DefaultMobileInit();
        p.Player = true;
        p.Body = 0x190;
        p.Str = p.Dex = p.Int = 100;
        p.AddItem(new Backpack());
        p.MoveToWorld(At(x, y), _map);
        _created.Add(p);
        return p;
    }

    private BaseFamiliar Familiar(int kind, PlayerMobile master, int x, int y)
    {
        BaseFamiliar f = kind switch
        {
            0 => new Wolf(),
            1 => new Bat(),
            2 => new Wisp(),
            3 => new Adder(),
            _ => new Minion()
        };

        Assert.True(BaseCreature.Summon(f, master, At(x, y), -1, TimeSpan.FromHours(1)));
        _created.Add(f);
        return f;
    }

    private EnemyStub Enemy(int x, int y)
    {
        var e = new EnemyStub();
        e.MoveToWorld(At(x, y), _map);
        _created.Add(e);
        return e;
    }

    // 8ms lockstep keeps the wheel and Core.TickCount in sync.
    private static void RunFor(long ms)
    {
        var deadline = Core._tickCount + ms;

        while (Core._tickCount - deadline < 0)
        {
            Core._tickCount += 8;
            Timer.Slice(Core._tickCount);
        }
    }

    private static bool RunUntil(Func<bool> condition, long maxMs)
    {
        var deadline = Core._tickCount + maxMs;

        while (Core._tickCount - deadline < 0)
        {
            if (condition())
            {
                return true;
            }

            Core._tickCount += 8;
            Timer.Slice(Core._tickCount);
        }

        return condition();
    }

    [SkippableFact]
    public void UsesFamiliarAI_AndStaysOnComeOrder()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1500, 1600);

        Assert.IsType<FamiliarAI>(f.AIObject);
        Assert.Equal(OrderType.Come, f.ControlOrder);
        Assert.Equal(0.1, f.ActiveSpeed);
        Assert.Equal(0.1, f.PassiveSpeed);
    }

    [SkippableFact]
    public void Follows_WithoutBacktracking()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1500, 1600);
        RunFor(400); // past the activation spread
        p.MoveToWorld(At(1494, 1600), _map);

        var best = f.GetDistanceToSqrt(p);
        var regressed = false;
        var arrived = RunUntil(
            () =>
            {
                var d = f.GetDistanceToSqrt(p);

                if (d > best + 0.01)
                {
                    regressed = true;
                }

                best = Math.Min(best, d);
                return f.InRange(p, 1);
            },
            3000
        );

        Assert.True(arrived, "familiar must reach range 1 of its master");
        Assert.False(regressed, "familiar must never step away from the master while following");
        Assert.Equal(OrderType.Come, f.ControlOrder);
        Assert.Equal(Point3D.Zero, f.Home);
    }

    [SkippableFact]
    public void Orders_AreInert()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1500, 1600);
        RunFor(400);

        f.IssueOrder(OrderType.Stay, p);

        Assert.Equal(Point3D.Zero, f.Home);
        Assert.Equal(0.1, f.CurrentSpeed);

        p.MoveToWorld(At(1495, 1600), _map);
        Assert.True(RunUntil(() => f.InRange(p, 1), 3000), "a familiar under a Stay order still follows");
    }

    [SkippableTheory]
    [InlineData(0, true)]  // dark wolf
    [InlineData(1, true)]  // vampire bat
    [InlineData(2, false)] // shadow wisp
    [InlineData(3, false)] // death adder
    [InlineData(4, true)]  // horde minion
    public void AssistsMastersTarget_OnlyIfCombatCapable(int kind, bool assists)
    {
        var p = Master(1500, 1600);
        var f = Familiar(kind, p, 1501, 1600);
        var e = Enemy(1495, 1600);
        RunFor(400);

        p.Warmode = true;
        p.Combatant = e;

        if (assists)
        {
            Assert.True(
                RunUntil(() => f.Combatant == e && f.InRange(e, f.RangeFight), 4000),
                "a combat familiar must engage the caster's target"
            );
            Assert.True(f.Warmode);
            Assert.Equal(OrderType.Come, f.ControlOrder);
        }
        else
        {
            RunFor(2000);
            Assert.Null(f.Combatant);
            Assert.False(f.Warmode);
            Assert.True(f.InRange(p, 1), "a non-combat familiar stays with the caster");
        }
    }

    [SkippableFact]
    public void DropsTarget_WhenMasterHides()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1501, 1600);
        var e = Enemy(1495, 1600);
        RunFor(400);
        p.Warmode = true;
        p.Combatant = e;
        Assert.True(RunUntil(() => f.Combatant == e, 2000));

        p.Hidden = true;
        Assert.True(RunUntil(() => f.Combatant == null && f.Hidden, 1000));
    }

    [SkippableFact]
    public void Leash_NeverEngagesATargetFarFromTheMaster()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1501, 1600);
        var e = Enemy(1500 - f.RangePerception - 4, 1600); // beyond the leash
        RunFor(400);
        p.Warmode = true;
        p.Combatant = e;
        RunFor(4000);

        Assert.Null(f.Combatant);
        Assert.True(f.InRange(p, 1), "assist must not carry the familiar past the leash");
    }

    [SkippableFact]
    public void Retaliates_IfCombatCapable_AndStaysOnComeOrder()
    {
        var p = Master(1500, 1600);
        var wolf = Familiar(0, p, 1501, 1600);
        var wisp = Familiar(2, p, 1499, 1600);
        var e = Enemy(1503, 1600);
        RunFor(400);

        // Combatant setter → DoHarmful → AggressiveAction with ChangingCombatant: the path that
        // issues a stand-down pet an Attack order.
        e.Combatant = wolf;
        Assert.True(RunUntil(() => wolf.Combatant == e, 1000), "a combat familiar fights back");
        Assert.Equal(OrderType.Come, wolf.ControlOrder);

        e.Combatant = wisp;
        RunFor(1000);
        Assert.Null(wisp.Combatant);
        Assert.False(wisp.Warmode);
    }

    [SkippableFact]
    public void KeepUp_SnapsWhenFarOnOpenGround()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1500, 1600);
        RunFor(400);

        p.MoveToWorld(At(1500 - BaseFamiliar.KeepUpRange - 2, 1600), _map);
        RunFor(300); // one think with a greedy step

        Assert.True(f.InRange(p, 1), "familiar must snap adjacent to a master 12 tiles away on open ground");
    }

    [SkippableFact]
    public void KeepUp_DoesNotSnapWhileRouting()
    {
        // Britain inn L-desk: master north, familiar south; ~17-step detour.
        var p = Master(1494, 1605);
        var f = Familiar(0, p, 1493, 1614);
        p.MoveToWorld(new Point3D(1494, 1605, 21), _map);
        f.MoveToWorld(new Point3D(1493, 1614, 20), _map);
        Server.Engines.Pathing.Cache.StepCache.Instance.Clear();

        // Observed from the first tick so an early snap cannot hide behind "arrived".
        var teleported = false;
        var last = f.Location;
        var arrived = RunUntil(
            () =>
            {
                if (f.Location != last && !f.InRange(last, 1))
                {
                    teleported = true;
                }

                last = f.Location;
                return f.InRange(p, 1);
            },
            8000
        );

        Assert.True(arrived, "familiar walks the detour");
        Assert.False(teleported, "a working detour must not be short-circuited by a snap");
    }

    [SkippableFact]
    public void KeepUp_SnapsAfterGiveUp()
    {
        var p = Master(1500, 1596);
        var f = Familiar(0, p, 1500, 1602);

        // 5x5 ring, open 3x3 interior: a landing tile exists, no route reaches it.
        var id = ApproachTargetTests.FirstImpassableItemId();
        Assert.NotEqual<ushort>(0, id);

        for (var x = 1498; x <= 1502; x++)
        {
            for (var y = 1594; y <= 1598; y++)
            {
                if (x is > 1498 and < 1502 && y is > 1594 and < 1598)
                {
                    continue;
                }

                _map.GetAverageZ(x, y, out _, out var rz, out _);
                _created.Add(new Item(World.NewItem) { ItemID = id, Map = _map, Location = new Point3D(x, y, (sbyte)rz) });
            }
        }

        Server.Engines.Pathing.Cache.StepCache.Instance.Clear();
        RunFor(400);

        Assert.True(
            RunUntil(() => f.InRange(p, 1), 15000),
            "after giving up on an unreachable master the familiar snaps to it"
        );
    }

    [SkippableFact]
    public void MirrorsHidden_EvenWhileWalking()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1500, 1600);
        RunFor(400);

        p.Hidden = true;
        p.MoveToWorld(At(1495, 1600), _map);
        Assert.True(RunUntil(() => f.Hidden, 500));

        var stayedHidden = true;
        var arrived = RunUntil(
            () =>
            {
                stayedHidden &= f.Hidden;
                return f.InRange(p, 1);
            },
            3000
        );

        Assert.True(arrived);
        Assert.True(stayedHidden, "steps must not strip the mirror");

        p.Hidden = false;
        Assert.True(RunUntil(() => !f.Hidden, 500));
    }

    [SkippableFact]
    public void Herding_TargetLocation_WinsAndClears()
    {
        var p = Master(1500, 1600);
        var f = Familiar(4, p, 1500, 1600);
        RunFor(400);

        var goal = new Point2D(1500, 1594);
        f.TargetLocation = goal;

        Assert.True(RunUntil(() => f.TargetLocation == null, 6000), "familiar walks to the herding target");
        Assert.True(f.InRange(goal, 1));
    }

    [SkippableFact]
    public void NonCombatFamiliar_RefusesAnyCombatant()
    {
        var p = Master(1500, 1600);
        var wisp = Familiar(2, p, 1501, 1600);
        var e = Enemy(1502, 1600);

        wisp.Combatant = e;
        Assert.Null(wisp.Combatant);
    }

    [SkippableFact]
    public void HiddenCaster_FamiliarRefusesRetaliation()
    {
        var p = Master(1500, 1600);
        var wolf = Familiar(0, p, 1501, 1600);
        var e = Enemy(1502, 1600);
        RunFor(400);
        p.Hidden = true;
        Assert.True(RunUntil(() => wolf.Hidden, 500));

        e.Combatant = wolf;

        // Synchronous: the veto is at the setter.
        Assert.Null(wolf.Combatant);
        Assert.False(wolf.Warmode);
        RunFor(500);
        Assert.Null(wolf.Combatant);
        Assert.True(wolf.Hidden);
    }

    [SkippableFact]
    public void LeftBehind_StandsDown()
    {
        var p = Master(1500, 1600);
        var wolf = Familiar(0, p, 1501, 1600);
        var e = Enemy(1495, 1600);
        RunFor(400);
        p.Warmode = true;
        p.Combatant = e;
        Assert.True(RunUntil(() => wolf.Combatant == e, 2000));

        p.MoveToWorld(new Point3D(1500, 1600, p.Z), Map.Felucca);
        Assert.True(RunUntil(() => wolf.Combatant == null && !wolf.Warmode, 500));
    }

    [SkippableFact]
    public void Herding_OutranksCombat_AndStandsDown()
    {
        var p = Master(1500, 1600);
        var f = Familiar(4, p, 1501, 1600);
        var e = Enemy(1502, 1600);
        RunFor(400);
        p.Warmode = true;
        p.Combatant = e;
        Assert.True(RunUntil(() => f.Combatant == e, 2000));

        // Caster visible and fighting: only herding clears this.
        var goal = new Point2D(1500, 1594);
        f.TargetLocation = goal;
        var distBefore = f.GetDistanceToSqrt(goal);
        RunFor(300);

        Assert.Null(f.Combatant);
        Assert.False(f.Warmode);
        Assert.NotNull(f.TargetLocation);
        Assert.True(f.GetDistanceToSqrt(goal) < distBefore, "the fetch makes progress while combat is set aside");
        Assert.Same(e, p.Combatant);
    }

    [SkippableFact]
    public void AssistsAgainstWhatTheCastersPetIsFighting()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1501, 1600);
        var e = Enemy(1496, 1600);
        var pet = new PetTestStub();
        pet.MoveToWorld(At(1497, 1600), _map);
        pet.SetControlMaster(p);
        _created.Add(pet);
        RunFor(400);

        // The pet attacks; the caster is credited indirectly without a Combatant.
        pet.Combatant = e;
        p.DoHarmful(e, true);
        e.Combatant = pet;
        Assert.Null(p.Combatant);

        Assert.True(
            RunUntil(() => f.Combatant == e && f.InRange(e, f.RangeFight), 4000),
            "familiar joins the fight the caster's pet is in"
        );
    }

    [SkippableFact]
    public void DefendsAnAttackedCaster_WhoseOwnCombatantExpired()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1501, 1600);
        var e = Enemy(1496, 1600);
        RunFor(400);

        e.Combatant = p;
        p.Combatant = null; // expired
        Assert.Null(p.Combatant);

        Assert.True(
            RunUntil(() => f.Combatant == e && f.InRange(e, f.RangeFight), 4000),
            "familiar defends the caster from a live aggressor"
        );
    }

    [SkippableFact]
    public void DropsATarget_ThatNoLongerFightsAnyone()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1501, 1600);
        var e = Enemy(1495, 1600);
        RunFor(400);
        p.Warmode = true;
        p.Combatant = e;
        Assert.True(RunUntil(() => f.Combatant == e, 2000));

        // Caster stops, target disengages.
        p.Combatant = null;
        p.Warmode = false;
        e.Combatant = null;

        Assert.True(RunUntil(() => f.Combatant == null && f.InRange(p, 1), 4000), "familiar disengages and returns");
    }

    [SkippableFact]
    public void AssistEnd_LeavesNoStaleMoveIntent()
    {
        var p = Master(1500, 1600);
        var f = Familiar(0, p, 1501, 1600);
        var e = Enemy(1495, 1600);
        RunFor(400);
        p.Warmode = true;
        p.Combatant = e;
        Assert.True(RunUntil(() => f.Combatant == e, 2000));

        // Assist ends with the familiar already beside the caster (MoveTo's arrival return).
        f.MoveToWorld(At(1501, 1600), _map);
        p.Combatant = null;
        p.Warmode = false;
        e.Combatant = null;
        Assert.True(RunUntil(() => f.Combatant == null, 500));

        Assert.False(f.AIObject.TryGetMoveWake(out _), "no pursuit may survive the stand-down");
    }
}
