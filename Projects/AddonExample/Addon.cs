using Server.Accounting;

namespace Server.Addon;

public class Addon
{
    public static void Configure()
    {
        // ModernUO lifecycle hook before world loads
        EventSink.AccountLogin += AccountLogin;
    }

    public static void Initialize()
    {
        // ModernUO lifecycle hook after world loads
    }

    public static void AccountLogin(AccountLoginEventArgs e)
    {
        if (Accounts.GetAccount(e.Username) is not Account account)
        {
            return;
        }

        if (!account.Young)
        {
            return;
        }

        account.RemoveYoungStatus(0);
    }
}
