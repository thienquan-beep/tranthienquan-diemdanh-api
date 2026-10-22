using Microsoft.Extensions.Time.Testing;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Tests;

public class QrTokenServiceTests
{
    private readonly FakeTimeProvider _gio = new(new DateTimeOffset(2026, 10, 22, 8, 0, 5, TimeSpan.Zero));
    private readonly BuoiHoc _buoi = new() { Id = 7, KhoaBiMat = "0123456789abcdef0123456789abcdef" };

    private static string Token(string payload)
    {
        Assert.True(QrTokenService.TachPayload(payload, out _, out var token));
        return token;
    }

    [Fact]
    public void CungKhungThoiGianThiCungPayload()
    {
        var s = new QrTokenService(_gio);
        var a = s.TaoPayload(_buoi);
        _gio.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(a, s.TaoPayload(_buoi));
        Assert.StartsWith("TTQ|7|", a);
        Assert.Equal(10, Token(a).Length);
    }

    [Fact]
    public void CungKhung90GiayThiCungPayload()
    {
        var s = new QrTokenService(_gio); // 08:00:05, khung 08:00:00–08:01:30
        var a = s.TaoPayload(_buoi);
        _gio.Advance(TimeSpan.FromSeconds(80));
        Assert.Equal(a, s.TaoPayload(_buoi));
    }

    [Fact]
    public void TokenKhungTruocVanHopLe()
    {
        // Sinh viên cần thời gian đo tiếng ồn + lấy vị trí sau khi quét
        var s = new QrTokenService(_gio);
        var token = Token(s.TaoPayload(_buoi));
        _gio.Advance(TimeSpan.FromSeconds(90));
        Assert.True(s.KiemTra(_buoi, token));
    }

    [Fact]
    public void TokenHaiKhungTruocHetHan()
    {
        var s = new QrTokenService(_gio);
        var token = Token(s.TaoPayload(_buoi));
        _gio.Advance(TimeSpan.FromSeconds(180));
        Assert.False(s.KiemTra(_buoi, token));
    }

    [Fact]
    public void TokenCuaBuoiKhacKhongHopLe()
    {
        var s = new QrTokenService(_gio);
        var khac = new BuoiHoc { Id = 8, KhoaBiMat = "fedcba9876543210fedcba9876543210" };
        Assert.False(s.KiemTra(_buoi, Token(s.TaoPayload(khac))));
    }

    [Fact]
    public void ConLaiGiayDemNguoc()
    {
        var s = new QrTokenService(_gio); // 08:00:05 → còn 85 giây
        Assert.Equal(85, s.ConLaiGiay());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("TTQ|x|abc")]
    [InlineData("XYZ|7|abcdef0123")]
    public void PayloadSaiDinhDangThiTachThatBai(string payload) =>
        Assert.False(QrTokenService.TachPayload(payload, out _, out _));
}
