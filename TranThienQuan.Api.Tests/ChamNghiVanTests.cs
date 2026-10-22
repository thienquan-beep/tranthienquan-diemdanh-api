using TranThienQuan.Api.Models;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Tests;

public class ChamNghiVanTests
{
    private static readonly DateTime MoLuc = new(2026, 10, 22, 7, 0, 0, DateTimeKind.Utc);
    private readonly BuoiHoc _buoi = new() { Id = 1, BanKinh = 100, MoLuc = MoLuc };

    private static DiemDanh Qr(string svId, double khoangCach, string thietBi, int phutSauMo = 5, string hoTen = "",
        double? doOn = null) => new()
    {
        SinhVienId = svId, KhoangCachMet = khoangCach, MaThietBi = thietBi, CachThuc = "QR", TrangThai = "CoMat",
        ThoiDiem = MoLuc.AddMinutes(phutSauMo), SinhVien = new User { Id = svId, HoTen = hoTen }, DoOn = doOn,
    };

    [Fact]
    public void DoOnLechXaLopCongHai()
    {
        // Lớp ồn ~60 dB, một bạn quét ở nơi yên tĩnh 35 dB
        var a = Qr("a", 10, "m1", doOn: 35);
        var ds = new[] { a, Qr("b", 10, "m2", doOn: 58), Qr("c", 10, "m3", doOn: 60), Qr("d", 10, "m4", doOn: 63) };
        var (diem, lyDo) = ChamNghiVan.Cham(a, _buoi, ds);
        Assert.Equal(2, diem);
        Assert.Equal("Độ ồn lệch 25 dB so với lớp (35 dB)", Assert.Single(lyDo));
    }

    [Fact]
    public void DoOnGanLopKhongNghiVan()
    {
        var a = Qr("a", 10, "m1", doOn: 50);
        var ds = new[] { a, Qr("b", 10, "m2", doOn: 58), Qr("c", 10, "m3", doOn: 60) };
        Assert.Equal(0, ChamNghiVan.Cham(a, _buoi, ds).diem);
    }

    [Fact]
    public void ItHonHaiLuotKhacThiKhongSoDoOn()
    {
        var a = Qr("a", 10, "m1", doOn: 30);
        var ds = new[] { a, Qr("b", 10, "m2", doOn: 70) };
        Assert.Equal(0, ChamNghiVan.Cham(a, _buoi, ds).diem);
    }

    [Fact]
    public void TrongBanKinhKhongNghiVan()
    {
        var d = Qr("a", 30, "m1");
        var (diem, lyDo) = ChamNghiVan.Cham(d, _buoi, new[] { d });
        Assert.Equal(0, diem);
        Assert.Empty(lyDo);
    }

    [Fact]
    public void NgoaiBanKinhCongBa()
    {
        var d = Qr("a", 250, "m1");
        var (diem, lyDo) = ChamNghiVan.Cham(d, _buoi, new[] { d });
        Assert.Equal(3, diem);
        Assert.Equal("Ngoài bán kính lớp (250 m)", Assert.Single(lyDo));
    }

    [Fact]
    public void SatMepBanKinhCongMot()
    {
        var d = Qr("a", 90, "m1");
        var (diem, lyDo) = ChamNghiVan.Cham(d, _buoi, new[] { d });
        Assert.Equal(1, diem);
        Assert.Equal("Sát mép bán kính (90 m)", Assert.Single(lyDo));
    }

    [Fact]
    public void CungThietBiCongBa()
    {
        var a = Qr("a", 10, "m1", hoTen: "An");
        var b = Qr("b", 10, "m1", hoTen: "Bình");
        var (diem, lyDo) = ChamNghiVan.Cham(a, _buoi, new[] { a, b });
        Assert.Equal(3, diem);
        Assert.Equal("Cùng thiết bị với Bình", Assert.Single(lyDo));
    }

    [Fact]
    public void QuetSatLucDongCongMot()
    {
        _buoi.DongLuc = MoLuc.AddMinutes(20);
        var d = Qr("a", 10, "m1", phutSauMo: 20);
        var (diem, lyDo) = ChamNghiVan.Cham(d, _buoi, new[] { d });
        Assert.Equal(1, diem);
        Assert.Equal("Quét sát lúc đóng buổi", Assert.Single(lyDo));
    }

    [Fact]
    public void CongDonNhieuLyDo()
    {
        var a = Qr("a", 400, "m1", hoTen: "An");
        var b = Qr("b", 10, "m1", hoTen: "Bình");
        var (diem, lyDo) = ChamNghiVan.Cham(a, _buoi, new[] { a, b });
        Assert.Equal(6, diem);
        Assert.Equal(2, lyDo.Count);
    }

    [Fact]
    public void DiemDanhTayKhongChamNghiVan()
    {
        var d = Qr("a", 400, "m1");
        d.CachThuc = "Tay";
        Assert.Equal(0, ChamNghiVan.Cham(d, _buoi, new[] { d }).diem);
    }
}
