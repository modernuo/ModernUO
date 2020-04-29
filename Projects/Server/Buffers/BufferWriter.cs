// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
  /// <summary>
  /// A fast access struct that wraps <see cref="IBufferWriter{T}"/>.
  /// </summary>
  /// <typeparam name="T">The type of element to be written.</typeparam>
  internal ref struct BufferWriter<T> where T : IBufferWriter<byte>
  {
    /// <summary>
    /// The underlying <see cref="IBufferWriter{T}"/>.
    /// </summary>
    private T m_Output;

    /// <summary>
    /// The result of the last call to <see cref="IBufferWriter{T}.GetSpan(int)"/>, less any bytes already "consumed" with <see cref="Advance(int)"/>.
    /// Backing field for the <see cref="Span"/> property.
    /// </summary>
    private Span<byte> m_Span;

    /// <summary>
    /// The number of uncommitted bytes (all the calls to <see cref="Advance(int)"/> since the last call to <see cref="Commit"/>).
    /// </summary>
    private int m_Buffered;

    /// <summary>
    /// The total number of bytes written with this writer.
    /// Backing field for the <see cref="BytesCommitted"/> property.
    /// </summary>
    private long m_BytesCommitted;

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferWriter{T}"/> struct.
    /// </summary>
    /// <param name="output">The <see cref="IBufferWriter{T}"/> to be wrapped.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferWriter(T output)
    {
      m_Buffered = 0;
      m_BytesCommitted = 0;
      m_Output = output;
      m_Span = output.GetSpan();
    }

    /// <summary>
    /// Gets the result of the last call to <see cref="IBufferWriter{T}.GetSpan(int)"/>.
    /// </summary>
    public Span<byte> Span => m_Span;

    /// <summary>
    /// Gets the total number of bytes written with this writer.
    /// </summary>
    public long BytesCommitted => m_BytesCommitted;

    /// <summary>
    /// Calls <see cref="IBufferWriter{T}.Advance(int)"/> on the underlying writer
    /// with the number of uncommitted bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Commit()
    {
      var buffered = m_Buffered;
      if (buffered > 0)
      {
        m_BytesCommitted += buffered;
        m_Buffered = 0;
        m_Output.Advance(buffered);
      }
    }

    /// <summary>
    /// Used to indicate that part of the buffer has been written to.
    /// </summary>
    /// <param name="count">The number of bytes written to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
      m_Buffered += count;
      m_Span = m_Span.Slice(count);
    }

    /// <summary>
    /// Copies the caller's buffer into this writer and calls <see cref="Advance(int)"/> with the length of the source buffer.
    /// </summary>
    /// <param name="source">The buffer to copy in.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> source)
    {
      if (m_Span.Length >= source.Length)
      {
        source.CopyTo(m_Span);
        Advance(source.Length);
      }
      else
      {
        WriteMultiBuffer(source);
      }
    }

    /// <summary>
    /// Acquires a new buffer if necessary to ensure that some given number of bytes can be written to a single buffer.
    /// </summary>
    /// <param name="count">The number of bytes that must be allocated in a single buffer.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Ensure(int count = 1)
    {
      if (m_Span.Length < count) EnsureMore(count);
    }

    /// <summary>
    /// Gets a fresh span to write to, with an optional minimum size.
    /// </summary>
    /// <param name="count">The minimum size for the next requested buffer.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureMore(int count = 0)
    {
      if (m_Buffered > 0) Commit();

      m_Span = m_Output.GetSpan(count);
    }

    /// <summary>
    /// Copies the caller's buffer into this writer, potentially across multiple buffers from the underlying writer.
    /// </summary>
    /// <param name="source">The buffer to copy into this writer.</param>
    private void WriteMultiBuffer(ReadOnlySpan<byte> source)
    {
      while (source.Length > 0)
      {
        if (m_Span.Length == 0) EnsureMore();

        var writable = Math.Min(source.Length, m_Span.Length);
        source.Slice(0, writable).CopyTo(m_Span);
        source = source.Slice(writable);
        Advance(writable);
      }
    }
  }
}
