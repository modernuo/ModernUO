using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Accounting.Security;

namespace Server.Accounting
{
    public partial class Account
    {
        private void MigrateFrom(V3Content content)
        {
            _username = content.Username;
            _passwordAlgorithm = content.PasswordAlgorithm;
            _password = content.Password;
            _accessLevel = content.AccessLevel;
            _flags = content.Flags;
            Created = content.Created;
            _lastLogin = content.LastLogin;
            _totalGold = content.TotalGold;
            _totalPlat = content.TotalPlat;
            _mobiles = content.Mobiles;
            _comments = content.Comments;
            _tags = content.Tags;
            _loginIPs = content.LoginIPs;
            _ipRestrictions = content.IpRestrictions;
            _totalGameTime = content.TotalGameTime;
            _email = content.Email;
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            if (version != 2)
            {
                // Due to a bug where we were not versioning at all, reset so we don't have an issue deserializing
                reader.Seek(0, SeekOrigin.Begin);
            }

            _username = reader.ReadString();
            _passwordAlgorithm = version < 2 ? (PasswordProtectionAlgorithm)reader.ReadInt() : reader.ReadEnum<PasswordProtectionAlgorithm>();
            _password = reader.ReadString();
            _accessLevel = version < 2 ? (AccessLevel)reader.ReadInt() : reader.ReadEnum<AccessLevel>();
            _flags = reader.ReadInt();
            Created = reader.ReadDateTime();
            _lastLogin = reader.ReadDateTime();

            _totalGold = reader.ReadInt();
            _totalPlat = reader.ReadInt();

            var length = reader.ReadInt();
            _mobiles = new Mobile[length];
            for (int i = 0; i < length; i++)
            {
                _mobiles[i] = reader.ReadEntity<Mobile>();
            }

            length = reader.ReadInt();
            _comments = length > 0 ? new List<AccountComment>(length) : null;
            for (int i = 0; i < length; i++)
            {
                _comments!.Add(new AccountComment(reader));
            }

            length = reader.ReadInt();
            _tags = length > 0 ? new List<AccountTag>(length) : null;
            for (int i = 0; i < length; i++)
            {
                _tags!.Add(new AccountTag(reader));
            }

            length = reader.ReadInt();
            _loginIPs = new IPAddress[length];
            for (int i = 0; i < length; i++)
            {
                if (version < 2)
                {
                    if (IPAddress.TryParse(reader.ReadString(), out var address))
                    {
                        _loginIPs[i] = Utility.Intern(address);
                    }
                }
                else
                {
                    _loginIPs[i] = reader.ReadIPAddress();
                }
            }

            length = reader.ReadInt();
            _ipRestrictions = new string[length];
            for (int i = 0; i < length; i++)
            {
                _ipRestrictions[i] = reader.ReadString();
            }

            _totalGameTime = reader.ReadTimeSpan();

            if (version > 1)
            {
                _email = reader.ReadString();
            }
        }
    }
}
