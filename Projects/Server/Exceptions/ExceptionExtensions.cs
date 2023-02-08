/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ExceptionExtensions.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Server.Exceptions;

public static class ExceptionExtensions
{
    public static Exception SetStackTrace(this Exception target, StackTrace stack) => _setStackTrace(target, stack);

    private static readonly Func<Exception, StackTrace, Exception> _setStackTrace = _createStackTraceMethod();

    private static Func<Exception, StackTrace, Exception> _createStackTraceMethod()
    {
        ParameterExpression target = Expression.Parameter(typeof(Exception));
        ParameterExpression stack = Expression.Parameter(typeof(StackTrace));
        Type traceFormatType = typeof(StackTrace).GetNestedType("TraceFormat", BindingFlags.NonPublic);
        MethodInfo toString = typeof(StackTrace).GetMethod("ToString", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { traceFormatType }, null);
        object normalTraceFormat = Enum.GetValues(traceFormatType!).GetValue(0);
        MethodCallExpression stackTraceString = Expression.Call(stack, toString!, Expression.Constant(normalTraceFormat, traceFormatType));
        FieldInfo stackTraceStringField = typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
        BinaryExpression assign = Expression.Assign(Expression.Field(target, stackTraceStringField!), stackTraceString);
        return Expression.Lambda<Func<Exception, StackTrace, Exception>>(Expression.Block(assign, target), target, stack).Compile();
    }
}
