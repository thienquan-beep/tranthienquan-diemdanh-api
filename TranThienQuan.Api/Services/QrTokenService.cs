using System.Security.Cryptography;
using System.Text;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Services;

// Mã QR đổi mỗi 90 giây: "TTQ|{buoiId}|{token}", token = HMAC theo khung thời gian
public class QrTokenService
{
    public const int ChuKyGiay = 90;
    private const string TienTo = "TTQ";
    private readonly TimeProvider _gio;

    public QrTokenService(TimeProvider gio) => _gio = gio;

    private long SlotHienTai() => _gio.GetUtcNow().ToUnixTimeSeconds() / ChuKyGiay;

    private static string Token(BuoiHoc b, long slot)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(b.KhoaBiMat));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{b.Id}:{slot}"));
        return Convert.ToHexString(hash)[..10].ToLowerInvariant();
    }

    public string TaoPayload(BuoiHoc b) => $"{TienTo}|{b.Id}|{Token(b, SlotHienTai())}";

    public int ConLaiGiay() => ChuKyGiay - (int)(_gio.GetUtcNow().ToUnixTimeSeconds() % ChuKyGiay);

    // Chấp nhận khung hiện tại và 1 khung trước (90–180 giây): sau khi quét, sinh viên còn
    // đo tiếng ồn, lấy vị trí rồi mới gửi
    public bool KiemTra(BuoiHoc b, string token)
    {
        var slot = SlotHienTai();
        return token == Token(b, slot) || token == Token(b, slot - 1);
    }

    public static bool TachPayload(string payload, out int buoiId, out string token)
    {
        buoiId = 0;
        token = "";
        var phan = payload.Split('|');
        if (phan.Length != 3 || phan[0] != TienTo || !int.TryParse(phan[1], out buoiId) || phan[2].Length == 0)
            return false;
        token = phan[2];
        return true;
    }
}
