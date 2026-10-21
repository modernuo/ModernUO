using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;

namespace Server.Tests;

/// <summary>
/// Shared setup for the generic-command compiler tests: teleporters to run conditions, sorts and
/// distincts against, deleted on dispose, and property bindings resolved for reading.
/// </summary>
internal sealed class CommandTestHarness : IDisposable
{
    private readonly List<Item> _items = [];

    public T Track<T>(T item) where T : Item
    {
        _items.Add(item);
        return item;
    }

    public SkillTeleporter Teleporter(TextDefinition message) => Track(new SkillTeleporter { Message = message });

    public static Property Bind(Type type, string binding)
    {
        var prop = new Property(binding);
        prop.BindTo(type, PropertyAccess.Read);
        return prop;
    }

    public void Dispose()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Delete();
        }

        _items.Clear();
    }
}
