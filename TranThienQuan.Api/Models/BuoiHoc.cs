namespace TranThienQuan.Api.Models;

public class BuoiHoc
{
    public int Id { get; set; }
    public int LopHocPhanId { get; set; }
    public LopHocPhan? LopHocPhan { get; set; }
    public DateTime MoLuc { get; set; }
    public DateTime? DongLuc { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double BanKinh { get; set; } = 100;
    public string KhoaBiMat { get; set; } = "";
    public int? SiSoThucTe { get; set; }
    public string? GhiChu { get; set; }
}
