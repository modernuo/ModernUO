using System;

namespace Server.Engines.Craft
{
  public class CraftSubRes
  {
    public CraftSubRes(Type type, TextDefinition name, int reqSkill, object message) : this(type, name, reqSkill, 0,
      message)
    {
    }

    public CraftSubRes(Type type, TextDefinition name, int reqSkill, int genericNameNumber, object message)
    {
      ItemType = type;
      NameNumber = name;
      NameString = name;
      RequiredSkill = reqSkill;
      GenericNameNumber = genericNameNumber;
      Message = message;
    }

    public Type ItemType{ get; }

    public string NameString{ get; }

    public int NameNumber{ get; }

    public int GenericNameNumber{ get; }

    public object Message{ get; }

    public int RequiredSkill{ get; }
  }
}
