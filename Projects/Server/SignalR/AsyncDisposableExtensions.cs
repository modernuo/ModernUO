// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace SignalR
{
  public static class AsyncDisposableExtensions
  {
    // Does a light up check to see if a type is IAsyncDisposable and calls DisposeAsync if it is
    public static ValueTask DisposeAsync(this IDisposable disposable)
    {
      if (disposable is IAsyncDisposable asyncDisposable) return asyncDisposable.DisposeAsync();
      disposable.Dispose();
      return default;
    }
  }
}
