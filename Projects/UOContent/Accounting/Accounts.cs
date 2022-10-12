using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Server.Logging;

namespace Server.Accounting
{
    public static class Accounts
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(Accounts));

        private static readonly Dictionary<string, Account> _accountsByName = new(32, StringComparer.OrdinalIgnoreCase);
        private static Dictionary<Serial, Account> _accountsById = new(32);
        private static Serial _lastAccount;

        private static void OutOfMemory(string message) => throw new OutOfMemoryException(message);

        public static Serial NewAccount
        {
            get
            {
                var last = _lastAccount;

                for (uint i = 0; i < uint.MaxValue; i++)
                {
                    last++;

                    if (FindAccount(last) == null)
                    {
                        return _lastAccount = last;
                    }
                }

                OutOfMemory("No serials left to allocate for accounts");
                return Serial.MinusOne;
            }
        }

        public static int Count => _accountsByName.Count;

        public static void Configure() =>
            Persistence.Register("Accounts", Serialize, WriteSnapshot, Deserialize);

        internal static void Serialize()
        {
            EntityPersistence.SaveEntities(
                _accountsById.Values,
                account => ((ISerializable)account).Serialize(World.SerializedTypes)
            );
        }

        internal static void WriteSnapshot(string basePath)
        {
            IIndexInfo<Serial> indexInfo = new EntityTypeIndex("Accounts");
            EntityPersistence.WriteEntities(indexInfo, _accountsById, basePath,World.SerializedTypes, out _);
        }

        public static IEnumerable<IAccount> GetAccounts() => _accountsByName.Values;

        public static Account GetAccount(string username)
        {
            _accountsByName.TryGetValue(username, out var a);
            return a;
        }

        public static void Add(Account a)
        {
            _accountsByName[a.Username] = a;
            _accountsById[a.Serial] = a;
        }

        public static void Remove(Account a)
        {
            _accountsByName.Remove(a.Username);
            _accountsById.Remove(a.Serial);
        }

        internal static void Deserialize(string path, Dictionary<ulong, string> typesDb)
        {
            var filePath = Path.Combine(path, "Accounts", "accounts.xml");

            // Backward Compatibility
            if (File.Exists(filePath))
            {
                DeserializeXml(filePath);
                return;
            }

            IIndexInfo<Serial> indexInfo = new EntityTypeIndex("Accounts");

            _accountsById = EntityPersistence.LoadIndex(path, indexInfo, typesDb, out List<EntitySpan<Account>> accounts);

            if (_accountsById.Count > 0)
            {
                _lastAccount = _accountsById.Keys.Max();
            }

            EntityPersistence.LoadData(path, indexInfo, typesDb, accounts);

            foreach (var a in _accountsById.Values)
            {
                _accountsByName[a.Username] = a;
            }
        }

        private static void DeserializeXml(string filePath)
        {
            var doc = new XmlDocument();
            doc.Load(filePath);

            var root = doc["accounts"];

            if (root == null)
            {
                throw new FileLoadException("Unable to load xml file");
            }

            foreach (XmlElement account in root.GetElementsByTagName("account"))
            {
                try
                {
                    new Account(account);
                }
                catch
                {
                    logger.Warning("Account instance load failed");
                }
            }
        }

        public static IAccount FindAccount(Serial serial)
        {
            _accountsById.TryGetValue(serial, out var account);
            return account;
        }
    }
}
