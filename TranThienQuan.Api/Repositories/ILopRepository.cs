using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Repositories;

public interface ILopRepository
{
    Task<IEnumerable<LopHocPhan>> GetAllAsync();
    Task<IEnumerable<LopHocPhan>> GetCuaGiangVienAsync(string giangVienId);
    Task<IEnumerable<LopHocPhan>> GetCuaSinhVienAsync(string sinhVienId);
    Task<LopHocPhan?> GetByIdAsync(int id);
    Task AddAsync(LopHocPhan lop);
    Task UpdateAsync(LopHocPhan lop);
    Task DeleteAsync(int id);
    Task<int> SiSoAsync(int lopId);
    Task<List<User>> GetSinhVienAsync(int lopId);
    Task<bool> ThemSinhVienAsync(int lopId, string sinhVienId);
    Task XoaSinhVienAsync(int lopId, string sinhVienId);
    Task<bool> LaGiangVienCuaLopAsync(int lopId, string userId);
    Task<bool> LaSinhVienCuaLopAsync(int lopId, string userId);
}
