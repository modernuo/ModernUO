using System;
using System.Linq;
using System.Reflection;

namespace Server.Utilities
{
  public static class ActivatorUtil
  {

    public static ConstructorInfo GetConstructor(Type type)
    {
      try
      {
        return type.GetConstructor(Type.EmptyTypes)
          ?? type.GetConstructors().Single(info => info.GetParameters().All(x => x.IsOptional))
          ?? throw new TypeInitializationException(type.ToString(), new Exception($"There is no empty/default constructor for {type}"));
      }
      catch (Exception e)
      {
        throw new TypeInitializationException(type.ToString(), e);
      }
    }
    public static ConstructorInfo GetConstructor(Type type, params Type[] args)
    {
      try
      {
        ConstructorInfo cctor = null;
        if (args.All(x => x != null))
          cctor = type.GetConstructor(args);
        if (cctor == null)
          cctor = type.GetConstructors().Single(info =>
          {
            var paramList = info.GetParameters().ToList();
            // If more args are given than parameters, skip.
            if (args.Length > paramList.Count)
              return false;
            // check all given args map to params.
            for (int i = 0; i < args.Length; i++)
            {
              // if a null reference is passed, but the type is not nullable, skip.
              if (args[i] == null)
              {
                if (paramList[i].ParameterType.IsValueType) return false;
              }
              // If an arg is not assignable to the parameter type, skip.
              else if (!paramList[i].ParameterType.IsAssignableFrom(args[i]))
                return false;
            }
            // If there are more parameters, check if they any are not optional, if any are not, skip.
            if (args.Length < paramList.Count && paramList.GetRange(args.Length, paramList.Count - args.Length).Any(x => !x.IsOptional))
              return false;
            // All checks have passed. We have found a match.
            return true;
          });
        if (cctor == null)
          throw new TypeInitializationException(type.ToString(), new Exception($"There is no empty/default constructor for {type}"));
        else
          return cctor;
      }
      catch (Exception e)
      {
        throw new TypeInitializationException(type.ToString(), e);
      }
    }
    public static object CreateInstance(Type type)
    {
      var cctor = GetConstructor(type);
      var args = cctor.GetParameters();
      if (args.Length == 0)
        return cctor.Invoke(Type.EmptyTypes);
      else
      {
        var argList = new object[args.Length];
        Array.Fill(argList, Type.Missing);
        return cctor.Invoke(argList);
      }
    }
    public static object CreateInstance(Type type, params object[] args)
    {
      if (args == null || args.Length == 0)
        return CreateInstance(type);
      var cctor = GetConstructor(type, args.Select(x => x?.GetType()).ToArray());
      return cctor.Invoke(args);
    }

    public static T CreateInstance<T>() => (T)CreateInstance(typeof(T));
    public static T CreateInstance<T>(params object[] args) => (T)CreateInstance(typeof(T), args);
  }
}
