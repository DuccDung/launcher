using app_server.Services.Auth;

namespace app_server.Infrastructure;

public static class AuthCookies
{
    public const string AccessTokenCookieName = "access_token";
    public const string RefreshTokenCookieName = "refresh_token";

    public static void AppendAuthCookies(HttpResponse response, TokenResult tokenResult)
    {
        response.Cookies.Append(
            AccessTokenCookieName,
            tokenResult.AccessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Expires = new DateTimeOffset(tokenResult.AccessTokenExpiresAt),
                Path = "/"
            });

        response.Cookies.Append(
            RefreshTokenCookieName,
            tokenResult.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Expires = new DateTimeOffset(tokenResult.RefreshTokenExpiresAt),
                Path = "/api/auth"
            });
    }

    public static void ClearAuthCookies(HttpResponse response)
    {
        response.Cookies.Delete(
            AccessTokenCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

        response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth"
            });
    }
}
