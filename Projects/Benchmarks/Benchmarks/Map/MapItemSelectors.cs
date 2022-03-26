using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetFabric.Hyperlinq;
using Server;
using StructLinq;
using StructLinq.Array;
using StructLinq.List;
using StructLinq.Select;
using StructLinq.Where;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using static NetFabric.Hyperlinq.ArrayExtensions;

namespace Benchmarks.ItemSelectors
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [MemoryDiagnoser]
    public class MapItemSelectors
    {
        private static readonly Sector sector = new();
        private static readonly Point3D[] locations = { new Point3D(0, 0, 0), new Point3D(50, 50, 0) };

        public static Rectangle2D[] BoundsArray() => new[]
        {
            new Rectangle2D(70, 70, 100, 100),
            new Rectangle2D(30, 30, 100, 100),
            new Rectangle2D(0, 0, 100, 100),
        };

        [GlobalSetup]
        public static void Init()
        {
            for (int j = 0; j < locations.Length; j++)
            {
                Point3D loc = locations[j];

                for (int i = 0; i < 500; ++i)
                {
                    sector.BItems.Add(new BItemDerived(loc));
                }
            }
        }

        [ParamsSource(nameof(BoundsArray))]
        public Rectangle2D bounds;

        [Benchmark(Baseline = true)]
        public BItemDerived SelectBItemsFor()
        {
            BItemDerived toRet = null;
            for (int i = sector.BItems.Count - 1; i >= 0; --i)
            {
                BItem BItem = sector.BItems[i];
                if (BItem is BItemDerived { Deleted: false, Parent: null } tItem && bounds.Contains(BItem.Location))
                {
                    toRet = tItem;
                }
            }

            return toRet;
        }

        [Benchmark]
        public BItemDerived SelectBItemsNew()
        {
            BItemDerived toRet = null;
            foreach (BItemDerived i in SelectBItems<BItemDerived>(sector, bounds))
            {
                toRet = i;
            }

            return toRet;
        }

        [Benchmark]
        public BItemDerived SelectBItemsLinq()
        {
            BItemDerived toRet = null;
            foreach (BItemDerived i in SelectBItemsLinq<BItemDerived>(sector, bounds))
            {
                toRet = i;
            }

            return toRet;
        }

        [Benchmark]
        public BItemDerived SelectBItemsLinqStruct()
        {
            BItemDerived toRet = null;
            foreach (BItemDerived i in SelectBItemsLinqStruct<BItemDerived>(sector, bounds))
            {
                toRet = i;
            }

            return toRet;
        }

        [Benchmark]
        public BItemDerived SelectBItemsLinqStructInterface()
        {
            BItemDerived toRet = null;
            IEnumerable<BItemDerived> enumerable = SelectBItemsLinqStruct<BItemDerived>(sector, bounds).ToEnumerable();

            foreach (BItemDerived i in enumerable)
            {
                toRet = i;
            }

            return toRet;
        }

        [Benchmark]
        public BItemDerived SelectBItemsHyperLinq()
        {
            BItemDerived toRet = null;
            foreach (BItemDerived i in SelectBItemsHyperlinq<BItemDerived>(sector, bounds))
            {
                toRet = i;
            }

            return toRet;
        }

        [Benchmark]
        public BItemDerived SelectBItemsHyperLinqInterface()
        {
            BItemDerived toRet = null;
            IEnumerable<BItemDerived> enumerable = SelectBItemsHyperlinq<BItemDerived>(sector, bounds);

            foreach (BItemDerived i in enumerable)
            {
                toRet = i;
            }

            return toRet;
        }

        [Benchmark]
        public BItemDerived SelectBItemsHyperLinqArrayPool()
        {
            BItemDerived toRet = null;
            using Lease<BItemDerived> lease = SelectBItemsHyperlinq<BItemDerived>(sector, bounds).ToArray(ArrayPool<BItemDerived>.Shared);

            foreach (BItemDerived i in lease)
            {
                toRet = i;
            }

            return toRet;
        }

        public IEnumerable<T> SelectBItemsLinq<T>(Sector s, Rectangle2D bounds) where T : BItem
        {
            return s.BItems.OfType<T>().Where(o => o is { Deleted: false, Parent: null } && bounds.Contains(o.Location));
        }

        public IEnumerable<T> SelectBItems<T>(Sector s, Rectangle2D bounds) where T : BItem
        {
            List<BItem> items = s.BItems;
            List<T> entities = new(items.Count);

            for (int i = items.Count - 1; i >= 0; --i)
            {
                if (items[i] is T { Deleted: false, Parent: null } tItem && bounds.Contains(tItem.Location))
                {
                    entities.Add(tItem);
                }
            }
            return entities;
        }

        public SelectEnumerable<BItem, T, WhereEnumerable<BItem, ListEnumerable<BItem>, ArrayStructEnumerator<BItem>, BItemWhere<T>>,
            WhereEnumerator<BItem, ArrayStructEnumerator<BItem>, BItemWhere<T>>, BItemSelect<T>>
            SelectBItemsLinqStruct<T>(Sector s, Rectangle2D bounds) where T : BItem
        {
            BItemWhere<T> bitemWhere = new(bounds);
            BItemSelect<T> bitemSelect = new();

            return s.BItems.ToStructEnumerable()
                .Where(ref bitemWhere, x => x)
                .Select(ref bitemSelect, x => x, x => x);
        }

        public ArraySegmentWhereSelectEnumerable<BItem, T, BItemWhereHyper<T>, SelectHyper<BItem, T>>
            SelectBItemsHyperlinq<T>(Sector s, Rectangle2D bounds) where T : BItem
        {
            return s.BItems.AsValueEnumerable()
                .Where(new BItemWhereHyper<T>(bounds))
                .Select<T, SelectHyper<BItem, T>>();
        }
    }

    public class BItem : IPoint3D, IEntity
    {
        public object Parent { get; set; } = null;

        public bool Deleted { get; set; } = false;

        public int Z { get; set; } = 1;

        public int X { get; set; } = 1;

        public int Y { get; set; } = 1;

        public Serial Serial => throw new NotImplementedException();

        public Point3D Location { get; }

        public Map Map { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Region Region => throw new NotImplementedException();

        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Hue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Direction Direction { get => throw new System.NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int TypeRef => throw new NotImplementedException();

        Point3D IEntity.Location => Location;

        int IPoint3D.Z => throw new NotImplementedException();

        int IPoint2D.X => throw new NotImplementedException();

        int IPoint2D.Y => throw new NotImplementedException();

        DateTime ISerializable.Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        DateTime ISerializable.LastSerialized { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        long ISerializable.SavePosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        BufferWriter ISerializable.SaveBuffer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        int ISerializable.TypeRef => throw new NotImplementedException();

        Serial ISerializable.Serial => throw new NotImplementedException();

        bool ISerializable.Deleted => throw new NotImplementedException();

        public BItem(Point3D location)
        {
            Location = location;
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public void ProcessDelta()
        {
            throw new NotImplementedException();
        }

        public void OnStatsQuery(Server.Mobile m)
        {
            throw new NotImplementedException();
        }

        public void InvalidateProperties()
        {
            throw new NotImplementedException();
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(IEntity other)
        {
            throw new NotImplementedException();
        }

        public void MoveToWorld(Point3D location, Map map)
        {
            throw new NotImplementedException();
        }

        public bool InRange(Point2D p, int range)
        {
            throw new NotImplementedException();
        }

        public bool InRange(Point3D p, int range)
        {
            throw new NotImplementedException();
        }

        public void RemoveBItem(BItem BItem)
        {
            throw new NotImplementedException();
        }

        public void BeforeSerialize()
        {
            throw new NotImplementedException();
        }

        public void Deserialize(IGenericReader reader)
        {
            throw new NotImplementedException();
        }

        public void Serialize(IGenericWriter writer)
        {
            throw new NotImplementedException();
        }

        public void SetTypeRef(Type type)
        {
            throw new NotImplementedException();
        }

        void ISerializable.BeforeSerialize()
        {
            throw new NotImplementedException();
        }

        void ISerializable.Deserialize(IGenericReader reader)
        {
            throw new NotImplementedException();
        }

        void ISerializable.Serialize(IGenericWriter writer)
        {
            throw new NotImplementedException();
        }

        void ISerializable.Delete()
        {
            throw new NotImplementedException();
        }

        void ISerializable.SetTypeRef(Type type)
        {
            throw new NotImplementedException();
        }

        public void RemoveItem(Item item)
        {
            throw new NotImplementedException();
        }
    }

    public class BItemDerived : BItem
    {
        public BItemDerived(Point3D location) : base(location) { }
    }

    public class Sector
    {
        public List<BItem> BItems { get; set; } = new();
    }

    public struct BItemWhere<T> : StructLinq.IFunction<BItem, bool> where T : BItem
    {
        private readonly Rectangle2D bounds;

        public BItemWhere(Rectangle2D bounds)
        {
            this.bounds = bounds;
        }

        public bool Eval(BItem element)
        {
            return element is T { Deleted: false, Parent: null } && bounds.Contains(element.Location);
        }
    }

    public struct BItemSelect<T> : StructLinq.IFunction<BItem, T> where T : BItem
    {
        public T Eval(BItem element)
        {
            return (T)element;
        }
    }

    public struct BItemWhereHyper<T> : NetFabric.Hyperlinq.IFunction<BItem, bool> where T : BItem
    {
        private readonly Rectangle2D bounds;

        public BItemWhereHyper(Rectangle2D bounds)
        {
            this.bounds = bounds;
        }

        public bool Invoke(BItem element)
        {
            return element is T { Deleted: false, Parent: null } && bounds.Contains(element.Location);
        }
    }

    public struct SelectHyper<TSource, TDest> : NetFabric.Hyperlinq.IFunction<TSource, TDest> where TDest : TSource
    {
        public TDest Invoke(TSource arg)
        {
            return (TDest)arg;
        }
    }
}
