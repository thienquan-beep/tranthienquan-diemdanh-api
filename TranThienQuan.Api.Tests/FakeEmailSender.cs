using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Tests;

public class FakeEmailSender : IEmailSender
{
    public ConcurrentDictionary<string, string> MailCuoi { get; } = new();
    public int SoMail;

    public Task GuiAsync(string den, string tieuDe, string noiDungHtml)
    {
        MailCuoi[den] = noiDungHtml;
        Interlocked.Increment(ref SoMail);
        return Task.CompletedTask;
    }

    public string LayOtp(string email) => Regex.Match(MailCuoi[email], @"\b\d{6}\b").Value;
}
