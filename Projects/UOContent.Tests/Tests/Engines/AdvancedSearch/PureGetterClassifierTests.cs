using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Threading;
using Server;
using Server.Engines.AdvancedSearch;
using Xunit;

namespace UOContent.Tests;

// Fixtures compile as Debug, so these also cover unoptimized IL (store-to-local-then-return).
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
        public int Cmp => _b.CompareTo(_c);
        public int R => Random.Shared.Next();

        public int Ping(int depth) => depth <= 0 ? _f : Pong(depth - 1);
        public int Pong(int depth) => depth <= 0 ? _g : Ping(depth - 1);
    }

    internal struct LocalStruct
    {
        public int X;

        public LocalStruct(int x)
        {
            X = x;
        }
    }

    internal class StructFixture
    {
        private int _x = 1;
        private int _y = 2;
        private int _z = 3;

        public Point3D Point => new Point3D(_x, _y, _z);
        public LocalStruct Local => new LocalStruct(_x);
        public int? Maybe => _x;
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

    internal abstract class PureBase
    {
        public abstract int Value { get; }
    }

    internal class PureOverrideOne : PureBase
    {
        public override int Value => 1;
    }

    internal class PureOverrideTwo : PureBase
    {
        public override int Value => 2;
    }

    internal class PureReader
    {
        private PureBase _other = new PureOverrideOne();
        public int Read => _other.Value;
        public PureBase Other => _other;
    }

    internal class MergedReceiverBase : ThisVirtualBase
    {
        private ThisVirtualBase _other = new ImpureDerived();

        // Either operand can be the receiver at the call, so it is not provably `this`.
        public string Via => (_other ?? this).DefaultName;
    }

    internal class MergedReceiverPure : MergedReceiverBase
    {
        public override string DefaultName => "p";
    }

    internal class ByRefFixture
    {
        private int _a = 7;
        private int _b = 2;
        private int _rem = 0;
        private string _s = "1";
        private DateTime _cached = default;

        public int DivRem => Math.DivRem(_a, _b, out _rem);
        public int TryParse => DateTime.TryParse(_s, out _cached) ? 1 : 0;
    }

    internal class InterlockedFixture
    {
        private int _n;
        private int _m;

        public int Inc => Interlocked.Increment(ref _n);
        public int Swap => Interlocked.Exchange(ref _m, 1);
        public int CompareSwap => Interlocked.CompareExchange(ref _m, 1, 0);
    }

    internal class IntrinsicFixture
    {
        // A JIT intrinsic whose IL body only calls itself; the IL says nothing about what really runs.
        public bool Accelerated => Vector128.IsHardwareAccelerated;
    }

    internal interface IGenericValue<T>
    {
        int Value { get; }
    }

    internal class BothInterfaces : IGenericValue<int>, IGenericValue<string>
    {
        int IGenericValue<int>.Value => 1;
        int IGenericValue<string>.Value => new string('x', 1).Length;
    }

    internal interface IPlainValue<T>
    {
        int Value { get; }
    }

    internal class PlainIntImpl : IPlainValue<int>
    {
        public int Value => 1;
    }

    internal class PlainGenericImpl<T> : IPlainValue<T>
    {
        public int Value => new string('x', 1).Length;
    }

    internal class PlainReader
    {
        private IPlainValue<int> _v = new PlainIntImpl();
        public int Read => _v.Value;
    }

    internal interface ICovariantValue<out T>
    {
        T Value { get; }
    }

    internal class CovariantStringImpl : ICovariantValue<string>
    {
        public string Value => new string('x', 1);
    }

    // Maps exactly, so only the variance implementer above can make the read impure.
    internal class CovariantObjectImpl : ICovariantValue<object>
    {
        public object Value => null;
    }

    internal class CovariantReader
    {
        private ICovariantValue<object> _v = new CovariantStringImpl();
        public object Read => _v.Value;
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
    public void ForeignVirtual_ImpureOverridePoisonsChainLink()
    {
        // Link 1 uses all-overrides semantics, so the impure override poisons it for every runtime type.
        var value = typeof(AbstractHolder).GetProperty(nameof(AbstractHolder.Value))!;
        var read = typeof(ForeignReader).GetProperty(nameof(ForeignReader.Read))!;

        Assert.False(PureGetterClassifier.IsPure([read, value], typeof(ForeignReader)));
    }

    [Fact]
    public void ForeignVirtual_AllPureOverrides_IsPure()
    {
        Assert.True(Pure(typeof(PureReader), nameof(PureReader.Read)));

        var other = typeof(PureReader).GetProperty(nameof(PureReader.Other))!;
        var value = typeof(PureBase).GetProperty(nameof(PureBase.Value))!;
        Assert.True(PureGetterClassifier.IsPure([other, value], typeof(PureReader)));
    }

    [Fact]
    public void BranchMergedReceiver_IsNotTreatedAsThis()
    {
        Assert.False(PureGetterClassifier.IsPure(
            Getter(typeof(MergedReceiverBase), nameof(MergedReceiverBase.Via)),
            typeof(MergedReceiverPure)
        ));
    }

    [Fact]
    public void AllowlistedBclWithByRefParameter_IsImpure()
    {
        Assert.False(Pure(typeof(ByRefFixture), nameof(ByRefFixture.DivRem)));
        Assert.False(Pure(typeof(ByRefFixture), nameof(ByRefFixture.TryParse)));
    }

    [Fact]
    public void Interlocked_OnField_IsImpure()
    {
        Assert.False(Pure(typeof(InterlockedFixture), nameof(InterlockedFixture.Inc)));
        Assert.False(Pure(typeof(InterlockedFixture), nameof(InterlockedFixture.Swap)));
        Assert.False(Pure(typeof(InterlockedFixture), nameof(InterlockedFixture.CompareSwap)));
    }

    [Fact]
    public void JitIntrinsic_NotAllowlisted_IsImpure() =>
        Assert.False(Pure(typeof(IntrinsicFixture), nameof(IntrinsicFixture.Accelerated)));

    [Fact]
    public void ConstructedInterface_MapsToItsOwnImplementation()
    {
        var pure = typeof(IGenericValue<int>).GetProperty("Value")!.GetGetMethod()!;
        var impure = typeof(IGenericValue<string>).GetProperty("Value")!.GetGetMethod()!;

        Assert.True(PureGetterClassifier.IsPure(pure, typeof(BothInterfaces)));
        Assert.False(PureGetterClassifier.IsPure(impure, typeof(BothInterfaces)));
    }

    [Fact]
    public void GenericImplementer_IsNotSkipped()
    {
        Assert.False(Pure(typeof(PlainReader), nameof(PlainReader.Read)));
    }

    [Fact]
    public void VarianceImplementer_IsNotSkipped()
    {
        Assert.False(Pure(typeof(CovariantReader), nameof(CovariantReader.Read)));
    }

    [Fact]
    public void AbstractOrInterfaceReceiver_IsImpure()
    {
        var iface = typeof(IGenericValue<int>).GetProperty("Value")!.GetGetMethod()!;
        Assert.False(PureGetterClassifier.IsPure(iface, typeof(IGenericValue<int>)));
        Assert.False(PureGetterClassifier.IsPure(
            Getter(typeof(ThisVirtualBase), nameof(ThisVirtualBase.DefaultName)),
            typeof(PureBase)
        ));
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
    public void PrimitiveCompareTo_IsPure() => Assert.True(Pure(typeof(Fixture), nameof(Fixture.Cmp)));

    [Fact]
    public void MapCompareTo_IsPure() =>
        Assert.True(PureGetterClassifier.IsPure(typeof(Map).GetMethod(nameof(Map.CompareTo), [typeof(Map)]), typeof(Map)));

    [Fact]
    public void UnknownBcl_IsImpure() => Assert.False(Pure(typeof(Fixture), nameof(Fixture.R)));

    [Fact]
    public void NewobjAllowlistedStruct_IsPure() => Assert.True(Pure(typeof(StructFixture), nameof(StructFixture.Point)));

    [Fact]
    public void NewobjNullable_IsPure() => Assert.True(Pure(typeof(StructFixture), nameof(StructFixture.Maybe)));

    [Fact]
    public void NewobjOtherStruct_IsImpure() => Assert.False(Pure(typeof(StructFixture), nameof(StructFixture.Local)));

    [Fact]
    public void UnrelatedReceiverType_IsImpure()
    {
        // Unresolvable for a type outside the hierarchy; must not fall back to the base getter.
        var name = typeof(ThisVirtualBase).GetProperty(nameof(ThisVirtualBase.DefaultName))!;
        Assert.False(PureGetterClassifier.IsPure([name], typeof(string)));
    }

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
