namespace ProductManagementSystem.Api.Helpers;

public static class RedisCacheHelperClass
{
    public static string UserProfileFailedLoginAttemptKey = "user-profile-failed-login-attempt";
    public static string UserProfileTokenKey = "user-profile-token";
    public static string LockedOutUsersKey = "locked-out-users";
    public static string ProductCategoryKey = "product-category";
    public static string ProductCategoryItemKey = "product-category";
    public static string PasswordResetTokensKey = "password-reset-user-tokens";
    public static string GetCacheKey(string prefix, params object[] parameters)
    {
        return $"{prefix}:{string.Join(":", parameters)}";
    }

    public static string GetUserProfileFailedLoginAttemptCacheKey(string userId)
    {
        return GetCacheKey(UserProfileFailedLoginAttemptKey, userId);
    }

    public static string GetUserProfileTokenCacheKey(string userId, string emailAddress)
    {
        return GetCacheKey(UserProfileTokenKey, userId, emailAddress);
    }

    public static string GetProductCategoryItemKey(int Id)
    {
        return GetCacheKey(ProductCategoryItemKey, Id);
    }
}
