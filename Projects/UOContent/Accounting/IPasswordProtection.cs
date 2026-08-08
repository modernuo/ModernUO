namespace Server.Accounting
{
    public interface IPasswordProtection
    {
        string EncryptPassword(string plainPassword);
        bool ValidatePassword(string encryptedPassword, string plainPassword);

        /// <summary>
        /// True when <paramref name="encryptedPassword"/> was produced with parameters that differ
        /// from the ones this protection currently uses, so a successful login should rewrite it.
        /// Algorithms whose cost is not embedded in the stored value never need this.
        /// </summary>
        bool NeedsRehash(string encryptedPassword) => false;
    }
}
