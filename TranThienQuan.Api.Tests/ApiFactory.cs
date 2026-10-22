using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using TranThienQuan.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _tenDb = "test-" + Guid.NewGuid();
    public FakeEmailSender Email { get; } = new();

    private static readonly Dictionary<string, string> MatKhauDemo = new()
    {
        ["admin"] = "Admin@123", ["gv01"] = "Gv@12345",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("TestDbName", _tenDb);
        builder.ConfigureLogging(l => l.ClearProviders());
        builder.ConfigureTestServices(s => s.AddSingleton<IEmailSender>(Email));
    }

    // Đăng nhập tài khoản demo (OTP cố định 123456) và trả client đã gắn Bearer token
    public async Task<HttpClient> DangNhapAsync(string userName, string? matKhau = null)
    {
        matKhau ??= MatKhauDemo.GetValueOrDefault(userName, "Sv@12345");
        var c = CreateClient();
        var dn = await c.PostAsJsonAsync("/api/tranthienquan/auth/dang-nhap", new { userName, password = matKhau });
        dn.EnsureSuccessStatusCode();
        string otp;
        using (var scope = Services.CreateScope())
        {
            var u = (await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByNameAsync(userName))!;
            otp = u.IsDemo ? "123456" : Email.LayOtp(u.Email!);
        }
        var r = await c.PostAsJsonAsync("/api/tranthienquan/auth/xac-nhan-dang-nhap", new { userName, otp });
        r.EnsureSuccessStatusCode();
        var token = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }
}
