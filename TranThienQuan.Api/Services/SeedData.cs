using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Services;

// Tạo role, tài khoản demo (OTP cố định 123456) và một lớp mẫu
public static class SeedData
{
    public static readonly string[] VaiTros = { "Admin", "GiangVien", "SinhVien" };

    public static async Task ChayAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var um = sp.GetRequiredService<UserManager<User>>();

        foreach (var r in VaiTros)
            if (!await rm.RoleExistsAsync(r))
                await rm.CreateAsync(new IdentityRole(r));

        await TaoUserAsync(um, "admin", "Admin@123", "Quản trị viên", null, "Admin");
        var gv = await TaoUserAsync(um, "gv01", "Gv@12345", "Nguyễn Văn Giảng", null, "GiangVien");
        var svs = new List<User>
        {
            await TaoUserAsync(um, "sv01", "Sv@12345", "Trần Văn An", "2380600001", "SinhVien"),
            await TaoUserAsync(um, "sv02", "Sv@12345", "Lê Thị Bình", "2380600002", "SinhVien"),
            await TaoUserAsync(um, "sv03", "Sv@12345", "Phạm Minh Châu", "2380600003", "SinhVien"),
        };

        if (await db.MonHocs.AnyAsync()) return;
        var mon = new MonHoc { MaMon = "LTDD", TenMon = "Lập trình di động", SoTinChi = 3 };
        db.MonHocs.Add(mon);
        var lop = new LopHocPhan { MaLop = "LTDD01", MonHoc = mon, GiangVienId = gv.Id };
        db.LopHocPhans.Add(lop);
        foreach (var sv in svs)
            db.DangKyLops.Add(new DangKyLop { LopHocPhan = lop, SinhVienId = sv.Id });
        await db.SaveChangesAsync();
    }

    private static async Task<User> TaoUserAsync(UserManager<User> um, string userName, string matKhau,
        string hoTen, string? mssv, string vaiTro)
    {
        var u = await um.FindByNameAsync(userName);
        if (u != null) return u;
        u = new User
        {
            UserName = userName, Email = $"{userName}@tranthienquan.demo", EmailConfirmed = true,
            HoTen = hoTen, Mssv = mssv, IsDemo = true,
        };
        var kq = await um.CreateAsync(u, matKhau);
        if (!kq.Succeeded) throw new InvalidOperationException(string.Join("; ", kq.Errors.Select(e => e.Description)));
        await um.AddToRoleAsync(u, vaiTro);
        return u;
    }
}
