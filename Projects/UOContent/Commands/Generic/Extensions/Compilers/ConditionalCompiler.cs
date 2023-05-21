using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Server.Utilities;

namespace Server.Commands.Generic
{
    public interface IConditional
    {
        bool Verify(object obj);
    }

    public interface ICondition
    {
        // Invoked during the constructor
        void Construct(TypeBuilder typeBuilder, ILGenerator il, int index);

        // Target object will be loaded on the stack
        void Compile(MethodEmitter emitter);
    }

    public sealed class TypeCondition : ICondition
    {
        public static TypeCondition Default = new();

        void ICondition.Construct(TypeBuilder typeBuilder, ILGenerator il, int index)
        {
        }

        void ICondition.Compile(MethodEmitter emitter)
        {
            // The object was safely cast to be the conditionals type
            // If it's null, then the type cast didn't work...

            emitter.LoadNull();
            emitter.Compare(OpCodes.Ceq);
            emitter.LogicalNot();
        }
    }

    public sealed class PropertyValue
    {
        public PropertyValue(Type type, object value)
        {
            Type = type;
            Value = value;
        }

        public Type Type { get; }

        public object Value { get; private set; }

        public FieldInfo Field { get; private set; }

        public bool HasField => Field != null;

        public void Load(MethodEmitter method)
        {
            if (Field != null)
            {
                method.LoadArgument(0);
                method.LoadField(Field);
            }
            else if (Value == null)
            {
                method.LoadNull(Type);
            }
            else
            {
                if (Value is int i)
                {
                    method.Load(i);
                }
                else if (Value is long l)
                {
                    method.Load(l);
                }
                else if (Value is float f)
                {
                    method.Load(f);
                }
                else if (Value is double d)
                {
                    method.Load(d);
                }
                else if (Value is char c)
                {
                    method.Load(c);
                }
                else if (Value is bool b)
                {
                    method.Load(b);
                }
                else if (Value is string s)
                {
                    method.Load(s);
                }
                else if (Value is Enum e)
                {
                    method.Load(e);
                }
                else
                {
                    throw new InvalidOperationException("Unrecognized comparison value.");
                }
            }
        }

        public void Acquire(TypeBuilder typeBuilder, ILGenerator il, string fieldName)
        {
            if (Value is not string toParse)
            {
                return;
            }

            if (!Type.IsValueType && toParse == "null")
            {
                Value = null;
            }
            else if (Type == typeof(string))
            {
                if (toParse == @"@""null""")
                {
                    toParse = "null";
                }

                Value = toParse;
            }
            else if (Type.IsEnum)
            {
                Value = Enum.Parse(Type, toParse, true);
            }
            else
            {
                MethodInfo parseMethod;
                object[] parseArgs;

                var parseNumber = Type.GetMethod(
                    "Parse",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    Types.ParseStringNumericParamTypes,
                    null
                );

                if (parseNumber != null)
                {
                    var style = NumberStyles.Integer;

                    if (toParse.InsensitiveStartsWith("0x"))
                    {
                        style = NumberStyles.HexNumber;
                        toParse = toParse[2..];
                    }

                    parseMethod = parseNumber;
                    parseArgs = new object[] { toParse, style };
                }
                else
                {
                    var parseGeneral = Type.GetMethod(
                        "Parse",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        Types.ParseStringParamTypes,
                        null
                    );

                    parseMethod = parseGeneral;
                    parseArgs = new object[] { toParse, null };
                }

                if (parseMethod != null)
                {
                    Value = parseMethod.Invoke(null, parseArgs);

                    if (!Type.IsPrimitive)
                    {
                        Field = typeBuilder.DefineField(
                            fieldName,
                            Type,
                            FieldAttributes.Private | FieldAttributes.InitOnly
                        );

                        il.Emit(OpCodes.Ldarg_0);

                        il.Emit(OpCodes.Ldstr, toParse);

                        if (parseArgs.Length == 2) // dirty evil hack :-(
                        {
                            if (parseArgs[1]?.GetType() == typeof(NumberStyles))
                            {
                                il.Emit(OpCodes.Ldc_I4, (int)parseArgs[1]);
                            }
                            else
                            {
                                // IFormatProvider for `IParsable<T>.Parse()` method.
                                il.Emit(OpCodes.Ldnull);
                            }
                        }

                        il.Emit(OpCodes.Call, parseMethod);
                        il.Emit(OpCodes.Stfld, Field);
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unable to convert string \"{Value}\" into type '{Type}'."
                    );
                }
            }
        }
    }

    public abstract class PropertyCondition : ICondition
    {
        protected bool m_Not;
        protected Property m_Property;

        public PropertyCondition(Property property, bool not)
        {
            m_Property = property;
            m_Not = not;
        }

        public abstract void Construct(TypeBuilder typeBuilder, ILGenerator il, int index);

        public abstract void Compile(MethodEmitter emitter);
    }

    public enum StringOperator
    {
        Equal,
        NotEqual,

        Contains,
        StartsWith,
        EndsWith
    }

    public sealed class StringCondition : PropertyCondition
    {
        private readonly bool m_IgnoreCase;
        private readonly StringOperator m_Operator;
        private readonly PropertyValue m_Value;

        public StringCondition(Property property, bool not, StringOperator op, object value, bool ignoreCase)
            : base(property, not)
        {
            m_Operator = op;
            m_Value = new PropertyValue(property.Type, value);

            m_IgnoreCase = ignoreCase;
        }

        public override void Construct(TypeBuilder typeBuilder, ILGenerator il, int index)
        {
            m_Value.Acquire(typeBuilder, il, $"v{index}");
        }

        public override void Compile(MethodEmitter emitter)
        {
            var inverse = false;

            Type type = m_IgnoreCase ? typeof(InsensitiveStringHelpers) : typeof(OrdinalStringHelpers);
            string methodName;

            switch (m_Operator)
            {
                case StringOperator.NotEqual:
                    {
                        inverse = true;
                        goto case StringOperator.Equal;
                    }
                case StringOperator.Equal:
                    {
                        if (m_IgnoreCase)
                        {
                            methodName = "InsensitiveEquals";
                        }
                        else
                        {
                            methodName = "EqualsOrdinal";
                        }
                        break;
                    }

                case StringOperator.Contains:
                    {
                        if (m_IgnoreCase)
                        {
                            methodName = "InsensitiveContains";
                        }
                        else
                        {
                            methodName = "ContainsOrdinal";
                        }
                        break;
                    }

                case StringOperator.StartsWith:
                    {
                        if (m_IgnoreCase)
                        {
                            methodName = "InsensitiveStartsWith";
                        }
                        else
                        {
                            methodName = "StartsWithOrdinal";
                        }
                        break;
                    }

                case StringOperator.EndsWith:
                    {
                        if (m_IgnoreCase)
                        {
                            methodName = "InsensitiveEndsWith";
                        }
                        else
                        {
                            methodName = "EndsWithOrdinal";
                        }
                        break;
                    }

                default:
                    {
                        throw new InvalidOperationException("Invalid string comparison operator.");
                    }
            }

            if (m_Operator is StringOperator.Equal or StringOperator.NotEqual)
            {
                emitter.BeginCall(
                    type.GetMethod(
                        methodName,
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[]
                        {
                            typeof(string),
                            typeof(string)
                        },
                        null
                    )
                );

                emitter.Chain(m_Property);
                m_Value.Load(emitter);

                emitter.FinishCall();
            }
            else
            {
                var notNull = emitter.CreateLabel();
                var moveOn = emitter.CreateLabel();

                var temp = emitter.AcquireTemp(m_Property.Type);

                emitter.Chain(m_Property);

                emitter.StoreLocal(temp);
                emitter.LoadLocal(temp);

                emitter.BranchIfTrue(notNull);

                emitter.Load(false);
                emitter.Pop();
                emitter.Branch(moveOn);

                emitter.MarkLabel(notNull);
                emitter.LoadLocal(temp);

                emitter.BeginCall(
                    type.GetMethod(
                        methodName,
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new[]
                        {
                            typeof(string)
                        },
                        null
                    )
                );

                m_Value.Load(emitter);

                emitter.FinishCall();

                emitter.MarkLabel(moveOn);
            }

            if (m_Not != inverse)
            {
                emitter.LogicalNot();
            }
        }
    }

    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        Greater,
        GreaterEqual,
        Lesser,
        LesserEqual
    }

    public sealed class ComparisonCondition : PropertyCondition
    {
        private readonly ComparisonOperator m_Operator;
        private readonly PropertyValue m_Value;

        public ComparisonCondition(Property property, bool not, ComparisonOperator op, object value)
            : base(property, not)
        {
            m_Operator = op;
            m_Value = new PropertyValue(property.Type, value);
        }

        public override void Construct(TypeBuilder typeBuilder, ILGenerator il, int index)
        {
            m_Value.Acquire(typeBuilder, il, $"v{index}");
        }

        public override void Compile(MethodEmitter emitter)
        {
            emitter.Chain(m_Property);

            var inverse = false;

            var couldCompare =
                emitter.CompareTo(1, () => { m_Value.Load(emitter); });

            if (couldCompare)
            {
                emitter.Load(0);

                switch (m_Operator)
                {
                    case ComparisonOperator.Equal:
                        {
                            emitter.Compare(OpCodes.Ceq);
                            break;
                        }

                    case ComparisonOperator.NotEqual:
                        {
                            emitter.Compare(OpCodes.Ceq);
                            inverse = true;
                            break;
                        }

                    case ComparisonOperator.Greater:
                        {
                            emitter.Compare(OpCodes.Cgt);
                            break;
                        }

                    case ComparisonOperator.GreaterEqual:
                        {
                            emitter.Compare(OpCodes.Clt);
                            inverse = true;
                            break;
                        }

                    case ComparisonOperator.Lesser:
                        {
                            emitter.Compare(OpCodes.Clt);
                            break;
                        }

                    case ComparisonOperator.LesserEqual:
                        {
                            emitter.Compare(OpCodes.Cgt);
                            inverse = true;
                            break;
                        }

                    default:
                        {
                            throw new InvalidOperationException("Invalid comparison operator.");
                        }
                }
            }
            else
            {
                // This type is -not- comparable
                // We can only support == and != operations

                m_Value.Load(emitter);

                switch (m_Operator)
                {
                    case ComparisonOperator.Equal:
                        {
                            emitter.Compare(OpCodes.Ceq);
                            break;
                        }

                    case ComparisonOperator.NotEqual:
                        {
                            emitter.Compare(OpCodes.Ceq);
                            inverse = true;
                            break;
                        }

                    case ComparisonOperator.Greater:
                    case ComparisonOperator.GreaterEqual:
                    case ComparisonOperator.Lesser:
                    case ComparisonOperator.LesserEqual:
                        {
                            throw new InvalidOperationException("Property does not support relational comparisons.");
                        }

                    default:
                        {
                            throw new InvalidOperationException("Invalid operator.");
                        }
                }
            }

            if (m_Not != inverse)
            {
                emitter.LogicalNot();
            }
        }
    }

    public static class ConditionalCompiler
    {
        public static IConditional Compile(AssemblyEmitter assembly, Type objectType, ICondition[] conditions, int index)
        {
            var typeBuilder = assembly.DefineType(
                $"__conditional{index}",
                TypeAttributes.Public,
                typeof(object)
            );
            {
                var ctor = typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    Type.EmptyTypes
                );

                var il = ctor.GetILGenerator();

                // : base()
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

                for (var i = 0; i < conditions.Length; ++i)
                {
                    conditions[i].Construct(typeBuilder, il, i);
                }

                // return;
                il.Emit(OpCodes.Ret);
            }

            typeBuilder.AddInterfaceImplementation(typeof(IConditional));

            MethodBuilder compareMethod;
            {
                var emitter = new MethodEmitter(typeBuilder);

                emitter.Define(
                    /*  name  */ "Verify",
                    /*  attr  */
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    /* return */
                    typeof(bool),
                    /* params */
                    new[] { typeof(object) }
                );

                var obj = emitter.CreateLocal(objectType);
                var eq = emitter.CreateLocal(typeof(bool));

                emitter.LoadArgument(1);
                emitter.CastAs(objectType);
                emitter.StoreLocal(obj);

                var done = emitter.CreateLabel();

                for (var i = 0; i < conditions.Length; ++i)
                {
                    if (i > 0)
                    {
                        emitter.LoadLocal(eq);

                        emitter.BranchIfFalse(done);
                    }

                    emitter.LoadLocal(obj);

                    conditions[i].Compile(emitter);

                    emitter.StoreLocal(eq);
                }

                emitter.MarkLabel(done);

                emitter.LoadLocal(eq);

                emitter.Return();

                typeBuilder.DefineMethodOverride(
                    emitter.Method,
                    typeof(IConditional).GetMethod(
                        "Verify",
                        new[]
                        {
                            typeof(object)
                        }
                    )
                );

                compareMethod = emitter.Method;
            }

            var conditionalType = typeBuilder.CreateType();

            return conditionalType.CreateInstance<IConditional>();
        }
    }
}
