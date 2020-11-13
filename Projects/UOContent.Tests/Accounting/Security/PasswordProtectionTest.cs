using System;
using Server.Accounting;
using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting.Security
{
    public class PasswordProtectionTest
    {
        private const string plainPassword = "hello-good-sir";

        [Theory, InlineData(typeof(Argon2PasswordProtection)), InlineData(typeof(PBKDF2PasswordProtection)),
         InlineData(typeof(SHA2PasswordProtection)), InlineData(typeof(SHA1PasswordProtection)),
         InlineData(typeof(MD5PasswordProtection))]
        public void TestValidates(Type protectionType)
        {
            var passwordProtection = Activator.CreateInstance(protectionType) as IPasswordProtection;
            if (passwordProtection == null)
            {
                Assert.False(true, $"{protectionType.Name} is not an IPasswordProtection.");
            }

            var encryptedPassword = passwordProtection.EncryptPassword(plainPassword);

            Assert.True(passwordProtection.ValidatePassword(encryptedPassword, plainPassword));
        }

        [Theory, InlineData(typeof(Argon2PasswordProtection)), InlineData(typeof(PBKDF2PasswordProtection)),
         InlineData(typeof(SHA2PasswordProtection)), InlineData(typeof(SHA1PasswordProtection)),
         InlineData(typeof(MD5PasswordProtection))]
        public void TestPasswordDoesNotValidate(Type protectionType)
        {
            var passwordProtection = Activator.CreateInstance(protectionType) as IPasswordProtection;
            if (passwordProtection == null)
            {
                Assert.False(true, $"{protectionType.Name} is not an IPasswordProtection.");
            }

            var encryptedPassword = passwordProtection.EncryptPassword(plainPassword);

            Assert.False(passwordProtection.ValidatePassword(encryptedPassword, "Not the same password"));
        }
    }
}
