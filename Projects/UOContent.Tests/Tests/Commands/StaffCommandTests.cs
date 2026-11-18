using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Commands;
using Server.Commands.Generic;
using Server.Engines.Help;
using Server.Items;
using Server.Menus;
using Server.Mobiles;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests.Commands;

[Collection("Sequential UOContent Tests")]
public class StaffCommandTests : IDisposable
{
    private readonly List<IEntity> _created = [];
    private readonly List<NetState> _states = [];

    public void Dispose()
    {
        for (var i = _created.Count - 1; i >= 0; i--)
        {
            if (_created[i] is Mobile m)
            {
                m.NetState = null;
            }

            _created[i].Delete();
        }

        for (var i = 0; i < _states.Count; i++)
        {
            _states[i].Dispose();
        }
    }

    private T Track<T>(T entity) where T : IEntity
    {
        _created.Add(entity);
        return entity;
    }

    private PlayerMobile Player(AccessLevel level, bool connected = false)
    {
        var player = Track(new PlayerMobile { Player = true, Body = 400, AccessLevel = level });
        player.AddItem(new Backpack());
        player.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);

        if (connected)
        {
            var ns = PacketTestUtilities.CreateTestNetState();
            _states.Add(ns);
            ns.Mobile = player;
            player.NetState = ns;
        }

        return player;
    }

    private sealed class ProbeCommand : BaseCommand
    {
        public int Runs;

        public ProbeCommand()
        {
            Commands = ["Probe"];
            AccessLevel = AccessLevel.Counselor;
            ObjectTypes = ObjectTypes.Both;
            Supports = CommandSupport.Single;
        }

        public override void Execute(CommandEventArgs e, object obj) => Runs++;
    }

    [Fact]
    public void SerialAccessorRefusesItemsHeldByHigherStaff()
    {
        var gm = Player(AccessLevel.GameMaster, true);
        var admin = Player(AccessLevel.Administrator);
        var item = Track(new Item(0x1));
        admin.Backpack.DropItem(item);

        var probe = new ProbeCommand();
        var impl = new SerialCommandImplementor();
        impl.Commands["Probe"] = probe;

        string[] args = [item.Serial.Value.ToString(), "Probe"];
        impl.Execute(new CommandEventArgs(gm, "Serial", string.Join(' ', args), args));

        Assert.Equal(0, probe.Runs);
    }
}
