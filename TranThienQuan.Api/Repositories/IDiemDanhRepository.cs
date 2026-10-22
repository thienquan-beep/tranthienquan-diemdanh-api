using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Repositories;

public interface IDiemDanhRepository
{
    Task<List<DiemDanh>> GetCuaBuoiAsync(int buoiId);
    Task<List<DiemDanh>> GetCuaSinhVienAsync(string sinhVienId);
    Task<DiemDanh?> GetByIdAsync(int id);
    Task<DiemDanh?> TimAsync(int buoiId, string sinhVienId);
    Task<int> DemCoMatAsync(int buoiId);
    Task AddAsync(DiemDanh diemDanh);
    Task UpdateAsync(DiemDanh diemDanh);
    Task DeleteAsync(int id);
}
