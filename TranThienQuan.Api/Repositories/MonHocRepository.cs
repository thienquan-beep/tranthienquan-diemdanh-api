using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Repositories;

public class MonHocRepository : IMonHocRepository
{
    private readonly ApplicationDbContext _context;

    public MonHocRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<MonHoc>> GetAllAsync() =>
        await _context.MonHocs.OrderBy(m => m.MaMon).ToListAsync();

    public async Task<MonHoc?> GetByIdAsync(int id) => await _context.MonHocs.FindAsync(id);

    public async Task AddAsync(MonHoc monHoc)
    {
        _context.MonHocs.Add(monHoc);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(MonHoc monHoc)
    {
        _context.Entry(monHoc).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var monHoc = await _context.MonHocs.FindAsync(id);
        if (monHoc != null)
        {
            _context.MonHocs.Remove(monHoc);
            await _context.SaveChangesAsync();
        }
    }

    public Task<bool> DangCoLopAsync(int id) => _context.LopHocPhans.AnyAsync(l => l.MonHocId == id);
}
