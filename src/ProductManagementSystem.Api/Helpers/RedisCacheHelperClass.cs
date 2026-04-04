namespace ProductManagementSystem.Api.Helpers;

public static class RedisCacheHelperClass
{
    public static string UserProfileFailedLoginAttemptKey = "user-profile-failed-login-attempt";
    public static string UserProfileTokenKey = "user-profile-token";
    public static string LockedOutUsersKey = "locked-out-users";
    public static string ProductCategoryKey = "product-category";
    public static string ProductCategoryItemKey = "product-category";
    public static string PasswordResetTokensKey = "password-reset-user-tokens";
    public static string RolesKey = "roles";
    public static string RoleBaseKey = "role";
    public static string FeedbackKey = "feedbacks";
    public static string ReviewKey = "review";
    public static string ProductsKey = "products";
    public static string ProductBaseKey = "product";
    public static string OrdersKey = "orders";
    public static string OrderBaseKey = "order";
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

    public static string GetRoleCacheKey(int Id)
    {
        return GetCacheKey(RoleBaseKey, Id);
    }

    public static string GetProductCacheKey(int Id)
    {
        return GetCacheKey(ProductBaseKey, Id);
    }

    public static string GetOrderCacheKey(int Id)
    {
        return GetCacheKey(OrderBaseKey, Id);
    }

    public static string GetOrderDetailKey(int Id)
    {
        return GetCacheKey("order-detail", Id);
    }
}
