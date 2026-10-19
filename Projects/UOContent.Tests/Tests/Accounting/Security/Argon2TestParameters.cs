using System;
using Server.Accounting.Security;

namespace Server.Tests.Accounting.Security;

/// <summary>
/// The PHC parameter prefix the configured <see cref="Argon2PasswordProtection"/> writes, read from
/// a real hash so a shard that tunes the Argon2 cost does not break assertions about the format.
/// </summary>
internal static class Argon2TestParameters
{
    /// <summary>For example <c>$argon2id$v=19$m=16384,t=1,p=1$</c>, ending at the salt.</summary>
    public static readonly string CurrentPrefix = ReadPrefix();

    public static string Type => CurrentPrefix.Split('$')[1];

    public static string Version => CurrentPrefix.Split('$')[2];

    public static int Memory => ReadCost('m');

    public static int Time => ReadCost('t');

    public static int Parallelism => ReadCost('p');

    public static string BuildPrefix(string type, int memory, int time, int parallelism) =>
        $"${type}${Version}$m={memory},t={time},p={parallelism}$";

    private static string ReadPrefix()
    {
        var hash = Argon2PasswordProtection.Instance.EncryptPassword("prefix-probe");

        // $type$version$params$salt$digest: the prefix runs through the fourth '$'.
        var end = 0;
        for (var i = 0; i < 4; i++)
        {
            end = hash.IndexOf('$', end) + 1;
        }

        return hash[..end];
    }

    private static int ReadCost(char key)
    {
        var costs = CurrentPrefix.Split('$')[3].Split(',');
        for (var i = 0; i < costs.Length; i++)
        {
            if (costs[i][0] == key)
            {
                return int.Parse(costs[i].AsSpan(2));
            }
        }

        return -1;
    }
}
