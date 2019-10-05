using System.Collections;

namespace Server.Engines.Craft
{
  public class CraftSkillCol : CollectionBase
  {
    public void Add(CraftSkill craftSkill)
    {
      List.Add(craftSkill);
    }

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

    public CraftSkill GetAt(int index) => (CraftSkill)List[index];
  }
}