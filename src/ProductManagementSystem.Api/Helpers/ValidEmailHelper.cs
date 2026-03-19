using System.Text.RegularExpressions;

namespace ProductManagementSystem.Api.Helpers;

public static class ValidEmailHelper
{
    public static bool IsValidEmailUsingRegex(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // This pattern checks for basic email structure: localpart@domain.tld
        // It's a simple, pragmatic regex for general use, not fully RFC 5322 compliant.
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        try
        {
            // Use a timeout to prevent potential Denial-of-Service attacks from malicious input
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
