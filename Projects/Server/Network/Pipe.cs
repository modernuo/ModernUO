using System.Threading;

namespace Server.Network
{
  public class Pipe
  {
    internal readonly byte[] m_Buffer;
    internal volatile uint m_WriteIdx;
    internal volatile uint m_ReadIdx;

    internal volatile bool m_AwaitBeginning;
    internal volatile WaitCallback m_ReaderContinuation;

    public PipeWriter Writer { get; }
    public PipeReader Reader { get; }

    public uint Size => (uint)m_Buffer.Length;

    public Pipe(byte[] buf)
    {
      m_Buffer = buf;
      m_WriteIdx = 0;
      m_ReadIdx = 0;

      Writer = new PipeWriter(this);
      Reader = new PipeReader(this);
    }
  }
};
