/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IPropertyList.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.CompilerServices;
using Server.Text;

namespace Server;

public interface IPropertyList : ISelfInterpolatedStringHandler
{
    public void Reset();
    public void Terminate();

    public void Add(int number);

    /** Convenience method for $"{argument}". */
    public void Add(int number, string argument);

    /** Convenience method for $"{text}". */
    public void Add(string text);

    /** Convenience method for $"{value}". */
    public void Add(int number, int value);

    /** Convenience method for $"{value:#}". */
    public void AddLocalized(int value);

    /** Convenience method for $"{value:#}". */
    public void AddLocalized(int number, int value);

    // String Interpolation
    public void Add([InterpolatedStringHandlerArgument("")] ref InterpolatedStringHandler handler);
    public void Add(int number, [InterpolatedStringHandlerArgument("")] ref InterpolatedStringHandler handler);
}
