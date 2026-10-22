using System.ComponentModel.DataAnnotations;

namespace TranThienQuan.Api.Dtos;

public record MonHocDto(int Id, string MaMon, string TenMon, int SoTinChi);

public record MonHocRequest(
    [Required(ErrorMessage = "Vui lòng nhập mã môn")] string MaMon,
    [Required(ErrorMessage = "Vui lòng nhập tên môn")] string TenMon,
    [Range(1, 10, ErrorMessage = "Số tín chỉ từ 1 đến 10")] int SoTinChi);

public record NguoiDungRequest(
    [Required(ErrorMessage = "Vui lòng nhập họ tên")] string HoTen,
    string? Mssv,
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")] string Email,
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] string UserName,
    string? Password,
    [Required(ErrorMessage = "Vui lòng chọn vai trò")] string VaiTro,
    bool BiKhoa);

public record LopDto(int Id, string MaLop, int MonHocId, string TenMon, string GiangVienId, string TenGiangVien, int SiSo);

public record LopRequest(
    [Required(ErrorMessage = "Vui lòng nhập mã lớp")] string MaLop,
    int MonHocId,
    [Required(ErrorMessage = "Vui lòng chọn giảng viên")] string GiangVienId);

public record ThemSinhVienRequest([Required(ErrorMessage = "Vui lòng nhập MSSV hoặc tên đăng nhập")] string MssvHoacUserName);
