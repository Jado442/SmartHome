
public class UnauthorizedAccessAttempt
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string AttemptedEmail { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime AttemptTime { get; set; } = DateTime.UtcNow;
    public bool NotifiedUser { get; set; }
}