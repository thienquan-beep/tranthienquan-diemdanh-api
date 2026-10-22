using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Dtos;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Repositories;

namespace TranThienQuan.Api.Controllers;

[ApiController]
[Route("api/tranthienquan/lop")]
[Authorize]
public class LopController : ControllerBase
{
    private readonly ILopRepository _repo;
    private readonly IMonHocRepository _monRepo;
    private readonly UserManager<User> _um;

    public LopController(ILopRepository repo, IMonHocRepository monRepo, UserManager<User> um)
    {
        _repo = repo;
        _monRepo = monRepo;
        _um = um;
    }

    private string UserId => User.FindFirst("sub")!.Value;

    public static async Task<LopDto> ToDtoAsync(ILopRepository repo, LopHocPhan l) => new(
        l.Id, l.MaLop, l.MonHocId, l.MonHoc?.TenMon ?? "", l.GiangVienId, l.GiangVien?.HoTen ?? "", await repo.SiSoAsync(l.Id));

    private async Task<List<LopDto>> ToDtosAsync(IEnumerable<LopHocPhan> ds)
    {
        var kq = new List<LopDto>();
        foreach (var l in ds) kq.Add(await ToDtoAsync(_repo, l));
        return kq;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll() => Ok(await ToDtosAsync(await _repo.GetAllAsync()));

    [HttpGet("cua-toi")]
    [Authorize(Roles = "GiangVien")]
    public async Task<IActionResult> CuaToi() => Ok(await ToDtosAsync(await _repo.GetCuaGiangVienAsync(UserId)));

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var l = await _repo.GetByIdAsync(id);
        return l == null ? NotFound(new ThongBao("Không tìm thấy lớp")) : Ok(await ToDtoAsync(_repo, l));
    }

    private async Task<IActionResult?> KiemTraAsync(LopRequest req, int? id)
    {
        if (await _monRepo.GetByIdAsync(req.MonHocId) == null) return BadRequest(new ThongBao("Môn học không tồn tại"));
        var gv = await _um.FindByIdAsync(req.GiangVienId);
        if (gv == null || !await _um.IsInRoleAsync(gv, "GiangVien")) return BadRequest(new ThongBao("Giảng viên không hợp lệ"));
        if ((await _repo.GetAllAsync()).Any(l => l.Id != id && l.MaLop.Equals(req.MaLop.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Conflict(new ThongBao($"Mã lớp '{req.MaLop}' đã tồn tại"));
        return null;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add(LopRequest req)
    {
        var loi = await KiemTraAsync(req, null);
        if (loi != null) return loi;
        var l = new LopHocPhan { MaLop = req.MaLop.Trim(), MonHocId = req.MonHocId, GiangVienId = req.GiangVienId };
        await _repo.AddAsync(l);
        return CreatedAtAction(nameof(GetById), new { id = l.Id }, await ToDtoAsync(_repo, (await _repo.GetByIdAsync(l.Id))!));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, LopRequest req)
    {
        var l = await _repo.GetByIdAsync(id);
        if (l == null) return NotFound(new ThongBao("Không tìm thấy lớp"));
        var loi = await KiemTraAsync(req, id);
        if (loi != null) return loi;
        l.MaLop = req.MaLop.Trim();
        l.MonHocId = req.MonHocId;
        l.GiangVienId = req.GiangVienId;
        await _repo.UpdateAsync(l);
        return Ok(await ToDtoAsync(_repo, (await _repo.GetByIdAsync(id))!));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _repo.GetByIdAsync(id) == null) return NotFound(new ThongBao("Không tìm thấy lớp"));
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    // Admin hoặc giảng viên của lớp
    private async Task<IActionResult?> KiemTraQuyenLopAsync(int lopId)
    {
        if (await _repo.GetByIdAsync(lopId) == null) return NotFound(new ThongBao("Không tìm thấy lớp"));
        if (User.IsInRole("Admin") || await _repo.LaGiangVienCuaLopAsync(lopId, UserId)) return null;
        return StatusCode(403, new ThongBao("Bạn không phụ trách lớp này"));
    }

    private async Task<List<NguoiDungDto>> DanhSachSvAsync(int lopId)
    {
        var kq = new List<NguoiDungDto>();
        foreach (var u in await _repo.GetSinhVienAsync(lopId)) kq.Add(await AuthController.ToDtoAsync(_um, u));
        return kq;
    }

    [HttpGet("{id}/sinh-vien")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> SinhVien(int id) => await KiemTraQuyenLopAsync(id) ?? Ok(await DanhSachSvAsync(id));

    [HttpPost("{id}/sinh-vien")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> ThemSinhVien(int id, ThemSinhVienRequest req)
    {
        var loi = await KiemTraQuyenLopAsync(id);
        if (loi != null) return loi;
        var khoa = req.MssvHoacUserName.Trim();
        var sv = await _um.Users.FirstOrDefaultAsync(u => u.Mssv == khoa || u.UserName == khoa);
        if (sv == null || !await _um.IsInRoleAsync(sv, "SinhVien"))
            return NotFound(new ThongBao($"Không tìm thấy sinh viên '{khoa}'"));
        if (!await _repo.ThemSinhVienAsync(id, sv.Id))
            return Conflict(new ThongBao($"Sinh viên {sv.HoTen} đã có trong lớp"));
        return Ok(await DanhSachSvAsync(id));
    }

    [HttpDelete("{id}/sinh-vien/{svId}")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> XoaSinhVien(int id, string svId)
    {
        var loi = await KiemTraQuyenLopAsync(id);
        if (loi != null) return loi;
        await _repo.XoaSinhVienAsync(id, svId);
        return NoContent();
    }
}
