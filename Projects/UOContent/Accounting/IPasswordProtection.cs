namespace Server.Accounting
{
    public interface IPasswordProtection
    {
        string EncryptPassword(string plainPassword);
        bool ValidatePassword(string encryptedPassword, string plainPassword);
    }
}
