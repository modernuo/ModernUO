using Microsoft.Extensions.DependencyInjection;

using Server.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Server.DataModel;
namespace Server
{
    public static partial class DataStore
    {
        public static ServiceProvider ServiceProvider;
        internal static void Load()
        {
            var stopwatch = Stopwatch.StartNew();

            Console.Write("DataStore: Loading...");

            var serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            Utility.PushColor(ConsoleColor.Green);
            Console.Write("done");
            Utility.PopColor();
            Console.WriteLine("({0:F2} seconds)", stopwatch.Elapsed.TotalSeconds);
        }
       

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IRegionStore>(new RegionStore("Data/CustomRegion.json"));
        }
    }
}
