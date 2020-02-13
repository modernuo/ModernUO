using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Utilities
{
  public static class ActivatorUtil
  {
    public static object CreateInstance(Type type)
    {
      try
      {
        if (type.GetConstructor(Type.EmptyTypes) != null)
          return Activator.CreateInstance(type);
        else
        {
          var emptyConstructor = type.GetConstructors().Single(info =>
          {
            var paramList = info.GetParameters();
            return paramList.Length == 1 && paramList[0].IsOptional;
          });
          if (emptyConstructor != null)
            return emptyConstructor.Invoke(new object[] { Type.Missing });
          else
            throw new TypeInitializationException(type.ToString(), new Exception($"There is no empty/default constructor for {type}"));
        }
      }
      catch (NullReferenceException e)
      {
        throw new TypeInitializationException(type.ToString(), new Exception($"There is no empty/default constructor for {type}", e));
      }
    }
    public static object CreateInstance(Type type, params object[] args)
    {
      if (args == null || args.Length == 0)
        return CreateInstance(type);
      try
      {
        return Activator.CreateInstance(type, args);
      }
      catch (NullReferenceException e)
      {
        throw new TypeInitializationException(type.ToString(), new Exception($"There is no constructor for {type} with the given parameter types", e));
      }
      throw new TypeInitializationException(type.ToString(), new Exception($"There is no constructor for {type} with the given parameter types"));
    }

    public static T CreateInstance<T>() => (T)CreateInstance(typeof(T));
  }
}
