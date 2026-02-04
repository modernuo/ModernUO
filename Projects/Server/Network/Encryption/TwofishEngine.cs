/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TwofishEngine.cs                                                *
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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Network;

/// <summary>
/// Twofish block cipher implementation for UO encryption.
/// Implements 128-bit block encryption with 128-bit key in ECB mode.
/// Based on the public domain Twofish algorithm by Bruce Schneier et al.
/// </summary>
public sealed class TwofishEngine
{
    private const int BlockSize = 16; // 128 bits
    private const int Rounds = 16;
    private const int InputWhiten = 0;
    private const int OutputWhiten = 4;
    private const int RoundSubkeys = 8;
    private const int TotalSubkeys = RoundSubkeys + 2 * Rounds;

    private const uint SkStep = 0x02020202u;
    private const uint SkBump = 0x01010101u;
    private const int SkRotl = 9;
    private const uint RsGfFdbk = 0x14D;
    private const int MdsGfFdbk = 0x169;

    // P0 and P1 permutation tables
    private static readonly byte[] P0 =
    {
        0xA9, 0x67, 0xB3, 0xE8, 0x04, 0xFD, 0xA3, 0x76, 0x9A, 0x92, 0x80, 0x78, 0xE4, 0xDD, 0xD1, 0x38,
        0x0D, 0xC6, 0x35, 0x98, 0x18, 0xF7, 0xEC, 0x6C, 0x43, 0x75, 0x37, 0x26, 0xFA, 0x13, 0x94, 0x48,
        0xF2, 0xD0, 0x8B, 0x30, 0x84, 0x54, 0xDF, 0x23, 0x19, 0x5B, 0x3D, 0x59, 0xF3, 0xAE, 0xA2, 0x82,
        0x63, 0x01, 0x83, 0x2E, 0xD9, 0x51, 0x9B, 0x7C, 0xA6, 0xEB, 0xA5, 0xBE, 0x16, 0x0C, 0xE3, 0x61,
        0xC0, 0x8C, 0x3A, 0xF5, 0x73, 0x2C, 0x25, 0x0B, 0xBB, 0x4E, 0x89, 0x6B, 0x53, 0x6A, 0xB4, 0xF1,
        0xE1, 0xE6, 0xBD, 0x45, 0xE2, 0xF4, 0xB6, 0x66, 0xCC, 0x95, 0x03, 0x56, 0xD4, 0x1C, 0x1E, 0xD7,
        0xFB, 0xC3, 0x8E, 0xB5, 0xE9, 0xCF, 0xBF, 0xBA, 0xEA, 0x77, 0x39, 0xAF, 0x33, 0xC9, 0x62, 0x71,
        0x81, 0x79, 0x09, 0xAD, 0x24, 0xCD, 0xF9, 0xD8, 0xE5, 0xC5, 0xB9, 0x4D, 0x44, 0x08, 0x86, 0xE7,
        0xA1, 0x1D, 0xAA, 0xED, 0x06, 0x70, 0xB2, 0xD2, 0x41, 0x7B, 0xA0, 0x11, 0x31, 0xC2, 0x27, 0x90,
        0x20, 0xF6, 0x60, 0xFF, 0x96, 0x5C, 0xB1, 0xAB, 0x9E, 0x9C, 0x52, 0x1B, 0x5F, 0x93, 0x0A, 0xEF,
        0x91, 0x85, 0x49, 0xEE, 0x2D, 0x4F, 0x8F, 0x3B, 0x47, 0x87, 0x6D, 0x46, 0xD6, 0x3E, 0x69, 0x64,
        0x2A, 0xCE, 0xCB, 0x2F, 0xFC, 0x97, 0x05, 0x7A, 0xAC, 0x7F, 0xD5, 0x1A, 0x4B, 0x0E, 0xA7, 0x5A,
        0x28, 0x14, 0x3F, 0x29, 0x88, 0x3C, 0x4C, 0x02, 0xB8, 0xDA, 0xB0, 0x17, 0x55, 0x1F, 0x8A, 0x7D,
        0x57, 0xC7, 0x8D, 0x74, 0xB7, 0xC4, 0x9F, 0x72, 0x7E, 0x15, 0x22, 0x12, 0x58, 0x07, 0x99, 0x34,
        0x6E, 0x50, 0xDE, 0x68, 0x65, 0xBC, 0xDB, 0xF8, 0xC8, 0xA8, 0x2B, 0x40, 0xDC, 0xFE, 0x32, 0xA4,
        0xCA, 0x10, 0x21, 0xF0, 0xD3, 0x5D, 0x0F, 0x00, 0x6F, 0x9D, 0x36, 0x42, 0x4A, 0x5E, 0xC1, 0xE0
    };

    private static readonly byte[] P1 =
    {
        0x75, 0xF3, 0xC6, 0xF4, 0xDB, 0x7B, 0xFB, 0xC8, 0x4A, 0xD3, 0xE6, 0x6B, 0x45, 0x7D, 0xE8, 0x4B,
        0xD6, 0x32, 0xD8, 0xFD, 0x37, 0x71, 0xF1, 0xE1, 0x30, 0x0F, 0xF8, 0x1B, 0x87, 0xFA, 0x06, 0x3F,
        0x5E, 0xBA, 0xAE, 0x5B, 0x8A, 0x00, 0xBC, 0x9D, 0x6D, 0xC1, 0xB1, 0x0E, 0x80, 0x5D, 0xD2, 0xD5,
        0xA0, 0x84, 0x07, 0x14, 0xB5, 0x90, 0x2C, 0xA3, 0xB2, 0x73, 0x4C, 0x54, 0x92, 0x74, 0x36, 0x51,
        0x38, 0xB0, 0xBD, 0x5A, 0xFC, 0x60, 0x62, 0x96, 0x6C, 0x42, 0xF7, 0x10, 0x7C, 0x28, 0x27, 0x8C,
        0x13, 0x95, 0x9C, 0xC7, 0x24, 0x46, 0x3B, 0x70, 0xCA, 0xE3, 0x85, 0xCB, 0x11, 0xD0, 0x93, 0xB8,
        0xA6, 0x83, 0x20, 0xFF, 0x9F, 0x77, 0xC3, 0xCC, 0x03, 0x6F, 0x08, 0xBF, 0x40, 0xE7, 0x2B, 0xE2,
        0x79, 0x0C, 0xAA, 0x82, 0x41, 0x3A, 0xEA, 0xB9, 0xE4, 0x9A, 0xA4, 0x97, 0x7E, 0xDA, 0x7A, 0x17,
        0x66, 0x94, 0xA1, 0x1D, 0x3D, 0xF0, 0xDE, 0xB3, 0x0B, 0x72, 0xA7, 0x1C, 0xEF, 0xD1, 0x53, 0x3E,
        0x8F, 0x33, 0x26, 0x5F, 0xEC, 0x76, 0x2A, 0x49, 0x81, 0x88, 0xEE, 0x21, 0xC4, 0x1A, 0xEB, 0xD9,
        0xC5, 0x39, 0x99, 0xCD, 0xAD, 0x31, 0x8B, 0x01, 0x18, 0x23, 0xDD, 0x1F, 0x4E, 0x2D, 0xF9, 0x48,
        0x4F, 0xF2, 0x65, 0x8E, 0x78, 0x5C, 0x58, 0x19, 0x8D, 0xE5, 0x98, 0x57, 0x67, 0x7F, 0x05, 0x64,
        0xAF, 0x63, 0xB6, 0xFE, 0xF5, 0xB7, 0x3C, 0xA5, 0xCE, 0xE9, 0x68, 0x44, 0xE0, 0x4D, 0x43, 0x69,
        0x29, 0x2E, 0xAC, 0x15, 0x59, 0xA8, 0x0A, 0x9E, 0x6E, 0x47, 0xDF, 0x34, 0x35, 0x6A, 0xCF, 0xDC,
        0x22, 0xC9, 0xC0, 0x9B, 0x89, 0xD4, 0xED, 0xAB, 0x12, 0xA2, 0x0D, 0x52, 0xBB, 0x02, 0x2F, 0xA9,
        0xD7, 0x61, 0x1E, 0xB4, 0x50, 0x04, 0xF6, 0xC2, 0x16, 0x25, 0x86, 0x56, 0x55, 0x09, 0xBE, 0x91
    };

    private readonly uint[] _sboxKeys = new uint[2]; // For 128-bit key
    private readonly uint[] _subKeys = new uint[TotalSubkeys];

    /// <summary>
    /// Creates a new Twofish engine with the specified 128-bit key.
    /// </summary>
    public TwofishEngine(ReadOnlySpan<byte> key)
    {
        if (key.Length != 16)
        {
            throw new ArgumentException("Key must be 16 bytes (128 bits)", nameof(key));
        }

        GenerateSubkeys(MemoryMarshal.Cast<byte, uint>(key));
    }

    private void GenerateSubkeys(ReadOnlySpan<uint> keyWords)
    {
        // Split key into even and odd words
        var k0 = keyWords[0];
        var k1 = keyWords[1];
        var k2 = keyWords[2];
        var k3 = keyWords[3];

        // Compute S-box keys using RS matrix
        _sboxKeys[0] = RsMdsEncode(k2, k3);
        _sboxKeys[1] = RsMdsEncode(k0, k1);

        // Generate round subkeys
        for (var i = 0; i < TotalSubkeys / 2; i++)
        {
            var a = F32((uint)(i * SkStep), k0, k2);
            var b = F32((uint)(i * SkStep + SkBump), k1, k3);
            b = BitOperations.RotateLeft(b, 8);

            _subKeys[2 * i] = a + b;
            _subKeys[2 * i + 1] = BitOperations.RotateLeft(a + 2 * b, SkRotl);
        }
    }

    /// <summary>
    /// Encrypts a 16-byte block in place.
    /// </summary>
    public void EncryptBlock(Span<byte> block)
    {
        if (block.Length < BlockSize)
        {
            throw new ArgumentException("Block must be at least 16 bytes", nameof(block));
        }

        var x = MemoryMarshal.Cast<byte, uint>(block);

        // Input whitening
        x[0] ^= _subKeys[InputWhiten];
        x[1] ^= _subKeys[InputWhiten + 1];
        x[2] ^= _subKeys[InputWhiten + 2];
        x[3] ^= _subKeys[InputWhiten + 3];

        // 16 rounds
        for (var r = 0; r < Rounds; r++)
        {
            var t0 = F32Sbox(x[0]);
            var t1 = F32Sbox(BitOperations.RotateLeft(x[1], 8));

            x[3] = BitOperations.RotateLeft(x[3], 1);
            x[2] ^= t0 + t1 + _subKeys[RoundSubkeys + 2 * r];
            x[3] ^= t0 + 2 * t1 + _subKeys[RoundSubkeys + 2 * r + 1];
            x[2] = BitOperations.RotateRight(x[2], 1);

            if (r < Rounds - 1)
            {
                // Swap for next round
                (x[0], x[2]) = (x[2], x[0]);
                (x[1], x[3]) = (x[3], x[1]);
            }
        }

        // Output whitening
        x[0] ^= _subKeys[OutputWhiten];
        x[1] ^= _subKeys[OutputWhiten + 1];
        x[2] ^= _subKeys[OutputWhiten + 2];
        x[3] ^= _subKeys[OutputWhiten + 3];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint F32Sbox(uint x)
    {
        // For 128-bit key, use 2 S-box keys
        // Permutation sequence from Twofish spec (P_ij constants):
        // b0: P0[P0[P0[x]^k1]^k0] then P1 final
        // b1: P0[P0[P1[x]^k1]^k0] then P0 final
        // b2: P1[P1[P0[x]^k1]^k0] then P1 final
        // b3: P1[P1[P1[x]^k1]^k0] then P0 final

        var b0 = (byte)x;
        var b1 = (byte)(x >> 8);
        var b2 = (byte)(x >> 16);
        var b3 = (byte)(x >> 24);

        var k0 = _sboxKeys[0];
        var k1 = _sboxKeys[1];

        // First layer: P_02=0(P0), P_12=1(P1), P_22=0(P0), P_32=1(P1)
        b0 = (byte)(P0[b0] ^ (byte)k1);
        b1 = (byte)(P1[b1] ^ (byte)(k1 >> 8));
        b2 = (byte)(P0[b2] ^ (byte)(k1 >> 16));
        b3 = (byte)(P1[b3] ^ (byte)(k1 >> 24));

        // Second layer: P_01=0(P0), P_11=0(P0), P_21=1(P1), P_31=1(P1)
        b0 = (byte)(P0[b0] ^ (byte)k0);
        b1 = (byte)(P0[b1] ^ (byte)(k0 >> 8));
        b2 = (byte)(P1[b2] ^ (byte)(k0 >> 16));
        b3 = (byte)(P1[b3] ^ (byte)(k0 >> 24));

        // Final layer: P_00=1(P1), P_10=0(P0), P_20=1(P1), P_30=0(P0)
        // MDS matrix multiply
        return MdsMultiply(P1[b0], P0[b1], P1[b2], P0[b3]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint F32(uint x, uint k0, uint k2)
    {
        var b0 = (byte)x;
        var b1 = (byte)(x >> 8);
        var b2 = (byte)(x >> 16);
        var b3 = (byte)(x >> 24);

        // First layer: P_02=0(P0), P_12=1(P1), P_22=0(P0), P_32=1(P1)
        b0 = (byte)(P0[b0] ^ (byte)k2);
        b1 = (byte)(P1[b1] ^ (byte)(k2 >> 8));
        b2 = (byte)(P0[b2] ^ (byte)(k2 >> 16));
        b3 = (byte)(P1[b3] ^ (byte)(k2 >> 24));

        // Second layer: P_01=0(P0), P_11=0(P0), P_21=1(P1), P_31=1(P1)
        b0 = (byte)(P0[b0] ^ (byte)k0);
        b1 = (byte)(P0[b1] ^ (byte)(k0 >> 8));
        b2 = (byte)(P1[b2] ^ (byte)(k0 >> 16));
        b3 = (byte)(P1[b3] ^ (byte)(k0 >> 24));

        // Final layer: P_00=1(P1), P_10=0(P0), P_20=1(P1), P_30=0(P0)
        return MdsMultiply(P1[b0], P0[b1], P1[b2], P0[b3]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MdsMultiply(byte b0, byte b1, byte b2, byte b3)
    {
        // MDS matrix multiplication (Galois Field 2^8)
        var m0 = (uint)(b0 ^ Lfsr2(b1) ^ Lfsr1(b2) ^ Lfsr1(b3));
        var m1 = (uint)(Lfsr1(b0) ^ Lfsr2(b1) ^ Lfsr2(b2) ^ b3);
        var m2 = (uint)(Lfsr2(b0) ^ Lfsr1(b1) ^ b2 ^ Lfsr2(b3));
        var m3 = (uint)(Lfsr2(b0) ^ b1 ^ Lfsr2(b2) ^ Lfsr1(b3));

        return m0 | (m1 << 8) | (m2 << 16) | (m3 << 24);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Lfsr1(int val) => val ^ Lfsr4(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Lfsr2(int val) => val ^ Lfsr3(val) ^ Lfsr4(val);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Lfsr3(int val) => (val >> 1) ^ ((val & 0x01) == 0x01 ? MdsGfFdbk / 2 : 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Lfsr4(int val) =>
        (val >> 2) ^ ((val & 0x02) == 0x02 ? MdsGfFdbk / 2 : 0) ^ ((val & 0x01) == 0x01 ? MdsGfFdbk / 4 : 0);

    private static uint RsMdsEncode(uint k0, uint k1)
    {
        uint r = 0;
        for (var i = 0; i < 2; i++)
        {
            r ^= i > 0 ? k0 : k1;

            for (var j = 0; j < 4; j++)
            {
                var v1 = (byte)(r >> 24);
                var v2 = (uint)(((v1 << 1) ^ ((v1 & 0x80) == 0x80 ? RsGfFdbk : 0)) & 0xFF);
                var v3 = (uint)(((v1 >> 1) & 0x7F) ^ ((v1 & 1) == 1 ? RsGfFdbk >> 1 : 0) ^ v2);

                r = (r << 8) ^ (v3 << 24) ^ (v2 << 16) ^ (v3 << 8) ^ v1;
            }
        }

        return r;
    }
}
