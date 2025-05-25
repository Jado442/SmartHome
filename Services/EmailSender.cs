using System.Net;
using System.Net.Mail;

public class EmailSender : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var client = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            Credentials = new NetworkCredential("jawad.w.arabi@gmail.com", "jwgastcjfxclzmux"),
            EnableSsl = true,
        };

        var mail = new MailMessage("jawad.w.arabi@gmail.com", email, subject, htmlMessage)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(mail);
    }
}
