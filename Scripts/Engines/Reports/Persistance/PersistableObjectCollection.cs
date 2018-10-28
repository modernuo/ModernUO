using System;
using System.Collections;

namespace Server.Engines.Reports
{
  /// <summary>
  ///   Strongly typed collection of Server.Engines.Reports.PersistableObject.
  /// </summary>
  public class ObjectCollection : CollectionBase
  {
    /// <summary>
    ///   Gets or sets the value of the Server.Engines.Reports.PersistableObject at a specific position in the ObjectCollection.
    /// </summary>
    public PersistableObject this[int index]
    {
      get => (PersistableObject)List[index];
      set => List[index] = value;
    }

    /// <summary>
    ///   Append a Server.Engines.Reports.PersistableObject entry to this collection.
    /// </summary>
    /// <param name="value">Server.Engines.Reports.PersistableObject instance.</param>
    /// <returns>The position into which the new element was inserted.</returns>
    public int Add(PersistableObject value)
    {
      return List.Add(value);
    }

    public void AddRange(PersistableObject[] col)
    {
      InnerList.AddRange(col);
    }

    /// <summary>
    ///   Determines whether a specified Server.Engines.Reports.PersistableObject instance is in this collection.
    /// </summary>
    /// <param name="value">Server.Engines.Reports.PersistableObject instance to search for.</param>
    /// <returns>True if the Server.Engines.Reports.PersistableObject instance is in the collection; otherwise false.</returns>
    public bool Contains(PersistableObject value)
    {
      return List.Contains(value);
    }

    /// <summary>
    ///   Retrieve the index a specified Server.Engines.Reports.PersistableObject instance is in this collection.
    /// </summary>
    /// <param name="value">Server.Engines.Reports.PersistableObject instance to find.</param>
    /// <returns>
    ///   The zero-based index of the specified Server.Engines.Reports.PersistableObject instance. If the object is not
    ///   found, the return value is -1.
    /// </returns>
    public int IndexOf(PersistableObject value)
    {
      return List.IndexOf(value);
    }

    /// <summary>
    ///   Removes a specified Server.Engines.Reports.PersistableObject instance from this collection.
    /// </summary>
    /// <param name="value">The Server.Engines.Reports.PersistableObject instance to remove.</param>
    public void Remove(PersistableObject value)
    {
      List.Remove(value);
    }

    /// <summary>
    ///   Returns an enumerator that can iterate through the Server.Engines.Reports.PersistableObject instance.
    /// </summary>
    /// <returns>An Server.Engines.Reports.PersistableObject's enumerator.</returns>
    public new ObjectCollectionEnumerator GetEnumerator()
    {
      return new ObjectCollectionEnumerator(this);
    }

    /// <summary>
    ///   Insert a Server.Engines.Reports.PersistableObject instance into this collection at a specified index.
    /// </summary>
    /// <param name="index">Zero-based index.</param>
    /// <param name="value">The Server.Engines.Reports.PersistableObject instance to insert.</param>
    public void Insert(int index, PersistableObject value)
    {
      List.Insert(index, value);
    }

    /// <summary>
    ///   Strongly typed enumerator of Server.Engines.Reports.PersistableObject.
    /// </summary>
    public class ObjectCollectionEnumerator : IEnumerator
    {
      /// <summary>
      ///   Collection to enumerate.
      /// </summary>
      private ObjectCollection _collection;

      /// <summary>
      ///   Current element pointed to.
      /// </summary>
      private PersistableObject _currentElement;

      /// <summary>
      ///   Current index
      /// </summary>
      private int _index;

      /// <summary>
      ///   Default constructor for enumerator.
      /// </summary>
      /// <param name="collection">Instance of the collection to enumerate.</param>
      internal ObjectCollectionEnumerator(ObjectCollection collection)
      {
        _index = -1;
        _collection = collection;
      }

      /// <summary>
      ///   Gets the Server.Engines.Reports.PersistableObject object in the enumerated ObjectCollection currently indexed by this
      ///   instance.
      /// </summary>
      public PersistableObject Current
      {
        get
        {
          if (_index == -1
              || _index >= _collection.Count)
            throw new IndexOutOfRangeException("Enumerator not started.");

          return _currentElement;
        }
      }

      /// <summary>
      ///   Gets the current element in the collection.
      /// </summary>
      object IEnumerator.Current
      {
        get
        {
          if (_index == -1
              || _index >= _collection.Count)
            throw new IndexOutOfRangeException("Enumerator not started.");

          return _currentElement;
        }
      }

      /// <summary>
      ///   Reset the cursor, so it points to the beginning of the enumerator.
      /// </summary>
      public void Reset()
      {
        _index = -1;
        _currentElement = null;
      }

      /// <summary>
      ///   Advances the enumerator to the next queue of the enumeration, if one is currently available.
      /// </summary>
      /// <returns>
      ///   true, if the enumerator was successfully advanced to the next queue; false, if the enumerator has reached the end
      ///   of the enumeration.
      /// </returns>
      public bool MoveNext()
      {
        if (_index
            < _collection.Count - 1)
        {
          _index = _index + 1;
          _currentElement = _collection[_index];
          return true;
        }

        _index = _collection.Count;
        return false;
      }
    }
  }
}