using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Dtos;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Repositories;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Controllers;

[ApiController]
[Route("api/tranthienquan")]
[Authorize(Roles = "SinhVien")]
public class SinhVienController : ControllerBase
{
    private readonly IBuoiRepository _buoiRepo;
    private readonly IDiemDanhRepository _ddRepo;
    private readonly ILopRepository _lopRepo;
    private readonly QrTokenService _qr;
    private readonly TimeProvider _gio;

    public SinhVienController(IBuoiRepository buoiRepo, IDiemDanhRepository ddRepo, ILopRepository lopRepo,
        QrTokenService qr, TimeProvider gio)
    {
        _buoiRepo = buoiRepo;
        _ddRepo = ddRepo;
        _lopRepo = lopRepo;
        _qr = qr;
        _gio = gio;
    }

    private string UserId => User.FindFirst("sub")!.Value;

    [HttpGet("sinh-vien/lop")]
    public async Task<IActionResult> LopCuaToi()
    {
        var kq = new List<LopDto>();
        foreach (var l in await _lopRepo.GetCuaSinhVienAsync(UserId)) kq.Add(await LopController.ToDtoAsync(_lopRepo, l));
        return Ok(kq);
    }

    [HttpGet("sinh-vien/lich-su")]
    public async Task<IActionResult> LichSu() => Ok((await _ddRepo.GetCuaSinhVienAsync(UserId)).Select(DiemDanhMap.ToDto));

    [HttpPost("diem-danh")]
    public async Task<IActionResult> Quet(QuetDiemDanhRequest form)
    {
        if (!QrTokenService.TachPayload(form.QrPayload, out var buoiId, out var token))
            return BadRequest(new ThongBao("Mã QR không hợp lệ"));
        var b = await _buoiRepo.GetByIdAsync(buoiId);
        if (b == null) return BadRequest(new ThongBao("Mã QR không hợp lệ"));
        if (b.DongLuc != null) return BadRequest(new ThongBao("Buổi điểm danh đã đóng"));
        if (!_qr.KiemTra(b, token)) return BadRequest(new ThongBao("Mã QR đã hết hạn"));
        if (!await _lopRepo.LaSinhVienCuaLopAsync(b.LopHocPhanId, UserId))
            return StatusCode(403, new ThongBao("Bạn không thuộc lớp này"));
        if (await _ddRepo.TimAsync(b.Id, UserId) != null)
            return Conflict(new ThongBao("Bạn đã điểm danh buổi này"));

        var khoangCach = KhoangCach.Met(b.Lat, b.Lng, form.Lat, form.Lng);
        var ngoaiBanKinh = khoangCach > b.BanKinh;
        var d = new DiemDanh
        {
            BuoiHocId = b.Id, SinhVienId = UserId, ThoiDiem = _gio.GetUtcNow().UtcDateTime,
            Lat = form.Lat, Lng = form.Lng, KhoangCachMet = khoangCach, CachThuc = "QR",
            TrangThai = ngoaiBanKinh ? TrangThaiDiemDanh.CoMatNghiVan : TrangThaiDiemDanh.CoMat,
            NghiVan = ngoaiBanKinh, DoOn = Math.Round(form.DoOn!.Value, 1), MaThietBi = form.MaThietBi,
        };
        try
        {
            await _ddRepo.AddAsync(d);
        }
        catch (DbUpdateException)
        {
            // Hai lượt quét đồng thời: lượt sau vấp unique index (buổi, sinh viên)
            return Conflict(new ThongBao("Bạn đã điểm danh buổi này"));
        }
        return Ok(DiemDanhMap.ToDto((await _ddRepo.GetByIdAsync(d.Id))!));
    }
}
