/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MovingShotManaTests.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using Server;
using Server.Items;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

/// <summary>
///     Moving Shot is paid for when the shot is fired, not when the swing starts: a swing that fires
///     nothing keeps the mana and the armed move, a fired shot charges once and disarms.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class MovingShotManaTests : IDisposable
{
    // Y=540 keeps us inside Felucca bounds; the X band is clear of the other sequential tests.
    private static readonly Point3D Location = new(4720, 540, 0);

    // Plain Mobiles: WeaponAbility.Validate skips the NetState and skill checks for non-players, and
    // with no skills or Lower Mana Cost the move costs its full BaseMana.
    private const int StartMana = 100;
    private const int MovingShotMana = 15;

    private readonly MobileTestHarness _harness = new();
    private Mobile _archer;

    public void Dispose()
    {
        // The armed move lives in WeaponAbility's static table, which deleting the mobile does not clear.
        if (_archer != null)
        {
            WeaponAbility.ClearCurrentAbility(_archer);
        }

        _harness.Dispose();
    }

    private Mobile CreateArcher()
    {
        var m = _harness.Track(new Mobile());
        m.RawInt = StartMana;
        m.Mana = StartMana;
        m.AddItem(new HeavyCrossbow());
        m.MoveToWorld(Location, Map.Felucca);
        return _archer = m;
    }

    private Mobile CreateTarget()
    {
        var m = _harness.Track(new Mobile());
        m.RawStr = 100;
        m.Hits = m.HitsMax;
        m.MoveToWorld(new Point3D(Location.X + 3, Location.Y, Location.Z), Map.Felucca);
        return m;
    }

    // Mirrors Mobile.CheckCombatTime, which calls OnBeforeSwing right before every OnSwing.
    private static void Swing(Mobile attacker, Mobile defender)
    {
        var weapon = attacker.Weapon;
        weapon.OnBeforeSwing(attacker, defender);
        weapon.OnSwing(attacker, defender);
    }

    [Fact]
    public void SwingThatFiresNothing_KeepsManaAndArmedMove()
    {
        var archer = CreateArcher();
        var target = CreateTarget();
        Assert.True(WeaponAbility.SetCurrentAbility(archer, WeaponAbility.MovingShot));

        archer.Paralyzed = true;
        Swing(archer, target);
        Swing(archer, target);

        Assert.Equal(StartMana, archer.Mana);
        Assert.IsType<MovingShot>(WeaponAbility.GetCurrentAbility(archer));
    }

    [Fact]
    public void FiredShot_ChargesManaOnceAndDisarms()
    {
        var archer = CreateArcher();
        var target = CreateTarget();
        Assert.True(WeaponAbility.SetCurrentAbility(archer, WeaponAbility.MovingShot));

        Swing(archer, target);

        Assert.Equal(StartMana - MovingShotMana, archer.Mana);
        Assert.Null(WeaponAbility.GetCurrentAbility(archer));
    }
}
