using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Server.Network
{
  public class SendQueue<T>
  {
    private BlockingCollection<T> m_Queue = new BlockingCollection<T>(new ConcurrentQueue<T>());

    public SendQueue()
    {
    }

    public void Enqueue(T t)
    {
      m_Queue.Add(t);
    }

    public Task<T> DequeueAsync()
    {
      TaskCompletionSource<T> taskCompletion = new TaskCompletionSource<T>();
      Task.Run(() => taskCompletion.SetResult(Dequeue()));
      return taskCompletion.Task;
    }

    public T Dequeue()
    {
      return m_Queue.Take();
    }
  }
}
