using System.ComponentModel.DataAnnotations;

namespace TranThienQuan.Api.Dtos;

public static class TrangThaiDiemDanh
{
    public const string CoMat = "CoMat", CoMatNghiVan = "CoMatNghiVan", Vang = "Vang", CoPhep = "CoPhep";
    public static readonly string[] TatCa = { CoMat, CoMatNghiVan, Vang, CoPhep };
    public static bool LaCoMat(string t) => t == CoMat || t == CoMatNghiVan;
}

public record BuoiDto(int Id, int LopId, DateTime MoLuc, DateTime? DongLuc, double Lat, double Lng, double BanKinh,
    bool DangMo, int? SiSoThucTe, int SoDaDiemDanh, string? GhiChu);

public record MoBuoiRequest(int LopId, double Lat, double Lng, double? BanKinh);

public record SuaBuoiRequest([Range(10, 5000, ErrorMessage = "Bán kính từ 10 đến 5000 mét")] double BanKinh, string? GhiChu);

public record QrDto(string Payload, int ConLaiGiay);

public record DiemDanhDto(int? Id, int BuoiId, string SinhVienId, string HoTen, string? Mssv, DateTime? ThoiDiem,
    double? Lat, double? Lng, double? KhoangCachMet, string? CachThuc, string? TrangThai, double? DoOn,
    bool NghiVan, string? MaThietBi, string? TenMon, DateTime? Ngay);

public record DoiChieuRequest([Range(0, 1000, ErrorMessage = "Sĩ số thực tế phải từ 0 đến 1000")] int SiSoThucTe);

public record LuotNghiVan(DiemDanhDto DiemDanh, int Diem, List<string> LyDo);

public record KetQuaDoiChieu(int SiSoThucTe, int SoDaDiemDanh, int ChenhLech, string KetLuan, string ThongBao,
    List<LuotNghiVan> DanhSachNghiVan);

public record DiemDanhTayRequest([Required(ErrorMessage = "Vui lòng chọn sinh viên")] string SinhVienId,
    [Required(ErrorMessage = "Vui lòng chọn trạng thái")] string TrangThai);

public record SuaDiemDanhRequest([Required(ErrorMessage = "Vui lòng chọn trạng thái")] string TrangThai);

public record QuetDiemDanhRequest(
    [Required(ErrorMessage = "Thiếu mã QR")] string QrPayload,
    double Lat,
    double Lng,
    string? MaThietBi,
    [Required(ErrorMessage = "Vui lòng đo tiếng ồn khi điểm danh"), Range(0, 140, ErrorMessage = "Độ ồn không hợp lệ")] double? DoOn);
