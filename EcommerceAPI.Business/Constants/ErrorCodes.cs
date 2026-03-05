namespace EcommerceAPI.Business.Constants;

public static class ErrorCodes
{
    public const string InvalidToken = "INVALID_TOKEN";
    public const string ExpiredToken = "EXPIRED_TOKEN";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string PasswordMismatch = "PASSWORD_MISMATCH";
    public const string EmailAlreadyInUse = "EMAIL_ALREADY_IN_USE";
    public const string CurrentPasswordInvalid = "CURRENT_PASSWORD_INVALID";
}
