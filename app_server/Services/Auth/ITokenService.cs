using app_server.Models;

namespace app_server.Services.Auth;

public interface ITokenService
{
    TokenResult GenerateTokens(User user);

    string ComputeRefreshTokenHash(string refreshToken);
}
