using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Engines.AdvancedSearch;
using Xunit;

namespace UOContent.Tests;

// Fixtures are compiled with the test project's configuration (Debug), so these also pin that the
// classifier accepts unoptimized IL (store-to-local-then-return shapes).
public class PureGetterClassifierTests
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                     BindingFlags.Static;

    internal class Fixture
    {
        public static int Counter;
        public static int StaticValue = 3;

        private int _b = 1;
        private int _c = 2;
        private string _n = null;
        private int[] _arr = [1, 2];
        private List<int> _cache;
        private int _secs = 5;
        private int _f = 0;
        private int _g = 0;

        public int A { get; set; }
        public int B => _b;

        public int C
        {
            get { return _c; }
        }

        public string N => _n ?? "x";
        public int StaticRead => StaticValue;
        public int E => _arr.Length > 0 ? _arr[0] : 0;

        public int LazyInit
        {
            get
            {
                _cache ??= new List<int>();
                return _cache.Count;
            }
        }

        public string S => new string('a', 2);

        public int StaticWrite
        {
            get
            {
                Counter++;
                return Counter;
            }
        }

        public TimeSpan T => TimeSpan.FromSeconds(_secs);
        public int R => Random.Shared.Next();

        public int Ping(int depth) => depth <= 0 ? _f : Pong(depth - 1);
        public int Pong(int depth) => depth <= 0 ? _g : Ping(depth - 1);
    }

    internal class ImpureCycle
    {
        private int _f = 0;
        private int _g = 0;

        public int Ping(int depth) => depth <= 0 ? _f : Pong(depth - 1);

        public int Pong(int depth)
        {
            if (depth <= 0)
            {
                _g = 1;
                return _g;
            }

            return Ping(depth - 1);
        }
    }

    internal class ThisVirtualBase
    {
        public string Name => DefaultName;
        public virtual string DefaultName => "a";
    }

    internal class PureDerived : ThisVirtualBase
    {
        public override string DefaultName => "b";
    }

    internal class ImpureDerived : ThisVirtualBase
    {
        public override string DefaultName => new string('b', 1);
    }

    internal abstract class AbstractHolder
    {
        public abstract int Value { get; }
    }

    internal class ForeignImpure : AbstractHolder
    {
        public override int Value => new string('x', 1).Length;
    }

    internal class ForeignPure : AbstractHolder
    {
        public override int Value => 1;
    }

    internal class ForeignReader
    {
        private AbstractHolder _other = new ForeignPure();
        public int Read => _other.Value;
    }

    private static MethodInfo Getter(Type t, string name) => t.GetProperty(name, All)!.GetGetMethod(true)!;

    private static bool Pure(Type t, string name) => PureGetterClassifier.IsPure(Getter(t, name), t);

    [Fact]
    public void AutoProperty_IsPure() => Assert.True(Pure(typeof(Fixture), nameof(Fixture.A)));

    [Fact]
    public void ExpressionBodiedFieldRead_IsPure() => Assert.True(Pure(typeof(Fixture), nameof(Fixture.B)));

    [Fact]
    public void BlockBodiedFieldRead_IsPure() => Assert.True(Pure(typeof(Fixture), nameof(Fixture.C)));

    [Fact]
    public void NullCoalesce_IsPure() => Assert.True(Pure(typeof(Fixture), nameof(Fixture.N)));

    [Fact]
    public void StaticRead_IsPure() => Assert.True(Pure(typeof(Fixture), nameof(Fixture.StaticRead)));

    [Fact]
    public void ArrayRead_IsPure() => Assert.True(Pure(typeof(Fixture), nameof(Fixture.E)));

    [Fact]
    public void LazyInit_IsImpure() => Assert.False(Pure(typeof(Fixture), nameof(Fixture.LazyInit)));

    [Fact]
    public void Allocation_IsImpure() => Assert.False(Pure(typeof(Fixture), nameof(Fixture.S)));

    [Fact]
    public void StaticWrite_IsImpure() => Assert.False(Pure(typeof(Fixture), nameof(Fixture.StaticWrite)));

    [Fact]
    public void ThisVirtual_DevirtualizedPerRuntimeType()
    {
        var name = typeof(ThisVirtualBase).GetProperty(nameof(ThisVirtualBase.Name))!;

        Assert.True(PureGetterClassifier.IsPure([name], typeof(PureDerived)));
        Assert.False(PureGetterClassifier.IsPure([name], typeof(ImpureDerived)));
    }

    [Fact]
    public void ForeignVirtual_RequiresAllOverrides()
    {
        Assert.False(Pure(typeof(ForeignReader), nameof(ForeignReader.Read)));
    }

    [Fact]
    public void ForeignVirtual_AllPureOverridesIsPureAsChainLink()
    {
        // Link 1 uses all-overrides semantics, so the impure override poisons it for every runtime type.
        var value = typeof(AbstractHolder).GetProperty(nameof(AbstractHolder.Value))!;
        var read = typeof(ForeignReader).GetProperty(nameof(ForeignReader.Read))!;

        Assert.False(PureGetterClassifier.IsPure([read, value], typeof(ForeignReader)));
    }

    [Fact]
    public void MutualRecursion_IsPure()
    {
        var ping = typeof(Fixture).GetMethod(nameof(Fixture.Ping))!;
        var pong = typeof(Fixture).GetMethod(nameof(Fixture.Pong))!;

        Assert.True(PureGetterClassifier.IsPure(ping, typeof(Fixture)));
        Assert.True(PureGetterClassifier.IsPure(pong, typeof(Fixture)));
    }

    [Fact]
    public void MutualRecursion_WithStore_IsImpure()
    {
        var ping = typeof(ImpureCycle).GetMethod(nameof(ImpureCycle.Ping))!;
        var pong = typeof(ImpureCycle).GetMethod(nameof(ImpureCycle.Pong))!;

        Assert.False(PureGetterClassifier.IsPure(ping, typeof(ImpureCycle)));
        Assert.False(PureGetterClassifier.IsPure(pong, typeof(ImpureCycle)));
    }

    [Fact]
    public void AllowlistedBcl_IsPure() => Assert.True(Pure(typeof(Fixture), nameof(Fixture.T)));

    [Fact]
    public void UnknownBcl_IsImpure() => Assert.False(Pure(typeof(Fixture), nameof(Fixture.R)));

    [Fact]
    public void AbstractGetter_IsImpure()
    {
        Assert.False(Pure(typeof(AbstractHolder), nameof(AbstractHolder.Value)));
    }

    [Fact]
    public void ItemHue_IsPure()
    {
        Assert.True(PureGetterClassifier.IsPure([typeof(Item).GetProperty("Hue")], typeof(Item)));
    }
}
