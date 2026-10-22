using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Dtos;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Controllers;

[ApiController]
[Route("api/tranthienquan/nguoi-dung")]
[Authorize]
public class NguoiDungController : ControllerBase
{
    private readonly UserManager<User> _um;
    private readonly ApplicationDbContext _db;

    public NguoiDungController(UserManager<User> um, ApplicationDbContext db)
    {
        _um = um;
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> GetAll()
    {
        var ds = new List<NguoiDungDto>();
        foreach (var u in await _um.Users.OrderBy(u => u.UserName).ToListAsync())
            ds.Add(await AuthController.ToDtoAsync(_um, u));
        return Ok(ds);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(string id)
    {
        var u = await _um.FindByIdAsync(id);
        return u == null ? NotFound(new ThongBao("Không tìm thấy người dùng")) : Ok(await AuthController.ToDtoAsync(_um, u));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add(NguoiDungRequest req)
    {
        if (!SeedData.VaiTros.Contains(req.VaiTro)) return BadRequest(new ThongBao("Vai trò không hợp lệ"));
        if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest(new ThongBao("Vui lòng nhập mật khẩu"));
        var u = new User
        {
            UserName = req.UserName.Trim(), Email = req.Email.Trim(), HoTen = req.HoTen.Trim(), Mssv = req.Mssv?.Trim(),
            EmailConfirmed = true, LockoutEnabled = true, LockoutEnd = req.BiKhoa ? DateTimeOffset.MaxValue : null,
        };
        var kq = await _um.CreateAsync(u, req.Password);
        if (!kq.Succeeded) return BadRequest(new ThongBao(kq.Errors.First().Description));
        await _um.AddToRoleAsync(u, req.VaiTro);
        return CreatedAtAction(nameof(GetById), new { id = u.Id }, await AuthController.ToDtoAsync(_um, u));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, NguoiDungRequest req)
    {
        if (!SeedData.VaiTros.Contains(req.VaiTro)) return BadRequest(new ThongBao("Vai trò không hợp lệ"));
        var u = await _um.FindByIdAsync(id);
        if (u == null) return NotFound(new ThongBao("Không tìm thấy người dùng"));
        if (u.Id == User.FindFirst("sub")!.Value && (req.BiKhoa || req.VaiTro != "Admin"))
            return BadRequest(new ThongBao("Không thể tự khóa hoặc bỏ quyền Admin của chính mình"));

        u.UserName = req.UserName.Trim();
        u.Email = req.Email.Trim();
        u.HoTen = req.HoTen.Trim();
        u.Mssv = req.Mssv?.Trim();
        u.LockoutEnabled = true;
        u.LockoutEnd = req.BiKhoa ? DateTimeOffset.MaxValue : null;
        var kq = await _um.UpdateAsync(u);
        if (!kq.Succeeded) return BadRequest(new ThongBao(kq.Errors.First().Description));

        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            var token = await _um.GeneratePasswordResetTokenAsync(u);
            var doiMk = await _um.ResetPasswordAsync(u, token, req.Password);
            if (!doiMk.Succeeded) return BadRequest(new ThongBao(doiMk.Errors.First().Description));
        }

        var vaiTroCu = await _um.GetRolesAsync(u);
        if (!vaiTroCu.SequenceEqual(new[] { req.VaiTro }))
        {
            await _um.RemoveFromRolesAsync(u, vaiTroCu);
            await _um.AddToRoleAsync(u, req.VaiTro);
        }
        return Ok(await AuthController.ToDtoAsync(_um, u));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var u = await _um.FindByIdAsync(id);
        if (u == null) return NotFound(new ThongBao("Không tìm thấy người dùng"));
        if (u.Id == User.FindFirst("sub")!.Value) return BadRequest(new ThongBao("Không thể xóa chính mình"));
        if (await _db.LopHocPhans.AnyAsync(l => l.GiangVienId == id))
            return Conflict(new ThongBao("Giảng viên đang phụ trách lớp, không thể xóa"));

        _db.DiemDanhs.RemoveRange(_db.DiemDanhs.Where(d => d.SinhVienId == id));
        _db.DangKyLops.RemoveRange(_db.DangKyLops.Where(d => d.SinhVienId == id));
        _db.OtpCodes.RemoveRange(_db.OtpCodes.Where(o => o.UserId == id));
        await _db.SaveChangesAsync();
        await _um.DeleteAsync(u);
        return NoContent();
    }
}
