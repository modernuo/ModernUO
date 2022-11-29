using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Xml;
using ModernUO.Serialization;
using Server.Accounting.Security;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Accounting
{
    [SerializationGenerator(4)]
    public partial class Account : IAccount, IComparable<Account>, ISerializable
    {
        public static readonly TimeSpan YoungDuration = TimeSpan.FromHours(40.0);
        public static readonly TimeSpan InactiveDuration = TimeSpan.FromDays(180.0);
        public static readonly TimeSpan EmptyInactiveDuration = TimeSpan.FromDays(30.0);

        [SerializableField(0)]
        private string _username;

        [SerializableField(1)]
        private PasswordProtectionAlgorithm _passwordAlgorithm;

        [SerializableField(2)]
        private string _password;

        [SerializableField(3)]
        private AccessLevel _accessLevel;

        [SerializableField(4)]
        private int _flags;

        [SerializableField(5)]
        private DateTime _lastLogin;

        /// <summary>
        ///     This amount represents the current amount of Gold owned by the player.
        ///     The value does not include the value of Platinum and ranges from
        ///     0 to 999,999,999 by default.
        /// </summary>
        [SerializableField(6, setter: "private")]
        [SerializedCommandProperty(AccessLevel.Administrator)]
        public int _totalGold;

        /// <summary>
        ///     This amount represents the current amount of Platinum owned by the player.
        ///     The value does not include the value of Gold and ranges from
        ///     0 to 2,147,483,647 by default.
        ///     One Platinum represents the value of CurrencyThreshold in Gold.
        /// </summary>
        [SerializableField(7, setter: "private")]
        [SerializedCommandProperty(AccessLevel.Administrator)]
        public int _totalPlat;

        private Mobile[] _mobiles;

        [SerializableProperty(9)]
        public List<AccountComment> Comments
        {
            get => _comments ??= new List<AccountComment>();
            private set
            {
                _comments = value;
                this.MarkDirty();
            }
        }

        [SerializableProperty(10)]
        public List<AccountTag> Tags
        {
            get => _tags ??= new List<AccountTag>();
            private set
            {
                _tags = value;
                this.MarkDirty();
            }
        }

        [SerializableField(11)]
        private IPAddress[] _loginIPs;

        /// <summary>
        ///     List of IP addresses for restricted access. '*' wildcard supported. If the array contains zero entries, all IP addresses
        ///     are allowed.
        /// </summary>
        [SerializableField(12)]
        private string[] _ipRestrictions;

        /// <summary>
        ///     Gets the total game time of this account, also considering the game time of characters
        ///     that have been deleted.
        /// </summary>
        [SerializableProperty(13)]
        public TimeSpan TotalGameTime
        {
            get
            {
                for (var i = 0; i < _mobiles.Length; i++)
                {
                    if (_mobiles[i] is PlayerMobile m && m.NetState != null)
                    {
                        return _totalGameTime + (Core.Now - m.SessionStart);
                    }
                }

                return _totalGameTime;
            }
            private set
            {
                _totalGameTime = value;
                this.MarkDirty();
            }
        }

        [SerializableField(14)]
        [SerializedCommandProperty(AccessLevel.Administrator)]
        private string _email;

        private Timer m_YoungTimer;

        public Account(string username, string password) : this(Accounts.NewAccount)
        {
            _username = username;

            SetPassword(password);

            _accessLevel = AccessLevel.Player;

            _lastLogin = Core.Now;
            _totalGameTime = TimeSpan.Zero;

            _mobiles = new Mobile[7];

            _ipRestrictions = Array.Empty<string>();
            _loginIPs = Array.Empty<IPAddress>();

            Accounts.Add(this);
            this.MarkDirty();
        }

        public Account(XmlElement node)
        {
            Serial = Accounts.NewAccount;

            _username = Utility.GetText(node["username"], "empty");

            Enum.TryParse(Utility.GetText(node["passwordAlgorithm"], null), true, out _passwordAlgorithm);

            // Backward compatibility with RunUO/ServUO
            if (_passwordAlgorithm == PasswordProtectionAlgorithm.None)
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
                _password = Utility.GetText(node["password"], null);
            }

            Enum.TryParse(Utility.GetText(node["accessLevel"], "Player"), true, out _accessLevel);
            _flags = Utility.GetXMLInt32(Utility.GetText(node["flags"], "0"), 0);
            Created = Utility.GetXMLDateTime(Utility.GetText(node["created"], null), Core.Now);
            _lastLogin = Utility.GetXMLDateTime(Utility.GetText(node["lastLogin"], null), Core.Now);

            _totalGold = Utility.GetXMLInt32(Utility.GetText(node["totalGold"], "0"), 0);
            _totalPlat = Utility.GetXMLInt32(Utility.GetText(node["totalPlat"], "0"), 0);

            _mobiles = LoadMobiles(node);
            _comments = LoadComments(node);
            _tags = LoadTags(node);
            _loginIPs = LoadAddressList(node);
            _ipRestrictions = LoadAccessCheck(node);

            for (var i = 0; i < _mobiles.Length; ++i)
            {
                if (_mobiles[i] != null)
                {
                    _mobiles[i].Account = this;
                }
            }

            var totalGameTime = Utility.GetXMLTimeSpan(Utility.GetText(node["totalGameTime"], null), TimeSpan.Zero);
            if (totalGameTime == TimeSpan.Zero)
            {
                for (var i = 0; i < _mobiles.Length; i++)
                {
                    if (_mobiles[i] is PlayerMobile m)
                    {
                        totalGameTime += m.GameTime;
                    }
                }
            }

            _totalGameTime = totalGameTime;

            if (Young)
            {
                CheckYoung();
            }

            Accounts.Add(this);
            this.MarkDirty();
        }

        /// <summary>
        ///     Object detailing information about the hardware of the last person to log into this account
        /// </summary>
        public HardwareInfo HardwareInfo { get; set; }

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

                var inactiveLength = Core.Now - _lastLogin;

                return inactiveLength > (Count == 0 ? EmptyInactiveDuration : InactiveDuration);
            }
        }

        public TimeSpan AccountAge => Core.Now - Created;

        [CommandProperty(AccessLevel.GameMaster, readOnly: true)]
        public DateTime Created { get; set; } = Core.Now;

        [CommandProperty(AccessLevel.GameMaster)]
        DateTime ISerializable.LastSerialized { get; set; } = Core.Now;

        public Serial Serial { get; set; }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (_comments?.Count == 0)
            {
                _comments = null;
            }

            if (_tags?.Count == 0)
            {
                _tags = null;
            }

            for (var i = 0; i < _mobiles.Length; ++i)
            {
                if (_mobiles[i] != null)
                {
                    _mobiles[i].Account = this;
                }
            }

            if (_totalGameTime == TimeSpan.Zero)
            {
                for (var i = 0; i < _mobiles.Length; i++)
                {
                    if (_mobiles[i] is PlayerMobile m)
                    {
                        _totalGameTime += m.GameTime;
                    }
                }
            }

            if (Young)
            {
                CheckYoung();
            }
        }

        public bool ShouldExecuteAfterSerialize => false;

        public void AfterSerialize()
        {
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
                _mobiles[i] = null;
            }

            if (_loginIPs.Length != 0 && AccountHandler.IPTable.ContainsKey(_loginIPs[0]))
            {
                --AccountHandler.IPTable[_loginIPs[0]];
            }

            Deleted = true;
            Accounts.Remove(this);
        }

        public bool Deleted { get; private set; }

        public void SetPassword(string plainPassword)
        {
            Password = AccountSecurity.CurrentPasswordProtection.EncryptPassword(plainPassword);
            PasswordAlgorithm = AccountSecurity.CurrentAlgorithm;
        }

        public bool CheckPassword(string plainPassword)
        {
            var phrase = _passwordAlgorithm == PasswordProtectionAlgorithm.SHA1
                ? $"{_username}{plainPassword}"
                : plainPassword;

            var ok = AccountSecurity.GetPasswordProtection(_passwordAlgorithm).ValidatePassword(Password, phrase);
            if (!ok)
            {
                return false;
            }

            // Upgrade the password protection in case we change the algorithm
            if (_passwordAlgorithm != AccountSecurity.CurrentAlgorithm)
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
        public int Limit => Core.SA ? 7 : Core.AOS ? 6 : 5;

        /// <summary>
        ///     Gets the maximum amount of characters that this account can hold.
        /// </summary>
        public int Length => _mobiles.Length;

        /// <summary>
        ///     Gets or sets the character at a specified index for this account. Out of bound index values are handled; null returned
        ///     for get, ignored for set.
        /// </summary>
        public Mobile this[int index]
        {
            get
            {
                if (index >= 0 && index < _mobiles.Length)
                {
                    var m = _mobiles[index];

                    if (m?.Deleted != true)
                    {
                        return m;
                    }

                    // This is the only place that clears a mobile for garbage collection
                    // outside of an entire account deletion.
                    m.Account = null;
                    _mobiles[index] = null;
                    this.MarkDirty();
                }

                return null;
            }
            set
            {
                if (index >= 0 && index < _mobiles.Length)
                {
                    if (_mobiles[index] != null)
                    {
                        _mobiles[index].Account = null;
                    }

                    _mobiles[index] = value;
                    this.MarkDirty();

                    if (_mobiles[index] != null)
                    {
                        _mobiles[index].Account = this;
                    }
                }
            }
        }

        public int CompareTo(IAccount other) => string.CompareOrdinal(Username, other?.Username);

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

            if (amount > _totalGold)
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

            if (amount > _totalPlat)
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
        public long GetTotalGold() => _totalGold + _totalPlat * AccountGold.CurrencyThreshold;

        public int CompareTo(Account other) => string.CompareOrdinal(_username, other?._username);

        /// <summary>
        ///     Gets the value of a specific flag in the Flags bitfield.
        /// </summary>
        /// <param name="index">The zero-based flag index.</param>
        public bool GetFlag(int index) => (_flags & (1 << index)) != 0;

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
            this.MarkDirty();
        }

        /// <summary>
        ///     Removes all tags with the specified name from this account.
        /// </summary>
        /// <param name="name">Tag name to remove.</param>
        public void RemoveTag(string name)
        {
            if (_tags == null)
            {
                return;
            }

            for (var i = _tags.Count - 1; i >= 0; --i)
            {
                if (i >= _tags.Count)
                {
                    continue;
                }

                var tag = _tags[i];

                if (tag.Name == name)
                {
                    _tags.RemoveAt(i);
                    this.MarkDirty();
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
                var tag = _tags[i];

                if (tag.Name == name)
                {
                    tag.Value = value;
                    this.MarkDirty();
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
                var tag = _tags[i];

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
            if (m.Account is not Account acc)
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
            if (m.Account is not Account acc)
            {
                return;
            }

            if (acc.m_YoungTimer != null)
            {
                acc.m_YoungTimer.Stop();
                acc.m_YoungTimer = null;
            }

            if (m is not PlayerMobile pm)
            {
                return;
            }

            acc.TotalGameTime += Core.Now - pm.SessionStart;
        }

        private static void EventSink_Login(Mobile m)
        {
            if (m is not PlayerMobile pm)
            {
                return;
            }

            if (m.Account is not Account acc)
            {
                return;
            }

            if (pm.Young && acc.Young)
            {
                var ts = YoungDuration - acc.TotalGameTime;
                var hours = Math.Max((int)ts.TotalHours, 0);

                if (hours == 1)
                {
                    m.SendAsciiMessage($"You will enjoy the benefits and relatively safe status of a young player for {hours} more hour.");
                }
                else
                {
                    m.SendAsciiMessage($"You will enjoy the benefits and relatively safe status of a young player for {hours} more hours.");
                }
            }
        }

        public void RemoveYoungStatus(int message)
        {
            Young = false;

            for (var i = 0; i < _mobiles.Length; i++)
            {
                if (_mobiles[i] is PlayerMobile { Young: true } m)
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
            if (password == null || algorithm < _passwordAlgorithm)
            {
                return false;
            }

            PasswordAlgorithm = algorithm;
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
                        var serial = (Serial)Utility.GetXMLUInt32(Utility.GetText(ele, "0"), 0);

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

                if (_accessLevel >= level)
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

                if (!hasAccess)
                {
                    return false;
                }
            }

            var accessAllowed = _ipRestrictions.Length == 0 || IPLimiter.IsExempt(ipAddress);

            for (var i = 0; !accessAllowed && i < _ipRestrictions.Length; ++i)
            {
                accessAllowed = IPAddress.Parse(_ipRestrictions[i]).Equals(ipAddress);
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

            if (_loginIPs.Length == 0)
            {
                AccountHandler.IPTable.TryGetValue(ipAddress, out var result);
                AccountHandler.IPTable[ipAddress] = result + 1;
            }

            var contains = false;

            for (var i = 0; !contains && i < _loginIPs.Length; ++i)
            {
                contains = _loginIPs[i].Equals(ipAddress);
            }

            if (contains)
            {
                return;
            }

            var old = _loginIPs;
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

        public override string ToString() => _username;

        private class YoungTimer : Timer
        {
            private readonly Account m_Account;

            public YoungTimer(Account account)
                : base(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0))
            {
                m_Account = account;
            }

            protected override void OnTick()
            {
                m_Account.CheckYoung();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(_mobiles);

        [SerializableProperty(8, useField: nameof(_mobiles))]
        public Enumerator Mobiles
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetEnumerator();
        }

        public ref struct Enumerator
        {
            private readonly Mobile[] _mobiles;
            private int _index;
            private Mobile _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(Mobile[] mobs)
            {
                _mobiles = mobs;
                _index = 0;
                _current = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                Mobile[] localList = _mobiles;

                while ((uint)_index < (uint)localList.Length)
                {
                    _current = _mobiles[_index++];
                    if (_current?.Deleted == false)
                    {
                        return true;
                    }
                }

                return false;
            }

            public Mobile Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
        }
    }
}
