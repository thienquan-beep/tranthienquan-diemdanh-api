using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Repositories;

public class LopRepository : ILopRepository
{
    private readonly ApplicationDbContext _context;

    public LopRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<LopHocPhan> CoChiTiet() =>
        _context.LopHocPhans.Include(l => l.MonHoc).Include(l => l.GiangVien).OrderBy(l => l.MaLop);

    public async Task<IEnumerable<LopHocPhan>> GetAllAsync() => await CoChiTiet().ToListAsync();

    public async Task<IEnumerable<LopHocPhan>> GetCuaGiangVienAsync(string giangVienId) =>
        await CoChiTiet().Where(l => l.GiangVienId == giangVienId).ToListAsync();

    public async Task<IEnumerable<LopHocPhan>> GetCuaSinhVienAsync(string sinhVienId) =>
        await CoChiTiet().Where(l => _context.DangKyLops.Any(d => d.LopHocPhanId == l.Id && d.SinhVienId == sinhVienId)).ToListAsync();

    public Task<LopHocPhan?> GetByIdAsync(int id) => CoChiTiet().FirstOrDefaultAsync(l => l.Id == id);

    public async Task AddAsync(LopHocPhan lop)
    {
        _context.LopHocPhans.Add(lop);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(LopHocPhan lop)
    {
        _context.Entry(lop).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var lop = await _context.LopHocPhans.FindAsync(id);
        if (lop == null) return;
        var buoiIds = _context.BuoiHocs.Where(b => b.LopHocPhanId == id).Select(b => b.Id);
        _context.DiemDanhs.RemoveRange(_context.DiemDanhs.Where(d => buoiIds.Contains(d.BuoiHocId)));
        _context.BuoiHocs.RemoveRange(_context.BuoiHocs.Where(b => b.LopHocPhanId == id));
        _context.DangKyLops.RemoveRange(_context.DangKyLops.Where(d => d.LopHocPhanId == id));
        _context.LopHocPhans.Remove(lop);
        await _context.SaveChangesAsync();
    }

    public Task<int> SiSoAsync(int lopId) => _context.DangKyLops.CountAsync(d => d.LopHocPhanId == lopId);

    public Task<List<User>> GetSinhVienAsync(int lopId) =>
        _context.DangKyLops.Where(d => d.LopHocPhanId == lopId).Select(d => d.SinhVien!)
            .OrderBy(u => u.Mssv).ToListAsync();

    public async Task<bool> ThemSinhVienAsync(int lopId, string sinhVienId)
    {
        if (await LaSinhVienCuaLopAsync(lopId, sinhVienId)) return false;
        _context.DangKyLops.Add(new DangKyLop { LopHocPhanId = lopId, SinhVienId = sinhVienId });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task XoaSinhVienAsync(int lopId, string sinhVienId)
    {
        _context.DangKyLops.RemoveRange(_context.DangKyLops.Where(d => d.LopHocPhanId == lopId && d.SinhVienId == sinhVienId));
        await _context.SaveChangesAsync();
    }

    public Task<bool> LaGiangVienCuaLopAsync(int lopId, string userId) =>
        _context.LopHocPhans.AnyAsync(l => l.Id == lopId && l.GiangVienId == userId);

    public Task<bool> LaSinhVienCuaLopAsync(int lopId, string userId) =>
        _context.DangKyLops.AnyAsync(d => d.LopHocPhanId == lopId && d.SinhVienId == userId);
}
