namespace TranThienQuan.Api.Models;

public class DiemDanh
{
    public int Id { get; set; }
    public int BuoiHocId { get; set; }
    public BuoiHoc? BuoiHoc { get; set; }
    public string SinhVienId { get; set; } = "";
    public User? SinhVien { get; set; }
    public DateTime ThoiDiem { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public double? KhoangCachMet { get; set; }
    public string CachThuc { get; set; } = "QR";   // QR | Tay
    public string TrangThai { get; set; } = "CoMat"; // CoMat | CoMatNghiVan | Vang | CoPhep
    public string? AnhSelfie { get; set; }
    public string? MaThietBi { get; set; }
    public bool NghiVan { get; set; }
}
