using System;
using Server.Accounting;

namespace Server.Engines.Reports
{
  public abstract class BaseInfo : IComparable
  {
    private string m_Display;

    public BaseInfo(string account)
    {
      Account = account;
      Pages = new PageInfoCollection();
    }

    public static TimeSpan SortRange{ get; set; }

    public string Account{ get; set; }

    public PageInfoCollection Pages{ get; set; }

    public string Display
    {
      get
      {
        if (m_Display != null)
          return m_Display;

        if (Account != null)
        {
          IAccount acct = Accounts.GetAccount(Account);

          if (acct != null)
          {
            Mobile mob = null;

            for (int i = 0; i < acct.Length; ++i)
            {
              Mobile check = acct[i];

              if (check != null && (mob == null || check.AccessLevel > mob.AccessLevel))
                mob = check;
            }

            if (mob?.Name != null && mob.Name.Length > 0)
              return m_Display = mob.Name;
          }
        }

        return m_Display = Account;
      }
    }

    public int CompareTo(object obj)
    {
      BaseInfo cmp = obj as BaseInfo;

      int v = cmp.GetPageCount(cmp is StaffInfo ? PageResolution.Handled : PageResolution.None,
                DateTime.UtcNow - SortRange, DateTime.UtcNow)
              - GetPageCount(this is StaffInfo ? PageResolution.Handled : PageResolution.None,
                DateTime.UtcNow - SortRange, DateTime.UtcNow);

      if (v == 0)
        v = string.Compare(Display, cmp.Display);

      return v;
    }

    public int GetPageCount(PageResolution res, DateTime min, DateTime max)
    {
      return StaffHistory.GetPageCount(Pages, res, min, max);
    }

    public void Register(PageInfo page)
    {
      Pages.Add(page);
    }

    public void Unregister(PageInfo page)
    {
      Pages.Remove(page);
    }
  }

  public class StaffInfo : BaseInfo
  {
    public StaffInfo(string account) : base(account)
    {
    }
  }

  public class UserInfo : BaseInfo
  {
    public UserInfo(string account) : base(account)
    {
    }
  }
}