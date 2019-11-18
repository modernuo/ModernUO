// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Libuv
{
  public class LibuvTransportContext
  {
    public LibuvTransportOptions Options { get; set; }

    public IHostApplicationLifetime AppLifetime { get; set; }

    public ILibuvTrace Log { get; set; }
  }
}
