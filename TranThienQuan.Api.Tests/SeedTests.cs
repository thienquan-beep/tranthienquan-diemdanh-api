using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Tests;

public class SeedTests
{
    [Fact]
    public async Task SeedTaoDuTaiKhoanDemo()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var mongDoi = new Dictionary<string, string>
        {
            ["admin"] = "Admin", ["gv01"] = "GiangVien",
            ["sv01"] = "SinhVien", ["sv02"] = "SinhVien", ["sv03"] = "SinhVien",
        };
        foreach (var (ten, vaiTro) in mongDoi)
        {
            var u = await um.FindByNameAsync(ten);
            Assert.NotNull(u);
            Assert.True(await um.IsInRoleAsync(u!, vaiTro), $"{ten} phải có role {vaiTro}");
            Assert.True(u!.IsDemo);
            Assert.True(u.EmailConfirmed);
        }

        var mon = await db.MonHocs.SingleAsync(m => m.MaMon == "LTDD");
        Assert.Equal("Lập trình di động", mon.TenMon);
        Assert.Equal(3, mon.SoTinChi);
        var gv = await um.FindByNameAsync("gv01");
        var lop = await db.LopHocPhans.SingleAsync(l => l.MaLop == "LTDD01");
        Assert.Equal(mon.Id, lop.MonHocId);
        Assert.Equal(gv!.Id, lop.GiangVienId);
        Assert.Equal(3, await db.DangKyLops.CountAsync(d => d.LopHocPhanId == lop.Id));
    }
}
