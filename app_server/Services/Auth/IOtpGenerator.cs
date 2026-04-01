namespace app_server.Services.Auth;

public interface IOtpGenerator
{
    string GenerateOtp(int length = 6);
}
