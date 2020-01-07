using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Server.Collections
{
  public static class ObjectPool
  {
    private static class Pool<T>
    {
      private static readonly ConcurrentBag<T> pool = new ConcurrentBag<T>();
      
      /// <summary>
      /// Methode puts an object in pool. Furthermore methode does not need
      /// a lock to be thread safe because of internal handling of ConcurrentBag.
      /// </summary>
      /// <param name="obj"></param>
      public static void PutObject(T obj)
      {
        pool.Add(obj);
      }

      /// <summary>
      /// Methode gets an object of pool. Furthermore methode does not need
      /// a lock to be thread safe because of internal handling of ConcurrentBag.
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public static bool TryGetObject(out T obj)
      {
        if (pool.Count > 0)
        {
          pool.TryTake(out obj);
          return true;

        }
        obj = default(T);
        return false;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    public static void Put<T>(T obj)
    {
      Pool<T>.PutObject(obj);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    private static bool TryGet<T>(out T obj)
    {
      return Pool<T>.TryGetObject(out obj);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetOrDefault<T>()
    {
      T ret;
      TryGet(out ret);
      return ret;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T Get<T>() where T : new()
    {
      T ret;
      return TryGet(out ret) ? ret : new T();
    }
  }
}
