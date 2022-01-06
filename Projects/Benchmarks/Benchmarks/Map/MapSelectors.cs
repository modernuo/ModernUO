using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class MapSelectors
    {
        static readonly Sector sector = new Sector();
        static Server.Rectangle2D bounds = new Server.Rectangle2D(0, 0, 100, 100);

        public static void Init()
        {
            for (int i = 0; i < 50; ++i)
            {
                sector.Multis.Add(new BaseMulti());
            }
            for (int i = 0; i < 1000; ++i)
            {
                sector.BItems.Add(new BItem());
            }
            for (int i = 0; i < 1000; ++i)
            {
                sector.Mobiles.Add(new Mobile());
            }
        }

        #region MultiTiles
        [Benchmark]
        public void SelectMultiTilesNew()
        {
            foreach(StaticTile[] tiles in SelectMultiTiles(sector, bounds))
            {
                for(int i = 0; i < tiles.Length; ++i)
                {
                    int id = tiles[i].ID;
                }
            }
        }

        [Benchmark]
        public void SelectMultiTilesLinq()
        {
            foreach(StaticTile[] tiles in SelectMultiTilesLinq(sector, bounds))
            {
                for(int i = 0; i < tiles.Length; ++i)
                {
                    int id = tiles[i].ID;
                }
            }
        }

        public IEnumerable<StaticTile[]> SelectMultiTilesLinq(Sector s, Server.Rectangle2D bounds)
        {
            foreach (var o in s.Multis.Where(o => o != null && !o.Deleted))
            {
                var c = o.Components;

                int x, y, xo, yo;
                StaticTile[] t, r;

                for (x = bounds.Start.X; x < bounds.End.X; x++)
                {
                    xo = x - (o.X + c.Min.X);

                    if (xo < 0 || xo >= c.Width)
                    {
                        continue;
                    }

                    for (y = bounds.Start.Y; y < bounds.End.Y; y++)
                    {
                        yo = y - (o.Y + c.Min.Y);

                        if (yo < 0 || yo >= c.Height)
                        {
                            continue;
                        }

                        t = c.Tiles[xo][yo];

                        if (t.Length <= 0)
                        {
                            continue;
                        }

                        r = new StaticTile[t.Length];

                        for (var i = 0; i < t.Length; i++)
                        {
                            r[i] = t[i];
                            r[i].Z += o.Z;
                        }

                        yield return r;
                    }
                }
            }
        }

        public IEnumerable<StaticTile[]> SelectMultiTiles(Sector s, Server.Rectangle2D bounds)
        {
            for (int l = s.Multis.Count - 1; l >= 0; --l)
            {
                BaseMulti o = s.Multis[l];
                if (o != null && !o.Deleted)
                {
                    MultiComponentList c = o.Components;

                    int x, y, xo, yo;
                    StaticTile[] t, r;

                    for (x = bounds.Start.X; x < bounds.End.X; x++)
                    {
                        xo = x - (o.X + c.Min.X);

                        if (xo < 0 || xo >= c.Width)
                        {
                            continue;
                        }

                        for (y = bounds.Start.Y; y < bounds.End.Y; y++)
                        {
                            yo = y - (o.Y + c.Min.Y);

                            if (yo < 0 || yo >= c.Height)
                            {
                                continue;
                            }

                            t = c.Tiles[xo][yo];

                            if (t.Length <= 0)
                            {
                                continue;
                            }

                            r = new StaticTile[t.Length];

                            for (var i = 0; i < t.Length; i++)
                            {
                                r[i] = t[i];
                                r[i].Z += o.Z;
                            }

                            yield return r;
                        }
                    }
                }
            }
        }

        #endregion

        #region Multis
        [Benchmark]
        public void SelectMultisNew()
        {
            SelectMultis(sector, bounds);
        }

        [Benchmark]
        public void SelectMultisLinq()
        {
            SelectMultisLinq(sector, bounds);
        }

        public IEnumerable<BaseMulti> SelectMultisLinq(Sector s, Server.Rectangle2D bounds)
        {
            return s.Multis.Where(o => o != null && !o.Deleted && bounds.Contains(o.Location));
        }

        public IEnumerable<BaseMulti> SelectMultis(Sector s, Server.Rectangle2D bounds)
        {
            List<BaseMulti> entities = new List<BaseMulti>(s.Multis.Count);
            for (int i = s.Multis.Count - 1; i >= 0; --i)
            {
                BaseMulti BItem = s.Multis[i];
                if (BItem != null && !BItem.Deleted && bounds.Contains(BItem.Location))
                    entities.Add(BItem);
            }
            return entities;
        }
        #endregion

        #region BItems
        [Benchmark]
        public void SelectBItemsNew()
        {
            SelectBItems<BItem>(sector, bounds);
        }

        [Benchmark]
        public void SelectBItemsLinq()
        {
            SelectBItemsLinq<BItem>(sector, bounds);
        }

        public IEnumerable<T> SelectBItemsLinq<T>(Sector s, Server.Rectangle2D bounds) where T : BItem
        {
            return s.BItems.OfType<T>().Where(o => o != null && !o.Deleted && o.Parent == null && bounds.Contains(o.Location));
        }

        public IEnumerable<T> SelectBItems<T>(Sector s, Server.Rectangle2D bounds) where T : BItem
        {
            List<T> entities = new List<T>(s.BItems.Count);
            Type type = typeof(T);
            for (int i = s.BItems.Count - 1; i >= 0; --i)
            {
                BItem BItem = s.BItems[i];
                if (BItem != null && !BItem.Deleted && BItem.Parent == null && bounds.Contains(BItem.Location) && type.IsAssignableFrom(BItem.GetType()))
                    entities.Add(BItem as T);
            }
            return entities;
        }
        #endregion

        #region Mobiles
        [Benchmark]
        public void SelectMobilesNew()
        {
            SelectMobiles<Mobile>(sector, bounds);
        }

        [Benchmark]
        public void SelectMobilesLinq()
        {
            SelectMobilesLinq<Mobile>(sector, bounds);
        }

        public IEnumerable<T> SelectMobilesLinq<T>(Sector s, Server.Rectangle2D bounds) where T : Mobile
        {
            return s.Mobiles.OfType<T>().Where(o => o != null && !o.Deleted && bounds.Contains(o.Location));
        }

        public IEnumerable<T> SelectMobiles<T>(Sector s, Server.Rectangle2D bounds) where T : Mobile
        {
            List<T> entities = new List<T>(s.Mobiles.Count);
            Type type = typeof(T);
            for (int i = s.Mobiles.Count - 1; i >= 0; --i)
            {
                Mobile mob = s.Mobiles[i];
                if (mob != null && !mob.Deleted && bounds.Contains(mob.Location) && type.IsAssignableFrom(mob.GetType()))
                    entities.Add(mob as T);
            }
            return entities;
        }
        #endregion

        #region Entities
        [Benchmark]
        public void SelectEntitiesNew()
        {
            SelectEntities(sector, bounds);
        }

        [Benchmark]
        public void SelectEntitiesLinq()
        {
            SelectEntitiesLinq(sector, bounds);
        }

        public IEnumerable<Server.IEntity> SelectEntitiesLinq(Sector s, Server.Rectangle2D bounds)
        {
            return Enumerable.Empty<IEntity>()
              .Union(s.Mobiles.Where(o => o != null && !o.Deleted))
              .Union(s.BItems.Where(o => o != null && !o.Deleted && o.Parent == null))
              .Where(o => bounds.Contains(o.Location));
        }

        private readonly List<Server.IEntity> entities = new (10);
        public IEnumerable<Server.IEntity> SelectEntities(Sector s, Server.Rectangle2D bounds)
        {
            entities.Clear();
            entities.Capacity = s.Mobiles.Count + s.BItems.Count;
            for (int i = s.Mobiles.Count - 1, j = s.BItems.Count - 1; i >= 0 || j >= 0; --i, --j)
            {
                if (j >= 0)
                {
                    BItem BItem = s.BItems[j];
                    if (BItem != null && !BItem.Deleted && BItem.Parent == null && bounds.Contains(BItem.Location))
                        entities.Add(BItem);
                }
                if (i >= 0)
                {
                    Mobile mob = s.Mobiles[i];
                    if (mob != null && !mob.Deleted && bounds.Contains(mob.Location))
                        entities.Add(mob);
                }
            }
            return entities;
        }
        #endregion
    }
    public class BItem : Server.IPoint3D, IEntity
    {
        public object Parent { get; set; } = null;

        public bool Deleted { get; set; } = false;

        public int Z { get; set; } = 1;

        public int X { get; set; } = 1;

        public int Y { get; set; } = 1;

        public Serial Serial => throw new System.NotImplementedException();

        public Point3D Location { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public Map Map { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public Region Region => throw new System.NotImplementedException();

        public string Name { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public int Hue { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public Direction Direction { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public DateTime Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int TypeRef => throw new NotImplementedException();

        Point3D IEntity.Location => throw new NotImplementedException();

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

        public BItem()
        {

        }

        public void Delete()
        {
            throw new System.NotImplementedException();
        }

        public void ProcessDelta()
        {
            throw new System.NotImplementedException();
        }

        public void OnStatsQuery(Server.Mobile m)
        {
            throw new System.NotImplementedException();
        }

        public void InvalidateProperties()
        {
            throw new System.NotImplementedException();
        }

        public int CompareTo(object obj)
        {
            throw new System.NotImplementedException();
        }

        public int CompareTo(IEntity other)
        {
            throw new System.NotImplementedException();
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

    public class Mobile : Server.IPoint3D, IEntity
    {
        public bool Deleted { get; set; } = false;

        public int Z { get; set; } = 1;

        public int X { get; set; } = 1;

        public int Y { get; set; } = 1;
      
        public Serial Serial => throw new System.NotImplementedException();

        public Point3D Location { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public Map Map { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public Region Region => throw new System.NotImplementedException();

        public string Name { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public int Hue { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public Direction Direction { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public DateTime Created { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int TypeRef => throw new NotImplementedException();

        Point3D IEntity.Location => throw new NotImplementedException();

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

        public Mobile()
        {

        }

        public void Delete()
        {
            throw new System.NotImplementedException();
        }

        public void ProcessDelta()
        {
            throw new System.NotImplementedException();
        }

        public void OnStatsQuery(Server.Mobile m)
        {
            throw new System.NotImplementedException();
        }

        public void InvalidateProperties()
        {
            throw new System.NotImplementedException();
        }

        public int CompareTo(object obj)
        {
            throw new System.NotImplementedException();
        }

        public int CompareTo(IEntity other)
        {
            throw new System.NotImplementedException();
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

    public class BaseMulti : BItem
    {
        public MultiComponentList Components = MultiComponentList.Empty;

        public BaseMulti()
        {
            for (int i = 0; i < 20; ++i)
                for (int j = 0; j < 20; ++j)
                    for (int z = 0; z < 20; ++z)
                        Components.Add(123, i, j, z);
        }

    }

    public class Sector
    {
        public List<BItem> BItems { get; set; } = new List<BItem>();
        public List<Mobile> Mobiles { get; set; } = new List<Mobile>();
        public List<BaseMulti> Multis { get; set; } = new List<BaseMulti>();
    }
}
