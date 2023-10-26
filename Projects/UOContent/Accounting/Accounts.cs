using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Server.Logging;

namespace Server.Accounting;

public class Accounts : GenericEntityPersistence<IAccount>
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(Accounts));

    private static readonly Dictionary<string, IAccount> _accountsByName = new(32, StringComparer.OrdinalIgnoreCase);

    public static int Count => _accountsByName.Count;

    private static Accounts _accountsPersistence;

    public static Serial NewAccount => _accountsPersistence.NewEntity;

    public static void Configure()
    {
        _accountsPersistence = new Accounts();
    }

    public Accounts() : base("Accounts", 3, 0x1, 0x7FFFFFFF)
    {
    }

    public static IEnumerable<IAccount> GetAccounts() => _accountsByName.Values;

    public static IAccount GetAccount(string username)
    {
        _accountsByName.TryGetValue(username, out var a);
        return a;
    }

    public static void Add(Account a)
    {
        _accountsByName[a.Username] = a;
        _accountsPersistence.AddEntity(a);
    }

    public static void Remove(IAccount a)
    {
        _accountsByName.Remove(a.Username);
        _accountsPersistence.RemoveEntity(a);
    }

    public override void Deserialize(string path, Dictionary<ulong, string> typesDb)
    {
        var filePath = Path.Combine(path, "Accounts", "accounts.xml");

        // Backward Compatibility
        if (File.Exists(filePath))
        {
            DeserializeXml(filePath);
            return;
        }

        base.Deserialize(path, typesDb);

        foreach (var a in EntitiesBySerial.Values)
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

    public static IAccount FindAccount(Serial serial) => _accountsPersistence.FindEntity<IAccount>(serial);
}
