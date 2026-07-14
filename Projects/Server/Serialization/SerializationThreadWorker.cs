/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationThreadWorker.cs                                    *
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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Server;

public class SerializationThreadWorker
{
    private const int MinHeapSize = 1024 * 1024; // 1MB
    private readonly int _index;
    private readonly Thread _thread;
    private readonly AutoResetEvent _startEvent; // Main thread tells the thread to start working
    private readonly AutoResetEvent _stopEvent; // Main thread waits for the worker finish draining
    private readonly SerializationChunkSource _chunkSource;
    private readonly int _heapSizeHint;
    private bool _pause;
    private bool _exit;
    private bool _exited;
    private byte[] _heap;
    private long _entitiesSerialized;
    private long _bytesSerialized;

    public SerializationThreadWorker(int index, SerializationChunkSource chunkSource, int heapSizeHint = 0)
    {
        _index = index;
        _chunkSource = chunkSource;
        _heapSizeHint = heapSizeHint;
        _startEvent = new AutoResetEvent(false);
        _stopEvent = new AutoResetEvent(false);
        _thread = new Thread(Execute);
        _thread.Start(this);
    }

    // Stats from the most recent save, for diagnosing load balance.
    public long EntitiesSerialized => _entitiesSerialized;
    public long BytesSerialized => _bytesSerialized;

    public void Wake()
    {
        _startEvent.Set();
    }

    public void Sleep()
    {
        Volatile.Write(ref _pause, true);
        _stopEvent.WaitOne();
    }

    public void Exit()
    {
        if (_exited)
        {
            return;
        }

        _exited = true;
        _exit = true;
        Wake();
        Sleep();
    }

    // Sized from the previous world load so the first save doesn't pay copy-on-grow during the freeze.
    public void AllocateHeap() =>
        _heap ??= GC.AllocateUninitializedArray<byte>(Math.Max(MinHeapSize, _heapSizeHint));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetHeap(int start, int length) => _heap.AsSpan(start, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Serialize(IGenericSerializable e, BufferWriter writer, byte threadIndex)
    {
        e.SerializedThread = threadIndex;
        var start = e.SerializedPosition = (int)writer.Position;
        e.Serialize(writer);
        e.SerializedLength = (int)(writer.Position - start);
    }

    private static void Execute(object obj)
    {
        var worker = (SerializationThreadWorker)obj;
        var threadIndex = (byte)worker._index;

        var chunkSource = worker._chunkSource;
        var serializedTypes = World.SerializedTypes;

        while (worker._startEvent.WaitOne())
        {
            var writer = new BufferWriter(worker._heap, true, serializedTypes);
            var entities = 0L;
            var spinner = new SpinWait();

            while (true)
            {
                var pauseRequested = Volatile.Read(ref worker._pause);
                if (chunkSource.TryTake(out var chunk))
                {
                    spinner.Reset();

                    if (chunk.Single != null)
                    {
                        Serialize(chunk.Single, writer, threadIndex);
                        entities++;
                    }
                    else
                    {
                        var buffer = chunk.Buffer;
                        var count = chunk.Count;
                        for (var i = 0; i < count; i++)
                        {
                            Serialize(buffer[i], writer, threadIndex);
                        }

                        entities += count;
                        chunkSource.Return(buffer, count);
                    }
                }
                else if (pauseRequested) // Break when finished
                {
                    break;
                }
                else
                {
                    // Idle backoff instead of hammering the queue head while the producer works.
                    // sleep1Threshold: -1 keeps escalation at Yield/Sleep(0) and never Sleep(1),
                    // avoiding timer-resolution stalls at the end of the drain.
                    spinner.SpinOnce(-1);
                }
            }

            worker._heap = writer.Buffer;
            worker._entitiesSerialized = entities;
            worker._bytesSerialized = writer.Position;

            writer.Close();

            worker._stopEvent.Set(); // Allow the main thread to continue now that we are finished
            worker._pause = false;

            if (Core.Closing || worker._exit)
            {
                return;
            }
        }
    }
}
