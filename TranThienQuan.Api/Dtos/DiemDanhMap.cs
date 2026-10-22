using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Dtos;

public static class DiemDanhMap
{
    public static DiemDanhDto ToDto(DiemDanh d) => new(
        d.Id, d.BuoiHocId, d.SinhVienId, d.SinhVien?.HoTen ?? "", d.SinhVien?.Mssv, d.ThoiDiem,
        d.Lat, d.Lng, d.KhoangCachMet is double k ? Math.Round(k, 1) : null, d.CachThuc, d.TrangThai,
        d.AnhSelfie, d.NghiVan, d.MaThietBi,
        d.BuoiHoc?.LopHocPhan?.MonHoc?.TenMon, d.BuoiHoc?.MoLuc);

    public static DiemDanhDto ChuaDiemDanh(int buoiId, User sv) => new(
        null, buoiId, sv.Id, sv.HoTen, sv.Mssv, null, null, null, null, null, null, null, false, null, null, null);
}
