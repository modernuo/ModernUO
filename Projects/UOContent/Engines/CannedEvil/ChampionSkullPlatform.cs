/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionSkullPlatform.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.CannedEvil;

[SerializationGenerator(0, false)]
public partial class ChampionSkullPlatform : BaseAddon
{
    [SerializableField(0)]
    private ChampionSkullBrazier _power;

    [SerializableField(1)]
    private ChampionSkullBrazier _enlightenment;

    [SerializableField(2)]
    private ChampionSkullBrazier _venom;

    [SerializableField(3)]
    private ChampionSkullBrazier _pain;

    [SerializableField(4)]
    private ChampionSkullBrazier _greed;

    [SerializableField(5)]
    private ChampionSkullBrazier _death;

    [Constructible]
    public ChampionSkullPlatform()
    {
        AddComponent(new AddonComponent(0x71A), -1, -1, -1);
        AddComponent(new AddonComponent(0x709), 0, -1, -1);
        AddComponent(new AddonComponent(0x709), 1, -1, -1);
        AddComponent(new AddonComponent(0x709), -1, 0, -1);
        AddComponent(new AddonComponent(0x709), 0, 0, -1);
        AddComponent(new AddonComponent(0x709), 1, 0, -1);
        AddComponent(new AddonComponent(0x709), -1, 1, -1);
        AddComponent(new AddonComponent(0x709), 0, 1, -1);
        AddComponent(new AddonComponent(0x71B), 1, 1, -1);

        AddComponent(new AddonComponent(0x50F), 0, -1, 4);
        AddComponent(_power = new ChampionSkullBrazier(this, ChampionSkullType.Power), 0, -1, 5);

        AddComponent(new AddonComponent(0x50F), 1, -1, 4);
        AddComponent(_enlightenment = new ChampionSkullBrazier(this, ChampionSkullType.Enlightenment), 1, -1, 5);

        AddComponent(new AddonComponent(0x50F), -1, 0, 4);
        AddComponent(_venom = new ChampionSkullBrazier(this, ChampionSkullType.Venom), -1, 0, 5);

        AddComponent(new AddonComponent(0x50F), 1, 0, 4);
        AddComponent(_pain = new ChampionSkullBrazier(this, ChampionSkullType.Pain), 1, 0, 5);

        AddComponent(new AddonComponent(0x50F), -1, 1, 4);
        AddComponent(_greed = new ChampionSkullBrazier(this, ChampionSkullType.Greed), -1, 1, 5);

        AddComponent(new AddonComponent(0x50F), 0, 1, 4);
        AddComponent(_death = new ChampionSkullBrazier(this, ChampionSkullType.Death), 0, 1, 5);

        AddonComponent comp = new LocalizedAddonComponent(0x20D2, 1049495) { Hue = 0x482 };
        AddComponent(comp, 0, 0, 5);

        comp = new LocalizedAddonComponent(0x0BCF, 1049496) { Hue = 0x482 };
        AddComponent(comp, 0, 2, -7);

        comp = new LocalizedAddonComponent(0x0BD0, 1049497) { Hue = 0x482 };
        AddComponent(comp, 2, 0, -7);
    }

    public void Validate()
    {
        if (Validate(_power) && Validate(_enlightenment) && Validate(_venom) && Validate(_pain) &&
            Validate(_greed) && Validate(_death))
        {
            Mobile harrower = Harrower.Spawn(new Point3D(X, Y, Z + 6), Map);

            if (harrower == null)
            {
                return;
            }

            Clear(_power);
            Clear(_enlightenment);
            Clear(_venom);
            Clear(_pain);
            Clear(_greed);
            Clear(_death);
        }
    }

    public static void Clear(ChampionSkullBrazier brazier)
    {
        if (brazier != null)
        {
            Effects.SendBoltEffect(brazier);

            brazier.Skull?.Delete();
        }
    }

    public static bool Validate(ChampionSkullBrazier brazier) => brazier is { Skull: { Deleted: false } };
}
