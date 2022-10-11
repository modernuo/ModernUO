/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Layer.cs                                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server;

/// <summary>
///     Enumeration of item layer values.
/// </summary>
public enum Layer : byte
{
    /// <summary>
    ///     Invalid layer.
    /// </summary>
    Invalid = 0x00,

    /// <summary>
    ///     First valid layer. Equivalent to <c>Layer.OneHanded</c>.
    /// </summary>
    FirstValid = 0x01,

    /// <summary>
    ///     One handed weapon.
    /// </summary>
    OneHanded = 0x01,

    /// <summary>
    ///     Two handed weapon or shield.
    /// </summary>
    TwoHanded = 0x02,

    /// <summary>
    ///     Shoes.
    /// </summary>
    Shoes = 0x03,

    /// <summary>
    ///     Pants.
    /// </summary>
    Pants = 0x04,

    /// <summary>
    ///     Shirts.
    /// </summary>
    Shirt = 0x05,

    /// <summary>
    ///     Helmets, hats, and masks.
    /// </summary>
    Helm = 0x06,

    /// <summary>
    ///     Gloves.
    /// </summary>
    Gloves = 0x07,

    /// <summary>
    ///     Rings.
    /// </summary>
    Ring = 0x08,

    /// <summary>
    ///     Talismans.
    /// </summary>
    Talisman = 0x09,

    /// <summary>
    ///     Gorgets and necklaces.
    /// </summary>
    Neck = 0x0A,

    /// <summary>
    ///     Hair.
    /// </summary>
    Hair = 0x0B,

    /// <summary>
    ///     Half aprons.
    /// </summary>
    Waist = 0x0C,

    /// <summary>
    ///     Torso, inner layer.
    /// </summary>
    InnerTorso = 0x0D,

    /// <summary>
    ///     Bracelets.
    /// </summary>
    Bracelet = 0x0E,

    /// <summary>
    ///     Unused.
    /// </summary>
    Unused_xF = 0x0F,

    /// <summary>
    ///     Beards and mustaches.
    /// </summary>
    FacialHair = 0x10,

    /// <summary>
    ///     Torso, outer layer.
    /// </summary>
    MiddleTorso = 0x11,

    /// <summary>
    ///     Earings.
    /// </summary>
    Earrings = 0x12,

    /// <summary>
    ///     Arms and sleeves.
    /// </summary>
    Arms = 0x13,

    /// <summary>
    ///     Cloaks.
    /// </summary>
    Cloak = 0x14,

    /// <summary>
    ///     Backpacks.
    /// </summary>
    Backpack = 0x15,

    /// <summary>
    ///     Torso, outer layer.
    /// </summary>
    OuterTorso = 0x16,

    /// <summary>
    ///     Leggings, outer layer.
    /// </summary>
    OuterLegs = 0x17,

    /// <summary>
    ///     Leggings, inner layer.
    /// </summary>
    InnerLegs = 0x18,

    /// <summary>
    ///     Last valid non-internal layer. Equivalent to <c>Layer.InnerLegs</c>.
    /// </summary>
    LastUserValid = 0x18,

    /// <summary>
    ///     Mount item layer.
    /// </summary>
    Mount = 0x19,

    /// <summary>
    ///     Vendor 'buy pack' layer.
    /// </summary>
    ShopBuy = 0x1A,

    /// <summary>
    ///     Vendor 'resale pack' layer.
    /// </summary>
    ShopResale = 0x1B,

    /// <summary>
    ///     Vendor 'sell pack' layer.
    /// </summary>
    ShopSell = 0x1C,

    /// <summary>
    ///     Bank box layer.
    /// </summary>
    Bank = 0x1D,

    /// <summary>
    ///     Last valid layer. Equivalent to <c>Layer.Bank</c>.
    /// </summary>
    LastValid = 0x1D
}
