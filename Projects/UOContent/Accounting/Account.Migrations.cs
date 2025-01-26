using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Accounting.Security;

namespace Server.Accounting;

public partial class Account
{
    private void MigrateFrom(V6Content content)
    {
        Username = content.Username;
        _passwordAlgorithm = content.PasswordAlgorithm;
        _password = content.Password;
        AccessLevel = content.AccessLevel;
        Flags = content.Flags;
        LastLogin = content.LastLogin;
        TotalGold = content.TotalGold;
        TotalPlat = content.TotalPlat;
        _mobiles = content.Mobiles;
        Comments = content.Comments;
        Tags = content.Tags;
        LoginIPs = content.LoginIPs;
        TotalGameTime = content.TotalGameTime;
        Email = content.Email;
    }

    // Deleted IP Restrictions
    private void MigrateFrom(V5Content content)
    {
        Username = content.Username;
        _passwordAlgorithm = content.PasswordAlgorithm;
        _password = content.Password;
        AccessLevel = content.AccessLevel;
        Flags = content.Flags;
        LastLogin = content.LastLogin;
        TotalGold = content.TotalGold;
        TotalPlat = content.TotalPlat;
        _mobiles = content.Mobiles;
        Comments = content.Comments;
        Tags = content.Tags;
        LoginIPs = content.LoginIPs;
        TotalGameTime = content.TotalGameTime;
        Email = content.Email;
    }

    // Username was not interned
    private void MigrateFrom(V4Content content)
    {
        Username = content.Username;
        _passwordAlgorithm = content.PasswordAlgorithm;
        _password = content.Password;
        AccessLevel = content.AccessLevel;
        Flags = content.Flags;
        LastLogin = content.LastLogin;
        TotalGold = content.TotalGold;
        TotalPlat = content.TotalPlat;
        _mobiles = content.Mobiles;
        Comments = content.Comments;
        Tags = content.Tags;
        LoginIPs = content.LoginIPs;
        TotalGameTime = content.TotalGameTime;
        Email = content.Email;
    }

    private void MigrateFrom(V3Content content)
    {
        Username = content.Username;
        Username.Intern();
        _passwordAlgorithm = content.PasswordAlgorithm;
        _password = content.Password;
        AccessLevel = content.AccessLevel;
        Flags = content.Flags;
        Created = content.Created;
        LastLogin = content.LastLogin;
        TotalGold = content.TotalGold;
        TotalPlat = content.TotalPlat;
        _mobiles = content.Mobiles;
        Comments = content.Comments;
        Tags = content.Tags;
        LoginIPs = content.LoginIPs;
        TotalGameTime = content.TotalGameTime;
        Email = content.Email;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        if (version != 2)
        {
            // Due to a bug where we were not versioning at all, reset so we don't have an issue deserializing
            reader.Seek(0, SeekOrigin.Begin);
        }

        Username = reader.ReadString(true);
        _passwordAlgorithm = version < 2
            ? (PasswordProtectionAlgorithm)reader.ReadInt()
            : reader.ReadEnum<PasswordProtectionAlgorithm>();
        _password = reader.ReadString();
        AccessLevel = version < 2 ? (AccessLevel)reader.ReadInt() : reader.ReadEnum<AccessLevel>();
        Flags = reader.ReadInt();
        Created = reader.ReadDateTime();
        LastLogin = reader.ReadDateTime();

        TotalGold = reader.ReadInt();
        TotalPlat = reader.ReadInt();

        var length = reader.ReadInt();
        _mobiles = new Mobile[length];
        for (int i = 0; i < length; i++)
        {
            _mobiles[i] = reader.ReadEntity<Mobile>();
        }

        length = reader.ReadInt();
        Comments = length > 0 ? new List<AccountComment>(length) : null;
        for (int i = 0; i < length; i++)
        {
            Comments!.Add(new AccountComment(reader));
        }

        length = reader.ReadInt();
        Tags = length > 0 ? new List<AccountTag>(length) : null;
        for (int i = 0; i < length; i++)
        {
            Tags!.Add(new AccountTag(reader));
        }

        length = reader.ReadInt();
        LoginIPs = new IPAddress[length];
        for (int i = 0; i < length; i++)
        {
            if (version < 2)
            {
                if (IPAddress.TryParse(reader.ReadString(), out var address))
                {
                    LoginIPs[i] = Utility.Intern(address);
                }
            }
            else
            {
                LoginIPs[i] = reader.ReadIPAddress();
            }
        }

        length = reader.ReadInt();
        for (int i = 0; i < length; i++)
        {
            reader.ReadString(); // IP Restrictions
        }

        TotalGameTime = reader.ReadTimeSpan();

        if (version > 1)
        {
            Email = reader.ReadString();
        }
    }
}
