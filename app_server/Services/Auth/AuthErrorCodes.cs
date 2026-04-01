namespace app_server.Services.Auth;

public static class AuthErrorCodes
{
    public const string AccountBlocked = "ACCOUNT_BLOCKED";
    public const string DefaultRoleMissing = "DEFAULT_ROLE_MISSING";
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string EmailDeliveryFailed = "EMAIL_DELIVERY_FAILED";
    public const string EmailVerificationRequired = "EMAIL_VERIFICATION_REQUIRED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string OtpExpired = "OTP_EXPIRED";
    public const string OtpInvalid = "OTP_INVALID";
    public const string UserNotFound = "USER_NOT_FOUND";
}
