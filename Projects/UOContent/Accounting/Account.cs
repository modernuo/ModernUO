using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using Server.Accounting.Security;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Accounting
{
    public class Account : IAccount, IComparable<Account>
    {
        public static readonly TimeSpan YoungDuration = TimeSpan.FromHours(40.0);
        public static readonly TimeSpan InactiveDuration = TimeSpan.FromDays(180.0);
        public static readonly TimeSpan EmptyInactiveDuration = TimeSpan.FromDays(30.0);

        private Mobile[] m_Mobiles;
        private AccessLevel m_AccessLevel;
        private List<AccountComment> m_Comments;
        private PasswordProtectionAlgorithm m_PasswordAlgorithm;
        private List<AccountTag> m_Tags;
        private TimeSpan m_TotalGameTime;
        private Timer m_YoungTimer;
        private BufferWriter _saveBuffer;

        public Account(string username, string password) : this(Accounts.NewAccount)
        {
            Username = username;

            SetPassword(password);

            m_AccessLevel = AccessLevel.Player;

            Created = LastLogin = Core.Now;
            m_TotalGameTime = TimeSpan.Zero;

            m_Mobiles = new Mobile[7];

            IPRestrictions = Array.Empty<string>();
            LoginIPs = Array.Empty<IPAddress>();

            Accounts.Add(this);
        }

        public Account(Serial serial)
        {
            Serial = serial;

            var ourType = GetType();
            TypeRef = Accounts.Types.IndexOf(ourType);

            if (TypeRef == -1)
            {
                Accounts.Types.Add(ourType);
                TypeRef = Accounts.Types.Count - 1;
            }
        }

        public Account(XmlElement node)
        {
            Serial = Accounts.NewAccount;

            var ourType = GetType();
            TypeRef = Accounts.Types.IndexOf(ourType);

            if (TypeRef == -1)
            {
                Accounts.Types.Add(ourType);
                TypeRef = Accounts.Types.Count - 1;
            }

            Username = Utility.GetText(node["username"], "empty");

            Enum.TryParse(Utility.GetText(node["passwordAlgorithm"], null), true, out m_PasswordAlgorithm);

            // Backward compatibility with RunUO/ServUO
            if (m_PasswordAlgorithm == PasswordProtectionAlgorithm.None)
            {
                var upgraded =
                    UpgradePassword(
                        Utility.GetText(node["newSecureCryptPassword"], null),
                        PasswordProtectionAlgorithm.SHA2
                    ) ||
                    UpgradePassword(Utility.GetText(node["newCryptPassword"], null), PasswordProtectionAlgorithm.SHA1) ||
                    UpgradePassword(Utility.GetText(node["cryptPassword"], null), PasswordProtectionAlgorithm.MD5);

                // Automatically upgrade plain passwords to current algorithm.
                if (!upgraded)
                {
                    SetPassword(Utility.GetText(node["password"], null));
                }
            }
            else
            {
                Password = Utility.GetText(node["password"], null);
            }

            Enum.TryParse(Utility.GetText(node["accessLevel"], "Player"), true, out m_AccessLevel);
            Flags = Utility.GetXMLInt32(Utility.GetText(node["flags"], "0"), 0);
            Created = Utility.GetXMLDateTime(Utility.GetText(node["created"], null), Core.Now);
            LastLogin = Utility.GetXMLDateTime(Utility.GetText(node["lastLogin"], null), Core.Now);

            TotalGold = Utility.GetXMLInt32(Utility.GetText(node["totalGold"], "0"), 0);
            TotalPlat = Utility.GetXMLInt32(Utility.GetText(node["totalPlat"], "0"), 0);

            m_Mobiles = LoadMobiles(node);
            m_Comments = LoadComments(node);
            m_Tags = LoadTags(node);
            LoginIPs = LoadAddressList(node);
            IPRestrictions = LoadAccessCheck(node);

            for (var i = 0; i < m_Mobiles.Length; ++i)
            {
                if (m_Mobiles[i] != null)
                {
                    m_Mobiles[i].Account = this;
                }
            }

            var totalGameTime = Utility.GetXMLTimeSpan(Utility.GetText(node["totalGameTime"], null), TimeSpan.Zero);
            if (totalGameTime == TimeSpan.Zero)
            {
                for (var i = 0; i < m_Mobiles.Length; i++)
                {
                    if (m_Mobiles[i] is PlayerMobile m)
                    {
                        totalGameTime += m.GameTime;
                    }
                }
            }

            m_TotalGameTime = totalGameTime;

            if (Young)
            {
                CheckYoung();
            }

            Accounts.Add(this);
        }

        /// <summary>
        ///     Object detailing information about the hardware of the last person to log into this account
        /// </summary>
        public HardwareInfo HardwareInfo { get; set; }

        /// <summary>
        ///     List of IP addresses for restricted access. '*' wildcard supported. If the array contains zero entries, all IP addresses
        ///     are allowed.
        /// </summary>
        public string[] IPRestrictions { get; set; }

        /// <summary>
        ///     List of IP addresses which have successfully logged into this account.
        /// </summary>
        public IPAddress[] LoginIPs { get; set; }

        /// <summary>
        ///     List of account comments. Type of contained objects is AccountComment.
        /// </summary>
        public List<AccountComment> Comments => m_Comments ?? (m_Comments = new List<AccountComment>());

        /// <summary>
        ///     List of account tags. Type of contained objects is AccountTag.
        /// </summary>
        public List<AccountTag> Tags => m_Tags ?? (m_Tags = new List<AccountTag>());

        /// <summary>
        ///     Account username and password. May be null.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     Internal bitfield of account flags. Consider using direct access properties (Banned, Young), or GetFlag/SetFlag methods
        /// </summary>
        public int Flags { get; set; }

        /// <summary>
        ///     Gets or sets a flag indicating if this account is banned.
        /// </summary>
        public bool Banned
        {
            get
            {
                var isBanned = GetFlag(0);

                if (!isBanned)
                {
                    return false;
                }

                if (GetBanTags(out var banTime, out var banDuration))
                {
                    if (banDuration != TimeSpan.MaxValue && Core.Now >= banTime + banDuration)
                    {
                        SetUnspecifiedBan(null); // clear
                        Banned = false;
                        return false;
                    }
                }

                return true;
            }
            set => SetFlag(0, value);
        }

        /// <summary>
        ///     Gets or sets a flag indicating if the characters created on this account will have the young status.
        /// </summary>
        public bool Young
        {
            get => !GetFlag(1);
            set
            {
                SetFlag(1, !value);

                m_YoungTimer?.Stop();
                m_YoungTimer = null;
            }
        }

        /// <summary>
        ///     The date and time of when this account was created.
        /// </summary>
        public DateTime Created { get; private set; }

        /// <summary>
        ///     Gets or sets the date and time when this account was last accessed.
        /// </summary>
        public DateTime LastLogin { get; set; }

        /// <summary>
        ///     An account is considered inactive based upon LastLogin and InactiveDuration.  If the account is empty, it is based upon
        ///     EmptyInactiveDuration
        /// </summary>
        public bool Inactive
        {
            get
            {
                if (AccessLevel != AccessLevel.Player)
                {
                    return false;
                }

                var inactiveLength = Core.Now - LastLogin;

                return inactiveLength > (Count == 0 ? EmptyInactiveDuration : InactiveDuration);
            }
        }

        /// <summary>
        ///     Gets the total game time of this account, also considering the game time of characters
        ///     that have been deleted.
        /// </summary>
        public TimeSpan TotalGameTime
        {
            get
            {
                for (var i = 0; i < m_Mobiles.Length; i++)
                {
                    if (m_Mobiles[i] is PlayerMobile m && m.NetState != null)
                    {
                        return m_TotalGameTime + (Core.Now - m.SessionStart);
                    }
                }

                return m_TotalGameTime;
            }
        }

        BufferWriter ISerializable.SaveBuffer
        {
            get => _saveBuffer;
            set => _saveBuffer = value;
        }

        public int TypeRef { get; private set; }

        public Serial Serial { get; set; }

        public void Deserialize(IGenericReader reader)
        {
            Username = reader.ReadString();
            m_PasswordAlgorithm = (PasswordProtectionAlgorithm)reader.ReadInt();
            Password = reader.ReadString();
            m_AccessLevel = (AccessLevel)reader.ReadInt();
            Flags = reader.ReadInt();
            Created = reader.ReadDateTime();
            LastLogin = reader.ReadDateTime();

            TotalGold = reader.ReadInt();
            TotalPlat = reader.ReadInt();

            m_Mobiles = new Mobile[7];
            var length = reader.ReadInt();
            for (int i = 0; i < length; i++)
            {
                m_Mobiles[i] = reader.ReadEntity<Mobile>();
            }

            length = reader.ReadInt();
            m_Comments = length > 0 ? new List<AccountComment>(length) : null;
            for (int i = 0; i < length; i++)
            {
                m_Comments!.Add(new AccountComment(reader));
            }

            length = reader.ReadInt();
            m_Tags = length > 0 ? new List<AccountTag>(length) : null;
            for (int i = 0; i < length; i++)
            {
                m_Tags!.Add(new AccountTag(reader));
            }

            length = reader.ReadInt();
            LoginIPs = new IPAddress[length];
            for (int i = 0; i < length; i++)
            {
                if (IPAddress.TryParse(reader.ReadString(), out var address))
                {
                    LoginIPs[i] = Utility.Intern(address);
                }
            }

            length = reader.ReadInt();
            IPRestrictions = new string[length];
            for (int i = 0; i < length; i++)
            {
                IPRestrictions[i] = reader.ReadString();
            }

            for (var i = 0; i < m_Mobiles.Length; ++i)
            {
                if (m_Mobiles[i] != null)
                {
                    m_Mobiles[i].Account = this;
                }
            }

            var totalGameTime = reader.ReadTimeSpan();
            if (totalGameTime == TimeSpan.Zero)
            {
                for (var i = 0; i < m_Mobiles.Length; i++)
                {
                    if (m_Mobiles[i] is PlayerMobile m)
                    {
                        totalGameTime += m.GameTime;
                    }
                }
            }

            m_TotalGameTime = totalGameTime;

            if (Young)
            {
                CheckYoung();
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(Username);
            writer.Write((int)m_PasswordAlgorithm);
            writer.Write(Password);
            writer.Write((int)m_AccessLevel);
            writer.Write(Flags);
            writer.Write(Created);
            writer.Write(LastLogin);
            writer.Write(TotalGold);
            writer.Write(TotalPlat);

            writer.Write(Count);
            for (int i = 0; i < m_Mobiles.Length; i++)
            {
                var m = m_Mobiles[i];
                if (m != null)
                {
                    writer.Write(m);
                }
            }

            var length = m_Comments?.Count ?? 0;
            writer.Write(length);
            for (int i = 0; i < length; i++)
            {
                m_Comments![i].Serialize(writer);
            }

            length = m_Tags?.Count ?? 0;
            writer.Write(length);
            for (int i = 0; i < length; i++)
            {
                m_Tags![i].Serialize(writer);
            }

            writer.Write(LoginIPs.Length);
            for (int i = 0; i < LoginIPs.Length; i++)
            {
                writer.Write(LoginIPs[i].ToString());
            }

            writer.Write(IPRestrictions.Length);
            for (int i = 0; i < IPRestrictions.Length; i++)
            {
                writer.Write(IPRestrictions[i]);
            }

            writer.Write(TotalGameTime);
        }

        /// <summary>
        ///     Deletes the account, all characters of the account, and all houses of those characters
        /// </summary>
        public void Delete()
        {
            for (var i = 0; i < Length; ++i)
            {
                var m = this[i];

                if (m == null)
                {
                    continue;
                }

                var list = BaseHouse.GetHouses(m);

                for (var j = 0; j < list.Count; ++j)
                {
                    list[j].Delete();
                }

                m.Delete();

                m.Account = null;
                m_Mobiles[i] = null;
            }

            if (LoginIPs.Length != 0 && AccountHandler.IPTable.ContainsKey(LoginIPs[0]))
            {
                --AccountHandler.IPTable[LoginIPs[0]];
            }

            Deleted = true;
            Accounts.Remove(this);
        }

        public bool Deleted { get; private set; }

        /// <summary>
        ///     Account username. Case insensitive validation.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     Account email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     Initial AccessLevel for new characters created on this account.
        /// </summary>
        public AccessLevel AccessLevel
        {
            get => m_AccessLevel;
            set => m_AccessLevel = value;
        }

        public void SetPassword(string plainPassword)
        {
            Password = AccountSecurity.CurrentPasswordProtection.EncryptPassword(plainPassword);
            m_PasswordAlgorithm = AccountSecurity.CurrentAlgorithm;
        }

        public bool CheckPassword(string plainPassword)
        {
            var phrase = m_PasswordAlgorithm == PasswordProtectionAlgorithm.SHA1
                ? $"{Username}{plainPassword}"
                : plainPassword;

            var ok = AccountSecurity.GetPasswordProtection(m_PasswordAlgorithm).ValidatePassword(Password, phrase);
            if (!ok)
            {
                return false;
            }

            // Upgrade the password protection in case we change the algorithm
            if (m_PasswordAlgorithm != AccountSecurity.CurrentAlgorithm)
            {
                SetPassword(plainPassword);
            }

            return true;
        }

        /// <summary>
        ///     Gets the current number of characters on this account.
        /// </summary>
        public int Count
        {
            get
            {
                var count = 0;

                for (var i = 0; i < Length; i++)
                {
                    if (this[i] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        ///     Gets the maximum amount of characters allowed to be created on this account. Values other than 1, 5, 6, or 7 are not
        ///     supported by the client.
        /// </summary>
        public int Limit => Core.SA ? 7 :
            Core.AOS ? 6 : 5;

        /// <summary>
        ///     Gets the maximum amount of characters that this account can hold.
        /// </summary>
        public int Length => m_Mobiles.Length;

        /// <summary>
        ///     Gets or sets the character at a specified index for this account. Out of bound index values are handled; null returned
        ///     for get, ignored for set.
        /// </summary>
        public Mobile this[int index]
        {
            get
            {
                if (index >= 0 && index < m_Mobiles.Length)
                {
                    var m = m_Mobiles[index];

                    if (m?.Deleted != true)
                    {
                        return m;
                    }

                    // This is the only place that clears a mobile for garbage collection
                    // outside of an entire account deletion.
                    m.Account = null;
                    m_Mobiles[index] = null;
                }

                return null;
            }
            set
            {
                if (index >= 0 && index < m_Mobiles.Length)
                {
                    if (m_Mobiles[index] != null)
                    {
                        m_Mobiles[index].Account = null;
                    }

                    m_Mobiles[index] = value;

                    if (m_Mobiles[index] != null)
                    {
                        m_Mobiles[index].Account = this;
                    }
                }
            }
        }

        public int CompareTo(IAccount other) => string.CompareOrdinal(Username, other?.Username);

        /// <summary>
        ///     This amount represents the current amount of Gold owned by the player.
        ///     The value does not include the value of Platinum and ranges from
        ///     0 to 999,999,999 by default.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public int TotalGold { get; private set; }

        /// <summary>
        ///     This amount represents the current amount of Platinum owned by the player.
        ///     The value does not include the value of Gold and ranges from
        ///     0 to 2,147,483,647 by default.
        ///     One Platinum represents the value of CurrencyThreshold in Gold.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public int TotalPlat { get; private set; }

        /// <summary>
        ///     Attempts to deposit the given amount of Gold into this account.
        ///     If the given amount is greater than the CurrencyThreshold,
        ///     Platinum will be deposited to offset the difference.
        /// </summary>
        /// <param name="amount">Amount to deposit.</param>
        /// <returns>True if successful, false if amount given is less than or equal to zero.</returns>
        public bool DepositGold(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            var plat = Math.DivRem(amount, AccountGold.CurrencyThreshold, out var gold);
            TotalPlat += plat;
            TotalGold += gold;

            return true;
        }

        /// <summary>
        ///     Attempts to deposit the given amount of Platinum into this account.
        /// </summary>
        /// <param name="amount">Amount to deposit.</param>
        /// <returns>True if successful, false if amount given is less than or equal to zero.</returns>
        public bool DepositPlat(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            TotalPlat += amount;
            return true;
        }

        /// <summary>
        ///     Attempts to withdraw the given amount of Gold from this account.
        ///     If the given amount is greater than the CurrencyThreshold,
        ///     Platinum will be withdrawn to offset the difference.
        /// </summary>
        /// <param name="amount">Amount to withdraw.</param>
        /// <returns>True if successful, false if balance was too low.</returns>
        public bool WithdrawGold(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (amount > TotalGold)
            {
                return false;
            }

            TotalGold -= amount;

            return true;
        }

        /// <summary>
        ///     Attempts to withdraw the given amount of Platinum from this account.
        /// </summary>
        /// <param name="amount">Amount to withdraw.</param>
        /// <returns>True if successful, false if balance was too low.</returns>
        public bool WithdrawPlat(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (amount > TotalPlat)
            {
                return false;
            }

            TotalPlat -= amount;

            return true;
        }

        /// <summary>
        ///     Returns total gold inclusive of platinum.
        ///     This is strictly for backwards compatibility
        /// </summary>
        /// <returns>Total gold, capped at Int32.MaxValue</returns>
        public long GetTotalGold() => TotalGold + TotalPlat * AccountGold.CurrencyThreshold;

        public int CompareTo(Account other) => string.CompareOrdinal(Username, other?.Username);

        /// <summary>
        ///     Gets the value of a specific flag in the Flags bitfield.
        /// </summary>
        /// <param name="index">The zero-based flag index.</param>
        public bool GetFlag(int index) => (Flags & (1 << index)) != 0;

        /// <summary>
        ///     Sets the value of a specific flag in the Flags bitfield.
        /// </summary>
        /// <param name="index">The zero-based flag index.</param>
        /// <param name="value">The value to set.</param>
        public void SetFlag(int index, bool value)
        {
            if (value)
            {
                Flags |= 1 << index;
            }
            else
            {
                Flags &= ~(1 << index);
            }
        }

        /// <summary>
        ///     Adds a new tag to this account. This method does not check for duplicate names.
        /// </summary>
        /// <param name="name">New tag name.</param>
        /// <param name="value">New tag value.</param>
        public void AddTag(string name, string value)
        {
            Tags.Add(new AccountTag(name, value));
        }

        /// <summary>
        ///     Removes all tags with the specified name from this account.
        /// </summary>
        /// <param name="name">Tag name to remove.</param>
        public void RemoveTag(string name)
        {
            for (var i = Tags.Count - 1; i >= 0; --i)
            {
                if (i >= Tags.Count)
                {
                    continue;
                }

                var tag = Tags[i];

                if (tag.Name == name)
                {
                    Tags.RemoveAt(i);
                }
            }
        }

        /// <summary>
        ///     Modifies an existing tag or adds a new tag if no tag exists.
        /// </summary>
        /// <param name="name">Tag name.</param>
        /// <param name="value">Tag value.</param>
        public void SetTag(string name, string value)
        {
            for (var i = 0; i < Tags.Count; ++i)
            {
                var tag = Tags[i];

                if (tag.Name == name)
                {
                    tag.Value = value;
                    return;
                }
            }

            AddTag(name, value);
        }

        /// <summary>
        ///     Gets the value of a tag -or- null if there are no tags with the specified name.
        /// </summary>
        /// <param name="name">Name of the desired tag value.</param>
        public string GetTag(string name)
        {
            for (var i = 0; i < Tags.Count; ++i)
            {
                var tag = Tags[i];

                if (tag.Name == name)
                {
                    return tag.Value;
                }
            }

            return null;
        }

        public void SetUnspecifiedBan(Mobile from)
        {
            SetBanTags(from, DateTime.MinValue, TimeSpan.Zero);
        }

        public void SetBanTags(Mobile from, DateTime banTime, TimeSpan banDuration)
        {
            if (from == null)
            {
                RemoveTag("BanDealer");
            }
            else
            {
                SetTag("BanDealer", from.ToString());
            }

            if (banTime == DateTime.MinValue)
            {
                RemoveTag("BanTime");
            }
            else
            {
                SetTag("BanTime", XmlConvert.ToString(banTime, XmlDateTimeSerializationMode.Utc));
            }

            if (banDuration == TimeSpan.Zero)
            {
                RemoveTag("BanDuration");
            }
            else
            {
                SetTag("BanDuration", banDuration.ToString());
            }
        }

        public bool GetBanTags(out DateTime banTime, out TimeSpan banDuration)
        {
            var tagDuration = GetTag("BanDuration");

            banTime = Utility.GetXMLDateTime(GetTag("BanTime"), DateTime.MinValue);

            if (tagDuration == "Infinite")
            {
                banDuration = TimeSpan.MaxValue;
            }
            else if (tagDuration != null)
            {
                banDuration = Utility.ToTimeSpan(tagDuration);
            }
            else
            {
                banDuration = TimeSpan.Zero;
            }

            return banTime != DateTime.MinValue && banDuration != TimeSpan.Zero;
        }

        public static void Initialize()
        {
            EventSink.Connected += EventSink_Connected;
            EventSink.Disconnected += EventSink_Disconnected;
            EventSink.Login += EventSink_Login;
        }

        private static void EventSink_Connected(Mobile m)
        {
            if (!(m.Account is Account acc))
            {
                return;
            }

            if (acc.Young && acc.m_YoungTimer == null)
            {
                acc.m_YoungTimer = new YoungTimer(acc);
                acc.m_YoungTimer.Start();
            }
        }

        private static void EventSink_Disconnected(Mobile m)
        {
            if (!(m.Account is Account acc))
            {
                return;
            }

            if (acc.m_YoungTimer != null)
            {
                acc.m_YoungTimer.Stop();
                acc.m_YoungTimer = null;
            }

            if (!(m is PlayerMobile pm))
            {
                return;
            }

            acc.m_TotalGameTime += Core.Now - pm.SessionStart;
        }

        private static void EventSink_Login(Mobile m)
        {
            if (!(m is PlayerMobile pm))
            {
                return;
            }

            if (!(m.Account is Account acc))
            {
                return;
            }

            if (pm.Young && acc.Young)
            {
                var ts = YoungDuration - acc.TotalGameTime;
                var hours = Math.Max((int)ts.TotalHours, 0);

                m.SendAsciiMessage(
                    "You will enjoy the benefits and relatively safe status of a young player for {0} more hour{1}.",
                    hours,
                    hours != 1 ? "s" : ""
                );
            }
        }

        public void RemoveYoungStatus(int message)
        {
            Young = false;

            for (var i = 0; i < m_Mobiles.Length; i++)
            {
                if (m_Mobiles[i] is PlayerMobile m && m.Young)
                {
                    m.Young = false;

                    if (m.NetState != null)
                    {
                        if (message > 0)
                        {
                            m.SendLocalizedMessage(message);
                        }

                        // You are no longer considered a young player of Ultima Online,
                        // and are no longer subject to the limitations and benefits of being in that caste.
                        m.SendLocalizedMessage(1019039);
                    }
                }
            }
        }

        public void CheckYoung()
        {
            if (TotalGameTime >= YoungDuration)
            {
                // You are old enough to be considered an adult, and have outgrown your status as a young player!
                RemoveYoungStatus(1019038);
            }
        }

        private bool UpgradePassword(string password, PasswordProtectionAlgorithm algorithm)
        {
            if (password == null || algorithm < m_PasswordAlgorithm)
            {
                return false;
            }

            m_PasswordAlgorithm = algorithm;
            Password = password.ReplaceOrdinal("-", string.Empty);
            return true;
        }

        /// <summary>
        ///     Deserializes a list of string values from an xml element. Null values are not added to the list.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>String list. Value will never be null.</returns>
        private static string[] LoadAccessCheck(XmlElement node)
        {
            string[] stringList;
            var accessCheck = node["accessCheck"];

            if (accessCheck != null)
            {
                var list = new List<string>();

                foreach (XmlElement ip in accessCheck.GetElementsByTagName("ip"))
                {
                    var text = Utility.GetText(ip, null);

                    if (text != null)
                    {
                        list.Add(text);
                    }
                }

                stringList = list.ToArray();
            }
            else
            {
                stringList = Array.Empty<string>();
            }

            return stringList;
        }

        /// <summary>
        ///     Deserializes a list of IPAddress values from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Address list. Value will never be null.</returns>
        private static IPAddress[] LoadAddressList(XmlElement node)
        {
            IPAddress[] list;
            var addressList = node["addressList"];

            if (addressList != null)
            {
                var count = Utility.GetXMLInt32(Utility.GetAttribute(addressList, "count", "0"), 0);

                list = new IPAddress[count];

                count = 0;

                foreach (XmlElement ip in addressList.GetElementsByTagName("ip"))
                {
                    if (count < list.Length)
                    {
                        if (IPAddress.TryParse(Utility.GetText(ip, null), out var address))
                        {
                            list[count] = Utility.Intern(address);
                            count++;
                        }
                    }
                }

                if (count != list.Length)
                {
                    var old = list;
                    list = new IPAddress[count];

                    for (var i = 0; i < count && i < old.Length; ++i)
                    {
                        list[i] = old[i];
                    }
                }
            }
            else
            {
                list = Array.Empty<IPAddress>();
            }

            return list;
        }

        /// <summary>
        ///     Deserializes a list of Mobile instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement instance from which to deserialize.</param>
        /// <returns>Mobile list. Value will never be null.</returns>
        private static Mobile[] LoadMobiles(XmlElement node)
        {
            var list = new Mobile[7];
            var chars = node["chars"];

            // int length = Accounts.GetInt32( Accounts.GetAttribute( chars, "length", "6" ), 6 );
            // list = new Mobile[length];
            // Above is legacy, no longer used

            if (chars != null)
            {
                foreach (XmlElement ele in chars.GetElementsByTagName("char"))
                {
                    try
                    {
                        var index = Utility.GetXMLInt32(Utility.GetAttribute(ele, "index", "0"), 0);
                        var serial = Utility.GetXMLUInt32(Utility.GetText(ele, "0"), 0);

                        if (index >= 0 && index < list.Length)
                        {
                            list[index] = World.FindMobile(serial);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            return list;
        }

        /// <summary>
        ///     Deserializes a list of AccountComment instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Comment list. Value will never be null.</returns>
        private static List<AccountComment> LoadComments(XmlElement node)
        {
            List<AccountComment> list = null;
            var comments = node["comments"];

            if (comments != null)
            {
                list = new List<AccountComment>();

                foreach (XmlElement comment in comments.GetElementsByTagName("comment"))
                {
                    try
                    {
                        list.Add(new AccountComment(comment));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            return list;
        }

        /// <summary>
        ///     Deserializes a list of AccountTag instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Tag list. Value will never be null.</returns>
        private static List<AccountTag> LoadTags(XmlElement node)
        {
            List<AccountTag> list = null;
            var tags = node["tags"];

            if (tags != null)
            {
                list = new List<AccountTag>();

                foreach (XmlElement tag in tags.GetElementsByTagName("tag"))
                {
                    try
                    {
                        list.Add(new AccountTag(tag));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            return list;
        }

        /// <summary>
        ///     Checks if a specific NetState is allowed access to this account.
        /// </summary>
        /// <param name="ns">NetState instance to check.</param>
        /// <returns>True if allowed, false if not.</returns>
        public bool HasAccess(NetState ns) => ns != null && HasAccess(ns.Address);

        public bool HasAccess(IPAddress ipAddress)
        {
            var level = AccountHandler.LockdownLevel;

            if (level > AccessLevel.Player)
            {
                var hasAccess = false;

                if (m_AccessLevel >= level)
                {
                    hasAccess = true;
                }
                else
                {
                    for (var i = 0; !hasAccess && i < Length; ++i)
                    {
                        var m = this[i];

                        if (m?.AccessLevel >= level)
                        {
                            hasAccess = true;
                        }
                    }
                }

                Console.WriteLine("{0} {1}", hasAccess ? "yes" : "no", m_AccessLevel);

                if (!hasAccess)
                {
                    return false;
                }
            }

            var accessAllowed = IPRestrictions.Length == 0 || IPLimiter.IsExempt(ipAddress);

            for (var i = 0; !accessAllowed && i < IPRestrictions.Length; ++i)
            {
                accessAllowed = IPAddress.Parse(IPRestrictions[i]).Equals(ipAddress);
            }

            return accessAllowed;
        }

        /// <summary>
        ///     Records the IP address of 'ns' in its 'LoginIPs' list.
        /// </summary>
        /// <param name="ns">NetState instance to record.</param>
        public void LogAccess(NetState ns)
        {
            if (ns != null)
            {
                LogAccess(ns.Address);
            }
        }

        public void LogAccess(IPAddress ipAddress)
        {
            if (IPLimiter.IsExempt(ipAddress))
            {
                return;
            }

            if (LoginIPs.Length == 0)
            {
                AccountHandler.IPTable.TryGetValue(ipAddress, out var result);
                AccountHandler.IPTable[ipAddress] = result + 1;
            }

            var contains = false;

            for (var i = 0; !contains && i < LoginIPs.Length; ++i)
            {
                contains = LoginIPs[i].Equals(ipAddress);
            }

            if (contains)
            {
                return;
            }

            var old = LoginIPs;
            LoginIPs = new IPAddress[old.Length + 1];

            for (var i = 0; i < old.Length; ++i)
            {
                LoginIPs[i] = old[i];
            }

            LoginIPs[old.Length] = ipAddress;
        }

        /// <summary>
        ///     Checks if a specific NetState is allowed access to this account. If true, the NetState IPAddress is added to the address
        ///     list.
        /// </summary>
        /// <param name="ns">NetState instance to check.</param>
        /// <returns>True if allowed, false if not.</returns>
        public bool CheckAccess(NetState ns) => ns != null && CheckAccess(ns.Address);

        public bool CheckAccess(IPAddress ipAddress)
        {
            var hasAccess = HasAccess(ipAddress);

            if (hasAccess)
            {
                LogAccess(ipAddress);
            }

            return hasAccess;
        }

        /// <summary>
        ///     Serializes this Account instance to an XmlTextWriter.
        /// </summary>
        /// <param name="xml">The XmlTextWriter instance from which to serialize.</param>
        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("account");

            xml.WriteStartElement("username");
            xml.WriteString(Username);
            xml.WriteEndElement();

            xml.WriteStartElement("passwordAlgorithm");
            xml.WriteString(m_PasswordAlgorithm.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("password");
            xml.WriteString(Password);
            xml.WriteEndElement();

            if (m_AccessLevel != AccessLevel.Player)
            {
                xml.WriteStartElement("accessLevel");
                xml.WriteString(m_AccessLevel.ToString());
                xml.WriteEndElement();
            }

            if (Flags != 0)
            {
                xml.WriteStartElement("flags");
                xml.WriteString(XmlConvert.ToString(Flags));
                xml.WriteEndElement();
            }

            xml.WriteStartElement("created");
            xml.WriteString(XmlConvert.ToString(Created, XmlDateTimeSerializationMode.Utc));
            xml.WriteEndElement();

            xml.WriteStartElement("lastLogin");
            xml.WriteString(XmlConvert.ToString(LastLogin, XmlDateTimeSerializationMode.Utc));
            xml.WriteEndElement();

            xml.WriteStartElement("totalGameTime");
            xml.WriteString(XmlConvert.ToString(TotalGameTime));
            xml.WriteEndElement();

            xml.WriteStartElement("chars");

            for (var i = 0; i < m_Mobiles.Length; ++i)
            {
                var m = m_Mobiles[i];

                if (m?.Deleted == false)
                {
                    xml.WriteStartElement("char");
                    xml.WriteAttributeString("index", i.ToString());
                    xml.WriteString(m.Serial.Value.ToString());
                    xml.WriteEndElement();
                }
            }

            xml.WriteEndElement();

            if (m_Comments?.Count > 0)
            {
                xml.WriteStartElement("comments");

                for (var i = 0; i < m_Comments.Count; ++i)
                {
                    m_Comments[i].Save(xml);
                }

                xml.WriteEndElement();
            }

            if (m_Tags?.Count > 0)
            {
                xml.WriteStartElement("tags");

                for (var i = 0; i < m_Tags.Count; ++i)
                {
                    m_Tags[i].Save(xml);
                }

                xml.WriteEndElement();
            }

            if (LoginIPs.Length > 0)
            {
                xml.WriteStartElement("addressList");

                xml.WriteAttributeString("count", LoginIPs.Length.ToString());

                for (var i = 0; i < LoginIPs.Length; ++i)
                {
                    xml.WriteStartElement("ip");
                    xml.WriteString(LoginIPs[i].ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            if (IPRestrictions.Length > 0)
            {
                xml.WriteStartElement("accessCheck");

                for (var i = 0; i < IPRestrictions.Length; ++i)
                {
                    xml.WriteStartElement("ip");
                    xml.WriteString(IPRestrictions[i]);
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            xml.WriteStartElement("totalGold");
            xml.WriteString(XmlConvert.ToString(TotalGold));
            xml.WriteEndElement();

            xml.WriteStartElement("totalPlat");
            xml.WriteString(XmlConvert.ToString(TotalPlat));
            xml.WriteEndElement();

            xml.WriteEndElement();
        }

        public override string ToString() => Username;

        private class YoungTimer : Timer
        {
            private readonly Account m_Account;

            public YoungTimer(Account account)
                : base(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0))
            {
                m_Account = account;

                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                m_Account.CheckYoung();
            }
        }
    }
}
