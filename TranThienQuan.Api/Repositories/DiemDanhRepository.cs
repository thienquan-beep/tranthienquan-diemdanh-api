using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Dtos;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Repositories;

public class DiemDanhRepository : IDiemDanhRepository
{
    private readonly ApplicationDbContext _context;

    public DiemDanhRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<DiemDanh> CoChiTiet() =>
        _context.DiemDanhs.Include(d => d.SinhVien)
            .Include(d => d.BuoiHoc).ThenInclude(b => b!.LopHocPhan).ThenInclude(l => l!.MonHoc);

    public Task<List<DiemDanh>> GetCuaBuoiAsync(int buoiId) =>
        CoChiTiet().Where(d => d.BuoiHocId == buoiId).ToListAsync();

    public Task<List<DiemDanh>> GetCuaSinhVienAsync(string sinhVienId) =>
        CoChiTiet().Where(d => d.SinhVienId == sinhVienId).OrderByDescending(d => d.ThoiDiem).ToListAsync();

    public Task<DiemDanh?> GetByIdAsync(int id) => CoChiTiet().FirstOrDefaultAsync(d => d.Id == id);

    public Task<DiemDanh?> TimAsync(int buoiId, string sinhVienId) =>
        _context.DiemDanhs.FirstOrDefaultAsync(d => d.BuoiHocId == buoiId && d.SinhVienId == sinhVienId);

    public Task<int> DemCoMatAsync(int buoiId) =>
        _context.DiemDanhs.CountAsync(d => d.BuoiHocId == buoiId &&
            (d.TrangThai == TrangThaiDiemDanh.CoMat || d.TrangThai == TrangThaiDiemDanh.CoMatNghiVan));

    public async Task AddAsync(DiemDanh diemDanh)
    {
        _context.DiemDanhs.Add(diemDanh);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DiemDanh diemDanh)
    {
        _context.Entry(diemDanh).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var diemDanh = await _context.DiemDanhs.FindAsync(id);
        if (diemDanh != null)
        {
            _context.DiemDanhs.Remove(diemDanh);
            await _context.SaveChangesAsync();
        }
    }
}
