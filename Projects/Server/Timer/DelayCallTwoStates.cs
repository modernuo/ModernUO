using System;

namespace Server
{
  public partial class Timer
  {
    public delegate void TimerStateCallback<in T1, in T2>(T1 t1, T2 t2);

    public static Timer DelayCall<T1, T2>(TimerStateCallback<T1, T2> callback, T1 t1, T2 t2) =>
      DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, t1, t2);

    public static Timer DelayCall<T1, T2>(TimeSpan delay, TimerStateCallback<T1, T2> callback, T1 t1, T2 t2) =>
      DelayCall(delay, TimeSpan.Zero, 1, callback, t1, t2);

    public static Timer DelayCall<T1, T2>(TimeSpan delay, TimeSpan interval, TimerStateCallback<T1, T2> callback, T1 t1, T2 t2) =>
      DelayCall(delay, interval, 0, callback, t1, t2);

    public static Timer DelayCall<T1, T2>(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T1, T2> callback,
      T1 t1, T2 t2)
    {
      Timer t = new DelayStateCallTimer<T1, T2>(delay, interval, count, callback, t1, t2);

      t.Priority = ComputePriority(count == 1 ? delay : interval);

      t.Start();

      return t;
    }

    private class DelayStateCallTimer<T1, T2> : Timer
    {
      private readonly T1 m_T1;
      private readonly T2 m_T2;

      public DelayStateCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T1, T2> callback,
        T1 t1, T2 t2) : base(delay, interval, count)
      {
        Callback = callback;
        m_T1 = t1;
        m_T2 = t2;

        RegCreation();
      }

      public TimerStateCallback<T1, T2> Callback { get; }

      public override bool DefRegCreation => false;

      protected override void OnTick()
      {
        Callback?.Invoke(m_T1, m_T2);
      }

      public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }
  }
}
