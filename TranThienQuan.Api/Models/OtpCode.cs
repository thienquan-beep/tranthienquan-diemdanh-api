namespace TranThienQuan.Api.Models;

public class OtpCode
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string MucDich { get; set; } = ""; // DangKy | DangNhap
    public string Ma { get; set; } = "";
    public DateTime HetHan { get; set; }
    public int SoLanSai { get; set; }
}
