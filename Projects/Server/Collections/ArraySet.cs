using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Collections
{
  public class ArraySet<T> : IList<T>
  {
    private readonly List<T> m_List = new List<T>();

    public T this[int index] { get => m_List[index]; set => m_List[index] = value; }

    public int Count => m_List.Count;

    public bool IsReadOnly => false;

    public int Add(T item)
    {
      int indexOf = m_List.IndexOf(item);

      if (indexOf >= 0) return indexOf;

      m_List.Add(item);
      return m_List.Count - 1;
    }

    public void Clear()
    {
      m_List.Clear();
    }

    public bool Contains(T item) => m_List.Contains(item);

    public void CopyTo(T[] array)
    {
      m_List.CopyTo(array);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      m_List.CopyTo(array, arrayIndex);
    }

    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
      m_List.CopyTo(index, array, arrayIndex, count);
    }

    public IEnumerator<T> GetEnumerator() => m_List.GetEnumerator();

    public int IndexOf(T item) => m_List.IndexOf(item);

    public void Insert(int index, T item)
    {
      throw new NotImplementedException();
    }

    public bool Remove(T item) => throw new NotImplementedException();

    public void RemoveAt(int index)
    {
      throw new NotImplementedException();
    }

    void ICollection<T>.Add(T item)
    {
      m_List.Add(item);
    }

    IEnumerator IEnumerable.GetEnumerator() => m_List.GetEnumerator();
  }
}
