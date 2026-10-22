using System.ComponentModel.DataAnnotations;

namespace TranThienQuan.Api.Dtos;

public record DangKyRequest(
    [Required(ErrorMessage = "Vui lòng nhập họ tên")] string HoTen,
    string? Mssv,
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")] string Email,
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] string UserName,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")] string Password);

public record DangNhapRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] string UserName,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")] string Password);

public record XacNhanOtpRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] string UserName,
    [Required(ErrorMessage = "Vui lòng nhập mã OTP")] string Otp);

public record GuiLaiOtpRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] string UserName,
    [Required(ErrorMessage = "Vui lòng nhập mục đích")] string MucDich);

public record SuaHoSoRequest(
    [Required(ErrorMessage = "Vui lòng nhập họ tên")] string HoTen,
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")] string Email,
    string? Mssv);

public record GoogleRequest([Required(ErrorMessage = "Thiếu idToken")] string IdToken);

public record AuthResponse(string Token, string VaiTro, string HoTen, string UserName);

public record ThongBao(string Message);

public record NguoiDungDto(string Id, string UserName, string HoTen, string? Mssv, string? Email, string VaiTro,
    bool BiKhoa);
