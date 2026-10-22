using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Repositories;

public class BuoiRepository : IBuoiRepository
{
    private readonly ApplicationDbContext _context;

    public BuoiRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<BuoiHoc>> GetCuaLopAsync(int lopId) =>
        await _context.BuoiHocs.Where(b => b.LopHocPhanId == lopId).OrderByDescending(b => b.MoLuc).ToListAsync();

    public Task<BuoiHoc?> GetByIdAsync(int id) =>
        _context.BuoiHocs.Include(b => b.LopHocPhan).ThenInclude(l => l!.MonHoc).FirstOrDefaultAsync(b => b.Id == id);

    public async Task AddAsync(BuoiHoc buoi)
    {
        _context.BuoiHocs.Add(buoi);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BuoiHoc buoi)
    {
        _context.Entry(buoi).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var buoi = await _context.BuoiHocs.FindAsync(id);
        if (buoi == null) return;
        _context.DiemDanhs.RemoveRange(_context.DiemDanhs.Where(d => d.BuoiHocId == id));
        _context.BuoiHocs.Remove(buoi);
        await _context.SaveChangesAsync();
    }
}
