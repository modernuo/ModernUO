using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Spells;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SolenAntHoleComponent : AddonComponent
    {
        public SolenAntHoleComponent(int itemID) : base(itemID)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this, 2))
            {
                var map = Map;

                if (map == Map.Trammel || map == Map.Felucca)
                {
                    from.MoveToWorld(new Point3D(5922, 2024, 0), map);
                    // * ~1_NAME~ dives into the mysterious hole! *
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 1114446, from.Name);
                }
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }
    }

    [SerializationGenerator(1)]
    public partial class SolenAntHole : BaseAddon
    {
        [SerializableField(0, getter: "private", setter: "private")]
        private List<Mobile> _spawned;

        [Constructible]
        public SolenAntHole()
        {
            _spawned = new List<Mobile>();

            AddComponent(new AddonComponent(0x914), "dirt", 0, 0, 0, 0);
            AddComponent(new SolenAntHoleComponent(0x122A), "a hole", 0x1, 0, 0, 0);
            AddComponent(new AddonComponent(0x1B23), "dirt", 0x970, 1, 1, 0);
            AddComponent(new AddonComponent(0xEE0), "dirt", 0, 1, 0, 0);
            AddComponent(new AddonComponent(0x1B24), "dirt", 0x970, 1, -1, 0);
            AddComponent(new AddonComponent(0xEE1), "dirt", 0, 0, -1, 0);
            AddComponent(new AddonComponent(0x1B25), "dirt", 0x970, -1, -1, 0);
            AddComponent(new AddonComponent(0xEE2), "dirt", 0, -1, 0, 0);
            AddComponent(new AddonComponent(0x1B26), "dirt", 0x970, -1, 1, 0);
            AddComponent(new AddonComponent(0xED3), "dirt", 0, 0, 1, 0);
        }

        public override bool ShareHue => false;
        public override bool HandlesOnMovement => true;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (!m.Player || !m.Alive || m.Hidden || !SpawnKilled())
            {
                return;
            }

            if (Utility.InRange(Location, m.Location, 3) && !Utility.InRange(Location, oldLocation, 3))
            {
                var count = 1 + Utility.Random(4);

                for (var i = 0; i < count; i++)
                {
                    SpawnAnt();
                }

                if (Utility.RandomDouble() < 0.05)
                {
                    SpawnAnt(new Beetle());
                }
            }
        }

        public void AddComponent(AddonComponent c, string name, int hue, int x, int y, int z)
        {
            c.Hue = hue;
            c.Name = name;
            AddComponent(c, x, y, z);
        }

        public void SpawnAnt()
        {
            var random = Utility.Random(3);
            var map = Map;

            if (map == Map.Trammel)
            {
                if (random < 2)
                {
                    SpawnAnt(new RedSolenWorker());
                }
                else
                {
                    SpawnAnt(new RedSolenWarrior());
                }
            }
            else if (map == Map.Felucca)
            {
                if (random < 2)
                {
                    SpawnAnt(new BlackSolenWorker());
                }
                else
                {
                    SpawnAnt(new BlackSolenWarrior());
                }
            }
        }

        public void SpawnAnt(BaseCreature ant)
        {
            this.Add(_spawned, ant);

            var map = Map;
            var p = Location;

            for (var i = 0; i < 5; i++)
            {
                if (SpellHelper.FindValidSpawnLocation(map, ref p, false))
                {
                    break;
                }
            }

            ant.MoveToWorld(p, map);
            ant.Home = Location;
            ant.RangeHome = 10;
        }

        public bool SpawnKilled()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                if (!_spawned[i].Alive || _spawned[i].Deleted)
                {
                    this.RemoveAt(_spawned, i);
                }
            }

            return _spawned.Count < 2;
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            _spawned = reader.ReadEntityList<Mobile>();
        }
    }
}
