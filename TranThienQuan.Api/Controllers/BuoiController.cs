using System.Security.Cryptography;
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
[Authorize(Roles = "GiangVien")]
public class BuoiController : ControllerBase
{
    private readonly IBuoiRepository _buoiRepo;
    private readonly IDiemDanhRepository _ddRepo;
    private readonly ILopRepository _lopRepo;
    private readonly QrTokenService _qr;
    private readonly TimeProvider _gio;

    public BuoiController(IBuoiRepository buoiRepo, IDiemDanhRepository ddRepo, ILopRepository lopRepo,
        QrTokenService qr, TimeProvider gio)
    {
        _buoiRepo = buoiRepo;
        _ddRepo = ddRepo;
        _lopRepo = lopRepo;
        _qr = qr;
        _gio = gio;
    }

    private string UserId => User.FindFirst("sub")!.Value;
    private DateTime BayGio => _gio.GetUtcNow().UtcDateTime;

    private async Task<BuoiDto> ToDtoAsync(BuoiHoc b) => new(
        b.Id, b.LopHocPhanId, b.MoLuc, b.DongLuc, b.Lat, b.Lng, b.BanKinh, b.DongLuc == null,
        b.SiSoThucTe, await _ddRepo.DemCoMatAsync(b.Id), b.GhiChu);

    // Buổi tồn tại và giảng viên đang đăng nhập phụ trách lớp của buổi
    private async Task<(BuoiHoc? buoi, IActionResult? loi)> LayBuoiCuaToiAsync(int id)
    {
        var b = await _buoiRepo.GetByIdAsync(id);
        if (b == null) return (null, NotFound(new ThongBao("Không tìm thấy buổi học")));
        if (!await _lopRepo.LaGiangVienCuaLopAsync(b.LopHocPhanId, UserId))
            return (null, StatusCode(403, new ThongBao("Bạn không phụ trách lớp này")));
        return (b, null);
    }

    [HttpGet("buoi/lop/{lopId}")]
    public async Task<IActionResult> CuaLop(int lopId)
    {
        if (!await _lopRepo.LaGiangVienCuaLopAsync(lopId, UserId))
            return StatusCode(403, new ThongBao("Bạn không phụ trách lớp này"));
        var kq = new List<BuoiDto>();
        foreach (var b in await _buoiRepo.GetCuaLopAsync(lopId)) kq.Add(await ToDtoAsync(b));
        return Ok(kq);
    }

    [HttpPost("buoi")]
    public async Task<IActionResult> MoBuoi(MoBuoiRequest req)
    {
        if (!await _lopRepo.LaGiangVienCuaLopAsync(req.LopId, UserId))
            return StatusCode(403, new ThongBao("Bạn không phụ trách lớp này"));
        var b = new BuoiHoc
        {
            LopHocPhanId = req.LopId, MoLuc = BayGio, Lat = req.Lat, Lng = req.Lng, BanKinh = req.BanKinh ?? 100,
            KhoaBiMat = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
        };
        await _buoiRepo.AddAsync(b);
        return Ok(await ToDtoAsync(b));
    }

    [HttpPut("buoi/{id}")]
    public async Task<IActionResult> SuaBuoi(int id, SuaBuoiRequest req)
    {
        var (b, loi) = await LayBuoiCuaToiAsync(id);
        if (loi != null) return loi;
        b!.BanKinh = req.BanKinh;
        b.GhiChu = req.GhiChu;
        await _buoiRepo.UpdateAsync(b);
        return Ok(await ToDtoAsync(b));
    }

    [HttpDelete("buoi/{id}")]
    public async Task<IActionResult> XoaBuoi(int id)
    {
        var (_, loi) = await LayBuoiCuaToiAsync(id);
        if (loi != null) return loi;
        await _buoiRepo.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("buoi/{id}/qr")]
    public async Task<IActionResult> Qr(int id)
    {
        var (b, loi) = await LayBuoiCuaToiAsync(id);
        if (loi != null) return loi;
        if (b!.DongLuc != null) return BadRequest(new ThongBao("Buổi điểm danh đã đóng"));
        return Ok(new QrDto(_qr.TaoPayload(b), _qr.ConLaiGiay()));
    }

    [HttpPost("buoi/{id}/dong")]
    public async Task<IActionResult> DongBuoi(int id)
    {
        var (b, loi) = await LayBuoiCuaToiAsync(id);
        if (loi != null) return loi;
        if (b!.DongLuc == null)
        {
            b.DongLuc = BayGio;
            await _buoiRepo.UpdateAsync(b);
            var daCo = (await _ddRepo.GetCuaBuoiAsync(id)).Select(d => d.SinhVienId).ToHashSet();
            foreach (var sv in await _lopRepo.GetSinhVienAsync(b.LopHocPhanId))
                if (!daCo.Contains(sv.Id))
                {
                    var vang = new DiemDanh
                    {
                        BuoiHocId = id, SinhVienId = sv.Id, ThoiDiem = BayGio, CachThuc = "Tay", TrangThai = TrangThaiDiemDanh.Vang,
                    };
                    try
                    {
                        await _ddRepo.AddAsync(vang);
                    }
                    catch (DbUpdateException)
                    {
                        // Sinh viên vừa quét xong đúng lúc đóng buổi: giữ bản ghi quét
                        await _ddRepo.BoTheoDoiAsync(vang);
                    }
                }
        }
        return Ok(await ToDtoAsync(b));
    }

    [HttpGet("buoi/{id}/diem-danh")]
    public async Task<IActionResult> DanhSach(int id)
    {
        var (b, loi) = await LayBuoiCuaToiAsync(id);
        if (loi != null) return loi;
        var banGhi = await _ddRepo.GetCuaBuoiAsync(id);
        var theoSv = banGhi.ToDictionary(d => d.SinhVienId);
        var kq = new List<DiemDanhDto>();
        foreach (var sv in await _lopRepo.GetSinhVienAsync(b!.LopHocPhanId))
            kq.Add(theoSv.TryGetValue(sv.Id, out var d) ? DiemDanhMap.ToDto(d) : DiemDanhMap.ChuaDiemDanh(id, sv));
        // Bản ghi của sinh viên đã bị xóa khỏi lớp vẫn hiển thị
        var trongLop = kq.Select(x => x.SinhVienId).ToHashSet();
        kq.AddRange(banGhi.Where(d => !trongLop.Contains(d.SinhVienId)).Select(DiemDanhMap.ToDto));
        return Ok(kq);
    }

    [HttpPost("buoi/{id}/diem-danh-tay")]
    public async Task<IActionResult> DiemDanhTay(int id, DiemDanhTayRequest req)
    {
        if (!TrangThaiDiemDanh.TatCa.Contains(req.TrangThai)) return BadRequest(new ThongBao("Trạng thái không hợp lệ"));
        var (b, loi) = await LayBuoiCuaToiAsync(id);
        if (loi != null) return loi;
        if (!await _lopRepo.LaSinhVienCuaLopAsync(b!.LopHocPhanId, req.SinhVienId))
            return BadRequest(new ThongBao("Sinh viên không thuộc lớp này"));

        var d = await _ddRepo.TimAsync(id, req.SinhVienId);
        if (d == null)
        {
            d = new DiemDanh { BuoiHocId = id, SinhVienId = req.SinhVienId, ThoiDiem = BayGio, CachThuc = "Tay", TrangThai = req.TrangThai };
            await _ddRepo.AddAsync(d);
        }
        else
        {
            d.TrangThai = req.TrangThai;
            d.CachThuc = "Tay";
            d.NghiVan = req.TrangThai == TrangThaiDiemDanh.CoMatNghiVan;
            await _ddRepo.UpdateAsync(d);
        }
        return Ok(DiemDanhMap.ToDto((await _ddRepo.GetByIdAsync(d.Id))!));
    }

    // Tính năng mới: đối chiếu sĩ số thực tế với số đã điểm danh để phát hiện điểm danh hộ
    [HttpPost("buoi/{id}/doi-chieu")]
    public async Task<IActionResult> DoiChieu(int id, DoiChieuRequest req)
    {
        var (b, loi) = await LayBuoiCuaToiAsync(id);
        if (loi != null) return loi;
        b!.SiSoThucTe = req.SiSoThucTe;
        await _buoiRepo.UpdateAsync(b);

        var banGhi = await _ddRepo.GetCuaBuoiAsync(id);
        var coMat = banGhi.Count(d => TrangThaiDiemDanh.LaCoMat(d.TrangThai));
        var chenhLech = coMat - req.SiSoThucTe;
        var (ketLuan, thongBao) = chenhLech switch
        {
            0 => ("Khop", $"Sĩ số chuẩn: {coMat}/{req.SiSoThucTe}"),
            > 0 => ("DuNguoi", $"Có thể có điểm danh hộ: dư {chenhLech} người"),
            _ => ("ThieuNguoi", $"Còn {-chenhLech} người có mặt chưa điểm danh"),
        };
        var nghiVan = banGhi
            .Select(d => (d, kq: ChamNghiVan.Cham(d, b, banGhi)))
            .Where(x => x.kq.diem > 0)
            .OrderByDescending(x => x.kq.diem)
            .Select(x => new LuotNghiVan(DiemDanhMap.ToDto(x.d), x.kq.diem, x.kq.lyDo))
            .ToList();
        return Ok(new KetQuaDoiChieu(req.SiSoThucTe, coMat, chenhLech, ketLuan, thongBao, nghiVan));
    }

    private async Task<(DiemDanh? d, IActionResult? loi)> LayBanGhiCuaToiAsync(int id)
    {
        var d = await _ddRepo.GetByIdAsync(id);
        if (d == null) return (null, NotFound(new ThongBao("Không tìm thấy bản ghi điểm danh")));
        var (_, loi) = await LayBuoiCuaToiAsync(d.BuoiHocId);
        return (d, loi);
    }

    [HttpPut("diem-danh/{id}")]
    public async Task<IActionResult> SuaDiemDanh(int id, SuaDiemDanhRequest req)
    {
        if (!TrangThaiDiemDanh.TatCa.Contains(req.TrangThai)) return BadRequest(new ThongBao("Trạng thái không hợp lệ"));
        var (d, loi) = await LayBanGhiCuaToiAsync(id);
        if (loi != null) return loi;
        d!.TrangThai = req.TrangThai;
        d.NghiVan = req.TrangThai == TrangThaiDiemDanh.CoMatNghiVan;
        await _ddRepo.UpdateAsync(d);
        return Ok(DiemDanhMap.ToDto(d));
    }

    [HttpDelete("diem-danh/{id}")]
    public async Task<IActionResult> XoaDiemDanh(int id)
    {
        var (_, loi) = await LayBanGhiCuaToiAsync(id);
        if (loi != null) return loi;
        await _ddRepo.DeleteAsync(id);
        return NoContent();
    }
}
