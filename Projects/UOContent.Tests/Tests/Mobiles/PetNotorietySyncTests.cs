using System;
using System.Buffers;
using Server;
using Server.Accounting;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests.Mobiles;

// In ML a controlled pet shows its master's notoriety, so a viewer's client must be refreshed for the
// pet whenever its view of the master changes.
[Collection("Sequential UOContent Tests")]
public class PetNotorietySyncTests : IDisposable
{
    private static readonly Point3D _loc = new(1800, 1800, 0);

    private readonly MobileTestHarness _harness = new();
    private readonly NotorietyHandler _notorietyWas;
    private readonly Expansion _expansionWas;

    public PetNotorietySyncTests()
    {
        _notorietyWas = Notoriety.Handler;
        Notoriety.Handler = NotorietyHandlers.MobileNotoriety;
        _expansionWas = Core.Expansion;
        Core.Expansion = Expansion.ML;
    }

    public void Dispose()
    {
        _harness.Dispose();
        Notoriety.Handler = _notorietyWas;
        Core.Expansion = _expansionWas;
    }

    private PlayerMobile NewPlayer(Point3D location, NetState ns = null)
    {
        var pm = _harness.NewPlayer();
        pm.Player = true;

        if (ns != null)
        {
            ns.Account = new MockAccount();
            ns.Mobile = pm;
            pm.NetState = ns;
        }

        pm.MoveToWorld(location, Map.Felucca);
        return pm;
    }

    private TestCreature NewPet(PlayerMobile master)
    {
        var pet = _harness.NewCreature();
        pet.SetControlMaster(master);
        pet.MoveToWorld(new Point3D(master.X + 1, master.Y, master.Z), Map.Felucca);
        return pet;
    }

    private static int NotorietyRefreshes(NetState ns, Mobile m)
    {
        Span<byte> serial = stackalloc byte[4];
        new SpanWriter(serial).Write(m.Serial);

        var span = ns.SendBuffer.GetReadSpan();
        var count = 0;

        for (var i = 1; i <= span.Length - 4; i++)
        {
            if (span[i - 1] == 0x77 && span.Slice(i, 4).SequenceEqual(serial))
            {
                count++;
            }
        }

        return count;
    }

    private static void ClearSent(NetState ns) => ns.SendBuffer.CommitRead(ns.SendBuffer.ReadableBytes);

    private static void Flush(params Mobile[] mobiles)
    {
        for (var i = 0; i < mobiles.Length; i++)
        {
            mobiles[i].ProcessDelta();
        }
    }

    [Fact]
    public void MasterAttackingViewer_RefreshesThePet()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        try
        {
            var viewer = NewPlayer(_loc, ns);
            var master = NewPlayer(new Point3D(_loc.X + 2, _loc.Y, 0));
            var pet = NewPet(master);
            Flush(viewer, master, pet);

            var before = Notoriety.Compute(viewer, pet);
            ClearSent(ns);

            viewer.AggressiveAction(master);

            Assert.NotEqual(before, Notoriety.Compute(viewer, pet));
            Assert.Equal(1, NotorietyRefreshes(ns, pet));
        }
        finally
        {
            ns.Mobile = null;
            ns.Dispose();
        }
    }

    [Fact]
    public void MasterTurningCriminal_RefreshesThePet()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        try
        {
            var viewer = NewPlayer(_loc, ns);
            var master = NewPlayer(new Point3D(_loc.X + 2, _loc.Y, 0));
            var pet = NewPet(master);
            Flush(viewer, master, pet);
            ClearSent(ns);

            master.Criminal = true;
            Flush(master, pet);

            Assert.Equal(Notoriety.Criminal, Notoriety.Compute(viewer, pet));
            Assert.Equal(1, NotorietyRefreshes(ns, pet));
        }
        finally
        {
            ns.Mobile = null;
            ns.Dispose();
        }
    }

    [Fact]
    public void MasterAttackingOwnPet_RefreshesThePetForTheMaster()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        try
        {
            var master = NewPlayer(_loc, ns);
            var pet = NewPet(master);
            Flush(master, pet);

            var before = Notoriety.Compute(master, pet);
            ClearSent(ns);

            pet.AggressiveAction(master);

            Assert.NotEqual(before, Notoriety.Compute(master, pet));
            Assert.Equal(1, NotorietyRefreshes(ns, pet));
        }
        finally
        {
            ns.Mobile = null;
            ns.Dispose();
        }
    }
}
