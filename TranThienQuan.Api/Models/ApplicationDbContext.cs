using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TranThienQuan.Api.Models;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<MonHoc> MonHocs { get; set; }
    public DbSet<LopHocPhan> LopHocPhans { get; set; }
    public DbSet<DangKyLop> DangKyLops { get; set; }
    public DbSet<BuoiHoc> BuoiHocs { get; set; }
    public DbSet<DiemDanh> DiemDanhs { get; set; }
    public DbSet<OtpCode> OtpCodes { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
        builder.Entity<User>().Property(u => u.HoTen).HasMaxLength(100);
        builder.Entity<MonHoc>().HasIndex(m => m.MaMon).IsUnique();
        builder.Entity<LopHocPhan>().HasIndex(l => l.MaLop).IsUnique();
        builder.Entity<LopHocPhan>().HasOne(l => l.MonHoc).WithMany().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LopHocPhan>().HasOne(l => l.GiangVien).WithMany().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<DangKyLop>().HasIndex(d => new { d.LopHocPhanId, d.SinhVienId }).IsUnique();
        builder.Entity<DangKyLop>().HasOne(d => d.SinhVien).WithMany().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<DiemDanh>().HasIndex(d => new { d.BuoiHocId, d.SinhVienId }).IsUnique();
        builder.Entity<DiemDanh>().HasOne(d => d.SinhVien).WithMany().OnDelete(DeleteBehavior.NoAction);
        builder.Entity<RefreshToken>().HasIndex(r => r.Token).IsUnique();
    }
}
