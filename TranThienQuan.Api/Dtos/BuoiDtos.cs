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
    double? Lat, double? Lng, double? KhoangCachMet, string? CachThuc, string? TrangThai, string? AnhSelfieUrl,
    bool NghiVan, string? MaThietBi, string? TenMon, DateTime? Ngay);

public record DiemDanhTayRequest([Required(ErrorMessage = "Vui lòng chọn sinh viên")] string SinhVienId,
    [Required(ErrorMessage = "Vui lòng chọn trạng thái")] string TrangThai);

public record SuaDiemDanhRequest([Required(ErrorMessage = "Vui lòng chọn trạng thái")] string TrangThai);

public class QuetDiemDanhForm
{
    [Required(ErrorMessage = "Thiếu mã QR")] public string QrPayload { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? MaThietBi { get; set; }
    [Required(ErrorMessage = "Vui lòng chụp ảnh selfie")] public IFormFile? Selfie { get; set; }
}
