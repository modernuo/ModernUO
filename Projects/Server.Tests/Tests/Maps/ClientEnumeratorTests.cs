using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class ClientEnumeratorTests
{
    [Fact]
    public void ClientEnumerator_FiltersByBoundsAndOrder()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(100, 100, 32, 32);

        var clients = new (NetState, Mobile)[3];
        try
        {
            clients[0] = CreateClientWithMobile(map, new Point3D(105, 105, 0));
            clients[1] = CreateClientWithMobile(map, new Point3D(130, 130, 0));
            clients[2] = CreateClientWithMobile(map, new Point3D(90, 90, 0));

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsInBounds(rect))
            {
                found.Add(ns);
            }

            Assert.Equal(2, found.Count);
            Assert.All(found, ns => Assert.True(rect.Contains(ns.Mobile.Location)));
            Assert.Equal(new[] { clients[0].Item1, clients[1].Item1 }, found);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientEnumerator_SkipsNullMobiles()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(200, 200, 16, 16);

        var clients = new (NetState, Mobile)[3];
        try
        {
            clients[0] = CreateClientWithMobile(map, new Point3D(205, 205, 0));
            clients[1] = CreateClientWithMobile(map, new Point3D(206, 205, 0));
            clients[2] = CreateClientWithMobile(map, new Point3D(207, 205, 0));

            // Remove the mobile from the second client
            clients[1].Item2.Delete();
            clients[1].Item1.Mobile = null;

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsInBounds(rect))
            {
                found.Add(ns);
            }

            Assert.Equal(2, found.Count);
            Assert.Equal(new[] { clients[0].Item1, clients[2].Item1 }, found);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientEnumerator_RespectsMakeBoundsInclusiveFlag()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(300, 300, 1, 1);

        var clients = new (NetState, Mobile)[1];
        try
        {
            clients[0] = CreateClientWithMobile(map, new Point3D(301, 301, 0));

            var enumerator = map.GetClientsInBounds(rect, makeBoundsInclusive: true).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(clients[0].Item1, enumerator.Current);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientEnumerator_MapNullYieldsEmpty()
    {
        var enumerator = new Map.ClientBoundsEnumerable(null, Rectangle2D.Empty, false).GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void ClientEnumerator_ThrowsOnVersionChange()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(400, 400, 16, 16);

        var clients = new[]
        {
            CreateClientWithMobile(map, new Point3D(405, 405, 0)),
            CreateClientWithMobile(map, new Point3D(406, 405, 0))
        };

        try
        {
            var enumerator = map.GetClientsInBounds(rect).GetEnumerator();
            Assert.True(enumerator.MoveNext());

            clients[1].Item2.Delete();

            // Ref structs cannot be captured in lambdas, so we test the exception directly
            var exceptionThrown = false;
            try
            {
                enumerator.MoveNext();
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown, "Expected InvalidOperationException when collection version changes");
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientEnumerator_StepsAcrossSectors()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(500, 500, Map.SectorSize * 2, Map.SectorSize * 2);

        var clients = new[]
        {
            CreateClientWithMobile(map, new Point3D(rect.X + 1, rect.Y + 1, 0)),
            CreateClientWithMobile(map, new Point3D(rect.X + Map.SectorSize + 1, rect.Y + 1, 0)),
            CreateClientWithMobile(map, new Point3D(rect.X + Map.SectorSize + 1, rect.Y + Map.SectorSize + 1, 0))
        };

        try
        {
            var result = new List<NetState>();
            foreach (var ns in map.GetClientsInBounds(rect))
            {
                result.Add(ns);
            }

            Assert.Equal(new[] { clients[0].Item1, clients[1].Item1, clients[2].Item1 }, result);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientEnumerator_MapBoundsAreClamped()
    {
        var map = Map.Felucca;
        var width = map.Width;
        var height = map.Height;

        var rect = new Rectangle2D(width - Map.SectorSize - 2, height - Map.SectorSize - 2, Map.SectorSize * 2, Map.SectorSize * 2);

        var clients = new[]
        {
            CreateClientWithMobile(map, new Point3D(width - 2, height - 2, 0))
        };

        try
        {
            var enumerator = map.GetClientsInBounds(rect).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(clients[0].Item1, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientAtEnumerator_FiltersExactLocation()
    {
        var map = Map.Felucca;
        var location = new Point3D(600, 600, 0);
        var differentLocation = new Point3D(601, 600, 0);

        var clients = new (NetState, Mobile)[3];
        try
        {
            clients[0] = CreateClientWithMobile(map, location);
            clients[1] = CreateClientWithMobile(map, location);
            clients[2] = CreateClientWithMobile(map, differentLocation);

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsAt(location))
            {
                found.Add(ns);
            }

            Assert.Equal(2, found.Count);
            Assert.All(found, ns =>
            {
                Assert.NotNull(ns.Mobile);
                Assert.Equal(location.X, ns.Mobile.X);
                Assert.Equal(location.Y, ns.Mobile.Y);
            });
            Assert.Contains(clients[0].Item1, found);
            Assert.Contains(clients[1].Item1, found);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientAtEnumerator_SkipsNullMobiles()
    {
        var map = Map.Felucca;
        var location = new Point3D(650, 650, 0);

        var clients = new (NetState, Mobile)[3];
        try
        {
            clients[0] = CreateClientWithMobile(map, location);
            clients[1] = CreateClientWithMobile(map, location);
            clients[2] = CreateClientWithMobile(map, location);

            // Remove the mobile from the second client
            clients[1].Item2.Delete();
            clients[1].Item1.Mobile = null;

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsAt(location))
            {
                found.Add(ns);
            }

            Assert.Equal(2, found.Count);
            Assert.Equal(new[] { clients[0].Item1, clients[2].Item1 }, found);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientAtEnumerator_MapNullYieldsEmpty()
    {
        var enumerator = new Map.ClientAtEnumerable(null, new Point2D(0, 0)).GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void ClientAtEnumerator_ThrowsOnVersionChange()
    {
        var map = Map.Felucca;
        var location = new Point3D(750, 750, 0);

        var clients = new[]
        {
            CreateClientWithMobile(map, location),
            CreateClientWithMobile(map, location)
        };

        try
        {
            var enumerator = map.GetClientsAt(location).GetEnumerator();
            Assert.True(enumerator.MoveNext());

            clients[1].Item2.Delete();

            // Ref structs cannot be captured in lambdas, so we test the exception directly
            var exceptionThrown = false;
            try
            {
                enumerator.MoveNext();
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown, "Expected InvalidOperationException when collection version changes");
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientAtEnumerator_UsesDifferentPoint3DOverloads()
    {
        var map = Map.Felucca;
        var location = new Point3D(800, 800, 5);

        var clients = new (NetState, Mobile)[1];
        try
        {
            clients[0] = CreateClientWithMobile(map, location);

            // Test Point3D overload
            var found1 = new List<NetState>();
            foreach (var ns in map.GetClientsAt(location))
            {
                found1.Add(ns);
            }

            // Test (int, int) overload - should find the same client (Z is ignored)
            var found2 = new List<NetState>();
            foreach (var ns in map.GetClientsAt(location.X, location.Y))
            {
                found2.Add(ns);
            }

            // Test Point2D overload
            var found3 = new List<NetState>();
            foreach (var ns in map.GetClientsAt(new Point2D(location.X, location.Y)))
            {
                found3.Add(ns);
            }

            Assert.Single(found1);
            Assert.Equal(clients[0].Item1, found1[0]);
            Assert.Equal(found1, found2);
            Assert.Equal(found1, found3);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientEnumerator_GetClientsInRange()
    {
        var map = Map.Felucca;
        var center = new Point3D(900, 900, 0);
        const int range = 5;

        var clients = new (NetState, Mobile)[3];
        try
        {
            clients[0] = CreateClientWithMobile(map, new Point3D(902, 902, 0)); // Within range
            clients[1] = CreateClientWithMobile(map, new Point3D(898, 898, 0)); // Within range
            clients[2] = CreateClientWithMobile(map, new Point3D(910, 910, 0)); // Outside range (906+ is outside)

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsInRange(center, range))
            {
                found.Add(ns);
            }

            // GetClientsInRange uses a bounding rectangle, not circular distance
            // Range of 5 means rectangle from (895, 895) to (905, 905)
            Assert.Equal(2, found.Count);
            Assert.Contains(clients[0].Item1, found);
            Assert.Contains(clients[1].Item1, found);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientEnumerator_DeletedMobilesAreSkipped()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(1000, 1000, 16, 16);

        var clients = new (NetState, Mobile)[3];
        try
        {
            clients[0] = CreateClientWithMobile(map, new Point3D(1005, 1005, 0));
            clients[1] = CreateClientWithMobile(map, new Point3D(1006, 1005, 0));
            clients[2] = CreateClientWithMobile(map, new Point3D(1007, 1005, 0));

            // Delete the mobile (but not the NetState)
            clients[1].Item2.Delete();

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsInBounds(rect))
            {
                found.Add(ns);
            }

            // Should skip the client whose mobile was deleted
            Assert.Equal(2, found.Count);
            Assert.Contains(clients[0].Item1, found);
            Assert.Contains(clients[2].Item1, found);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    private class MockAccount : IAccount
    {
        public int TotalGold { get; }
        public int TotalPlat { get; }
        public bool DepositGold(int amount) => throw new NotImplementedException();
        public bool DepositPlat(int amount) => throw new NotImplementedException();
        public bool WithdrawGold(int amount) => throw new NotImplementedException();
        public bool WithdrawPlat(int amount) => throw new NotImplementedException();
        public long GetTotalGold() => throw new NotImplementedException();
        public int CompareTo(IAccount other) => throw new NotImplementedException();
        public string Username { get; }
        public string Email { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public int Length { get; }
        public int Limit { get; set; } = 6; // Default to 6 character slots
        public int Count { get; }

        private readonly Dictionary<int, Mobile> _mobiles = new();
        public Mobile this[int index]
        {
            get => _mobiles.GetValueOrDefault(index);
            set => _mobiles[index] = value;
        }

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public void Deserialize(IGenericReader reader) => throw new NotImplementedException();
        public byte SerializedThread { get; set; }
        public int SerializedPosition { get; set; }
        public int SerializedLength { get; set; }
        public void Serialize(IGenericWriter writer) => throw new NotImplementedException();
        public bool Deleted { get; }
        public void Delete() => throw new NotImplementedException();
        public bool TrySetUsername(string username) => throw new NotImplementedException();
        public void SetPassword(string password) => throw new NotImplementedException();
        public bool CheckPassword(string password) => throw new NotImplementedException();
    }

    private static (NetState, Mobile) CreateClientWithMobile(Map map, Point3D location)
    {
        // Create test NetState with real socket and buffers
        var ns = PacketTestUtilities.CreateTestNetState();

        // Assign a mock account to avoid null reference issues
        ns.Account = new MockAccount();

        // Use a unique serial for each mobile
        var serial = World.NewMobile;
        var mobile = new Mobile(serial);
        mobile.DefaultMobileInit();

        // Set the NetState on the mobile BEFORE moving it to the world
        // so the sector's client list gets updated properly
        ns.Mobile = mobile;
        mobile.NetState = ns;

        mobile.MoveToWorld(location, map);
        return (ns, mobile);
    }

    [Fact]
    public void ClientEnumerator_ZeroRangeReturnsOnlyCenter()
    {
        var map = Map.Felucca;
        var center = new Point3D(950, 950, 0);
        const int range = 0;

        var clients = new (NetState, Mobile)[2];
        try
        {
            clients[0] = CreateClientWithMobile(map, center);              // Exact center
            clients[1] = CreateClientWithMobile(map, new Point3D(951, 950, 0)); // 1 tile away

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsInRange(center, range))
            {
                found.Add(ns);
            }

            Assert.Single(found);
            Assert.Equal(clients[0].Item1, found[0]);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    [Fact]
    public void ClientEnumerator_NegativeRangeCreates1x1Bounds()
    {
        var map = Map.Felucca;
        var center = new Point3D(1050, 1050, 0);
        const int range = -5;

        var clients = new (NetState, Mobile)[2];
        try
        {
            clients[0] = CreateClientWithMobile(map, center);
            clients[1] = CreateClientWithMobile(map, new Point3D(1051, 1050, 0)); // 1 tile away

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsInRange(center, range))
            {
                found.Add(ns);
            }

            // With negative range creating a 1x1 bounds, only exact center matches
            Assert.Single(found);
            Assert.Equal(clients[0].Item1, found[0]);
        }
        finally
        {
            DeleteAll(clients);
        }
    }

    private static void DeleteAll((NetState state, Mobile m)[] clients)
    {
        for (var i = 0; i < clients.Length; i++)
        {
            if (clients[i].state != null)
            {
                clients[i].state.Mobile = null;
                clients[i].state.Dispose();
            }
            clients[i].m?.Delete();
        }
    }
}

