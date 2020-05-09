using System;
using System.Collections;

namespace Server.Engines.Craft
{
  public class CraftItemCol : CollectionBase
  {
    public int Add(CraftItem craftItem) => List.Add(craftItem);

    public void Remove(int index)
    {
      if (index > Count - 1 || index < 0)
      {
      }
      else
      {
        List.RemoveAt(index);
      }
    }

    public CraftItem GetAt(int index) => (CraftItem)List[index];

    public CraftItem SearchForSubclass(Type type)
    {
      for (int i = 0; i < List.Count; i++)
      {
        CraftItem craftItem = (CraftItem)List[i];

        if (craftItem.ItemType == type || type.IsSubclassOf(craftItem.ItemType))
          return craftItem;
      }

      return null;
    }

    public CraftItem SearchFor(Type type)
    {
      for (int i = 0; i < List.Count; i++)
      {
        CraftItem craftItem = (CraftItem)List[i];
        if (craftItem.ItemType == type) return craftItem;
      }

      return null;
    }
  }
}