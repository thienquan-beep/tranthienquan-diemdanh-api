using TranThienQuan.Api.Dtos;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Services;

// Chấm mức nghi vấn điểm danh hộ cho một lượt quét QR
public static class ChamNghiVan
{
    private const double DoOnLechToiDa = 15;

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

        // Độ ồn lệch xa các bạn cùng buổi → có thể không ngồi trong lớp (cần ≥ 2 lượt khác để so)
        if (d.DoOn is double on)
        {
            var khac = tatCa.Where(x => x.SinhVienId != d.SinhVienId && x.CachThuc == "QR" && x.DoOn != null)
                .Select(x => x.DoOn!.Value).Order().ToList();
            if (khac.Count >= 2)
            {
                var giua = khac.Count % 2 == 1 ? khac[khac.Count / 2] : (khac[khac.Count / 2 - 1] + khac[khac.Count / 2]) / 2;
                if (Math.Abs(on - giua) >= DoOnLechToiDa)
                {
                    diem += 2;
                    lyDo.Add($"Độ ồn lệch {Math.Abs(on - giua):0} dB so với lớp ({on:0} dB)");
                }
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
