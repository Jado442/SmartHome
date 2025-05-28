using Microsoft.Extensions.Caching.Memory;
using QRCoder;
using System.Net;

public class QrLoginService
{
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<QrLoginService> _logger;

    public QrLoginService(IMemoryCache cache,
                        IHttpContextAccessor httpContextAccessor,
                        ILogger<QrLoginService> logger)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public (string Image, string Token) GenerateLoginQr()
    {
        var token = Guid.NewGuid().ToString();
        var ip = GetClientIp();

        _cache.Set(token, new QrLoginState
        {
            Email = null,
            IsAuthenticated = false,
            Expiry = DateTime.UtcNow.AddMinutes(2),
            IpAddress = ip,
            CreatedAt = DateTime.UtcNow
        }, TimeSpan.FromMinutes(2));

        var qrContent = GenerateQrContent(token);
        var image = GenerateQrImage(qrContent);

        return (image, token);
    }

    private string GenerateQrContent(string token)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        return $"https://d201-92-80-206-34.ngrok-free.app ";
    }

    private string GenerateQrImage(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrBytes = qrCode.GetGraphic(5);
        return $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
    }

    public bool ValidateToken(string token, out QrLoginState state)
    {
        if (_cache.TryGetValue(token, out state))
        {
            var isValid = state.Expiry > DateTime.UtcNow &&
                         state.IpAddress == GetClientIp();

            _logger.LogInformation($"Token validation: {isValid} for {token}");
            return isValid;
        }
        return false;
    }

    public bool CompleteAuthentication(string token, string email)
    {
        if (_cache.TryGetValue(token, out QrLoginState state))
        {
            state.Email = email;
            state.IsAuthenticated = true;
            _cache.Set(token, state, state.Expiry - DateTime.UtcNow);
            _logger.LogInformation($"Authentication completed for {email}");
            return true;
        }
        return false;
    }

    private string GetClientIp()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}


public class QrLoginState
{
    public string Email { get; set; }
    public bool IsAuthenticated { get; set; }
    public DateTime Expiry { get; set; }
    public string IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}