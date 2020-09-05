/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Argon2Error.cs                                                  *
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
    public enum Argon2Error
    {
        OK = 0,
        OUTPUT_TOO_SHORT = -2,
        SALT_TOO_SHORT = -6,
        SALT_TOO_LONG = -7,
        TIME_TOO_SMALL = -12,
        MEMORY_TOO_LITTLE = -14,
        MEMORY_TOO_MUCH = -15,
        LANES_TOO_FEW = -16,
        LANES_TOO_MANY = -17,
        MEMORY_ALLOCATION_ERROR = -22,
        THREADS_TOO_FEW = -28,
        THREADS_TOO_MANY = -29,
        DECODING_FAIL = -32,
        THREAD_FAIL = -33,
        VERIFY_MISMATCH = -35
    }
}
