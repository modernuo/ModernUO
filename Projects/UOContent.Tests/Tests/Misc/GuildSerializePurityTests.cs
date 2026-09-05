using System;
using System.IO;
using Server;
using Server.Guilds;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class GuildSerializePurityTests
{
    private static PlayerMobile CreatePlayer()
    {
        var m = new PlayerMobile(World.NewMobile);
        m.DefaultMobileInit();
        m.MoveToWorld(new Point3D(1500, 1500, 0), Map.Felucca);
        return m;
    }

    [Fact]
    public void SerializeDoesNotRecalculateFealtyOrMutateState()
    {
        var leader = CreatePlayer();
        var guild = new Guild(leader, "Purity Test Guild", "PTG");
        var staleFealty = Core.Now - TimeSpan.FromDays(3);
        guild.LastFealty = staleFealty;

        var writer = new BufferWriter(true);
        guild.Serialize(writer);

        Assert.Equal(staleFealty, guild.LastFealty);
        Assert.Same(leader, guild.Leader);

        var first = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
        writer.Seek(0, SeekOrigin.Begin);
        guild.Serialize(writer);
        var second = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();

        Assert.Equal(first, second);

        writer.Close();
        guild.Disband();
        leader.Delete();
    }

    [Fact]
    public void RunMaintenanceRecalculatesStaleFealty()
    {
        var leader = CreatePlayer();
        var guild = new Guild(leader, "Maintenance Test Guild", "MTG");
        guild.LastFealty = Core.Now - TimeSpan.FromDays(3);

        guild.RunMaintenance();

        Assert.True(Core.Now - guild.LastFealty < TimeSpan.FromMinutes(1));

        guild.Disband();
        leader.Delete();
    }
}
