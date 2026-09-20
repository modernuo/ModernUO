/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NetworkStats.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.  *
 *************************************************************************/

namespace Server.Network;

/// <summary>One send buffer growth tier's pool usage, in buffers of <paramref name="BufferSize"/> bytes.</summary>
/// <param name="BufferSize">Physical size of every buffer in this tier.</param>
/// <param name="Capacity">Buffers the tier's allocated slabs hold.</param>
/// <param name="InUse">Buffers currently handed out.</param>
/// <param name="RetainFloor">Peak usage the retention window is still holding capacity for.</param>
public readonly record struct SendBufferTierUsage(int BufferSize, int Capacity, int InUse, int RetainFloor);

/// <summary>
/// What the last once-a-minute send buffer maintenance sweep saw. The refusal counts cover the
/// minute before that sweep only.
/// </summary>
/// <param name="Ran">False until the first sweep; every other field is then meaningless.</param>
/// <param name="Tick"><see cref="Core.TickCount"/> when the sweep ran; compare by subtraction only.</param>
/// <param name="TierCapacityBytes">Slab capacity allocated across the growth tiers.</param>
/// <param name="TierInUse">Growth-tier buffers handed out, summed across tiers.</param>
/// <param name="TierRetainFloor">Retention floors summed across tiers, in buffers.</param>
/// <param name="TierBuffersReleased">Growth-tier buffers the sweep returned to the OS.</param>
/// <param name="BaseCapacityBytes">Slab capacity allocated across the base and pre-auth pools.</param>
/// <param name="BaseBuffersReleased">Base and pre-auth buffers the sweep returned to the OS.</param>
/// <param name="BudgetRefusals">Growth refused because the tiers were at the growth budget.</param>
/// <param name="CapRefusals">Growth refused because the buffer was already at the maximum size.</param>
/// <param name="CeilingRefusals">Growth refused because the process was above the memory ceiling.</param>
public readonly record struct SendBufferSweep(
    bool Ran,
    long Tick,
    long TierCapacityBytes,
    int TierInUse,
    int TierRetainFloor,
    int TierBuffersReleased,
    long BaseCapacityBytes,
    int BaseBuffersReleased,
    int BudgetRefusals,
    int CapRefusals,
    int CeilingRefusals
);

/// <summary>
/// Read-only snapshot of the network layer for diagnostics. Connection counts and tier usage are
/// live; everything under <see cref="LastSweep"/> is up to a minute old. Cold path: allocates.
/// </summary>
/// <param name="Connected">Sockets the transport currently holds, authenticated or not.</param>
/// <param name="MaxConnections">Socket slots the transport was built with.</param>
/// <param name="Authenticated">Connections whose credentials have verified, so on full-size buffers.</param>
/// <param name="RecvBufferSize">Base receive buffer size.</param>
/// <param name="SendBufferSize">Base send buffer size.</param>
/// <param name="MaxSendBufferSize">Largest size a send buffer can grow to.</param>
/// <param name="InitialRecvBufferSize">Pre-auth receive buffer size; 0 when pre-auth buffers are off.</param>
/// <param name="InitialSendBufferSize">Pre-auth send buffer size; 0 when pre-auth buffers are off.</param>
/// <param name="SendBufferGrowthBudget">Bytes the growth tiers may hold in total.</param>
/// <param name="MemoryCeilingPercent">Growth stops once the process exceeds this share of available memory.</param>
/// <param name="AvailableMemoryBytes">Memory available to the process as of the last sweep.</param>
/// <param name="Tiers">Live usage of each growth tier, smallest first.</param>
/// <param name="LastSweep">What the last maintenance sweep saw.</param>
/// <param name="Throttled">Connections whose parser is paused on a packet throttle.</param>
/// <param name="FlushPending">Connections with unsent bytes queued for the next flush.</param>
/// <param name="PendingDisconnects">Connections closing after their final flush.</param>
public readonly record struct NetworkStats(
    int Connected,
    int MaxConnections,
    int Authenticated,
    int RecvBufferSize,
    int SendBufferSize,
    int MaxSendBufferSize,
    int InitialRecvBufferSize,
    int InitialSendBufferSize,
    long SendBufferGrowthBudget,
    int MemoryCeilingPercent,
    long AvailableMemoryBytes,
    SendBufferTierUsage[] Tiers,
    SendBufferSweep LastSweep,
    int Throttled,
    int FlushPending,
    int PendingDisconnects
);
