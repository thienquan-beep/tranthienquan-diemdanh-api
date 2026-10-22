using Google.Apis.Auth;

namespace TranThienQuan.Api.Services;

public interface IGoogleTokenValidator
{
    // Trả về (email, họ tên) nếu idToken hợp lệ, ngược lại null
    Task<(string email, string hoTen)?> KiemTraAsync(string idToken);
}

// OAuth2.0: xác thực idToken do Google cấp cho app (audience = Web Client ID)
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleTokenValidator> _log;

    public GoogleTokenValidator(IConfiguration config, ILogger<GoogleTokenValidator> log)
    {
        _config = config;
        _log = log;
    }

    public async Task<(string email, string hoTen)?> KiemTraAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _config["Google:WebClientId"] },
            });
            if (!payload.EmailVerified) return null;
            return (payload.Email, payload.Name ?? payload.Email);
        }
        catch (InvalidJwtException e)
        {
            _log.LogWarning("idToken Google không hợp lệ: {Loi}", e.Message);
            return null;
        }
    }
}
