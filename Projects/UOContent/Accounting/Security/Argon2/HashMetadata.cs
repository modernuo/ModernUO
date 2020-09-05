/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: HashMetadata.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace System.Security.Cryptography
{
    public class HashMetadata
    {
        public Argon2Type ArgonType { get; set; }

        public uint MemoryCost { get; set; }

        public uint TimeCost { get; set; }

        public uint Parallelism { get; set; }

        public byte[] Salt { get; set; }

        public byte[] Hash { get; set; }

        public string GetBase64Salt() => Convert.ToBase64String(Salt).Replace("=", "");

        public string GetBase64Hash() => Convert.ToBase64String(Hash).Replace("=", "");

        public override string ToString() =>
            $"$argon2{(ArgonType == Argon2Type.Argon2i ? "i" : "d")}$v=19$m={MemoryCost},t={TimeCost},p={Parallelism}${GetBase64Salt()}${GetBase64Hash()}";
    }
}
