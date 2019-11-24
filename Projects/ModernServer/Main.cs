/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: Main.cs - Created: 2019/11/17 - Updated: 2019/11/17             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ModernServer
{
  public static class Core
  {
    public static bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long VolatileRead(ref long value) => IntPtr.Size == 8 ? Volatile.Read(ref value) : Interlocked.Read(ref value);

    public static bool Closing { get; private set; }
    public static readonly AutoResetEvent UnloadSignal = new AutoResetEvent(true);

    public static void Main(string[] args)
    {
      // TODO: Add UnhandledException/ProcessExit to CurrentDomain

      Thread.CurrentThread.Name = "Engine";
      Version ver = Assembly.GetEntryAssembly()?.GetName().Version ?? Version.Parse("0.0.0.0");

      Console.ForegroundColor = ConsoleColor.Green;
      // Added to help future code support on forums, as a 'check' people can ask for to it see if they recompiled core or not
      Console.WriteLine($"ModernUO - [https://github.com/modernuo/ModernUO] Version {ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}");
      Console.WriteLine("Engine: Running on {0}", RuntimeInformation.FrameworkDescription);
      Console.ResetColor();
      Console.WriteLine();

      // Load Engine.dll assembly

      // TODO: Make configurable
      GameThread gameThread = new GameThread();
      gameThread.StartAsync().Wait(); // Synchronously wait for this to complete



      while (UnloadSignal.WaitOne())
      {

      }

      // Load Everything
      // Wait for restart signal
      // Unload Everything
      // Restart

      HandleClosed();
    }

    public static void HandleClosed()
    {
      // TODO: Clean up the loop network IO, etc.
      throw new NotImplementedException();
    }
  }
}
