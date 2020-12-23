using Server.Items;

namespace Server.Engines.Quests.Haven
{
    public enum CannonDirection
    {
        North,
        East,
        South,
        West
    }

    public class Cannon : BaseAddon
    {
        [Constructible]
        public Cannon(CannonDirection direction)
        {
            CannonDirection = direction;

            switch (direction)
            {
                case CannonDirection.North:
                    {
                        AddComponent(new CannonComponent(0xE8D), 0, 0, 0);
                        AddComponent(new CannonComponent(0xE8C), 0, 1, 0);
                        AddComponent(new CannonComponent(0xE8B), 0, 2, 0);

                        break;
                    }
                case CannonDirection.East:
                    {
                        AddComponent(new CannonComponent(0xE96), 0, 0, 0);
                        AddComponent(new CannonComponent(0xE95), -1, 0, 0);
                        AddComponent(new CannonComponent(0xE94), -2, 0, 0);

                        break;
                    }
                case CannonDirection.South:
                    {
                        AddComponent(new CannonComponent(0xE91), 0, 0, 0);
                        AddComponent(new CannonComponent(0xE92), 0, -1, 0);
                        AddComponent(new CannonComponent(0xE93), 0, -2, 0);

                        break;
                    }
                default:
                    {
                        AddComponent(new CannonComponent(0xE8E), 0, 0, 0);
                        AddComponent(new CannonComponent(0xE8F), 1, 0, 0);
                        AddComponent(new CannonComponent(0xE90), 2, 0, 0);

                        break;
                    }
            }
        }

        public Cannon(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CannonDirection CannonDirection { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public MilitiaCanoneer Canoneer { get; set; }

        public override bool HandlesOnMovement => Canoneer?.Deleted == false && Canoneer.Active;

        public void DoFireEffect(Point3D target)
        {
            var from = CannonDirection switch
            {
                CannonDirection.North => new Point3D(X, Y - 1, Z),
                CannonDirection.East  => new Point3D(X + 1, Y, Z),
                CannonDirection.South => new Point3D(X, Y + 1, Z),
                _                     => new Point3D(X - 1, Y, Z)
            };

            Effects.SendLocationEffect(from, Map, 0x36B0, 16, 1);
            Effects.PlaySound(from, Map, 0x11D);

            Effects.SendLocationEffect(target, Map, 0x36B0, 16, 1);
            Effects.PlaySound(target, Map, 0x11D);
        }

        public void Fire(Mobile from, Mobile target)
        {
            DoFireEffect(target.Location);

            target.Damage(9999, from);
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (!(Canoneer?.Deleted == false && Canoneer.Active))
            {
                return;
            }

            var canFire = CannonDirection switch
            {
                CannonDirection.North => m.X >= X - 7 && m.X <= X + 7 && m.Y == Y - 7 && oldLocation.Y < Y - 7,
                CannonDirection.East  => m.Y >= Y - 7 && m.Y <= Y + 7 && m.X == X + 7 && oldLocation.X > X + 7,
                CannonDirection.South => m.X >= X - 7 && m.X <= X + 7 && m.Y == Y + 7 && oldLocation.Y > Y + 7,
                _                     => m.Y >= Y - 7 && m.Y <= Y + 7 && m.X == X - 7 && oldLocation.X < X - 7
            };

            if (canFire && Canoneer.WillFire(this, m))
            {
                Fire(Canoneer, m);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            if (Canoneer?.Deleted == true)
            {
                Canoneer = null;
            }

            base.Serialize(writer);

            writer.Write(0); // version

            writer.WriteEncodedInt((int)CannonDirection);
            writer.Write(Canoneer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            CannonDirection = (CannonDirection)reader.ReadEncodedInt();
            Canoneer = (MilitiaCanoneer)reader.ReadEntity<Mobile>();
        }
    }

    public class CannonComponent : AddonComponent
    {
        public CannonComponent(int itemID) : base(itemID)
        {
        }

        public CannonComponent(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MilitiaCanoneer Canoneer
        {
            get => Addon is Cannon cannon ? cannon.Canoneer : null;
            set
            {
                if (Addon is Cannon cannon)
                {
                    cannon.Canoneer = value;
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
