// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers
{
  /// <summary>
  /// This wraps PipeWriter.FlushAsync() in a way that allows multiple awaiters making it safe to call from publicly
  /// exposed Stream implementations while also tracking response data rate.
  /// </summary>
  internal class TimingPipeFlusher
  {
    private readonly PipeWriter _writer;

    public TimingPipeFlusher(PipeWriter writer)
    {
      _writer = writer;
    }

    public ValueTask<FlushResult> FlushAsync() => FlushAsync(default);

    public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken)
    {
      var pipeFlushTask = _writer.FlushAsync(cancellationToken);

      return pipeFlushTask.IsCompletedSuccessfully ?
        new ValueTask<FlushResult>(pipeFlushTask.Result) :
        TimeFlushAsyncAwaited(pipeFlushTask, cancellationToken);
    }

    private async ValueTask<FlushResult> TimeFlushAsyncAwaited(ValueTask<FlushResult> pipeFlushTask, CancellationToken cancellationToken)
    {
      try
      {
        return await pipeFlushTask;
      }
      catch (Exception ex)
      {
        // A canceled token is the only reason flush should ever throw.
        Console.WriteLine($"Unexpected exception in {nameof(TimingPipeFlusher)}.{nameof(FlushAsync)}.");
        Console.WriteLine(ex);
      }
      finally
      {
        cancellationToken.ThrowIfCancellationRequested();
      }

      return default;
    }
  }
}
