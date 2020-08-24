using System;
using System.Collections.Generic;

namespace Server.Commands.Generic
{
    public sealed class ObjectConditional
    {
        private static readonly Type typeofItem = typeof(Item);
        private static readonly Type typeofMobile = typeof(Mobile);

        public static readonly ObjectConditional Empty = new ObjectConditional(null, null);

        private IConditional[] m_Conditionals;

        private readonly ICondition[][] m_Conditions;

        public ObjectConditional(Type objectType, ICondition[][] conditions)
        {
            Type = objectType;
            m_Conditions = conditions;
        }

        public Type Type { get; }

        public bool IsItem => Type == null || Type == typeofItem || Type.IsSubclassOf(typeofItem);

        public bool IsMobile => Type == null || Type == typeofMobile || Type.IsSubclassOf(typeofMobile);

        public bool HasCompiled => m_Conditionals != null;

        public void Compile(ref AssemblyEmitter emitter)
        {
            emitter ??= new AssemblyEmitter("__dynamic");

            m_Conditionals = new IConditional[m_Conditions.Length];

            for (int i = 0; i < m_Conditionals.Length; ++i)
                m_Conditionals[i] = ConditionalCompiler.Compile(emitter, Type, m_Conditions[i], i);
        }

        public bool CheckCondition(object obj)
        {
            if (Type == null)
                return true; // null type means no condition

            if (!HasCompiled)
            {
                AssemblyEmitter emitter = null;

                Compile(ref emitter);
            }

            for (int i = 0; i < m_Conditionals.Length; ++i)
                if (m_Conditionals[i].Verify(obj))
                    return true;

            return false; // all conditions false
        }

        public static ObjectConditional Parse(Mobile from, ref string[] args)
        {
            string[] conditionArgs = null;

            for (int i = 0; i < args.Length; ++i)
                if (Insensitive.Equals(args[i], "where"))
                {
                    string[] origArgs = args;

                    args = new string[i];

                    for (int j = 0; j < args.Length; ++j)
                        args[j] = origArgs[j];

                    conditionArgs = new string[origArgs.Length - i - 1];

                    for (int j = 0; j < conditionArgs.Length; ++j)
                        conditionArgs[j] = origArgs[i + j + 1];

                    break;
                }

            return ParseDirect(from, conditionArgs, 0, conditionArgs?.Length ?? 0);
        }

        public static ObjectConditional ParseDirect(Mobile from, string[] args, int offset, int size)
        {
            if (args == null || size == 0)
                return Empty;

            int index = 0;

            Type objectType = AssemblyHandler.FindFirstTypeForName(args[offset + index], true);

            if (objectType == null)
                throw new Exception($"No type with that name ({args[offset + index]}) was found.");

            ++index;

            List<ICondition[]> conditions = new List<ICondition[]>();
            List<ICondition> current = new List<ICondition>();

            current.Add(TypeCondition.Default);

            while (index < size)
            {
                string cur = args[offset + index];

                bool inverse = false;

                if (Insensitive.Equals(cur, "not") || cur == "!")
                {
                    inverse = true;
                    ++index;

                    if (index >= size)
                        throw new Exception("Improperly formatted object conditional.");
                }
                else if (Insensitive.Equals(cur, "or") || cur == "||")
                {
                    if (current.Count > 1)
                    {
                        conditions.Add(current.ToArray());

                        current.Clear();
                        current.Add(TypeCondition.Default);
                    }

                    ++index;

                    continue;
                }

                string binding = args[offset + index];
                index++;

                if (index >= size)
                    throw new Exception("Improperly formatted object conditional.");

                string oper = args[offset + index];
                index++;

                if (index >= size)
                    throw new Exception("Improperly formatted object conditional.");

                string val = args[offset + index];
                index++;

                Property prop = new Property(binding);

                prop.BindTo(objectType, PropertyAccess.Read);
                prop.CheckAccess(from);

                var condition = oper switch
                {
                    "=" => (ICondition)new ComparisonCondition(prop, inverse, ComparisonOperator.Equal, val),
                    "==" => new ComparisonCondition(prop, inverse, ComparisonOperator.Equal, val),
                    "is" => new ComparisonCondition(prop, inverse, ComparisonOperator.Equal, val),
                    "!=" => new ComparisonCondition(prop, inverse, ComparisonOperator.NotEqual, val),
                    ">" => new ComparisonCondition(prop, inverse, ComparisonOperator.Greater, val),
                    "<" => new ComparisonCondition(prop, inverse, ComparisonOperator.Lesser, val),
                    ">=" => new ComparisonCondition(prop, inverse, ComparisonOperator.GreaterEqual, val),
                    "<=" => new ComparisonCondition(prop, inverse, ComparisonOperator.LesserEqual, val),
                    "==~" => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "~==" => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "=~" => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "~=" => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "is~" => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "~is" => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "!=~" => new StringCondition(prop, inverse, StringOperator.NotEqual, val, true),
                    "~!=" => new StringCondition(prop, inverse, StringOperator.NotEqual, val, true),
                    "starts" => new StringCondition(prop, inverse, StringOperator.StartsWith, val, false),
                    "starts~" => new StringCondition(prop, inverse, StringOperator.StartsWith, val, true),
                    "~starts" => new StringCondition(prop, inverse, StringOperator.StartsWith, val, true),
                    "ends" => new StringCondition(prop, inverse, StringOperator.EndsWith, val, false),
                    "ends~" => new StringCondition(prop, inverse, StringOperator.EndsWith, val, true),
                    "~ends" => new StringCondition(prop, inverse, StringOperator.EndsWith, val, true),
                    "contains" => new StringCondition(prop, inverse, StringOperator.Contains, val, false),
                    "contains~" => new StringCondition(prop, inverse, StringOperator.Contains, val, true),
                    "~contains" => new StringCondition(prop, inverse, StringOperator.Contains, val, true),
                    _ => null
                };

                if (condition == null)
                    throw new InvalidOperationException($"Unrecognized operator (\"{oper}\").");

                current.Add(condition);
            }

            conditions.Add(current.ToArray());

            return new ObjectConditional(objectType, conditions.ToArray());
        }
    }
}
