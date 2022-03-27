using System;
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server.Items;

public partial class TrappableContainer
{
    public virtual bool ExecuteTrap(Mobile from)
    {
        if (_trapType == TrapType.None)
        {
            return false;
        }

        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            SendMessageTo(from, "That is trapped, but you open it with your godly powers.", 0x3B2);
            return false;
        }

        SendMessageTo(from, 502999, 0x3B2); // You set off a trap!

        var loc = GetWorldLocation();

        switch (_trapType)
        {
            case TrapType.ExplosionTrap:
                {
                    ExecuteExplosionTrap(from, loc);
                    break;
                }
            case TrapType.MagicTrap:
                {
                    ExecuteMagicTrap(from, loc);
                    break;
                }
            case TrapType.DartTrap:
                {
                    ExecuteDartTrap(from, loc);
                    break;
                }
            case TrapType.PoisonTrap:
                {
                    ExecutePoisonTrap(from, loc);
                    break;
                }
        }

        TrapType = TrapType.None;
        TrapPower = 0;
        TrapLevel = 0;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteExplosionTrap(Mobile from, Point3D loc)
    {
        var facet = Map;
        if (from.InRange(loc, 3))
        {
            int damage;

            if (_trapLevel > 0)
            {
                damage = Utility.RandomMinMax(10, 30) * _trapLevel;
            }
            else
            {
                damage = _trapPower;
            }

            AOS.Damage(from, damage, 0, 100, 0, 0, 0);

            // Your skin blisters from the heat!
            from.LocalOverheadMessage(MessageType.Regular, 0x2A, 503000);
        }

        Effects.SendLocationEffect(loc, facet, 0x36BD, 15);
        Effects.PlaySound(loc, facet, 0x307);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteMagicTrap(Mobile from, Point3D loc)
    {
        var facet = Map;
        if (from.InRange(loc, 1))
        {
            from.Damage(_trapPower);
        }

        Effects.PlaySound(loc, facet, 0x307);

        Effects.SendLocationEffect(new Point3D(loc.X - 1, loc.Y, loc.Z), facet, 0x36BD, 15);
        Effects.SendLocationEffect(new Point3D(loc.X + 1, loc.Y, loc.Z), facet, 0x36BD, 15);

        Effects.SendLocationEffect(new Point3D(loc.X, loc.Y - 1, loc.Z), facet, 0x36BD, 15);
        Effects.SendLocationEffect(new Point3D(loc.X, loc.Y + 1, loc.Z), facet, 0x36BD, 15);

        Effects.SendLocationEffect(new Point3D(loc.X + 1, loc.Y + 1, loc.Z + 11), facet, 0x36BD, 15);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteDartTrap(Mobile from, Point3D loc)
    {
        if (from.InRange(loc, 3))
        {
            var damage = _trapLevel > 0 ? Utility.RandomMinMax(5, 15) * _trapLevel : _trapPower;

            AOS.Damage(from, damage, 100, 0, 0, 0, 0);

            // A dart embeds itself in your flesh!
            from.LocalOverheadMessage(MessageType.Regular, 0x62, 502998);
        }

        Effects.PlaySound(loc, Map, 0x223);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecutePoisonTrap(Mobile from, Point3D loc)
    {
        var facet = Map;
        if (from.InRange(loc, 3))
        {
            Poison poison;

            if (_trapLevel > 0)
            {
                poison = Poison.GetPoison(Math.Max(0, Math.Min(4, _trapLevel - 1)));
            }
            else
            {
                AOS.Damage(from, _trapPower, 0, 0, 0, 100, 0);
                poison = Poison.Greater;
            }

            from.ApplyPoison(from, poison);

            // You are enveloped in a noxious green cloud!
            from.LocalOverheadMessage(MessageType.Regular, 0x44, 503004);
        }

        Effects.SendLocationEffect(loc, facet, 0x113A, 10, 20);
        Effects.PlaySound(loc, facet, 0x231);
    }
}
