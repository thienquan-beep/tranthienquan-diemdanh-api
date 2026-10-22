using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Repositories;

public interface IBuoiRepository
{
    Task<IEnumerable<BuoiHoc>> GetCuaLopAsync(int lopId);
    Task<BuoiHoc?> GetByIdAsync(int id);
    Task AddAsync(BuoiHoc buoi);
    Task UpdateAsync(BuoiHoc buoi);
    Task DeleteAsync(int id);
}
