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

    /// <summary>
    /// Literal digests of <see cref="plainPassword"/>, so the stored format cannot drift. These are
    /// compared as strings against what is already in every account database -- a casing or encoding
    /// change would lock out every SHA and MD5 account on the shard at once.
    /// </summary>
    [Theory]
    [InlineData("MD5", "52284053181040AC90DBDE74A0E7FF5E")]
    [InlineData("SHA1", "9AC635509803AAE2D8312BA1879289259A50C5F0")]
    [InlineData(
        "SHA2",
        "5A727BFF8F8E08A24BDF6B0CD5065F30A1F8E0060B857BB8AFD6955BE0ACBC489DA63F19B8F4CF08D73DE4069CF4B" +
        "29D94B353F31513B2FB2D9382EFE15AE975"
    )]
    public void HashAlgorithm_StoredFormatIsStable(string algorithmType, string expected)
    {
        var protection = algorithmType switch
        {
            "SHA1" => HashAlgorithmPasswordProtection.SHA1Instance,
            "SHA2" => HashAlgorithmPasswordProtection.SHA2Instance,
            _      => HashAlgorithmPasswordProtection.MD5Instance,
        };

        Assert.Equal(expected, protection.EncryptPassword(plainPassword));
        Assert.True(protection.ValidatePassword(expected, plainPassword));
    }

    // The shipping default before this change, as a literal so it cannot drift with the configured
    // defaults. Password: "hunter2".
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

    // Digest and salt lengths are decoded base64 sizes rather than parameter-list entries, so they
    // need their own literals. Current type and cost throughout; only a length differs.
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
