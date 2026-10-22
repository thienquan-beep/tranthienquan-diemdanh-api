namespace TranThienQuan.Api.Models;

public class LopHocPhan
{
    public int Id { get; set; }
    public string MaLop { get; set; } = "";
    public int MonHocId { get; set; }
    public MonHoc? MonHoc { get; set; }
    public string GiangVienId { get; set; } = "";
    public User? GiangVien { get; set; }
}
