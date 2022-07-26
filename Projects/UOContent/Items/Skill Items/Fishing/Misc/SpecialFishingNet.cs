using System;
using Server.Mobiles;
using Server.Multis;
using Server.Spells;
using Server.Targeting;

namespace Server.Items;

public class SpecialFishingNet : Item
{
    private static int[] _hues =
    {
        0x09B,
        0x0CD,
        0x0D3,
        0x14D,
        0x1DD,
        0x1E9,
        0x1F4,
        0x373,
        0x451,
        0x47F,
        0x489,
        0x492,
        0x4B5,
        0x8AA
    };

    private static int[] _waterTiles =
    {
        0x00A8, 0x00AB,
        0x0136, 0x0137
    };

    private static int[] _shallowWaterTiles =
    {
        0x1797, 0x179C
    };

    [Constructible]
    public SpecialFishingNet() : base(0x0DCA)
    {
        Weight = 1.0;
        Hue = Utility.RandomDouble() < 0.01 ? _hues.RandomElement() : 0x8A0;
    }

    public SpecialFishingNet(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1041079; // a special fishing net

    [CommandProperty(AccessLevel.GameMaster)]
    public bool InUse { get; set; }

    public virtual bool RequireDeepWater => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        AddNetProperties(list);
    }

    protected virtual void AddNetProperties(IPropertyList list)
    {
        // as if the name wasn't enough..
        list.Add(1017410); // Special Fishing Net
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(1); // version

        writer.Write(InUse);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        switch (version)
        {
            case 1:
                {
                    InUse = reader.ReadBool();

                    if (InUse)
                    {
                        Delete();
                    }

                    break;
                }
        }

        Stackable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (InUse)
        {
            from.SendLocalizedMessage(1010483); // Someone is already using that net!
        }
        else if (IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1010484); // Where do you wish to use the net?
            from.BeginTarget(-1, true, TargetFlags.None, OnTarget);
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    public void OnTarget(Mobile from, object obj)
    {
        if (Deleted || InUse)
        {
            return;
        }

        if (obj is not IPoint3D p3D)
        {
            return;
        }

        var map = from.Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        int x = p3D.X, y = p3D.Y, z = map.GetAverageZ(x, y); // OSI just takes the targeted Z

        if (!from.InRange(p3D, 6))
        {
            from.SendLocalizedMessage(500976); // You need to be closer to the water to fish!
        }
        else if (!from.InLOS(obj))
        {
            from.SendLocalizedMessage(500979); // You cannot see that location.
        }
        else if (RequireDeepWater
                     ? FullValidation(map, x, y)
                     : ValidateDeepWater(map, x, y) || ValidateUndeepWater(map, obj, ref z))
        {
            var p = new Point3D(x, y, z);

            if (GetType() == typeof(SpecialFishingNet))
            {
                for (var i = 1; i < Amount; ++i) // these were stackable before, doh
                {
                    from.AddToBackpack(new SpecialFishingNet());
                }
            }

            InUse = true;
            Movable = false;
            MoveToWorld(p, map);

            SpellHelper.Turn(from, p);
            from.Animate(12, 5, 1, true, false, 0);

            Effects.SendLocationEffect(p, map, 0x352D, 16, 4);
            Effects.PlaySound(p, map, 0x364);

            var index = 0;

            Timer.StartTimer(
                TimeSpan.FromSeconds(1.0),
                TimeSpan.FromSeconds(1.25),
                14,
                () => DoEffect(from, p, index++)
            );

            // You plunge the net into the sea... / You plunge the net into the water...
            from.SendLocalizedMessage(RequireDeepWater ? 1010487 : 1074492);
        }
        else
        {
            // You can only use this net in deep water! / You can only use this net in water!
            from.SendLocalizedMessage(RequireDeepWater ? 1010485 : 1074491);
        }
    }

    private void DoEffect(Mobile from, Point3D p, int index)
    {
        if (Deleted)
        {
            return;
        }

        if (index == 0)
        {
            Effects.SendLocationEffect(p, Map, 0x352D, 16, 4);
            Effects.PlaySound(p, Map, 0x364);
        }
        else if (index is <= 6 or 13)
        {
            if (RequireDeepWater)
            {
                for (var i = 0; i < 3; ++i)
                {
                    int x, y;

                    switch (Utility.Random(8))
                    {
                        default: // 0
                            {
                                x = -1;
                                y = -1;
                                break;
                            }
                        case 1:
                            {
                                x = -1;
                                y = 0;
                                break;
                            }
                        case 2:
                            {
                                x = -1;
                                y = +1;
                                break;
                            }
                        case 3:
                            {
                                x = 0;
                                y = -1;
                                break;
                            }
                        case 4:
                            {
                                x = 0;
                                y = +1;
                                break;
                            }
                        case 5:
                            {
                                x = +1;
                                y = -1;
                                break;
                            }
                        case 6:
                            {
                                x = +1;
                                y = 0;
                                break;
                            }
                        case 7:
                            {
                                x = +1;
                                y = +1;
                                break;
                            }
                    }

                    Effects.SendLocationEffect(new Point3D(p.X + x, p.Y + y, p.Z), Map, 0x352D, 16, 4);
                }
            }
            else
            {
                Effects.SendLocationEffect(p, Map, 0x352D, 16, 4);
            }

            if (Utility.RandomBool())
            {
                Effects.PlaySound(p, Map, 0x364);
            }

            if (index == 13)
            {
                FinishEffect(p, Map, from);
            }
            else
            {
                Z -= 1;
            }
        }
    }

    protected virtual int GetSpawnCount() => Hue != 0x8A0 ? Utility.RandomMinMax(2, 5) : Utility.RandomMinMax(1, 3);

    protected void Spawn(Point3D p, Map map, BaseCreature spawn)
    {
        if (map == null)
        {
            spawn.Delete();
            return;
        }

        int x = p.X, y = p.Y;

        for (var j = 0; j < 20; ++j)
        {
            var tx = p.X - 2 + Utility.Random(5);
            var ty = p.Y - 2 + Utility.Random(5);

            var t = map.Tiles.GetLandTile(tx, ty);

            if (t.Z == p.Z && t.ID is >= 0xA8 and <= 0xAB or >= 0x136 and <= 0x137 &&
                !SpellHelper.CheckMulti(new Point3D(tx, ty, p.Z), map))
            {
                x = tx;
                y = ty;
                break;
            }
        }

        spawn.MoveToWorld(new Point3D(x, y, p.Z), map);

        if (spawn is Kraken && Utility.RandomDouble() < 0.2)
        {
            spawn.PackItem(new MessageInABottle(map == Map.Felucca ? Map.Felucca : Map.Trammel));
        }
    }

    protected virtual void FinishEffect(Point3D p, Map map, Mobile from)
    {
        from.RevealingAction();

        var count = GetSpawnCount();

        for (var i = 0; map != null && i < count; ++i)
        {
            var spawn = Utility.Random(4) switch
            {
                0 => (BaseCreature)new SeaSerpent(),
                1 => new DeepSeaSerpent(),
                2 => new WaterElemental(),
                3 => new Kraken(),
                _ => new SeaSerpent()
            };

            Spawn(p, map, spawn);

            spawn.Combatant = from;
        }

        Delete();
    }

    public static bool FullValidation(Map map, int x, int y)
    {
        if (!ValidateDeepWater(map, x, y))
        {
            return false;
        }

        for (int j = 1, offset = 5; j <= 5; ++j, offset += 5)
        {
            if (!ValidateDeepWater(map, x + offset, y + offset))
            {
                return false;
            }

            if (!ValidateDeepWater(map, x + offset, y - offset))
            {
                return false;
            }

            if (!ValidateDeepWater(map, x - offset, y + offset))
            {
                return false;
            }

            if (!ValidateDeepWater(map, x - offset, y - offset))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateDeepWater(Map map, int x, int y)
    {
        var tileID = map.Tiles.GetLandTile(x, y).ID;
        var water = false;

        for (var i = 0; !water && i < _waterTiles.Length; i += 2)
        {
            water = tileID >= _waterTiles[i] && tileID <= _waterTiles[i + 1];
        }

        return water;
    }

    private static bool ValidateUndeepWater(Map map, object obj, ref int z)
    {
        if (obj is not StaticTarget)
        {
            return false;
        }

        var target = (StaticTarget)obj;

        if (BaseHouse.FindHouseAt(target.Location, map, 0) != null)
        {
            return false;
        }

        var itemID = target.ItemID;

        for (var i = 0; i < _shallowWaterTiles.Length; i += 2)
        {
            if (itemID >= _shallowWaterTiles[i] && itemID <= _shallowWaterTiles[i + 1])
            {
                z = target.Z;
                return true;
            }
        }

        return false;
    }
}

public class FabledFishingNet : SpecialFishingNet
{
    [Constructible]
    public FabledFishingNet() => Hue = 0x481;

    public FabledFishingNet(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1063451; // a fabled fishing net

    protected override void AddNetProperties(IPropertyList list)
    {
    }

    protected override int GetSpawnCount() => base.GetSpawnCount() + 4;

    protected override void FinishEffect(Point3D p, Map map, Mobile from)
    {
        Spawn(p, map, new Leviathan(from));

        base.FinishEffect(p, map, from);
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
