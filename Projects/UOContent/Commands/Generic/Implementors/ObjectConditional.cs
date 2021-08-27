using System;
using System.Collections.Generic;

using static Server.Types;

namespace Server.Commands.Generic
{
    public sealed class ObjectConditional
    {
        public static readonly ObjectConditional Empty = new(null, null);

        private readonly ICondition[][] m_Conditions;

        private IConditional[] m_Conditionals;

        public ObjectConditional(Type objectType, ICondition[][] conditions)
        {
            Type = objectType;
            m_Conditions = conditions;
        }

        public Type Type { get; }

        public bool IsItem => Type?.IsAssignableTo(OfItem) != false;

        public bool IsMobile => Type?.IsAssignableTo(OfMobile) != false;

        public bool HasCompiled => m_Conditionals != null;

        public void Compile(ref AssemblyEmitter emitter)
        {
            emitter ??= new AssemblyEmitter("__dynamic");

            m_Conditionals = new IConditional[m_Conditions.Length];

            for (var i = 0; i < m_Conditionals.Length; ++i)
            {
                m_Conditionals[i] = ConditionalCompiler.Compile(emitter, Type, m_Conditions[i], i);
            }
        }

        public bool CheckCondition(object obj)
        {
            if (Type == null)
            {
                return true; // null type means no condition
            }

            if (!HasCompiled)
            {
                AssemblyEmitter emitter = null;

                Compile(ref emitter);
            }

            for (var i = 0; i < m_Conditionals.Length; ++i)
            {
                if (m_Conditionals[i].Verify(obj))
                {
                    return true;
                }
            }

            return false; // all conditions false
        }

        public static ObjectConditional Parse(Mobile from, ref string[] args)
        {
            string[] conditionArgs = null;

            for (var i = 0; i < args.Length; ++i)
            {
                if (args[i].InsensitiveEquals("where"))
                {
                    var origArgs = args;

                    args = new string[i];

                    for (var j = 0; j < args.Length; ++j)
                    {
                        args[j] = origArgs[j];
                    }

                    conditionArgs = new string[origArgs.Length - i - 1];

                    for (var j = 0; j < conditionArgs.Length; ++j)
                    {
                        conditionArgs[j] = origArgs[i + j + 1];
                    }

                    break;
                }
            }

            return ParseDirect(from, conditionArgs, 0, conditionArgs?.Length ?? 0);
        }

        public static ObjectConditional ParseDirect(Mobile from, string[] args, int offset, int size)
        {
            if (args == null || size == 0)
            {
                return Empty;
            }

            var objectType = AssemblyHandler.FindTypeByName(args[offset]);

            if (objectType == null)
            {
                throw new Exception($"No type with that name ({args[offset]}) was found.");
            }

            var conditions = new List<ICondition[]>();
            var current = new List<ICondition> { TypeCondition.Default };

            var index = 1;

            while (index < size)
            {
                var cur = args[offset + index];

                var inverse = false;

                if (cur.InsensitiveEquals("not") || cur == "!")
                {
                    inverse = true;
                    ++index;

                    if (index >= size)
                    {
                        throw new Exception("Improperly formatted object conditional.");
                    }
                }
                else if (cur.InsensitiveEquals("or") || cur == "||")
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

                var binding = args[offset + index];
                index++;

                if (index >= size)
                {
                    throw new Exception("Improperly formatted object conditional.");
                }

                var oper = args[offset + index];
                index++;

                if (index >= size)
                {
                    throw new Exception("Improperly formatted object conditional.");
                }

                var val = args[offset + index];
                index++;

                var prop = new Property(binding);

                prop.BindTo(objectType, PropertyAccess.Read);
                prop.CheckAccess(from);

                var condition = oper switch
                {
                    "="         => (ICondition)new ComparisonCondition(prop, inverse, ComparisonOperator.Equal, val),
                    "=="        => new ComparisonCondition(prop, inverse, ComparisonOperator.Equal, val),
                    "is"        => new ComparisonCondition(prop, inverse, ComparisonOperator.Equal, val),
                    "!="        => new ComparisonCondition(prop, inverse, ComparisonOperator.NotEqual, val),
                    ">"         => new ComparisonCondition(prop, inverse, ComparisonOperator.Greater, val),
                    "<"         => new ComparisonCondition(prop, inverse, ComparisonOperator.Lesser, val),
                    ">="        => new ComparisonCondition(prop, inverse, ComparisonOperator.GreaterEqual, val),
                    "<="        => new ComparisonCondition(prop, inverse, ComparisonOperator.LesserEqual, val),
                    "==~"       => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "~=="       => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "=~"        => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "~="        => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "is~"       => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "~is"       => new StringCondition(prop, inverse, StringOperator.Equal, val, true),
                    "!=~"       => new StringCondition(prop, inverse, StringOperator.NotEqual, val, true),
                    "~!="       => new StringCondition(prop, inverse, StringOperator.NotEqual, val, true),
                    "starts"    => new StringCondition(prop, inverse, StringOperator.StartsWith, val, false),
                    "starts~"   => new StringCondition(prop, inverse, StringOperator.StartsWith, val, true),
                    "~starts"   => new StringCondition(prop, inverse, StringOperator.StartsWith, val, true),
                    "ends"      => new StringCondition(prop, inverse, StringOperator.EndsWith, val, false),
                    "ends~"     => new StringCondition(prop, inverse, StringOperator.EndsWith, val, true),
                    "~ends"     => new StringCondition(prop, inverse, StringOperator.EndsWith, val, true),
                    "contains"  => new StringCondition(prop, inverse, StringOperator.Contains, val, false),
                    "contains~" => new StringCondition(prop, inverse, StringOperator.Contains, val, true),
                    "~contains" => new StringCondition(prop, inverse, StringOperator.Contains, val, true),
                    _            => null
                };

                if (condition == null)
                {
                    throw new InvalidOperationException($"Unrecognized operator (\"{oper}\").");
                }

                current.Add(condition);
            }

            conditions.Add(current.ToArray());

            return new ObjectConditional(objectType, conditions.ToArray());
        }
    }
}
