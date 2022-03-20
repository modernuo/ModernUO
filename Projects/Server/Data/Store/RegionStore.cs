using Server.Json;
using System;
using System.Collections.Generic;
using static Server.DataModel;

namespace Server
{
    public static partial class DataModel
    {
        public interface IRegionStore : IRegistredStore
        {
            public RegionModel Get(string name);
            public Dictionary<string, RegionModel> GetStore();
        }
    }

    public class RegionStore : BaseStore<RegionModel>, IRegionStore
    {
        public RegionStore(string path) : base(path) { }
    }
}
