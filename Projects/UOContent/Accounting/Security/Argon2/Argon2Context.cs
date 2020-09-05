/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Argon2Context.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    [StructLayout(LayoutKind.Sequential)]
    internal class Argon2Context
    {
        public IntPtr Out;
        public uint OutLen;

        public IntPtr Pwd;
        public uint PwdLen;

        public IntPtr Salt;
        public uint SaltLen;

        public IntPtr Secret;
        public uint SecretLen;

        public IntPtr AssocData;
        public uint AssocDataLen;

        public uint TimeCost;
        public uint MemoryCost;
        public uint Lanes;
        public uint Threads;

        public IntPtr AllocateCallback;
        public IntPtr FreeCallback;

        public uint Flags;
    }
}
