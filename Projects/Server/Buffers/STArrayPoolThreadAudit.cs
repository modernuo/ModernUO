/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: STArrayPoolThreadAudit.cs                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

#if DEBUG_ARRAYPOOL_THREADS
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Server.Buffers;

/// <summary>
/// Opt-in diagnostic (<c>-p:ArrayPoolThreadAudit=true</c>) that records <see cref="STArrayPool{T}"/> use from
/// the wrong thread and writes a report on exit. Under xUnit it reports test classes outside a
/// <c>DisableParallelization</c> collection and calls with no test frame (finalizer, workers); otherwise it
/// reports calls off the event-loop thread.
/// </summary>
/// <remarks>
/// Concurrent collections are deliberate: the point is to record calls arriving from other threads.
/// </remarks>
internal static class STArrayPoolThreadAudit
{
    private static readonly ConcurrentDictionary<string, Finding> _findings = new();
    private static readonly ConcurrentDictionary<Type, bool> _sequentialTypes = new();

    private sealed class Finding
    {
        public int Count;
        public string Stack;
    }

    static STArrayPoolThreadAudit() => AppDomain.CurrentDomain.ProcessExit += (_, _) => WriteReport();

    public static void Record(Type elementType)
    {
        var thread = Thread.CurrentThread;
        string key;

        if (Core.IsRunningFromXUnit)
        {
            var testType = FindTestType(new StackTrace());
            if (testType != null && IsSequential(testType))
            {
                return;
            }

            key = testType != null
                ? $"non-sequential test class {testType.FullName} [STArrayPool<{elementType.Name}>]"
                : $"no test frame on thread '{thread.Name}' (pool thread: {thread.IsThreadPoolThread}) [STArrayPool<{elementType.Name}>]";
        }
        else
        {
            if (Core.LoopContext?.IsLoopThread != false)
            {
                return;
            }

            key = $"off-loop thread '{thread.Name}' (pool thread: {thread.IsThreadPoolThread}) [STArrayPool<{elementType.Name}>]";
        }

        var finding = _findings.GetOrAdd(key, static _ => new Finding { Stack = Environment.StackTrace });
        Interlocked.Increment(ref finding.Count);
    }

    // The outermost test-assembly frame is the test (or fixture) xUnit invoked; inner ones are helpers.
    private static Type FindTestType(StackTrace trace)
    {
        for (var i = trace.FrameCount - 1; i >= 0; i--)
        {
            var type = trace.GetFrame(i)?.GetMethod()?.DeclaringType;
            if (type?.Assembly.GetName().Name?.EndsWith(".Tests", StringComparison.Ordinal) != true)
            {
                continue;
            }

            // Lambdas and async state machines are nested in the test class.
            while (type.DeclaringType != null)
            {
                type = type.DeclaringType;
            }

            return type;
        }

        return null;
    }

    // Matched by attribute name so the server does not reference xUnit.
    private static bool IsSequential(Type testType) =>
        _sequentialTypes.GetOrAdd(
            testType,
            static type =>
            {
                if (HasDisableParallelization(type))
                {
                    return true; // collection fixtures carry the definition themselves
                }

                var collection = type.GetCustomAttributesData()
                    .FirstOrDefault(a => a.AttributeType.Name == "CollectionAttribute")
                    ?.ConstructorArguments.FirstOrDefault().Value as string;

                return collection != null && type.Assembly.GetTypes().Any(
                    t => t.GetCustomAttributesData().Any(
                        a => a.AttributeType.Name == "CollectionDefinitionAttribute" &&
                             a.ConstructorArguments.FirstOrDefault().Value as string == collection &&
                             IsDisableParallelization(a)
                    )
                );
            }
        );

    private static bool HasDisableParallelization(Type type) =>
        type.GetCustomAttributesData().Any(
            a => a.AttributeType.Name == "CollectionDefinitionAttribute" && IsDisableParallelization(a)
        );

    private static bool IsDisableParallelization(CustomAttributeData attribute) =>
        attribute.NamedArguments.Any(n => n.MemberName == "DisableParallelization" && n.TypedValue.Value is true);

    private static void WriteReport()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "starraypool-thread-audit.txt");
        using var writer = new StreamWriter(path);

        if (_findings.IsEmpty)
        {
            writer.WriteLine("No STArrayPool use from the wrong thread.");
            return;
        }

        foreach (var (key, finding) in _findings.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            writer.WriteLine($"{key} -- {finding.Count} call(s)");
            writer.WriteLine(finding.Stack);
            writer.WriteLine();
        }
    }
}
#endif
