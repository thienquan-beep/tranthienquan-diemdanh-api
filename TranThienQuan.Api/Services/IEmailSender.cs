using System.Net;
using System.Net.Mail;

namespace TranThienQuan.Api.Services;

public interface IEmailSender
{
    Task GuiAsync(string den, string tieuDe, string noiDungHtml);
}

// Gửi mail qua SMTP (Gmail + mật khẩu ứng dụng)
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config) => _config = config;

    public async Task GuiAsync(string den, string tieuDe, string noiDungHtml)
    {
        using var smtp = new SmtpClient(_config["Email:SmtpHost"], int.Parse(_config["Email:SmtpPort"] ?? "587"))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_config["Email:User"], _config["Email:Password"]),
        };
        var from = _config["Email:From"];
        if (string.IsNullOrEmpty(from)) from = _config["Email:User"];
        using var mail = new MailMessage(from!, den, tieuDe, noiDungHtml) { IsBodyHtml = true };
        await smtp.SendMailAsync(mail);
    }
}

// Chưa cấu hình SMTP: ghi nội dung mail (kèm OTP) ra log
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _log;

    public LogEmailSender(ILogger<LogEmailSender> log) => _log = log;

    public Task GuiAsync(string den, string tieuDe, string noiDungHtml)
    {
        _log.LogWarning("[EMAIL chưa cấu hình SMTP] Tới: {Den} | {TieuDe} | {NoiDung}", den, tieuDe, noiDungHtml);
        return Task.CompletedTask;
    }
}
