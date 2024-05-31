using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Server;

public class Application
{
    public static CancellationTokenSource ClosingTokenSource { get; } = new();

    public static bool Closing => ClosingTokenSource.IsCancellationRequested;

    public static void Main(string[] args)
    {
        bool profiling = false;

        foreach (var a in args)
        {
            if (a.InsensitiveEquals("-profile"))
            {
                profiling = true;
            }
        }

        Core.Setup(profiling, Assembly.GetEntryAssembly(), Process.GetCurrentProcess(), ClosingTokenSource);
    }
}
