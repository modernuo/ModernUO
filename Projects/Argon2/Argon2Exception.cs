namespace System.Security.Cryptography
{
  /// <summary>
  /// An exception class to wrap the errors returned by Daniel Dinu and Dmitry Khovratovich's Argon2 library.
  ///
  /// Except through very unusual conditions, the only exceptions which could be thrown from PasswordHasher
  /// are Argon2Exception, ArgumentNullException, DllNotFoundException (if libargon2.dll is not found)
  /// </summary>
  public class Argon2Exception : Exception
  {
    /// <summary>
    /// Construct an Argon2Exception with the specified Argon2 error code
    /// <param name="action">Which method the Argon2Exception originated from</param>
    /// <param name="error">The error returned from the Argon2 library</param>
    /// </summary>
    public Argon2Exception(string action, Argon2Error error) : base(string.Format("Error during Argon2 {0}: ({1}) {2}", action, (int)error, error)) {}
  }
}
