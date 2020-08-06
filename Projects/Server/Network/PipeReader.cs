using System;
using System.Runtime.CompilerServices;

namespace Server.Network
{
  public class PipeReader : INotifyCompletion
  {
    private readonly Pipe m_Pipe;

    private readonly PipeResult _result = new PipeResult(2);

    internal PipeReader(Pipe pipe) => m_Pipe = pipe;

    private void UpdateBufferReader()
    {
      var read = m_Pipe.m_ReadIdx;
      var write = m_Pipe.m_WriteIdx;

      if (read <= write)
      {
        _result.Buffer[0] = write - read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(m_Pipe.m_Buffer, (int)read, (int)(write - read));
        _result.Buffer[1] = ArraySegment<byte>.Empty;
      }
      else
      {
        _result.Buffer[0] = m_Pipe.Size - read == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(m_Pipe.m_Buffer, (int)read, (int)(m_Pipe.Size - read));
        _result.Buffer[1] = write == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(m_Pipe.m_Buffer, 0, (int)write);
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint BytesAvailable()
    {
      var read = m_Pipe.m_ReadIdx;
      var write = m_Pipe.m_WriteIdx;

      if (read <= write) return write - read;

      return write + m_Pipe.Size - read;
    }

    public PipeResult TryGetBytes()
    {
      if (BytesAvailable() > 0) UpdateBufferReader();

      return _result;
    }

    // The PipeReader itself is awaitable
    public PipeReader GetBytes()
    {
      if (BytesAvailable() > 0) UpdateBufferReader();

      if (m_Pipe.m_AwaitBeginning)
        throw new Exception("Double await on reader");

      return this;
    }

    public void Advance(uint bytes)
    {
      var read = m_Pipe.m_ReadIdx;
      var write = m_Pipe.m_WriteIdx;

      if (read <= write)
      {
        if (bytes > write - read) throw new InvalidOperationException();

        read += bytes;
      }
      else
      {
        var sz = Math.Min(bytes, m_Pipe.Size - read);

        read += sz;
        if (read > m_Pipe.Size - 1) read = 0;
        bytes -= sz;

        if (bytes > 0)
        {
          if (bytes > write) throw new InvalidOperationException();

          read = bytes;
        }
      }

      m_Pipe.m_ReadIdx = read;
    }

    #region Awaitable

    // The following makes it possible to await the reader. Do not use any of this directly.

    public PipeReader GetAwaiter() => this;

    public bool Complete() => _result.IsCompleted = true;

    public bool IsCompleted {
      get
      {
        if (BytesAvailable() > 0)
          return true;

        m_Pipe.m_AwaitBeginning = true;
        return false;
      }
    }

    // TODO: Return by ref?
    public PipeResult GetResult()
    {
      UpdateBufferReader();
      return _result;
    }

    public void OnCompleted(Action continuation) => m_Pipe.m_ReaderContinuation = state => continuation();

    #endregion
  }
}
