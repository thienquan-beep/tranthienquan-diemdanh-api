using TranThienQuan.Api.Models;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Tests;

public class ChamNghiVanTests
{
    private static readonly DateTime MoLuc = new(2026, 10, 22, 7, 0, 0, DateTimeKind.Utc);
    private readonly BuoiHoc _buoi = new() { Id = 1, BanKinh = 100, MoLuc = MoLuc };

    private static DiemDanh Qr(string svId, double khoangCach, string thietBi, int phutSauMo = 5, string hoTen = "") => new()
    {
        SinhVienId = svId, KhoangCachMet = khoangCach, MaThietBi = thietBi, CachThuc = "QR", TrangThai = "CoMat",
        ThoiDiem = MoLuc.AddMinutes(phutSauMo), SinhVien = new User { Id = svId, HoTen = hoTen },
    };

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
