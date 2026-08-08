using System;
using Server.Accounting;
using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting.Security;

public class PasswordProtectionTest
{
    private const string plainPassword = "hello-good-sir";

    [Theory]
    [InlineData(typeof(Argon2PasswordProtection), null)]
    [InlineData(typeof(PBKDF2PasswordProtection), null)]
    [InlineData(typeof(HashAlgorithmPasswordProtection), "MD5")]
    [InlineData(typeof(HashAlgorithmPasswordProtection), "SHA1")]
    [InlineData(typeof(HashAlgorithmPasswordProtection), "SHA2")]
    public void TestValidates(Type protectionType, string algorithmType)
    {
        IPasswordProtection passwordProtection;
        if (protectionType == typeof(HashAlgorithmPasswordProtection))
        {
            passwordProtection = algorithmType switch
            {
                "SHA1" => HashAlgorithmPasswordProtection.SHA1Instance,
                "SHA2" => HashAlgorithmPasswordProtection.SHA2Instance,
                _      => HashAlgorithmPasswordProtection.MD5Instance,
            };
        }
        else
        {
            passwordProtection = Activator.CreateInstance(protectionType) as IPasswordProtection;
        }

        if (passwordProtection == null)
        {
            Assert.Fail($"{protectionType.Name} is not an IPasswordProtection.");
        }

        var encryptedPassword = passwordProtection.EncryptPassword(plainPassword);

        Assert.True(passwordProtection.ValidatePassword(encryptedPassword, plainPassword));
    }

    [Theory]
    [InlineData(typeof(Argon2PasswordProtection), null)]
    [InlineData(typeof(PBKDF2PasswordProtection), null)]
    [InlineData(typeof(HashAlgorithmPasswordProtection), "MD5")]
    [InlineData(typeof(HashAlgorithmPasswordProtection), "SHA1")]
    [InlineData(typeof(HashAlgorithmPasswordProtection), "SHA2")]
    public void TestPasswordDoesNotValidate(Type protectionType, string algorithmType)
    {
        IPasswordProtection passwordProtection;
        if (protectionType == typeof(HashAlgorithmPasswordProtection))
        {
            passwordProtection = algorithmType switch
            {
                "SHA1" => HashAlgorithmPasswordProtection.SHA1Instance,
                "SHA2" => HashAlgorithmPasswordProtection.SHA2Instance,
                _      => HashAlgorithmPasswordProtection.MD5Instance,
            };
        }
        else
        {
            passwordProtection = Activator.CreateInstance(protectionType) as IPasswordProtection;
        }

        if (passwordProtection == null)
        {
            Assert.Fail($"{protectionType.Name} is not an IPasswordProtection.");
        }

        var encryptedPassword = passwordProtection.EncryptPassword(plainPassword);

        Assert.False(passwordProtection.ValidatePassword(encryptedPassword, "Not the same password"));
    }

    // Produced by ModernUO's shipping default before this change: Argon2i, m=8192, t=3, p=1.
    // Pinned as a literal so it cannot drift with the configured defaults. Password: "hunter2".
    private const string LegacyArgon2iHash =
        "$argon2i$v=19$m=8192,t=3,p=1$LD1XJz7P3wQmIJ+Tu6ScgA$NO5hBABsHQ172C5nDO2X4gWnB4jDef3x6WhLdVE2LFw";

    [Fact]
    public void Argon2_ValidatesLegacyArgon2iHash()
    {
        Assert.True(Argon2PasswordProtection.Instance.ValidatePassword(LegacyArgon2iHash, "hunter2"));
        Assert.False(Argon2PasswordProtection.Instance.ValidatePassword(LegacyArgon2iHash, "wrong"));
    }

    [Theory]
    // type, memory, time, parallelism -> expected NeedsRehash
    [InlineData("argon2id", 16384, 1, 1, false)] // current defaults
    [InlineData("argon2i", 8192, 3, 1, true)]    // the old shipping default
    [InlineData("argon2id", 8192, 1, 1, true)]   // right type, stale memory
    [InlineData("argon2id", 16384, 3, 1, true)]  // right type, stale iterations
    [InlineData("argon2id", 16384, 1, 2, true)]  // right type, stale parallelism
    [InlineData("argon2i", 16384, 1, 1, true)]   // right cost, stale type
    public void Argon2_NeedsRehash_ComparesTypeAndCost(
        string type, int memory, int time, int parallelism, bool expected
    )
    {
        var hash = $"${type}$v=19$m={memory},t={time},p={parallelism}$" +
                   "LD1XJz7P3wQmIJ+Tu6ScgA$NO5hBABsHQ172C5nDO2X4gWnB4jDef3x6WhLdVE2LFw";

        Assert.Equal(expected, Argon2PasswordProtection.Instance.NeedsRehash(hash));
    }

    // The digest and salt lengths are not in the parameter list -- they are the decoded sizes of the
    // two base64 segments -- so they cannot be varied through the theory template above. Both hashes
    // here carry the current type and cost; only a segment length differs from the library defaults
    // (32-byte digest, 16-byte salt). The "current defaults" row of the theory above is the negative
    // control: it uses those default lengths and must stay false.
    [Theory]
    // 16-byte digest: 22 base64 chars instead of the 43 a 32-byte digest encodes to.
    [InlineData("$argon2id$v=19$m=16384,t=1,p=1$LD1XJz7P3wQmIJ+Tu6ScgA$NO5hBABsHQ172C5nDO2X4g")]
    // 8-byte salt: 11 base64 chars instead of the 22 a 16-byte salt encodes to.
    [InlineData("$argon2id$v=19$m=16384,t=1,p=1$LD1XJz7P3wQ$NO5hBABsHQ172C5nDO2X4gWnB4jDef3x6WhLdVE2LFw")]
    public void Argon2_NeedsRehash_ComparesSaltAndDigestLengths(string hash)
    {
        Assert.True(Argon2PasswordProtection.Instance.NeedsRehash(hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    public void Argon2_NeedsRehash_IsTrueForUnparseableHashes(string hash)
    {
        Assert.True(Argon2PasswordProtection.Instance.NeedsRehash(hash));
    }

    [Fact]
    public void NonArgon2Protections_NeverNeedRehash()
    {
        Assert.False(PBKDF2PasswordProtection.Instance.NeedsRehash("anything"));
        Assert.False(HashAlgorithmPasswordProtection.SHA2Instance.NeedsRehash("anything"));
        Assert.False(HashAlgorithmPasswordProtection.SHA1Instance.NeedsRehash("anything"));
        Assert.False(HashAlgorithmPasswordProtection.MD5Instance.NeedsRehash("anything"));
    }
}
