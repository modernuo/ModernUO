using System.Collections.Generic;

namespace Server.Engines.Chat
{
    public class Channel
    {
        private readonly List<ChatUser> _Banned;
        private readonly List<ChatUser> _Moderators;
        private readonly List<ChatUser> _Users;
        private readonly List<ChatUser> _Voices;
        private string _Name;
        private string _Password;
        private bool _VoiceRestricted;

        public Channel(string name)
        {
            _Name = name;

            _Users = new List<ChatUser>();
            _Banned = new List<ChatUser>();
            _Moderators = new List<ChatUser>();
            _Voices = new List<ChatUser>();
        }

        public Channel(string name, string password) : this(name) => _Password = password;

        public string Name
        {
            get => _Name;
            set
            {
                SendCommand(ChatCommand.RemoveChannel, _Name);
                _Name = value;
                SendCommand(ChatCommand.AddChannel, _Name);
                SendCommand(ChatCommand.JoinedChannel, _Name);
            }
        }

        public string Password
        {
            get => _Password;
            set => _Password = (value?.Trim()).DefaultIfNullOrEmpty(null);
        }

        public bool VoiceRestricted
        {
            get => _VoiceRestricted;
            set
            {
                _VoiceRestricted = value;

                if (value)
                {
                    // From now on, only moderators will have speaking privileges in this conference by default.
                    SendMessage(56);
                }
                else
                {
                    // From now on, everyone in the conference will have speaking privileges by default.
                    SendMessage(55);
                }
            }
        }

        public bool AlwaysAvailable { get; set; }
        public static List<Channel> Channels { get; } = new();
        public bool Contains(ChatUser user) => _Users.Contains(user);
        public bool IsBanned(ChatUser user) => _Banned.Contains(user);
        public bool CanTalk(ChatUser user) => !_VoiceRestricted || _Voices.Contains(user) || _Moderators.Contains(user);
        public bool IsModerator(ChatUser user) => _Moderators.Contains(user);
        public bool IsVoiced(ChatUser user) => _Voices.Contains(user);
        public bool ValidatePassword(string password) => _Password?.InsensitiveEquals(password) != false;

        public bool ValidateModerator(ChatUser user)
        {
            if (user != null && !IsModerator(user))
            {
                user.SendMessage(29); // You must have operator status to do this.
                return false;
            }

            return true;
        }

        public bool ValidateAccess(ChatUser from, ChatUser target)
        {
            if (from == null || target == null || from.Mobile.AccessLevel >= target.Mobile.AccessLevel)
            {
                return true;
            }

            from.Mobile.SendMessage("Your access level is too low to do this.");
            return false;
        }

        public bool AddUser(ChatUser user, string password = null)
        {
            if (Contains(user))
            {
                user.SendMessage(46, _Name); // You are already in the conference '%1'.
                return true;
            }

            if (IsBanned(user))
            {
                user.SendMessage(64); // You have been banned from this conference.
                return false;
            }

            if (!ValidatePassword(password))
            {
                user.SendMessage(34); // That is not the correct password.
                return false;
            }

            user.CurrentChannel?.RemoveUser(user); // Remove them from their current channel first

            ChatSystem.SendCommandTo(user.Mobile, ChatCommand.JoinedChannel, _Name);

            SendCommand(ChatCommand.AddUserToChannel, user.GetColorCharacter() + user.Username);

            _Users.Add(user);
            user.CurrentChannel = this;

            if (user.Mobile.AccessLevel >= AccessLevel.GameMaster || !AlwaysAvailable && _Users.Count == 1)
            {
                AddModerator(user);
            }

            SendUsersTo(user);

            return true;
        }

        public void RemoveUser(ChatUser user)
        {
            if (Contains(user))
            {
                _Users.Remove(user);
                user.CurrentChannel = null;

                if (_Moderators.Contains(user))
                {
                    _Moderators.Remove(user);
                }

                if (_Voices.Contains(user))
                {
                    _Voices.Remove(user);
                }

                SendCommand(ChatCommand.RemoveUserFromChannel, user, user.Username);
                ChatSystem.SendCommandTo(user.Mobile, ChatCommand.LeaveChannel);

                if (!AlwaysAvailable && _Users.Count == 0)
                {
                    RemoveChannel(this);
                }
            }
        }

        public void AddBan(ChatUser user, ChatUser moderator = null)
        {
            if (!ValidateModerator(moderator) || !ValidateAccess(moderator, user))
            {
                return;
            }

            if (!_Banned.Contains(user))
            {
                _Banned.Add(user);
            }

            Kick(user, moderator, true);
        }

        public void RemoveBan(ChatUser user)
        {
            if (_Banned.Contains(user))
            {
                _Banned.Remove(user);
            }
        }

        public void Kick(ChatUser user, ChatUser moderator = null)
        {
            Kick(user, moderator, false);
        }

        public void Kick(ChatUser user, ChatUser moderator, bool wasBanned)
        {
            if (!ValidateModerator(moderator) || !ValidateAccess(moderator, user))
            {
                return;
            }

            if (Contains(user))
            {
                if (moderator != null)
                {
                    if (wasBanned)
                    {
                        // %1, a conference moderator, has banned you from the conference.
                        user.SendMessage(63, moderator.Username);
                    }
                    else
                    {
                        // %1, a conference moderator, has kicked you out of the conference.
                        user.SendMessage(45, moderator.Username);
                    }
                }

                RemoveUser(user);
                ChatSystem.SendCommandTo(
                    user.Mobile,
                    ChatCommand.AddUserToChannel,
                    user.GetColorCharacter() + user.Username
                );

                SendMessage(44, user.Username); // %1 has been kicked out of the conference.
            }

            if (wasBanned)
            {
                moderator?.SendMessage(62, user.Username); // You are banning %1 from this conference.
            }
        }

        public void AddVoiced(ChatUser user, ChatUser moderator = null)
        {
            if (!ValidateModerator(moderator))
            {
                return;
            }

            if (!IsBanned(user) && !IsModerator(user) && !IsVoiced(user))
            {
                _Voices.Add(user);

                if (moderator != null)
                {
                    // %1, a conference moderator, has granted you speaking privileges in this conference.
                    user.SendMessage(54, moderator.Username);
                }

                SendMessage(52, user, user.Username); // %1 now has speaking privileges in this conference.
                SendCommand(ChatCommand.AddUserToChannel, user, user.GetColorCharacter() + user.Username);
            }
        }

        public void RemoveVoiced(ChatUser user, ChatUser moderator)
        {
            if (!ValidateModerator(moderator) || !ValidateAccess(moderator, user))
            {
                return;
            }

            if (!IsModerator(user) && IsVoiced(user))
            {
                _Voices.Remove(user);

                if (moderator != null)
                {
                    // %1, a conference moderator, has removed your speaking privileges for this conference.
                    user.SendMessage(53, moderator.Username);
                }

                SendMessage(51, user, user.Username); // %1 no longer has speaking privileges in this conference.
                SendCommand(ChatCommand.AddUserToChannel, user, user.GetColorCharacter() + user.Username);
            }
        }

        public void AddModerator(ChatUser user, ChatUser moderator = null)
        {
            if (!ValidateModerator(moderator))
            {
                return;
            }

            if (IsBanned(user) || IsModerator(user))
            {
                return;
            }

            if (IsVoiced(user))
            {
                _Voices.Remove(user);
            }

            _Moderators.Add(user);

            if (moderator != null)
            {
                user.SendMessage(50, moderator.Username); // %1 has made you a conference moderator.
            }

            SendMessage(48, user, user.Username); // %1 is now a conference moderator.
            SendCommand(ChatCommand.AddUserToChannel, user.GetColorCharacter() + user.Username);
        }

        public void RemoveModerator(ChatUser user, ChatUser moderator = null)
        {
            if (!ValidateModerator(moderator) || !ValidateAccess(moderator, user))
            {
                return;
            }

            if (IsModerator(user))
            {
                _Moderators.Remove(user);

                if (moderator != null)
                {
                    user.SendMessage(49, moderator.Username); // %1 has removed you from the list of conference moderators.
                }

                SendMessage(47, user, user.Username); // %1 is no longer a conference moderator.
                SendCommand(ChatCommand.AddUserToChannel, user.GetColorCharacter() + user.Username);
            }
        }

        public void SendMessage(int number, string param1 = null)
        {
            SendMessage(number, null, param1);
        }

        public void SendMessage(int number, ChatUser initiator, string param1 = null, string param2 = null)
        {
            for (var i = _Users.Count - 1; i >= 0; --i)
            {
            var user = _Users[i];

            if (user == initiator)
            {
                continue;
            }

            if (user.CheckOnline())
            {
                user.SendMessage(number, param1, param2);
            }
            else
            {
                RemoveUser(user);
            }
            }
        }

        public void SendIgnorableMessage(int number, ChatUser from, string param1, string param2)
        {
            for (var i = _Users.Count - 1; i >= 0; --i)
            {
                var user = _Users[i];

                if (user.IsIgnored(from))
                {
                    continue;
                }

                if (user.CheckOnline())
                {
                    user.SendMessage(number, from.Mobile, param1, param2);
                }
                else
                {
                    RemoveUser(user);
                }
            }
        }

        public void SendCommand(ChatCommand command, string param1 = null, string param2 = null)
        {
            SendCommand(command, null, param1, param2);
        }

        public void SendCommand(ChatCommand command, ChatUser initiator, string param1 = null, string param2 = null)
        {
            for (var i = _Users.Count - 1; i >= 0; --i)
            {
                var user = _Users[i];

                if (user == initiator)
                {
                    continue;
                }

                if (user.CheckOnline())
                {
                    ChatSystem.SendCommandTo(user.Mobile, command, param1, param2);
                }
                else
                {
                    RemoveUser(user);
                }
            }
        }

        public void SendUsersTo(ChatUser to)
        {
            for (var i = 0; i < _Users.Count; ++i)
            {
                var user = _Users[i];

                ChatSystem.SendCommandTo(to.Mobile, ChatCommand.AddUserToChannel, user.GetColorCharacter() + user.Username);
            }
        }

        public static void SendChannelsTo(ChatUser user)
        {
            for (var i = 0; i < Channels.Count; ++i)
            {
                var channel = Channels[i];

                if (!channel.IsBanned(user))
                {
                    ChatSystem.SendCommandTo(user.Mobile, ChatCommand.AddChannel, channel.Name, "0");
                }
            }
        }

        public static Channel AddChannel(string name, string password = null)
        {
            var channel = FindChannelByName(name);

            if (channel == null)
            {
                channel = new Channel(name, password);
                Channels.Add(channel);
            }

            ChatUser.GlobalSendCommand(ChatCommand.AddChannel, name, "0");

            return channel;
        }

        public static void RemoveChannel(string name)
        {
            RemoveChannel(FindChannelByName(name));
        }

        public static void RemoveChannel(Channel channel)
        {
            if (channel == null)
            {
                return;
            }

            if (Channels.Contains(channel) && channel._Users.Count == 0)
            {
                ChatUser.GlobalSendCommand(ChatCommand.RemoveChannel, channel.Name);

                channel._Moderators.Clear();
                channel._Voices.Clear();

                Channels.Remove(channel);
            }
        }

        public static Channel FindChannelByName(string name)
        {
            for (var i = 0; i < Channels.Count; ++i)
            {
                var channel = Channels[i];

                if (channel._Name == name)
                {
                    return channel;
                }
            }

            return null;
        }

        // currently, all chat is combined to a single webhook
        // all static channels will be captured
        // also captures player created channels
        public static void Initialize()
        {
            AddStaticChannel("Newbie Help");
        }

        public static void AddStaticChannel(string name)
        {
            AddChannel(name).AlwaysAvailable = true;
        }
    }
}
