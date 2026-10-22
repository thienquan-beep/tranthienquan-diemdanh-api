using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Tests;

public class OtpServiceTests
{
    private readonly ApplicationDbContext _db = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase("otp-" + Guid.NewGuid()).Options);
    private readonly FakeEmailSender _mail = new();
    private readonly FakeTimeProvider _gio = new(new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero));
    private readonly User _u = new() { Id = "u1", UserName = "sv99", Email = "sv99@test.vn", HoTen = "SV" };

    private OtpService TaoService() => new(_db, _mail, _gio);

    [Fact]
    public async Task MaDungThiHopLeVaChiDungMotLan()
    {
        var s = TaoService();
        await s.TaoVaGuiAsync(_u, "DangNhap");
        var ma = _mail.LayOtp("sv99@test.vn");

        Assert.Equal(KetQuaOtp.HopLe, await s.KiemTraAsync(_u, "DangNhap", ma));
        Assert.Equal(KetQuaOtp.KhongCo, await s.KiemTraAsync(_u, "DangNhap", ma));
    }

    [Fact]
    public async Task SaiNamLanThiMaBiHuy()
    {
        var s = TaoService();
        await s.TaoVaGuiAsync(_u, "DangNhap");
        var ma = _mail.LayOtp("sv99@test.vn");
        var sai = ma == "000000" ? "111111" : "000000";

        for (var i = 0; i < 5; i++)
            Assert.Equal(KetQuaOtp.Sai, await s.KiemTraAsync(_u, "DangNhap", sai));
        Assert.Equal(KetQuaOtp.KhongCo, await s.KiemTraAsync(_u, "DangNhap", ma));
    }

    [Fact]
    public async Task QuaNamPhutThiHetHan()
    {
        var s = TaoService();
        await s.TaoVaGuiAsync(_u, "DangKy");
        var ma = _mail.LayOtp("sv99@test.vn");
        _gio.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        Assert.Equal(KetQuaOtp.HetHan, await s.KiemTraAsync(_u, "DangKy", ma));
    }

    [Fact]
    public async Task UserDemoDungMaCoDinhVaKhongGuiMail()
    {
        var demo = new User { Id = "d1", UserName = "admin", Email = "admin@x.vn", HoTen = "A", IsDemo = true };
        var s = TaoService();
        await s.TaoVaGuiAsync(demo, "DangNhap");

        Assert.Equal(0, _mail.SoMail);
        Assert.Equal(KetQuaOtp.HopLe, await s.KiemTraAsync(demo, "DangNhap", "123456"));
    }

    [Fact]
    public async Task MaKhacMucDichKhongDungDuoc()
    {
        var s = TaoService();
        await s.TaoVaGuiAsync(_u, "DangKy");
        var ma = _mail.LayOtp("sv99@test.vn");

        Assert.Equal(KetQuaOtp.KhongCo, await s.KiemTraAsync(_u, "DangNhap", ma));
    }
}
