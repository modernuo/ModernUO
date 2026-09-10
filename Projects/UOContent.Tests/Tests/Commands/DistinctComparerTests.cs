using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Commands.Generic;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands;

// The distinct comparer doubles as an IEqualityComparer: objects that compare equal on every
// listed property must hash the same, whatever shape the property is -- an int, a reference that
// may be null, a struct, or a chain with a null link in the middle.
[Collection("Sequential UOContent Tests")]
public class DistinctComparerTests : IDisposable
{
    private readonly List<Item> _items = [];

    public void Dispose()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Delete();
        }

        _items.Clear();
    }

    private SkillTeleporter Teleporter(Action<SkillTeleporter> setup = null)
    {
        var tp = new SkillTeleporter();
        setup?.Invoke(tp);
        _items.Add(tp);
        return tp;
    }

    private static IEqualityComparer<object> Comparer(params string[] bindings)
    {
        var props = new Property[bindings.Length];

        for (var i = 0; i < bindings.Length; i++)
        {
            props[i] = new Property(bindings[i]);
            props[i].BindTo(typeof(SkillTeleporter), PropertyAccess.Read);
        }

        return (IEqualityComparer<object>)DistinctCompiler.Compile<object>(typeof(SkillTeleporter), props);
    }

    [Fact]
    public void EqualObjectsHashTheSameAcrossPropertyShapes()
    {
        var comparer = Comparer("Hue", "Name", "Location", "Message.String");

        var one = Teleporter(tp =>
        {
            tp.Hue = 7;
            tp.Name = "Gate";
            tp.Location = new Point3D(1, 2, 3);
            tp.Message = TextDefinition.Of("Alpha");
        });

        var two = Teleporter(tp =>
        {
            tp.Hue = 7;
            tp.Name = "Gate";
            tp.Location = new Point3D(1, 2, 3);
            tp.Message = TextDefinition.Of("Alpha");
        });

        Assert.True(comparer.Equals(one, two));
        Assert.Equal(comparer.GetHashCode(one), comparer.GetHashCode(two));
    }

    [Fact]
    public void NullReferencesAndNullChainLinksHashWithoutThrowing()
    {
        var comparer = Comparer("Name", "Message.String");

        var blank = Teleporter();
        var alsoBlank = Teleporter();

        Assert.True(comparer.Equals(blank, alsoBlank));
        Assert.Equal(comparer.GetHashCode(blank), comparer.GetHashCode(alsoBlank));
    }

    [Fact]
    public void DifferingObjectsAreNotEqual()
    {
        var comparer = Comparer("Hue", "Location");

        var one = Teleporter(tp => tp.Hue = 1);
        var two = Teleporter(tp => tp.Hue = 2);

        Assert.False(comparer.Equals(one, two));
    }
}
