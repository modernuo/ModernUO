using System;
using Server.Accounting;
using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting.Security
{
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
    }
}
