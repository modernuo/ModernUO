using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetFabric.Hyperlinq;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using static NetFabric.Hyperlinq.ArrayExtensions;

namespace Benchmarks.EntitiesSelectors
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [MemoryDiagnoser]
    public class MapEntitiesSelectors
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
                    sector.BItems.Add(new BItem(loc));
                }

                for (int i = 0; i < 25; ++i)
                {
                    sector.Mobiles.Add(new Mobile(loc));
                }
            }
        }

        [ParamsSource(nameof(BoundsArray))]
        public Rectangle2D bounds;

        [Benchmark(Baseline = true)]
        public IEntity SelectEntitiesFor()
        {
            IEntity toRet = null;
            for (int i = sector.Mobiles.Count - 1; i >= 0; --i)
            {
                Mobile mob = sector.Mobiles[i];
                if (mob is { Deleted: false } tMob && bounds.Contains(mob.Location))
                {
                    toRet = tMob;
                }
            }

            for (int i = sector.BItems.Count - 1; i >= 0; --i)
            {
                BItem item = sector.BItems[i];
                if (item is { Deleted: false, Parent: null } tItem && bounds.Contains(item.Location))
                {
                    toRet = tItem;
                }
            }

            return toRet;
        }

        [Benchmark]
        public IEntity SelectEntitiesNew()
        {
            IEntity toRet = null;
            foreach (IEntity e in SelectEntitiesNew(sector, bounds))
            {
                toRet = e;
            }

            return toRet;
        }

        [Benchmark]
        public IEntity SelectEntitiesLinq()
        {
            IEntity toRet = null;
            foreach (IEntity e in SelectEntitiesLinq(sector, bounds))
            {
                toRet = e;
            }

            return toRet;
        }

        [Benchmark]
        public IEntity SelectMobilesHyperLinq()
        {
            IEntity toRet = null;
            foreach (IEntity e in SelectEntitiesHyperlinq(sector, bounds))
            {
                toRet = e;
            }

            return toRet;
        }


        public IEnumerable<IEntity> SelectEntitiesLinq(Sector s, Rectangle2D bounds)
        {
            return Enumerable.Empty<IEntity>()
              .Union(s.Mobiles.Where(o => o is { Deleted: false } && bounds.Contains(o.Location)))
              .Union(s.BItems.Where(o => o is { Deleted: false, Parent: null } && bounds.Contains(o.Location)));
        }

        private readonly List<IEntity> entities = new(10);

        public IEnumerable<IEntity> SelectEntitiesNew(Sector s, Rectangle2D bounds)
        {
            entities.Clear();
            entities.EnsureCapacity(s.Mobiles.Count + s.BItems.Count);

            for (int i = s.Mobiles.Count - 1, j = s.BItems.Count - 1; i >= 0 || j >= 0; --i, --j)
            {
                if (j >= 0)
                {
                    BItem BItem = s.BItems[j];
                    if (BItem is { Deleted: false, Parent: null } && bounds.Contains(BItem.Location))
                    {
                        entities.Add(BItem);
                    }
                }
                if (i >= 0)
                {
                    Mobile mob = s.Mobiles[i];
                    if (mob is { Deleted: false } && bounds.Contains(mob.Location))
                    {
                        entities.Add(mob);
                    }
                }
            }
            return entities;
        }

        public IEnumerable<IEntity> SelectEntitiesHyperlinq(Sector s, Rectangle2D bounds)
        {
            ArraySegmentWhereSelectEnumerable<Mobile, IEntity, MobileWhereHyper, SelectHyper<Mobile, IEntity>> mobiles =
                s.Mobiles.AsValueEnumerable().Where(new MobileWhereHyper(bounds)).Select<IEntity, SelectHyper<Mobile, IEntity>>();

            ArraySegmentWhereSelectEnumerable<BItem, IEntity, BItemWhereHyper, SelectHyper<BItem, IEntity>> items =
                s.BItems.AsValueEnumerable().Where(new BItemWhereHyper(bounds)).Select<IEntity, SelectHyper<BItem, IEntity>>();

            return mobiles.Concat(items);
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

        Map IEntity.Map => throw new NotImplementedException();

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

        void IEntity.MoveToWorld(Point3D location, Map map)
        {
            throw new NotImplementedException();
        }

        void IEntity.ProcessDelta()
        {
            throw new NotImplementedException();
        }

        bool IEntity.InRange(Point2D p, int range)
        {
            throw new NotImplementedException();
        }

        bool IEntity.InRange(Point3D p, int range)
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

    public class Mobile : IPoint3D, IEntity
    {
        public bool Deleted { get; set; } = false;

        public int Z { get; set; } = 1;

        public int X { get; set; } = 1;

        public int Y { get; set; } = 1;

        public Serial Serial => throw new NotImplementedException();

        public Point3D Location { get; }
        public Map Map { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Region Region => throw new NotImplementedException();

        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Hue { get => throw new System.NotImplementedException(); set => throw new NotImplementedException(); }
        public Direction Direction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int TypeRef => throw new NotImplementedException();

        Point3D IEntity.Location => Location;

        Map IEntity.Map => throw new NotImplementedException();

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

        public Mobile(Point3D location)
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

        void IEntity.MoveToWorld(Point3D location, Map map)
        {
            throw new NotImplementedException();
        }

        void IEntity.ProcessDelta()
        {
            throw new NotImplementedException();
        }

        bool IEntity.InRange(Point2D p, int range)
        {
            throw new NotImplementedException();
        }

        bool IEntity.InRange(Point3D p, int range)
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

    public class Sector
    {
        public List<BItem> BItems { get; set; } = new();
        public List<Mobile> Mobiles { get; set; } = new();
    }

    public struct BItemWhereHyper : NetFabric.Hyperlinq.IFunction<BItem, bool>
    {
        private readonly Rectangle2D bounds;

        public BItemWhereHyper(Rectangle2D bounds)
        {
            this.bounds = bounds;
        }

        public bool Invoke(BItem element)
        {
            return element is { Deleted: false, Parent: null } && bounds.Contains(element.Location);
        }
    }

    public struct MobileWhereHyper : NetFabric.Hyperlinq.IFunction<Mobile, bool>
    {
        private readonly Rectangle2D bounds;

        public MobileWhereHyper(Rectangle2D bounds)
        {
            this.bounds = bounds;
        }

        public bool Invoke(Mobile element)
        {
            return element is { Deleted: false } && bounds.Contains(element.Location);
        }
    }

    public struct SelectHyper<TSource, TDest> : NetFabric.Hyperlinq.IFunction<TSource, TDest> where TSource : TDest
    {
        public TDest Invoke(TSource arg)
        {
            return arg;
        }
    }
}
