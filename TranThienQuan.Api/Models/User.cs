using Microsoft.AspNetCore.Identity;

namespace TranThienQuan.Api.Models;

// User = IdentityUser + thông tin sinh viên/giảng viên
public class User : IdentityUser
{
    public string HoTen { get; set; } = "";
    public string? Mssv { get; set; }
    public bool IsDemo { get; set; }
}
