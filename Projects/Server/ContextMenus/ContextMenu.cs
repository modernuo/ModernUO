/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ContextMenu.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.ContextMenus;

/// <summary>
///     Represents the state of an active context menu. This includes who opened the menu, the menu's focus object, and a list
///     of
///     <see cref="ContextMenuEntry">entries</see> that the menu is composed of.
///     <seealso cref="ContextMenuEntry" />
/// </summary>
public class ContextMenu
{
    /// <summary>
    ///     Instantiates a new ContextMenu instance.
    /// </summary>
    /// <param name="from">
    ///     The <see cref="Mobile" /> who opened this ContextMenu.
    ///     <seealso cref="From" />
    /// </param>
    /// <param name="target">
    ///     The <see cref="Mobile" /> or <see cref="Item" /> to execute the ContextMenu on.
    ///     <seealso cref="Target" />
    /// </param>
    /// <param name="entries">
    ///   An array of <see cref="ContextMenuEntry">entries</see> contained in this ContextMenu.
    ///   <seealso cref="Entries" />
    /// </param>
    public ContextMenu(Mobile from, IEntity target, ContextMenuEntry[] entries)
    {
        From = from;
        Target = target;
        Entries = entries;

        for (var i = 0; i < Entries.Length; i++)
        {
            var entry = Entries[i];

            if (entry.Number is < 3000000 or > 3032767)
            {
                RequiresNewPacket = true;
                break;
            }
        }
    }

    /// <summary>
    ///     Gets the <see cref="Mobile" /> who opened this ContextMenu.
    /// </summary>
    public Mobile From { get; }

    /// <summary>
    ///     Gets an object of the <see cref="Mobile" /> or <see cref="Item" /> for which this ContextMenu is on.
    /// </summary>
    public IEntity Target { get; }

    /// <summary>
    ///     Gets the list of <see cref="ContextMenuEntry">entries</see> contained in this ContextMenu.
    /// </summary>
    public ContextMenuEntry[] Entries { get; }

    /// <summary>
    ///     Returns true if this ContextMenu requires packet version 2.
    /// </summary>
    public bool RequiresNewPacket { get; }
}
