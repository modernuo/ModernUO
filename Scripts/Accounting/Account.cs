using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Server;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Accounting
{
	public class Account : IAccount, IComparable, IComparable<Account>
	{
		public static readonly TimeSpan YoungDuration = TimeSpan.FromHours( 40.0 );

		private string m_Username, m_PlainPassword, m_CryptPassword, m_NewCryptPassword;
		private AccessLevel m_AccessLevel;
		private int m_Flags;
		private DateTime m_Created, m_LastLogin;
		private TimeSpan m_TotalGameTime;
		private List<AccountComment> m_Comments;
		private List<AccountTag> m_Tags;
		private Mobile[] m_Mobiles;
		private string[] m_IPRestrictions;
		private IPAddress[] m_LoginIPs;
		private HardwareInfo m_HardwareInfo;

		/// <summary>
		/// Deletes the account, all characters of the account, and all houses of those characters
		/// </summary>
		public void Delete()
		{
			for ( int i = 0; i < this.Length; ++i )
			{
				Mobile m = this[i];

				if ( m == null )
					continue;

				List<BaseHouse> list = BaseHouse.GetHouses( m );

				for ( int j = 0; j < list.Count; ++j )
					list[j].Delete();

				m.Delete();

				m.Account = null;
				m_Mobiles[i] = null;
			}

			Accounts.Remove( m_Username );
		}

		/// <summary>
		/// Object detailing information about the hardware of the last person to log into this account
		/// </summary>
		public HardwareInfo HardwareInfo
		{
			get { return m_HardwareInfo; }
			set { m_HardwareInfo = value; }
		}

		/// <summary>
		/// List of IP addresses for restricted access. '*' wildcard supported. If the array contains zero entries, all IP addresses are allowed.
		/// </summary>
		public string[] IPRestrictions
		{
			get { return m_IPRestrictions; }
			set { m_IPRestrictions = value; }
		}

		/// <summary>
		/// List of IP addresses which have successfully logged into this account.
		/// </summary>
		public IPAddress[] LoginIPs
		{
			get { return m_LoginIPs; }
			set { m_LoginIPs = value; }
		}

		/// <summary>
		/// List of account comments. Type of contained objects is AccountComment.
		/// </summary>
		public List<AccountComment> Comments
		{
			get { if ( m_Comments == null ) m_Comments = new List<AccountComment>(); return m_Comments; }
		}

		/// <summary>
		/// List of account tags. Type of contained objects is AccountTag.
		/// </summary>
		public List<AccountTag> Tags
		{
			get { if ( m_Tags == null ) m_Tags = new List<AccountTag>(); return m_Tags; }
		}

		/// <summary>
		/// Account username. Case insensitive validation.
		/// </summary>
		public string Username
		{
			get { return m_Username; }
			set { m_Username = value; }
		}

		/// <summary>
		/// Account password. Plain text. Case sensitive validation. May be null.
		/// </summary>
		public string PlainPassword
		{
			get { return m_PlainPassword; }
			set { m_PlainPassword = value; }
		}

		/// <summary>
		/// Account password. Hashed with MD5. May be null.
		/// </summary>
		public string CryptPassword
		{
			get { return m_CryptPassword; }
			set { m_CryptPassword = value; }
		}

		/// <summary>
		/// Account username and password hashed with SHA1. May be null.
		/// </summary>
		public string NewCryptPassword
		{
			get { return m_NewCryptPassword; }
			set { m_NewCryptPassword = value; }
		}

		/// <summary>
		/// Initial AccessLevel for new characters created on this account.
		/// </summary>
		public AccessLevel AccessLevel
		{
			get { return m_AccessLevel; }
			set { m_AccessLevel = value; }
		}

		/// <summary>
		/// Internal bitfield of account flags. Consider using direct access properties (Banned, Young), or GetFlag/SetFlag methods
		/// </summary>
		public int Flags
		{
			get { return m_Flags; }
			set { m_Flags = value; }
		}

		/// <summary>
		/// Gets or sets a flag indiciating if this account is banned.
		/// </summary>
		public bool Banned
		{
			get
			{
				bool isBanned = GetFlag( 0 );

				if ( !isBanned )
					return false;

				DateTime banTime;
				TimeSpan banDuration;

				if ( GetBanTags( out banTime, out banDuration ) )
				{
					if ( banDuration != TimeSpan.MaxValue && DateTime.Now >= ( banTime + banDuration ) )
					{
						SetUnspecifiedBan( null ); // clear
						Banned = false;
						return false;
					}
				}

				return true;
			}
			set { SetFlag( 0, value ); }
		}

		/// <summary>
		/// Gets or sets a flag indicating if the characters created on this account will have the young status.
		/// </summary>
		public bool Young
		{
			get { return !GetFlag( 1 ); }
			set
			{
				SetFlag( 1, !value );

				if ( m_YoungTimer != null )
				{
					m_YoungTimer.Stop();
					m_YoungTimer = null;
				}
			}
		}

		/// <summary>
		/// The date and time of when this account was created.
		/// </summary>
		public DateTime Created
		{
			get { return m_Created; }
		}

		/// <summary>
		/// Gets or sets the date and time when this account was last accessed.
		/// </summary>
		public DateTime LastLogin
		{
			get { return m_LastLogin; }
			set { m_LastLogin = value; }
		}

		/// <summary>
		/// Gets the total game time of this account, also considering the game time of characters
		/// that have been deleted.
		/// </summary>
		public TimeSpan TotalGameTime
		{
			get
			{
				for ( int i = 0; i < m_Mobiles.Length; i++ )
				{
					PlayerMobile m = m_Mobiles[i] as PlayerMobile;

					if ( m != null && m.NetState != null )
						return m_TotalGameTime + ( DateTime.Now - m.SessionStart );
				}

				return m_TotalGameTime;
			}
		}

		/// <summary>
		/// Gets the value of a specific flag in the Flags bitfield.
		/// </summary>
		/// <param name="index">The zero-based flag index.</param>
		public bool GetFlag( int index )
		{
			return ( m_Flags & ( 1 << index ) ) != 0;
		}

		/// <summary>
		/// Sets the value of a specific flag in the Flags bitfield.
		/// </summary>
		/// <param name="index">The zero-based flag index.</param>
		/// <param name="value">The value to set.</param>
		public void SetFlag( int index, bool value )
		{
			if ( value )
				m_Flags |= ( 1 << index );
			else
				m_Flags &= ~( 1 << index );
		}

		/// <summary>
		/// Adds a new tag to this account. This method does not check for duplicate names.
		/// </summary>
		/// <param name="name">New tag name.</param>
		/// <param name="value">New tag value.</param>
		public void AddTag( string name, string value )
		{
			Tags.Add( new AccountTag( name, value ) );
		}

		/// <summary>
		/// Removes all tags with the specified name from this account.
		/// </summary>
		/// <param name="name">Tag name to remove.</param>
		public void RemoveTag( string name )
		{
			for ( int i = Tags.Count - 1; i >= 0; --i )
			{
				if ( i >= Tags.Count )
					continue;

				AccountTag tag = Tags[i];

				if ( tag.Name == name )
					Tags.RemoveAt( i );
			}
		}

		/// <summary>
		/// Modifies an existing tag or adds a new tag if no tag exists.
		/// </summary>
		/// <param name="name">Tag name.</param>
		/// <param name="value">Tag value.</param>
		public void SetTag( string name, string value )
		{
			for ( int i = 0; i < Tags.Count; ++i )
			{
				AccountTag tag = Tags[i];

				if ( tag.Name == name )
				{
					tag.Value = value;
					return;
				}
			}

			AddTag( name, value );
		}

		/// <summary>
		/// Gets the value of a tag -or- null if there are no tags with the specified name.
		/// </summary>
		/// <param name="name">Name of the desired tag value.</param>
		public string GetTag( string name )
		{
			for ( int i = 0; i < Tags.Count; ++i )
			{
				AccountTag tag = Tags[i];

				if ( tag.Name == name )
					return tag.Value;
			}

			return null;
		}

		public void SetUnspecifiedBan( Mobile from )
		{
			SetBanTags( from, DateTime.MinValue, TimeSpan.Zero );
		}

		public void SetBanTags( Mobile from, DateTime banTime, TimeSpan banDuration )
		{
			if ( from == null )
				RemoveTag( "BanDealer" );
			else
				SetTag( "BanDealer", from.ToString() );

			if ( banTime == DateTime.MinValue )
				RemoveTag( "BanTime" );
			else
				SetTag( "BanTime", XmlConvert.ToString( banTime, XmlDateTimeSerializationMode.Local ) );

			if ( banDuration == TimeSpan.Zero )
				RemoveTag( "BanDuration" );
			else
				SetTag( "BanDuration", banDuration.ToString() );
		}

		public bool GetBanTags( out DateTime banTime, out TimeSpan banDuration )
		{
			string tagTime = GetTag( "BanTime" );
			string tagDuration = GetTag( "BanDuration" );

			if ( tagTime != null )
				banTime = Utility.GetDateTime( tagTime, DateTime.MinValue );
			else
				banTime = DateTime.MinValue;

			if ( tagDuration == "Infinite" )
			{
				banDuration = TimeSpan.MaxValue;
			}
			else if ( tagDuration != null )
			{
				banDuration = Utility.ToTimeSpan( tagDuration );
			}
			else
			{
				banDuration = TimeSpan.Zero;
			}

			return ( banTime != DateTime.MinValue && banDuration != TimeSpan.Zero );
		}

		private static MD5CryptoServiceProvider m_MD5HashProvider;
		private static SHA1CryptoServiceProvider m_SHA1HashProvider;
		private static byte[] m_HashBuffer;

		public static string HashMD5( string phrase )
		{
			if ( m_MD5HashProvider == null )
				m_MD5HashProvider = new MD5CryptoServiceProvider();

			if ( m_HashBuffer == null )
				m_HashBuffer = new byte[256];

			int length = Encoding.ASCII.GetBytes( phrase, 0, phrase.Length > 256 ? 256 : phrase.Length, m_HashBuffer, 0 );
			byte[] hashed = m_MD5HashProvider.ComputeHash( m_HashBuffer, 0, length );

			return BitConverter.ToString( hashed );
		}

		public static string HashSHA1( string phrase )
		{
			if ( m_SHA1HashProvider == null )
				m_SHA1HashProvider = new SHA1CryptoServiceProvider();

			if ( m_HashBuffer == null )
				m_HashBuffer = new byte[256];

			int length = Encoding.ASCII.GetBytes( phrase, 0, phrase.Length > 256 ? 256 : phrase.Length, m_HashBuffer, 0 );
			byte[] hashed = m_SHA1HashProvider.ComputeHash( m_HashBuffer, 0, length );

			return BitConverter.ToString( hashed );
		}

		public void SetPassword( string plainPassword )
		{
			switch ( AccountHandler.ProtectPasswords )
			{
				case PasswordProtection.None:
					{
						m_PlainPassword = plainPassword;
						m_CryptPassword = null;
						m_NewCryptPassword = null;

						break;
					}
				case PasswordProtection.Crypt:
					{
						m_PlainPassword = null;
						m_CryptPassword = HashMD5( plainPassword );
						m_NewCryptPassword = null;

						break;
					}
				default: // PasswordProtection.NewCrypt
					{
						m_PlainPassword = null;
						m_CryptPassword = null;
						m_NewCryptPassword = HashSHA1( m_Username + plainPassword );

						break;
					}
			}
		}

		public bool CheckPassword( string plainPassword )
		{
			bool ok;
			PasswordProtection curProt;

			if ( m_PlainPassword != null )
			{
				ok = ( m_PlainPassword == plainPassword );
				curProt = PasswordProtection.None;
			}
			else if ( m_CryptPassword != null )
			{
				ok = ( m_CryptPassword == HashMD5( plainPassword ) );
				curProt = PasswordProtection.Crypt;
			}
			else
			{
				ok = ( m_NewCryptPassword == HashSHA1( m_Username + plainPassword ) );
				curProt = PasswordProtection.NewCrypt;
			}

			if ( ok && curProt != AccountHandler.ProtectPasswords )
				SetPassword( plainPassword );

			return ok;
		}

		private Timer m_YoungTimer;

		public static void Initialize()
		{
			EventSink.Connected += new ConnectedEventHandler( EventSink_Connected );
			EventSink.Disconnected += new DisconnectedEventHandler( EventSink_Disconnected );
			EventSink.Login += new LoginEventHandler( EventSink_Login );
		}

		private static void EventSink_Connected( ConnectedEventArgs e )
		{
			Account acc = e.Mobile.Account as Account;

			if ( acc == null )
				return;

			if ( acc.Young && acc.m_YoungTimer == null )
			{
				acc.m_YoungTimer = new YoungTimer( acc );
				acc.m_YoungTimer.Start();
			}
		}

		private static void EventSink_Disconnected( DisconnectedEventArgs e )
		{
			Account acc = e.Mobile.Account as Account;

			if ( acc == null )
				return;

			if ( acc.m_YoungTimer != null )
			{
				acc.m_YoungTimer.Stop();
				acc.m_YoungTimer = null;
			}

			PlayerMobile m = e.Mobile as PlayerMobile;
			if ( m == null )
				return;

			acc.m_TotalGameTime += DateTime.Now - m.SessionStart;
		}

		private static void EventSink_Login( LoginEventArgs e )
		{
			PlayerMobile m = e.Mobile as PlayerMobile;

			if ( m == null )
				return;

			Account acc = m.Account as Account;

			if ( acc == null )
				return;

			if ( m.Young && acc.Young )
			{
				TimeSpan ts = YoungDuration - acc.TotalGameTime;
				int hours = Math.Max( (int) ts.TotalHours, 0 );

				m.SendAsciiMessage( "You will enjoy the benefits and relatively safe status of a young player for {0} more hour{1}.", hours, hours != 1 ? "s" : "" );
			}
		}

		public void RemoveYoungStatus( int message )
		{
			this.Young = false;

			for ( int i = 0; i < m_Mobiles.Length; i++ )
			{
				PlayerMobile m = m_Mobiles[i] as PlayerMobile;

				if ( m != null && m.Young )
				{
					m.Young = false;

					if ( m.NetState != null )
					{
						if ( message > 0 )
							m.SendLocalizedMessage( message );

						m.SendLocalizedMessage( 1019039 ); // You are no longer considered a young player of Ultima Online, and are no longer subject to the limitations and benefits of being in that caste.
					}
				}
			}
		}

		public void CheckYoung()
		{
			if ( TotalGameTime >= YoungDuration )
				RemoveYoungStatus( 1019038 ); // You are old enough to be considered an adult, and have outgrown your status as a young player!
		}

		private class YoungTimer : Timer
		{
			private Account m_Account;

			public YoungTimer( Account account )
				: base( TimeSpan.FromMinutes( 1.0 ), TimeSpan.FromMinutes( 1.0 ) )
			{
				m_Account = account;

				Priority = TimerPriority.FiveSeconds;
			}

			protected override void OnTick()
			{
				m_Account.CheckYoung();
			}
		}

		public Account( string username, string password )
		{
			m_Username = username;
			
			SetPassword( password );

			m_AccessLevel = AccessLevel.Player;

			m_Created = m_LastLogin = DateTime.Now;
			m_TotalGameTime = TimeSpan.Zero;

			m_Mobiles = new Mobile[6];

			m_IPRestrictions = new string[0];
			m_LoginIPs = new IPAddress[0];

			Accounts.Add( this );
		}

		public Account( XmlElement node )
		{
			m_Username = Utility.GetText( node["username"], "empty" );

			string plainPassword = Utility.GetText( node["password"], null );
			string cryptPassword = Utility.GetText( node["cryptPassword"], null );
			string newCryptPassword = Utility.GetText( node["newCryptPassword"], null );

			switch ( AccountHandler.ProtectPasswords )
			{
				case PasswordProtection.None:
					{
						if ( plainPassword != null )
							SetPassword( plainPassword );
						else if ( newCryptPassword != null )
							m_NewCryptPassword = newCryptPassword;
						else if ( cryptPassword != null )
							m_CryptPassword = cryptPassword;
						else
							SetPassword( "empty" );

						break;
					}
				case PasswordProtection.Crypt:
					{
						if ( cryptPassword != null )
							m_CryptPassword = cryptPassword;
						else if ( plainPassword != null )
							SetPassword( plainPassword );
						else if ( newCryptPassword != null )
							m_NewCryptPassword = newCryptPassword;
						else
							SetPassword( "empty" );

						break;
					}
				default: // PasswordProtection.NewCrypt
					{
						if ( newCryptPassword != null )
							m_NewCryptPassword = newCryptPassword;
						else if ( plainPassword != null )
							SetPassword( plainPassword );
						else if ( cryptPassword != null )
							m_CryptPassword = cryptPassword;
						else
							SetPassword( "empty" );

						break;
					}
			}

			m_AccessLevel = (AccessLevel)Enum.Parse( typeof( AccessLevel ), Utility.GetText( node["accessLevel"], "Player" ), true );
			m_Flags = Utility.GetInt32( Utility.GetText( node["flags"], "0" ), 0 );
			m_Created = Utility.GetDateTime( Utility.GetText( node["created"], null ), DateTime.Now );
			m_LastLogin = Utility.GetDateTime( Utility.GetText( node["lastLogin"], null ), DateTime.Now );

			m_Mobiles = LoadMobiles( node );
			m_Comments = LoadComments( node );
			m_Tags = LoadTags( node );
			m_LoginIPs = LoadAddressList( node );
			m_IPRestrictions = LoadAccessCheck( node );

			for ( int i = 0; i < m_Mobiles.Length; ++i )
			{
				if ( m_Mobiles[i] != null )
					m_Mobiles[i].Account = this;
			}

			TimeSpan totalGameTime = Utility.GetTimeSpan( Utility.GetText( node["totalGameTime"], null ), TimeSpan.Zero );
			if ( totalGameTime == TimeSpan.Zero )
			{
				for ( int i = 0; i < m_Mobiles.Length; i++ )
				{
					PlayerMobile m = m_Mobiles[i] as PlayerMobile;

					if ( m != null )
						totalGameTime += m.GameTime;
				}
			}
			m_TotalGameTime = totalGameTime;

			if ( this.Young )
				CheckYoung();

			Accounts.Add( this );
		}

		/// <summary>
		/// Deserializes a list of string values from an xml element. Null values are not added to the list.
		/// </summary>
		/// <param name="node">The XmlElement from which to deserialize.</param>
		/// <returns>String list. Value will never be null.</returns>
		public static string[] LoadAccessCheck( XmlElement node )
		{
			string[] stringList;
			XmlElement accessCheck = node["accessCheck"];

			if ( accessCheck != null )
			{
				List<string> list = new List<string>();

				foreach ( XmlElement ip in accessCheck.GetElementsByTagName( "ip" ) )
				{
					string text = Utility.GetText( ip, null );

					if ( text != null )
						list.Add( text );
				}

				stringList = list.ToArray();
			}
			else
			{
				stringList = new string[0];
			}

			return stringList;
		}

		/// <summary>
		/// Deserializes a list of IPAddress values from an xml element.
		/// </summary>
		/// <param name="node">The XmlElement from which to deserialize.</param>
		/// <returns>Address list. Value will never be null.</returns>
		public static IPAddress[] LoadAddressList( XmlElement node )
		{
			IPAddress[] list;
			XmlElement addressList = node["addressList"];

			if ( addressList != null )
			{
				int count = Utility.GetInt32( Utility.GetAttribute( addressList, "count", "0" ), 0 );

				list = new IPAddress[count];

				count = 0;

				foreach ( XmlElement ip in addressList.GetElementsByTagName( "ip" ) )
				{
					if ( count < list.Length )
					{
						IPAddress address;

						if( IPAddress.TryParse( Utility.GetText( ip, null ), out address ) )
						{
							list[count] = Utility.Intern( address );
							count++;
						}
					}
				}

				if ( count != list.Length )
				{
					IPAddress[] old = list;
					list = new IPAddress[count];

					for ( int i = 0; i < count && i < old.Length; ++i )
						list[i] = old[i];
				}
			}
			else
			{
				list = new IPAddress[0];
			}

			return list;
		}

		/// <summary>
		/// Deserializes a list of Mobile instances from an xml element.
		/// </summary>
		/// <param name="node">The XmlElement instance from which to deserialize.</param>
		/// <returns>Mobile list. Value will never be null.</returns>
		public static Mobile[] LoadMobiles( XmlElement node )
		{
			Mobile[] list = new Mobile[6];
			XmlElement chars = node["chars"];

			//int length = Accounts.GetInt32( Accounts.GetAttribute( chars, "length", "6" ), 6 );
			//list = new Mobile[length];
			//Above is legacy, no longer used

			if ( chars != null )
			{
				foreach ( XmlElement ele in chars.GetElementsByTagName( "char" ) )
				{
					try
					{
						int index = Utility.GetInt32( Utility.GetAttribute( ele, "index", "0" ), 0 );
						int serial = Utility.GetInt32( Utility.GetText( ele, "0" ), 0 );

						if ( index >= 0 && index < list.Length )
							list[index] = World.FindMobile( serial );
					}
					catch
					{
					}
				}
			}

			return list;
		}

		/// <summary>
		/// Deserializes a list of AccountComment instances from an xml element.
		/// </summary>
		/// <param name="node">The XmlElement from which to deserialize.</param>
		/// <returns>Comment list. Value will never be null.</returns>
		public static List<AccountComment> LoadComments( XmlElement node )
		{
			List<AccountComment> list = null;
			XmlElement comments = node["comments"];

			if ( comments != null )
			{
				list = new List<AccountComment>();

				foreach ( XmlElement comment in comments.GetElementsByTagName( "comment" ) )
				{
					try { list.Add( new AccountComment( comment ) ); }
					catch { }
				}
			}

			return list;
		}

		/// <summary>
		/// Deserializes a list of AccountTag instances from an xml element.
		/// </summary>
		/// <param name="node">The XmlElement from which to deserialize.</param>
		/// <returns>Tag list. Value will never be null.</returns>
		public static List<AccountTag> LoadTags( XmlElement node )
		{
			List<AccountTag> list = null;
			XmlElement tags = node["tags"];

			if ( tags != null )
			{
				list = new List<AccountTag>();

				foreach ( XmlElement tag in tags.GetElementsByTagName( "tag" ) )
				{
					try { list.Add( new AccountTag( tag ) ); }
					catch { }
				}
			}

			return list;
		}

		/// <summary>
		/// Checks if a specific NetState is allowed access to this account.
		/// </summary>
		/// <param name="ns">NetState instance to check.</param>
		/// <returns>True if allowed, false if not.</returns>
		public bool HasAccess( NetState ns )
		{
			return ( ns != null && HasAccess( ns.Address ) );
		}

		public bool HasAccess( IPAddress ipAddress ) {
			AccessLevel level = Misc.AccountHandler.LockdownLevel;

			if ( level > AccessLevel.Player )
			{
				bool hasAccess = false;

				if ( m_AccessLevel >= level )
				{
					hasAccess = true;
				}
				else
				{
					for ( int i = 0; !hasAccess && i < this.Length; ++i )
					{
						Mobile m = this[i];

						if ( m != null && m.AccessLevel >= level )
							hasAccess = true;
					}
				}

				if ( !hasAccess )
					return false;
			}

			bool accessAllowed = ( m_IPRestrictions.Length == 0 || IPLimiter.IsExempt( ipAddress ) );

			for ( int i = 0; !accessAllowed && i < m_IPRestrictions.Length; ++i )
				accessAllowed = Utility.IPMatch( m_IPRestrictions[i], ipAddress );

			return accessAllowed;
		}

		/// <summary>
		/// Records the IP address of 'ns' in its 'LoginIPs' list.
		/// </summary>
		/// <param name="ns">NetState instance to record.</param>
		public void LogAccess( NetState ns )
		{
			if ( ns != null ) {
				LogAccess( ns.Address );
			}
		}

		public void LogAccess( IPAddress ipAddress ) {
			if ( IPLimiter.IsExempt( ipAddress ) )
				return;

			if ( m_LoginIPs.Length == 0 ) {
				if ( AccountHandler.IPTable.ContainsKey( ipAddress ) )
					AccountHandler.IPTable[ipAddress]++;
				else
					AccountHandler.IPTable[ipAddress] = 1;
			}

			bool contains = false;

			for ( int i = 0; !contains && i < m_LoginIPs.Length; ++i )
				contains = m_LoginIPs[i].Equals( ipAddress );

			if ( contains )
				return;

			IPAddress[] old = m_LoginIPs;
			m_LoginIPs = new IPAddress[old.Length + 1];

			for ( int i = 0; i < old.Length; ++i )
				m_LoginIPs[i] = old[i];

			m_LoginIPs[old.Length] = ipAddress;
		}

		/// <summary>
		/// Checks if a specific NetState is allowed access to this account. If true, the NetState IPAddress is added to the address list.
		/// </summary>
		/// <param name="ns">NetState instance to check.</param>
		/// <returns>True if allowed, false if not.</returns>
		public bool CheckAccess( NetState ns )
		{
			return ( ns != null && CheckAccess( ns.Address ) );
		}

		public bool CheckAccess( IPAddress ipAddress ) {
			bool hasAccess = this.HasAccess( ipAddress );

			if ( hasAccess ) {
				LogAccess( ipAddress );
			}

			return hasAccess;
		}

		/// <summary>
		/// Serializes this Account instance to an XmlTextWriter.
		/// </summary>
		/// <param name="xml">The XmlTextWriter instance from which to serialize.</param>
		public void Save( XmlTextWriter xml )
		{
			xml.WriteStartElement( "account" );

			xml.WriteStartElement( "username" );
			xml.WriteString( m_Username );
			xml.WriteEndElement();

			if ( m_PlainPassword != null )
			{
				xml.WriteStartElement( "password" );
				xml.WriteString( m_PlainPassword );
				xml.WriteEndElement();
			}

			if ( m_CryptPassword != null )
			{
				xml.WriteStartElement( "cryptPassword" );
				xml.WriteString( m_CryptPassword );
				xml.WriteEndElement();
			}

			if ( m_NewCryptPassword != null )
			{
				xml.WriteStartElement( "newCryptPassword" );
				xml.WriteString( m_NewCryptPassword );
				xml.WriteEndElement();
			}

			if ( m_AccessLevel != AccessLevel.Player )
			{
				xml.WriteStartElement( "accessLevel" );
				xml.WriteString( m_AccessLevel.ToString() );
				xml.WriteEndElement();
			}

			if ( m_Flags != 0 )
			{
				xml.WriteStartElement( "flags" );
				xml.WriteString( XmlConvert.ToString( m_Flags ) );
				xml.WriteEndElement();
			}

			xml.WriteStartElement( "created" );
			xml.WriteString( XmlConvert.ToString( m_Created, XmlDateTimeSerializationMode.Local ) );
			xml.WriteEndElement();

			xml.WriteStartElement( "lastLogin" );
			xml.WriteString( XmlConvert.ToString( m_LastLogin, XmlDateTimeSerializationMode.Local ) );
			xml.WriteEndElement();

			xml.WriteStartElement( "totalGameTime" );
			xml.WriteString( XmlConvert.ToString( TotalGameTime ) );
			xml.WriteEndElement();

			xml.WriteStartElement( "chars" );

			//xml.WriteAttributeString( "length", m_Mobiles.Length.ToString() );	//Legacy, Not used anymore

			for ( int i = 0; i < m_Mobiles.Length; ++i )
			{
				Mobile m = m_Mobiles[i];

				if ( m != null && !m.Deleted )
				{
					xml.WriteStartElement( "char" );
					xml.WriteAttributeString( "index", i.ToString() );
					xml.WriteString( m.Serial.Value.ToString() );
					xml.WriteEndElement();
				}
			}

			xml.WriteEndElement();

			if ( m_Comments != null && m_Comments.Count > 0 )
			{
				xml.WriteStartElement( "comments" );

				for ( int i = 0; i < m_Comments.Count; ++i )
					m_Comments[i].Save( xml );

				xml.WriteEndElement();
			}

			if ( m_Tags != null && m_Tags.Count > 0 )
			{
				xml.WriteStartElement( "tags" );

				for ( int i = 0; i < m_Tags.Count; ++i )
					m_Tags[i].Save( xml );

				xml.WriteEndElement();
			}

			if ( m_LoginIPs.Length > 0 )
			{
				xml.WriteStartElement( "addressList" );

				xml.WriteAttributeString( "count", m_LoginIPs.Length.ToString() );

				for ( int i = 0; i < m_LoginIPs.Length; ++i )
				{
					xml.WriteStartElement( "ip" );
					xml.WriteString( m_LoginIPs[i].ToString() );
					xml.WriteEndElement();
				}

				xml.WriteEndElement();
			}

			if ( m_IPRestrictions.Length > 0 )
			{
				xml.WriteStartElement( "accessCheck" );

				for ( int i = 0; i < m_IPRestrictions.Length; ++i )
				{
					xml.WriteStartElement( "ip" );
					xml.WriteString( m_IPRestrictions[i] );
					xml.WriteEndElement();
				}

				xml.WriteEndElement();
			}

			xml.WriteEndElement();
		}

		/// <summary>
		/// Gets the current number of characters on this account.
		/// </summary>
		public int Count
		{
			get
			{
				int count = 0;

				for ( int i = 0; i < this.Length; ++i )
				{
					if ( this[i] != null )
						++count;
				}

				return count;
			}
		}

		/// <summary>
		/// Gets the maximum amount of characters allowed to be created on this account. Values other than 1, 5, or 6 are not supported by the client.
		/// </summary>
		public int Limit
		{
			get { return ( Core.AOS ? 6 : 5 ); }
		}

		/// <summary>
		/// Gets the maxmimum amount of characters that this account can hold.
		/// </summary>
		public int Length
		{
			get { return m_Mobiles.Length; }
		}

		/// <summary>
		/// Gets or sets the character at a specified index for this account. Out of bound index values are handled; null returned for get, ignored for set.
		/// </summary>
		public Mobile this[int index]
		{
			get
			{
				if ( index >= 0 && index < m_Mobiles.Length )
				{
					Mobile m = m_Mobiles[index];

					if ( m != null && m.Deleted )
					{
						m.Account = null;
						m_Mobiles[index] = m = null;
					}

					return m;
				}

				return null;
			}
			set
			{
				if ( index >= 0 && index < m_Mobiles.Length )
				{
					if ( m_Mobiles[index] != null )
						m_Mobiles[index].Account = null;

					m_Mobiles[index] = value;

					if ( m_Mobiles[index] != null )
						m_Mobiles[index].Account = this;
				}
			}
		}

		public override string ToString()
		{
			return m_Username;
		}

		public int CompareTo( Account other )
		{
			if ( other == null )
				return -1;

			return m_Username.CompareTo( other.m_Username );
		}

		public int CompareTo( object obj )
		{
			if ( obj is Account )
				return this.CompareTo( (Account) obj );

			throw new ArgumentException();
		}
	}
}