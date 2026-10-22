namespace TranThienQuan.Api.Models;

public class DangKyLop
{
    public int Id { get; set; }
    public int LopHocPhanId { get; set; }
    public LopHocPhan? LopHocPhan { get; set; }
    public string SinhVienId { get; set; } = "";
    public User? SinhVien { get; set; }
}
