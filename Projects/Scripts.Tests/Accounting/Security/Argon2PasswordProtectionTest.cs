using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting.Security
{
  public class Argon2PasswordProtectionTest
  {
    private const string plainPassword = "hello-good-sir";

    [Fact]
    public void TestValidates()
    {
      var passwordProtection = new Argon2PasswordProtection();

      string encryptedPassword = passwordProtection.EncryptPassword(plainPassword);

      Assert.True(passwordProtection.ValidatePassword(encryptedPassword, plainPassword));
    }

    [Fact]
    public void TestPasswordDoesNotValidate()
    {
      var passwordProtection = new Argon2PasswordProtection();

      string encryptedPassword = passwordProtection.EncryptPassword(plainPassword);

      Assert.False(passwordProtection.ValidatePassword(encryptedPassword, "Not the same password"));
    }
  }
}
