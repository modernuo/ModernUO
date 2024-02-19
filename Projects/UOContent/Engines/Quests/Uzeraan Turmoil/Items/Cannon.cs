using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.Quests.Haven;

public enum CannonDirection
{
    North,
    East,
    South,
    West
}

[SerializationGenerator(0, false)]
public partial class Cannon : BaseAddon
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CannonDirection _cannonDirection;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private MilitiaCanoneer _canoneer;

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

    public override bool HandlesOnMovement => _canoneer?.Deleted == false && _canoneer.Active;

    public void DoFireEffect(Point3D target)
    {
        var from = _cannonDirection switch
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
}

[SerializationGenerator(0, false)]
public partial class CannonComponent : AddonComponent
{
    public CannonComponent(int itemID) : base(itemID)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public MilitiaCanoneer Canoneer
    {
        get => (Addon as Cannon)?.Canoneer;
        set
        {
            if (Addon is Cannon cannon)
            {
                cannon.Canoneer = value;
            }
        }
    }
}
