using System.Security.Cryptography;

namespace app_server.Services.Auth;

public class OtpGenerator : IOtpGenerator
{
    public string GenerateOtp(int length = 6)
    {
        if (length <= 0)
        {
            length = 6;
        }

        var buffer = new char[length];
        for (var index = 0; index < length; index++)
        {
            buffer[index] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(buffer);
    }
}
