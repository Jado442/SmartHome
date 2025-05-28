
using SmartHome.Data;

public class UnauthorizedAccessAlertService
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UnauthorizedAccessAlertService(
        AppDbContext context,
        IEmailSender emailSender,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _emailSender = emailSender;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RecordFailedAttempt(string attemptedEmail, string userId = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

        var attempt = new UnauthorizedAccessAttempt
        {
            UserId = userId,
            AttemptedEmail = attemptedEmail,
            IpAddress = ip,
            UserAgent = userAgent
        };

        _context.UnauthorizedAccessAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        // Alert user if they have an account
        if (userId != null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                await SendAlertEmail(user.Email, ip, attempt.AttemptTime);
                attempt.NotifiedUser = true;
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task SendAlertEmail(string userEmail, string ip, DateTime attemptTime)
    {
        var subject = "Suspicious Login Attempt";
        var message = $@"
            <h3>Unauthorized Access Attempt</h3>
            <p>We detected a login attempt for your account:</p>
            <ul>
                <li>Time: {attemptTime.ToString("g")}</li>
                <li>IP Address: {ip}</li>
            </ul>
            <p>If this wasn't you, please change your password immediately.</p>";

        await _emailSender.SendEmailAsync(userEmail, subject, message);
    }
}