using System;

namespace Server.Network
{
  public class PipeResult
  {
    public ArraySegment<byte>[] Buffer { get; }
    public bool IsCanceled { get; }
    public bool IsCompleted { get; set; }

    public int Length
    {
      get
      {
        var length = 0;
        for (int i = 0; i < Buffer.Length; i++)
          length += Buffer[i].Count;

        return length;
      }
    }

    public void CopyFrom(Span<byte> bytes)
    {
      var remaining = bytes.Length;
      var offset = 0;

      if (remaining == 0)
        return;

      for (int i = 0; i < Buffer.Length; i++)
      {
        var sz = Math.Min(remaining, Buffer[0].Count);
        bytes.Slice(offset, sz).CopyTo(Buffer[i].AsSpan());

        remaining -= sz;
        offset += sz;

        if (remaining == 0)
          return;
      }

      throw new OutOfMemoryException();
    }

    public PipeResult(int segments)
    {
      IsCanceled = false;
      IsCompleted = false;
      Buffer = new ArraySegment<byte>[segments];
    }
  }
}
