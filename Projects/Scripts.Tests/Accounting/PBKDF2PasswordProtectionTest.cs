using Xunit;
using Server.Accounting;

namespace Server.Tests.Accounting
{
  public class PBKDF2PasswordProtectionTest
  {
    private const string plainPassword = "hello-good-sir";

    [Fact]
    public void TestValidates()
    {
      var passwordProtection = new PBKDF2PasswordProtection();

      string encryptedPassword = passwordProtection.EncryptPassword(plainPassword);

      Assert.True(passwordProtection.ValidatePassword(encryptedPassword, plainPassword));
    }

    [Fact]
    public void TestPasswordDoesNotValidate()
    {
      var passwordProtection = new PBKDF2PasswordProtection();

      string encryptedPassword = passwordProtection.EncryptPassword(plainPassword);

      Assert.False(passwordProtection.ValidatePassword(encryptedPassword, "Not the same password"));
    }
  }
}
