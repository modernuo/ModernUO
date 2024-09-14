/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
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
using System.Collections.Concurrent;
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
    private bool _pause;
    private bool _exit;
    private byte[] _heap;

    private readonly ConcurrentQueue<IGenericSerializable> _entities;

    public SerializationThreadWorker(int index)
    {
        _index = index;
        _startEvent = new AutoResetEvent(false);
        _stopEvent = new AutoResetEvent(false);
        _entities = new ConcurrentQueue<IGenericSerializable>();
        _thread = new Thread(Execute);
        _thread.Start(this);
    }

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
        _exit = true;
        Wake();
        Sleep();
    }

    public void AllocateHeap() => _heap ??= GC.AllocateUninitializedArray<byte>(MinHeapSize); // 1MB

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(IGenericSerializable entity) => _entities.Enqueue(entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetHeap(int start, int length) => _heap.AsSpan(start, length);

    private static void Execute(object obj)
    {
        var worker = (SerializationThreadWorker)obj;
        var threadIndex = (byte)worker._index;

        var queue = worker._entities;
        var serializedTypes = World.SerializedTypes;

        while (worker._startEvent.WaitOne())
        {
            var writer = new BufferWriter(worker._heap, true, serializedTypes);

            while (true)
            {
                var pauseRequested = Volatile.Read(ref worker._pause);
                if (queue.TryDequeue(out var e))
                {
                    e.SerializedThread = threadIndex;
                    var start = e.SerializedPosition = (int)writer.Position;
                    e.Serialize(writer);
                    e.SerializedLength = (int)(writer.Position - start);
                }
                else if (pauseRequested) // Break when finished
                {
                    break;
                }
            }

            worker._heap = writer.Buffer;

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
