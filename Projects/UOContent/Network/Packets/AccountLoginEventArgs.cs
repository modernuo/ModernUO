namespace Server.Network;

public class AccountLoginEventArgs
{
    public AccountLoginEventArgs(NetState state, string username, string password)
    {
        State = state;
        Username = username;
        Password = password;
    }

    public NetState State { get; }

    public string Username { get; }

    public string Password { get; }

    public bool Accepted { get; set; }

    public ALRReason RejectReason { get; set; }
}
