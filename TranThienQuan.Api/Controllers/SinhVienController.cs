using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private const long AnhToiDa = 5 * 1024 * 1024;

    private readonly IBuoiRepository _buoiRepo;
    private readonly IDiemDanhRepository _ddRepo;
    private readonly ILopRepository _lopRepo;
    private readonly QrTokenService _qr;
    private readonly TimeProvider _gio;
    private readonly IWebHostEnvironment _env;

    public SinhVienController(IBuoiRepository buoiRepo, IDiemDanhRepository ddRepo, ILopRepository lopRepo,
        QrTokenService qr, TimeProvider gio, IWebHostEnvironment env)
    {
        _buoiRepo = buoiRepo;
        _ddRepo = ddRepo;
        _lopRepo = lopRepo;
        _qr = qr;
        _gio = gio;
        _env = env;
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
    [RequestSizeLimit(AnhToiDa + 1024 * 1024)]
    public async Task<IActionResult> Quet([FromForm] QuetDiemDanhForm form)
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

        var anh = form.Selfie!;
        if (!anh.ContentType.StartsWith("image/")) return BadRequest(new ThongBao("Ảnh selfie không hợp lệ"));
        if (anh.Length > AnhToiDa) return BadRequest(new ThongBao("Ảnh selfie quá lớn (tối đa 5 MB)"));
        var thuMuc = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(thuMuc);
        var tenFile = $"{Guid.NewGuid():N}{(anh.ContentType == "image/png" ? ".png" : ".jpg")}";
        await using (var f = System.IO.File.Create(Path.Combine(thuMuc, tenFile)))
            await anh.CopyToAsync(f);

        var khoangCach = KhoangCach.Met(b.Lat, b.Lng, form.Lat, form.Lng);
        var ngoaiBanKinh = khoangCach > b.BanKinh;
        var d = new DiemDanh
        {
            BuoiHocId = b.Id, SinhVienId = UserId, ThoiDiem = _gio.GetUtcNow().UtcDateTime,
            Lat = form.Lat, Lng = form.Lng, KhoangCachMet = khoangCach, CachThuc = "QR",
            TrangThai = ngoaiBanKinh ? TrangThaiDiemDanh.CoMatNghiVan : TrangThaiDiemDanh.CoMat,
            NghiVan = ngoaiBanKinh, AnhSelfie = $"/uploads/{tenFile}", MaThietBi = form.MaThietBi,
        };
        await _ddRepo.AddAsync(d);
        return Ok(DiemDanhMap.ToDto((await _ddRepo.GetByIdAsync(d.Id))!));
    }
}
