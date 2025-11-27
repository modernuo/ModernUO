using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Server.Network;
using Xunit;

namespace Server.Tests.Tests.Maps;

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

        var clients = new (NetState, Mobile)[3];
        try
        {
            clients[0] = CreateClientWithMobile(map, location);
            clients[1] = CreateClientWithMobile(map, location);
            clients[2] = CreateClientWithMobile(map, new Point3D(601, 600, 0)); // Different location

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsAt(location))
            {
                found.Add(ns);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(clients[0].Item1, found);
            Assert.Contains(clients[1].Item1, found);
            Assert.DoesNotContain(clients[2].Item1, found);
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
        var range = 5;

        var clients = new (NetState, Mobile)[3];
        try
        {
            clients[0] = CreateClientWithMobile(map, new Point3D(902, 902, 0)); // Within range
            clients[1] = CreateClientWithMobile(map, new Point3D(898, 898, 0)); // Within range
            clients[2] = CreateClientWithMobile(map, new Point3D(910, 910, 0)); // Outside range

            var found = new List<NetState>();
            foreach (var ns in map.GetClientsInRange(center, range))
            {
                found.Add(ns);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(clients[0].Item1, found);
            Assert.Contains(clients[1].Item1, found);
            Assert.DoesNotContain(clients[2].Item1, found);
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

    private static (NetState, Mobile) CreateClientWithMobile(Map map, Point3D location)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ns = new NetState(socket);

        var mobile = new Mobile((Serial)Utility.RandomMinMax(0x100u, 0xFFFu));
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, map);

        ns.Mobile = mobile;

        return (ns, mobile);
    }

    private static void DeleteAll((NetState, Mobile)[] clients)
    {
        for (var i = 0; i < clients.Length; i++)
        {
            if (clients[i].Item1 != null)
            {
                clients[i].Item1.Mobile = null;
                clients[i].Item1.Disconnect("Test cleanup");
            }
            clients[i].Item2?.Delete();
        }
    }
}

