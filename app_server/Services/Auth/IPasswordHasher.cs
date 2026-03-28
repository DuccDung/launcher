namespace app_server.Services.Auth;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string storedHash);
}
