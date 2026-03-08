using Microsoft.AspNetCore.Identity;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;
using System.Security.Cryptography;

namespace ProductManagementSystem.Api.Utilities;

public class PasswordHasher : IPasswordHasher
{
    private const int iterations = 1000000;
    private const int hashSize = 32;
    private const int saltSize = 16;

    private readonly HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        try
        {
            byte[] salt = RandomNumberGenerator.GetBytes(saltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, hashSize);

            return string.Concat(Convert.ToHexString(hash), "-", Convert.ToHexString(salt));
        }
        catch (Exception ex)
        {
            Log.ForContext("MethodName", "HashPassword").ForContext("ClassName", "PasswordHasher").Error(ex, "An Error occurred hashing password.");
            throw;
        }

    }

    public bool ValidatePassword(string hashedPassword, string passwordToValidate)
    {
        try
        {
            string[] passwordParts = hashedPassword.Split("-");
            string hash = passwordParts[0];
            string salt = passwordParts[1];

            byte[] hashByte = Convert.FromHexString(hash);
            byte[] saltByte = Convert.FromHexString(salt);

            byte[] hashPassword = Rfc2898DeriveBytes.Pbkdf2(passwordToValidate, saltByte, iterations, hashAlgorithm, hashSize);

            return CryptographicOperations.FixedTimeEquals(hashPassword, hashByte);
        }
        catch (Exception ex)
        {
            Log.ForContext("MethodName", "ValidatePassword").ForContext("ClassName", "PasswordHasher").Error(ex, "An Error occurred validating hashing password.");
            return false;
        }
    }
}


//public class PasswordHaher2 : IPasswordHasher<User>
//{
//    public string HashPassword(User user, string password)
//    {
//        throw new NotImplementedException();
//    }

//    public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
//    {
//        throw new NotImplementedException();
//    }
//}