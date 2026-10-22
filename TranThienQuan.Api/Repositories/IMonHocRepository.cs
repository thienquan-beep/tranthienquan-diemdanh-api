using TranThienQuan.Api.Models;

namespace TranThienQuan.Api.Repositories;

public interface IMonHocRepository
{
    Task<IEnumerable<MonHoc>> GetAllAsync();
    Task<MonHoc?> GetByIdAsync(int id);
    Task AddAsync(MonHoc monHoc);
    Task UpdateAsync(MonHoc monHoc);
    Task DeleteAsync(int id);
    Task<bool> DangCoLopAsync(int id);
}
