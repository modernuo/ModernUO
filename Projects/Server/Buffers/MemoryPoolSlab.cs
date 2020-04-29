// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Buffers
{
  /// <summary>
  /// Slab tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
  /// individual blocks are then treated as independent array segments.
  /// </summary>
  public class MemoryPoolSlab : IDisposable
  {
    /// <summary>
    /// This handle pins the managed array in memory until the slab is disposed. This prevents it from being
    /// relocated and enables any subsections of the array to be used as native memory pointers to P/Invoked API calls.
    /// </summary>
    private GCHandle m_GcHandle;

    private bool m_IsDisposed;

    public MemoryPoolSlab(byte[] data)
    {
      Array = data;
      m_GcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
      NativePointer = m_GcHandle.AddrOfPinnedObject();
    }

    /// <summary>
    /// True as long as the blocks from this slab are to be considered returnable to the pool. In order to shrink the
    /// memory pool size an entire slab must be removed. That is done by (1) setting IsActive to false and removing the
    /// slab from the pool's _slabs collection, (2) as each block currently in use is Return()ed to the pool it will
    /// be allowed to be garbage collected rather than re-pooled, and (3) when all block tracking objects are garbage
    /// collected and the slab is no longer references the slab will be garbage collected and the memory unpinned will
    /// be unpinned by the slab's Dispose.
    /// </summary>
    public bool IsActive => !m_IsDisposed;

    public IntPtr NativePointer { get; private set; }

    public byte[] Array { get; private set; }

    public static MemoryPoolSlab Create(int length)
    {
      // allocate and pin requested memory length
      var array = new byte[length];

      // allocate and return slab tracking object
      return new MemoryPoolSlab(array);
    }

    protected void Dispose(bool disposing)
    {
      if (m_IsDisposed) return;

      m_IsDisposed = true;

      Array = null;
      NativePointer = IntPtr.Zero;

      if (m_GcHandle.IsAllocated) m_GcHandle.Free();
    }

    ~MemoryPoolSlab()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}
