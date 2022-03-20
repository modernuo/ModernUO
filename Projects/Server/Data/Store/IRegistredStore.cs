using Server.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Server.DataModel;

namespace Server
{
    public static partial class DataModel
    {
        public interface IRegistredStore
        {
            public void Add(string name, object obj);
            public bool Exist(string name);
            public bool Delete(string name);
            public void Save();
        }
    }

    public class BaseStore<T> : IRegistredStore
    {
        private readonly string _path;
        public readonly Dictionary<string, T> _data;
        public BaseStore(string path)
        {
            _path = path;
            _data = JsonConfig.Deserialize<Dictionary<string, T>>(path) ?? new Dictionary<string, T>();
        }

        public virtual void Add(string name, object obj)
        {
            if(obj is T item)
            {
                if (Exist(name))
                {
                    _data[name] = item;
                }
                else
                {
                    _data.Add(name, item);
                }

                Save();
            }
        }
        public Dictionary<string, T> GetStore()
        {
            return _data;
        }
        public virtual bool Delete(string name)
        {
            if (_data.Remove(name))
            {
                Save();
                return true;
            }

            return false;
        }

        public virtual bool Exist(string name)
        {
            return _data.ContainsKey(name);
        }

        public virtual T Get(string name)
        {
            if (_data.TryGetValue(name, out var result))
            {
                return result;
            }

            return default;
        }

        public virtual void Save()
        {
            JsonConfig.Serialize(_path, _data);
        }
    }
}
