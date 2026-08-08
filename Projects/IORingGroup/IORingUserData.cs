// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.CompilerServices;

namespace System.Network;

/// <summary>
/// Standard userData encoding utilities for IORingGroup completions.
/// </summary>
/// <remarks>
/// <para>
/// Encoding format (64 bits):
/// <code>
/// [8 bits: opType][16 bits: generation][8 bits: reserved][32 bits: socketId]
/// </code>
/// </para>
/// <para>
/// The generation counter prevents stale completion misattribution when socket IDs are reused.
/// When a socket is closed and its ID recycled to a new connection, any in-flight completions
/// from the old connection will have a different generation and can be safely ignored.
/// </para>
/// </remarks>
public static class IORingUserData
{
    /// <summary>Operation type: Accept a new connection.</summary>
    public const int OpAccept = 1;

    /// <summary>Operation type: Receive data.</summary>
    public const int OpRecv = 2;

    /// <summary>Operation type: Send data.</summary>
    public const int OpSend = 3;

    /// <summary>Operation type: Shutdown socket (send FIN). Completion is ignored.</summary>
    public const int OpShutdown = 4;

    private const ulong OpTypeMask = 0xFF00_0000_0000_0000UL;
    private const ulong GenerationMask = 0x00FF_FF00_0000_0000UL;
    private const ulong SocketIdMask = 0x0000_0000_FFFF_FFFFUL;
    private const int OpTypeShift = 56;
    private const int GenerationShift = 40;

    /// <summary>
    /// Encodes operation type, socket ID, and generation into a 64-bit userData value.
    /// </summary>
    /// <param name="opType">The operation type (OpAccept, OpRecv, OpSend).</param>
    /// <param name="socketId">The socket identifier (0 to 2^32-1).</param>
    /// <param name="generation">The generation counter for stale detection.</param>
    /// <returns>Encoded userData value for use with ring operations.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Encode(int opType, int socketId, ushort generation) =>
        ((ulong)opType << OpTypeShift) |
        ((ulong)generation << GenerationShift) |
        ((uint)socketId & SocketIdMask);

    /// <summary>
    /// Encodes an accept operation userData (no socket ID or generation needed).
    /// </summary>
    /// <param name="listenerIndex">Optional listener index for multi-listener setups.</param>
    /// <returns>Encoded userData value for accept operations.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong EncodeAccept(int listenerIndex = 0) =>
        ((ulong)OpAccept << OpTypeShift) | ((uint)listenerIndex & SocketIdMask);

    /// <summary>
    /// Decodes a userData value into its components.
    /// </summary>
    /// <param name="userData">The userData from a completion.</param>
    /// <returns>Tuple of (opType, socketId, generation).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int opType, int socketId, ushort generation) Decode(ulong userData) =>
    (
        (int)((userData & OpTypeMask) >> OpTypeShift),
        (int)(userData & SocketIdMask),
        (ushort)((userData & GenerationMask) >> GenerationShift)
    );

    /// <summary>
    /// Gets just the operation type from userData.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOpType(ulong userData) => (int)((userData & OpTypeMask) >> OpTypeShift);

    /// <summary>
    /// Gets just the socket ID from userData.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSocketId(ulong userData) => (int)(userData & SocketIdMask);

    /// <summary>
    /// Gets just the generation from userData.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetGeneration(ulong userData) => (ushort)((userData & GenerationMask) >> GenerationShift);

    /// <summary>
    /// Checks if the given generation matches the expected generation.
    /// </summary>
    /// <param name="userData">The userData from a completion.</param>
    /// <param name="expectedGeneration">The expected generation for the socket.</param>
    /// <returns>True if generations match, false if this is a stale completion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCurrentGeneration(ulong userData, ushort expectedGeneration) =>
        GetGeneration(userData) == expectedGeneration;
}
