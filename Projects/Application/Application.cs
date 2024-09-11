using System.Diagnostics;
using System.Reflection;

namespace Server;

public class Application
{
    public static void Main(string[] args)
    {
        Core.Setup(Assembly.GetEntryAssembly(), Process.GetCurrentProcess());
    }
}
