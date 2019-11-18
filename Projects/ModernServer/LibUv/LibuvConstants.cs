// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using ModernServer;

namespace Libuv
{
  public static class LibuvConstants
  {
    public const int EOF = -4095;
    public static readonly int? ECONNRESET = GetECONNRESET();
    public static readonly int? EADDRINUSE = GetEADDRINUSE();
    public static readonly int? ENOTSUP = GetENOTSUP();
    public static readonly int? EPIPE = GetEPIPE();
    public static readonly int? ECANCELED = GetECANCELED();
    public static readonly int? ENOTCONN = GetENOTCONN();
    public static readonly int? EINVAL = GetEINVAL();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConnectionReset(int errno) => errno == ECONNRESET || errno == EPIPE || errno == ENOTCONN || errno == EINVAL;

    private static int? GetECONNRESET()
    {
      if (Core.IsWindows)
        return -4077;
      if (Core.IsLinux)
        return -104;
      if (Core.IsDarwin)
        return -5;
      return null;
    }

    private static int? GetEPIPE()
    {
      if (Core.IsWindows)
        return -4047;
      if (Core.IsLinux)
        return -32;
      if (Core.IsDarwin)
        return -32;
      return null;
    }

    private static int? GetENOTCONN()
    {
      if (Core.IsWindows)
        return -4053;
      if (Core.IsLinux)
        return -107;
      if (Core.IsDarwin)
        return -57;
      return null;
    }

    private static int? GetEINVAL()
    {
      if (Core.IsWindows)
        return -4071;
      if (Core.IsLinux)
        return -22;
      if (Core.IsDarwin)
        return -22;
      return null;
    }

    private static int? GetEADDRINUSE()
    {
      if (Core.IsWindows)
        return -4091;
      if (Core.IsLinux)
        return -98;
      if (Core.IsDarwin)
        return -48;
      return null;
    }

    private static int? GetENOTSUP()
    {
      if (Core.IsLinux)
        return -95;
      if (Core.IsDarwin)
        return -45;
      return null;
    }

    private static int? GetECANCELED()
    {
      if (Core.IsWindows)
        return -4081;
      if (Core.IsLinux)
        return -125;
      if (Core.IsDarwin)
        return -89;
      return null;
    }
  }
}
