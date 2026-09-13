using System;
using System.Reflection;
using Server.Items;
using Server.Mobiles;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests;

[Collection("Sequential Pathfinding Tests")]
public class FamiliarAITests
{
    private sealed class SwingProbe : Club
    {
        public int Swings { get; private set; }
        public override TimeSpan OnSwing(Mobile attacker, Mobile defender, double damageBonus = 1.0)
        {
            Swings++;
            return TimeSpan.FromSeconds(1);
        }
    }
    private readonly ITestOutputHelper _output;
    public FamiliarAITests(ITestOutputHelper output) => _output = output;
    private sealed class Wolf : DarkWolfFamiliar { public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.11; } }
    private sealed class Bat : VampireBatFamiliar { public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.11; } }
    private sealed class Wisp : ShadowWispFamiliar { public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.11; } }
    private sealed class Adder : DeathAdder { public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.11; } }
    private sealed class Minion : HordeMinionFamiliar { public override void GetSpeeds(out double a, out double p) { a = 0.1; p = 0.11; } }

    private static Point3D At(int x, int y)
    {
        Map.Trammel.GetAverageZ(x, y, out _, out var z, out _);
        return new Point3D(x, y, (sbyte)z);
    }
    private static PlayerMobile Player()
    {
        Skip.If(!TestServerInitializer.TileDataLoaded, "Requires UO client map data.");
        var p = new PlayerMobile(World.NewMobile);
        p.DefaultMobileInit();
        p.Player = true;
        World.AddEntity(p);
        p.Body = 0x190;
        p.Str = p.Dex = p.Int = 100;
        p.AddItem(new Backpack());
        p.MoveToWorld(At(1500, 1600), Map.Trammel);
        return p;
    }
    private static BaseFamiliar Create(int kind) => kind switch
    {
        0 => new Wolf(), 1 => new Bat(), 2 => new Wisp(), 3 => new Adder(), _ => new Minion()
    };

    // Drive the same OnThink -> Obey phases as AITimer, making one movement opportunity
    // available at each synthetic tick. No production timers or saves are advanced.
    private void Tick(BaseFamiliar f, string label)
    {
        f.AIObject.AITimer.Stop();
        f.AIObject.NextMove = Core.TickCount;
        var before = f.Location;
        f.OnThink();
        var afterThink = f.Location;
        f.AIObject.Obey();
        _output.WriteLine($"{label}: {before} -> think {afterThink} -> obey {f.Location}; order={f.ControlOrder}, home={f.Home}, combat={f.Combatant?.Serial}, speed={f.CurrentSpeed}");
    }

    [SkippableTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TraceFollowAndCombat(int kind)
    {
        var old = Core.Expansion;
        var p = Player();
        Core.Expansion = Expansion.ML;
        var f = Create(kind);
        var enemy = new Mobile { Body = 0x190, Str = 100 };
        try
        {
            Assert.True(BaseCreature.Summon(f, p, At(1499, 1600), -1, TimeSpan.FromHours(1)));

            Tick(f, "summoned");

            // Also verify that familiars already stuck in Stay recover without resummoning.
            f.ControlOrder = OrderType.Stay;
            p.MoveToWorld(At(1494, 1600), Map.Trammel);
            for (var i = 0; i < 16; i++)
            {
                Tick(f, $"follow {i}");
            }
            Assert.True(f.InRange(p, 1), "Familiar must reach its owner.");
            // Place beside owner to isolate combat acquisition from the following defect.
            f.MoveToWorld(At(1495, 1600), Map.Trammel);
            enemy.MoveToWorld(At(1490, 1600), Map.Trammel);
            p.Warmode = true;
            p.Combatant = enemy;
            for (var i = 0; i < 8; i++)
            {
                Tick(f, $"combat {i}");
            }
            _output.WriteLine($"FINAL enemy distance={f.GetDistanceToSqrt(enemy)}, order={f.ControlOrder}");
            Assert.True(f.InRange(enemy, f.RangeFight), "Familiar must approach the owner target.");
            Assert.Same(enemy, f.Combatant);
            var probe = new SwingProbe();
            f.AddItem(probe);
            f.NextCombatTime = Core.TickCount;
            typeof(Mobile).GetMethod("CheckCombatTime", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(f, null);
            Assert.Equal(1, probe.Swings);
            probe.Delete();
            // A move-only wake must not pursue an old combat goal after the owner hides.
            f.MoveToWorld(At(1495, 1600), Map.Trammel);
            f.AIObject.NextMove = Core.TickCount;
            f.OnThink();
            p.MoveToWorld(At(f.X + 1, f.Y), Map.Trammel);
            p.Hidden = true;
            f.OnThink();
            var hiddenLocation = f.Location;
            f.AIObject.NextMove = Core.TickCount;
            f.AIObject.ContinueMove();
            Assert.Equal(hiddenLocation, f.Location);
            Assert.Null(f.Combatant);
            p.Hidden = false;
            p.MoveToWorld(At(1494, 1600), Map.Trammel);
            p.Combatant = null;
            for (var i = 0; i < 10; i++)
            {
                Tick(f, $"return {i}");
            }
            Assert.Null(f.Combatant);
            Assert.True(f.InRange(p, 1));
        }
        finally
        {
            f.Delete();
            enemy.Delete();
            p.Delete();
            Core.Expansion = old;
        }
    }

    [SkippableFact]
    public void TraceMinionGroundPickup()
    {
        var old = Core.Expansion;
        var p = Player();
        Core.Expansion = Expansion.ML;
        var f = new Minion();
        var gold = new Gold(50);
        try
        {
            Assert.True(BaseCreature.Summon(f, p, At(1499, 1600), -1, TimeSpan.FromHours(1)));
            gold.MoveToWorld(At(1499, 1600), Map.Trammel);
            Tick(f, "pickup");
            _output.WriteLine($"Gold parent={gold.Parent?.GetType().Name}, collected={gold.IsChildOf(f.Backpack)}, holding={f.Holding?.GetType().Name}");
            Assert.True(gold.IsChildOf(f.Backpack));
        }
        finally
        {
            gold.Delete();
            f.Delete();
            p.Delete();
            Core.Expansion = old;
        }
    }
}
