using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TranThienQuan.Api.Dtos;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Controllers;

[ApiController]
[Route("api/tranthienquan/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _um;
    private readonly OtpService _otp;
    private readonly JwtService _jwt;

    public AuthController(UserManager<User> um, OtpService otp, JwtService jwt)
    {
        _um = um;
        _otp = otp;
        _jwt = jwt;
    }

    [HttpPost("dang-ky")]
    public async Task<IActionResult> DangKy(DangKyRequest req)
    {
        var u = new User { UserName = req.UserName.Trim(), Email = req.Email.Trim(), HoTen = req.HoTen.Trim(), Mssv = req.Mssv?.Trim() };
        var kq = await _um.CreateAsync(u, req.Password);
        if (!kq.Succeeded) return BadRequest(new ThongBao(kq.Errors.First().Description));
        await _um.AddToRoleAsync(u, "SinhVien");
        await _otp.TaoVaGuiAsync(u, "DangKy");
        return Ok(new ThongBao($"Đã gửi mã OTP tới {u.Email}"));
    }

    [HttpPost("xac-nhan-dang-ky")]
    public async Task<IActionResult> XacNhanDangKy(XacNhanOtpRequest req)
    {
        var u = await _um.FindByNameAsync(req.UserName);
        if (u == null) return NotFound(new ThongBao("Không tìm thấy tài khoản"));
        var loi = LoiOtp(await _otp.KiemTraAsync(u, "DangKy", req.Otp));
        if (loi != null) return loi;
        u.EmailConfirmed = true;
        await _um.UpdateAsync(u);
        return Ok(new ThongBao("Xác nhận email thành công"));
    }

    [HttpPost("dang-nhap")]
    public async Task<IActionResult> DangNhap(DangNhapRequest req)
    {
        var u = await _um.FindByNameAsync(req.UserName);
        if (u == null || !await _um.CheckPasswordAsync(u, req.Password))
            return Unauthorized(new ThongBao("Sai tên đăng nhập hoặc mật khẩu"));
        var chan = KiemTraChan(u);
        if (chan != null) return chan;
        await _otp.TaoVaGuiAsync(u, "DangNhap");
        return Ok(new ThongBao($"Đã gửi mã OTP tới {AnEmail(u.Email!)}"));
    }

    [HttpPost("xac-nhan-dang-nhap")]
    public async Task<IActionResult> XacNhanDangNhap(XacNhanOtpRequest req)
    {
        var u = await _um.FindByNameAsync(req.UserName);
        if (u == null) return NotFound(new ThongBao("Không tìm thấy tài khoản"));
        var loi = LoiOtp(await _otp.KiemTraAsync(u, "DangNhap", req.Otp));
        if (loi != null) return loi;
        return Ok(await TaoAuthResponseAsync(u));
    }

    [HttpPost("gui-lai-otp")]
    public async Task<IActionResult> GuiLaiOtp(GuiLaiOtpRequest req)
    {
        if (req.MucDich != "DangKy" && req.MucDich != "DangNhap")
            return BadRequest(new ThongBao("Mục đích không hợp lệ"));
        var u = await _um.FindByNameAsync(req.UserName);
        if (u == null) return NotFound(new ThongBao("Không tìm thấy tài khoản"));
        await _otp.TaoVaGuiAsync(u, req.MucDich);
        return Ok(new ThongBao("Đã gửi lại mã OTP"));
    }

    [Authorize]
    [HttpGet("toi")]
    public async Task<IActionResult> Toi()
    {
        var u = await _um.FindByIdAsync(User.FindFirst("sub")!.Value);
        if (u == null) return Unauthorized();
        return Ok(await ToDtoAsync(_um, u));
    }

    [Authorize]
    [HttpPut("toi")]
    public async Task<IActionResult> SuaToi(SuaHoSoRequest req)
    {
        var u = await _um.FindByIdAsync(User.FindFirst("sub")!.Value);
        if (u == null) return Unauthorized();
        u.HoTen = req.HoTen.Trim();
        u.Email = req.Email.Trim();
        u.Mssv = req.Mssv?.Trim();
        var kq = await _um.UpdateAsync(u);
        if (!kq.Succeeded) return BadRequest(new ThongBao(kq.Errors.First().Description));
        return Ok(await ToDtoAsync(_um, u));
    }

    private IActionResult? KiemTraChan(User u)
    {
        if (u.LockoutEnd > DateTimeOffset.UtcNow)
            return StatusCode(403, new ThongBao("Tài khoản đã bị khóa"));
        if (!u.EmailConfirmed)
            return StatusCode(403, new ThongBao("Tài khoản chưa xác nhận email"));
        return null;
    }

    private IActionResult? LoiOtp(KetQuaOtp kq) => kq switch
    {
        KetQuaOtp.HopLe => null,
        KetQuaOtp.Sai => BadRequest(new ThongBao("Mã OTP không đúng")),
        KetQuaOtp.HetHan => BadRequest(new ThongBao("Mã OTP đã hết hạn, vui lòng gửi lại")),
        _ => BadRequest(new ThongBao("Mã OTP không còn hiệu lực, vui lòng gửi lại")),
    };

    private async Task<AuthResponse> TaoAuthResponseAsync(User u)
    {
        var vaiTro = (await _um.GetRolesAsync(u)).FirstOrDefault() ?? "SinhVien";
        return new AuthResponse(_jwt.TaoToken(u, vaiTro), vaiTro, u.HoTen, u.UserName!);
    }

    private static string AnEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 2 ? email : email[..2] + new string('*', at - 2) + email[at..];
    }

    public static async Task<NguoiDungDto> ToDtoAsync(UserManager<User> um, User u) => new(
        u.Id, u.UserName!, u.HoTen, u.Mssv, u.Email,
        (await um.GetRolesAsync(u)).FirstOrDefault() ?? "SinhVien",
        u.LockoutEnd > DateTimeOffset.UtcNow, u.FaceLoginEnabled);
}
