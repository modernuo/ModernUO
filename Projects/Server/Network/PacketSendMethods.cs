using System;

namespace Server.Network
{
  #region Fixed Length Packet Delegates
  public delegate WriteFixedPacketMethod FixedPacketMethod(out int length);
  public delegate void WriteFixedPacketMethod(Memory<byte> mem);

  public delegate WriteFixedPacketMethod<T1> FixedPacketMethod<T1>(out int length);
  public delegate void WriteFixedPacketMethod<T1>(Memory<byte> mem, T1 t1);

  public delegate WriteFixedPacketMethod<T1, T2> FixedPacketMethod<T1, T2>(out int length);
  public delegate void WriteFixedPacketMethod<T1, T2>(Memory<byte> mem, T1 t1, T2 t2);

  public delegate WriteFixedPacketMethod<T1, T2, T3> FixedPacketMethod<T1, T2, T3>(out int length);
  public delegate void WriteFixedPacketMethod<T1, T2, T3>(Memory<byte> mem, T1 t1, T2 t2, T3 t3);

  public delegate WriteFixedPacketMethod<T1, T2, T3, T4> FixedPacketMethod<T1, T2, T3, T4>(out int length);
  public delegate void WriteFixedPacketMethod<T1, T2, T3, T4>(Memory<byte> mem, T1 t1, T2 t2, T3 t3, T4 t4);

  public delegate WriteFixedPacketMethod<T1, T2, T3, T4, T5> FixedPacketMethod<T1, T2, T3, T4, T5>(out int length);
  public delegate void WriteFixedPacketMethod<T1, T2, T3, T4, T5>(Memory<byte> mem, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);

  public delegate WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6> FixedPacketMethod<T1, T2, T3, T4, T5, T6>(out int length);
  public delegate void WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6>(Memory<byte> mem, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);

  public delegate WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6, T7> FixedPacketMethod<T1, T2, T3, T4, T5, T6, T7>(out int length);
  public delegate void WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6, T7>(Memory<byte> mem, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);

  public delegate WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8> FixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8>(out int length);
  public delegate void WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8>(Memory<byte> mem, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);

  public delegate WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> FixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9>(out int length);
  public delegate void WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Memory<byte> mem, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9);
  #endregion

  #region Dynamic Length Packet Delegates
  public delegate WriteDynamicPacketMethod DynamicPacketMethod(out int length);
  public delegate int WriteDynamicPacketMethod(Memory<byte> mem, int length);

  public delegate WriteDynamicPacketMethod<T1> DynamicPacketMethod<T1>(out int length, T1 t1);
  public delegate int WriteDynamicPacketMethod<T1>(Memory<byte> mem, int length, T1 t1);

  public delegate WriteDynamicPacketMethod<T1, T2> DynamicPacketMethod<T1, T2>(out int length, T1 t1, T2 t2);
  public delegate int WriteDynamicPacketMethod<T1, T2>(Memory<byte> mem, int length, T1 t1, T2 t2);

  public delegate WriteDynamicPacketMethod<T1, T2, T3> DynamicPacketMethod<T1, T2, T3>(out int length, T1 t1, T2 t2, T3 t3);
  public delegate int WriteDynamicPacketMethod<T1, T2, T3>(Memory<byte> mem, int length, T1 t1, T2 t2, T3 t3);

  public delegate WriteDynamicPacketMethod<T1, T2, T3, T4> DynamicPacketMethod<T1, T2, T3, T4>(out int length, T1 t1, T2 t2, T3 t3, T4 t4);
  public delegate int WriteDynamicPacketMethod<T1, T2, T3, T4>(Memory<byte> mem, int length, T1 t1, T2 t2, T3 t3, T4 t4);

  public delegate WriteDynamicPacketMethod<T1, T2, T3, T4, T5> DynamicPacketMethod<T1, T2, T3, T4, T5>(out int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
  public delegate int WriteDynamicPacketMethod<T1, T2, T3, T4, T5>(Memory<byte> mem, int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);

  public delegate WriteDynamicPacketMethod<T1, T2, T3, T4, T5, T6> DynamicPacketMethod<T1, T2, T3, T4, T5, T6>(out int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
  public delegate int WriteDynamicPacketMethod<T1, T2, T3, T4, T5, T6>(Memory<byte> mem, int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);

  public delegate WriteDynamicPacketMethod<T1, T2, T3, T4, T5, T6, T7> DynamicPacketMethod<T1, T2, T3, T4, T5, T6, T7>(out int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
  public delegate int WriteDynamicPacketMethod<T1, T2, T3, T4, T5, T6, T7>(Memory<byte> mem, int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);

  public delegate WriteDynamicPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8> DynamicPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8>(out int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);
  public delegate int WriteDynamicPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8>(Memory<byte> mem, int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);

  public delegate WriteDynamicPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> DynamicPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9>(out int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9);
  public delegate int WriteDynamicPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Memory<byte> mem, int length, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9);
  #endregion

  public static partial class Packets
  {
    public static void TestSend(NetState ns)
    {
      Send(ns, SetArrowHS, (short)1, (short)1, Serial.MinusOne);
      Send(ns, DisplaySecureTrade, Serial.MinusOne, Serial.MinusOne, Serial.MinusOne, "name");
    }

    #region Send Fixed Packet Methods
    public static void Send(NetState ns, FixedPacketMethod f)
    {
      WriteFixedPacketMethod func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem);
      _ = ns.Flush(length);
    }

    public static void Send<T1>(NetState ns, FixedPacketMethod<T1> f, T1 t1)
    {
      WriteFixedPacketMethod<T1> func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem, t1);
      _ = ns.Flush(length);
    }

    public static void Send<T1, T2>(NetState ns, FixedPacketMethod<T1, T2> f, T1 t1, T2 t2)
    {
      WriteFixedPacketMethod<T1, T2> func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem, t1, t2);
      _ = ns.Flush(length);
    }

    public static void Send<T1, T2, T3>(NetState ns, FixedPacketMethod<T1, T2, T3> f, T1 t1, T2 t2, T3 t3)
    {
      WriteFixedPacketMethod<T1, T2, T3> func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem, t1, t2, t3);
      _ = ns.Flush(length);
    }

    public static void Send<T1, T2, T3, T4>(NetState ns, FixedPacketMethod<T1, T2, T3, T4> f, T1 t1, T2 t2, T3 t3, T4 t4)
    {
      WriteFixedPacketMethod<T1, T2, T3, T4> func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem, t1, t2, t3, t4);
      _ = ns.Flush(length);
    }

    public static void Send<T1, T2, T3, T4, T5>(NetState ns, FixedPacketMethod<T1, T2, T3, T4, T5> f, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
    {
      WriteFixedPacketMethod<T1, T2, T3, T4, T5> func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem, t1, t2, t3, t4, t5);
      _ = ns.Flush(length);
    }

    public static void Send<T1, T2, T3, T4, T5, T6>(NetState ns, FixedPacketMethod<T1, T2, T3, T4, T5, T6> f, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
    {
      WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6> func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem, t1, t2, t3, t4, t5, t6);
      _ = ns.Flush(length);
    }

    public static void Send<T1, T2, T3, T4, T5, T6, T7>(NetState ns, FixedPacketMethod<T1, T2, T3, T4, T5, T6, T7> f, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
    {
      WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6, T7> func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem, t1, t2, t3, t4, t5, t6, t7);
      _ = ns.Flush(length);
    }

    public static void Send<T1, T2, T3, T4, T5, T6, T7, T8>(NetState ns, FixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8> f, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
    {
      WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8> func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem, t1, t2, t3, t4, t5, t6, t7, t8);
      _ = ns.Flush(length);
    }

    public static void Send<T1, T2, T3, T4, T5, T6, T7, T8, T9>(NetState ns, FixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> f, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9)
    {
      WriteFixedPacketMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      func(mem, t1, t2, t3, t4, t5, t6, t7, t8, t9);
      _ = ns.Flush(length);
    }
    #endregion

    #region Send Dynamic Packet Methods
    public static void Send(NetState ns, DynamicPacketMethod f)
    {
      WriteDynamicPacketMethod func = f(out int length);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      int bytesWritten = func(mem, length);
      _ = ns.Flush(bytesWritten);
    }

    public static void Send<T1>(NetState ns, DynamicPacketMethod<T1> f, T1 t1)
    {
      WriteDynamicPacketMethod<T1> func = f(out int length, t1);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      int bytesWritten = func(mem, length, t1);
      _ = ns.Flush(bytesWritten);
    }

    public static void Send<T1, T2>(NetState ns, DynamicPacketMethod<T1, T2> f, T1 t1, T2 t2)
    {
      WriteDynamicPacketMethod<T1, T2> func = f(out int length, t1, t2);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      int bytesWritten = func(mem, length, t1, t2);
      _ = ns.Flush(bytesWritten);
    }

    public static void Send<T1, T2, T3>(NetState ns, DynamicPacketMethod<T1, T2, T3> f, T1 t1, T2 t2, T3 t3)
    {
      WriteDynamicPacketMethod<T1, T2, T3> func = f(out int length, t1, t2, t3);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      int bytesWritten = func(mem, length, t1, t2, t3);
      _ = ns.Flush(bytesWritten);
    }

    public static void Send<T1, T2, T3, T4>(NetState ns, DynamicPacketMethod<T1, T2, T3, T4> f, T1 t1, T2 t2, T3 t3, T4 t4)
    {
      WriteDynamicPacketMethod<T1, T2, T3, T4> func = f(out int length, t1, t2, t3, t4);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      int bytesWritten = func(mem, length, t1, t2, t3, t4);
      _ = ns.Flush(bytesWritten);
    }

    public static void Send<T1, T2, T3, T4, T5>(NetState ns, DynamicPacketMethod<T1, T2, T3, T4, T5> f, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
    {
      WriteDynamicPacketMethod<T1, T2, T3, T4, T5> func = f(out int length, t1, t2, t3, t4, t5);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      int bytesWritten = func(mem, length, t1, t2, t3, t4, t5);
      _ = ns.Flush(bytesWritten);
    }

    public static void Send<T1, T2, T3, T4, T5, T6>(NetState ns, DynamicPacketMethod<T1, T2, T3, T4, T5, T6> f, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
    {
      WriteDynamicPacketMethod<T1, T2, T3, T4, T5, T6> func = f(out int length, t1, t2, t3, t4, t5, t6);
      Memory<byte> mem = ns.SendPipe.Writer.GetMemory(length);
      int bytesWritten = func(mem, length, t1, t2, t3, t4, t5, t6);
      _ = ns.Flush(bytesWritten);
    }
    #endregion
  }
}
