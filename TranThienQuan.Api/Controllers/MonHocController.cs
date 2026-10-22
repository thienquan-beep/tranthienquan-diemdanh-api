using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TranThienQuan.Api.Dtos;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Repositories;

namespace TranThienQuan.Api.Controllers;

[ApiController]
[Route("api/tranthienquan/mon-hoc")]
[Authorize]
public class MonHocController : ControllerBase
{
    private readonly IMonHocRepository _repo;

    public MonHocController(IMonHocRepository repo) => _repo = repo;

    private static MonHocDto ToDto(MonHoc m) => new(m.Id, m.MaMon, m.TenMon, m.SoTinChi);

    [HttpGet]
    [Authorize(Roles = "Admin,GiangVien,SinhVien")]
    public async Task<IActionResult> GetAll() => Ok((await _repo.GetAllAsync()).Select(ToDto));

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,GiangVien,SinhVien")]
    public async Task<IActionResult> GetById(int id)
    {
        var m = await _repo.GetByIdAsync(id);
        return m == null ? NotFound(new ThongBao("Không tìm thấy môn học")) : Ok(ToDto(m));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add(MonHocRequest req)
    {
        if ((await _repo.GetAllAsync()).Any(m => m.MaMon == req.MaMon.Trim()))
            return Conflict(new ThongBao($"Mã môn '{req.MaMon}' đã tồn tại"));
        var m = new MonHoc { MaMon = req.MaMon.Trim(), TenMon = req.TenMon.Trim(), SoTinChi = req.SoTinChi };
        await _repo.AddAsync(m);
        return CreatedAtAction(nameof(GetById), new { id = m.Id }, ToDto(m));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, MonHocRequest req)
    {
        var m = await _repo.GetByIdAsync(id);
        if (m == null) return NotFound(new ThongBao("Không tìm thấy môn học"));
        if ((await _repo.GetAllAsync()).Any(x => x.Id != id && x.MaMon == req.MaMon.Trim()))
            return Conflict(new ThongBao($"Mã môn '{req.MaMon}' đã tồn tại"));
        m.MaMon = req.MaMon.Trim();
        m.TenMon = req.TenMon.Trim();
        m.SoTinChi = req.SoTinChi;
        await _repo.UpdateAsync(m);
        return Ok(ToDto(m));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _repo.GetByIdAsync(id) == null) return NotFound(new ThongBao("Không tìm thấy môn học"));
        if (await _repo.DangCoLopAsync(id)) return Conflict(new ThongBao("Môn học đang có lớp, không thể xóa"));
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}
