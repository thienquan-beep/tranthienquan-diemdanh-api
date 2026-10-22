using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Services;

public enum KetQuaOtp { HopLe, Sai, HetHan, KhongCo }

public class OtpService
{
    public const string MaDemo = "123456";
    private static readonly TimeSpan ThoiHan = TimeSpan.FromMinutes(5);
    private const int SoLanSaiToiDa = 5;

    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly TimeProvider _gio;

    public OtpService(ApplicationDbContext db, IEmailSender email, TimeProvider gio)
    {
        _db = db;
        _email = email;
        _gio = gio;
    }

    public async Task TaoVaGuiAsync(User u, string mucDich)
    {
        _db.OtpCodes.RemoveRange(_db.OtpCodes.Where(o => o.UserId == u.Id && o.MucDich == mucDich));
        var ma = u.IsDemo ? MaDemo : RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _db.OtpCodes.Add(new OtpCode
        {
            UserId = u.Id, MucDich = mucDich, Ma = ma, HetHan = _gio.GetUtcNow().UtcDateTime + ThoiHan,
        });
        await _db.SaveChangesAsync();

        if (u.IsDemo) return;
        var viec = mucDich == "DangKy" ? "xác nhận đăng ký tài khoản" : "đăng nhập";
        await _email.GuiAsync(u.Email!, $"Mã OTP {viec} - App điểm danh",
            $"<p>Xin chào {u.HoTen},</p><p>Mã OTP để {viec} của bạn là: <b>{ma}</b></p><p>Mã có hiệu lực trong 5 phút.</p>");
    }

    public async Task<KetQuaOtp> KiemTraAsync(User u, string mucDich, string ma)
    {
        var otp = await _db.OtpCodes.FirstOrDefaultAsync(o => o.UserId == u.Id && o.MucDich == mucDich);
        if (otp == null) return KetQuaOtp.KhongCo;

        if (_gio.GetUtcNow().UtcDateTime > otp.HetHan)
        {
            _db.OtpCodes.Remove(otp);
            await _db.SaveChangesAsync();
            return KetQuaOtp.HetHan;
        }

        if (otp.Ma != ma)
        {
            otp.SoLanSai++;
            if (otp.SoLanSai >= SoLanSaiToiDa) _db.OtpCodes.Remove(otp);
            await _db.SaveChangesAsync();
            return KetQuaOtp.Sai;
        }

        _db.OtpCodes.Remove(otp);
        await _db.SaveChangesAsync();
        return KetQuaOtp.HopLe;
    }
}
