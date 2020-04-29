using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server
{
  public delegate void ValidationEventHandler();

  public static class ValidationQueue
  {
    public static event ValidationEventHandler StartValidation;

    public static void Initialize()
    {
      StartValidation?.Invoke();
      StartValidation = null;
    }
  }

  public static class ValidationQueue<T>
  {
    // TODO: Find a better way
#pragma warning disable CA1000 // Do not declare static members on generic types
    private static List<T> m_Queue;
#pragma warning restore CA1000 // Do not declare static members on generic types

    static ValidationQueue()
    {
      m_Queue = new List<T>();
      ValidationQueue.StartValidation += ValidateAll;
    }

#pragma warning disable CA1000 // Do not declare static members on generic types
    public static void Add(T obj)
    {
      m_Queue.Add(obj);
    }
#pragma warning restore CA1000 // Do not declare static members on generic types

    private static void ValidateAll()
    {
      Type type = typeof(T);

      MethodInfo m = type.GetMethod("Validate", BindingFlags.Instance | BindingFlags.Public);

      if (m != null)
        for (int i = 0; i < m_Queue.Count; ++i)
          m.Invoke(m_Queue[i], null);

      m_Queue.Clear();
      m_Queue = null;
    }
  }
}
