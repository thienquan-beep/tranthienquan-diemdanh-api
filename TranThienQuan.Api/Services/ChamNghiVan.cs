using TranThienQuan.Api.Dtos;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Services;

// Chấm mức nghi vấn điểm danh hộ cho một lượt quét QR
public static class ChamNghiVan
{
    public static (int diem, List<string> lyDo) Cham(DiemDanh d, BuoiHoc b, IReadOnlyCollection<DiemDanh> tatCa)
    {
        var lyDo = new List<string>();
        var diem = 0;
        if (d.CachThuc != "QR" || !TrangThaiDiemDanh.LaCoMat(d.TrangThai)) return (0, lyDo);

        if (d.KhoangCachMet is double k)
        {
            if (k > b.BanKinh)
            {
                diem += 3;
                lyDo.Add($"Ngoài bán kính lớp ({k:0} m)");
            }
            else if (k > 0.8 * b.BanKinh)
            {
                diem += 1;
                lyDo.Add($"Sát mép bán kính ({k:0} m)");
            }
        }

        if (!string.IsNullOrEmpty(d.MaThietBi))
        {
            var cungMay = tatCa.Where(x => x.SinhVienId != d.SinhVienId && x.CachThuc == "QR" && x.MaThietBi == d.MaThietBi).ToList();
            if (cungMay.Count > 0)
            {
                diem += 3;
                lyDo.Add($"Cùng thiết bị với {string.Join(", ", cungMay.Select(x => x.SinhVien?.HoTen))}");
            }
        }

        if (b.DongLuc is DateTime dong && d.ThoiDiem >= dong.AddSeconds(-60))
        {
            diem += 1;
            lyDo.Add("Quét sát lúc đóng buổi");
        }

        return (diem, lyDo);
    }
}
