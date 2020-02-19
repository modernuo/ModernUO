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
        return args.All(x => x != null) ? type.GetConstructor(args) : null ?? type.GetConstructors().Single(info =>
          {
            var paramList = info.GetParameters().ToList();
            // If more args are given than parameters, skip.
            if (args.Length > paramList.Count)
              return false;
            // check all given args map to params.
            for (int i = 0; i < args.Length; i++)
            {
              // if a null reference is passed, but the type is not nullable
              if ((args[i] == null && paramList[i].ParameterType.IsValueType)
              // or if an arg is not null and is not assignable to the parameter type, skip.
                || !(args[i] == null || paramList[i].ParameterType.IsAssignableFrom(args[i])))
                return false;
            }
            // If there are more parameters, check if they any are not optional, if any are not, skip.
            // Otherwise all checks have passed. We have found a match 
            return args.Length <= paramList.Count || paramList.GetRange(args.Length, paramList.Count - args.Length).All(x => x.IsOptional);
          }) ?? throw new Exception($"There is no empty/default constructor for {type}");
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
      var argList = new object[args.Length];
      Array.Fill(argList, Type.Missing);
      return cctor.Invoke(argList);
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
