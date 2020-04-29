// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Buffers
{
  /// <summary>
  /// Block tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
  /// individual blocks are then treated as independent array segments.
  /// </summary>
  public sealed class MemoryPoolBlock : IMemoryOwner<byte>
  {
    private readonly int m_Offset;
    private readonly int m_Length;

    /// <summary>
    /// This object cannot be instantiated outside of the static Create method
    /// </summary>
    internal MemoryPoolBlock(SlabMemoryPool pool, MemoryPoolSlab slab, int offset, int length)
    {
      m_Offset = offset;
      m_Length = length;

      Pool = pool;
      Slab = slab;

      Memory = MemoryMarshal.CreateFromPinnedArray(slab.Array, m_Offset, m_Length);
    }

    /// <summary>
    /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
    /// </summary>
    public SlabMemoryPool Pool { get; }

    /// <summary>
    /// Back-reference to the slab from which this block was taken, or null if it is one-time-use memory.
    /// </summary>
    public MemoryPoolSlab Slab { get; }

    public Memory<byte> Memory { get; }

    ~MemoryPoolBlock()
    {
      Pool.RefreshBlock(Slab, m_Offset, m_Length);
    }

    public void Dispose()
    {
      Pool.Return(this);
    }

    public void Lease()
    {
    }
  }
}
