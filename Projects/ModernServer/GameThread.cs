/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: GameThread.cs - Created: 2019/11/24 - Updated: 2019/11/24       *
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

using System.Buffers;
using Libuv;
using Libuv.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace ModernServer
{
  public class GameThread : LibuvThread
  {
    private static LibuvFunctions libuvFunctions = new LibuvFunctions();

    // TODO: Use a factory
    public GameThread() : this(
      //TODO: Change to configurable logger via Factory
      new ApplicationLifetime(
        LoggerFactory.Create(
          builder => { builder.AddConsole(); }
        ).CreateLogger<ApplicationLifetime>()
      ),
      SlabMemoryPoolFactory.Create(),
      new LibuvTrace(
        LoggerFactory.Create(
          builder => { builder.AddConsole(); }
        ).CreateLogger("game_thread")
      )
    )
    {
    }

    public GameThread(IHostApplicationLifetime appLifetime, MemoryPool<byte> pool, ILibuvTrace log, int maxLoops = 8) : base(
      libuvFunctions, appLifetime, pool, log, maxLoops
    )
    {
    }
  }
}
