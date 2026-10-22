using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Tests;

public class DiemDanhApiTests : IClassFixture<ApiFactory>
{
    private const string Goc = "/api/tranthienquan";
    private const double LatLop = 10.8231, LngLop = 106.6297;
    private const double MotMetVi = 1 / 111195.0;
    private static readonly byte[] AnhPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private readonly ApiFactory _f;

    public DiemDanhApiTests(ApiFactory f) => _f = f;

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static async Task<JsonElement> Json(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<int> LopLtddAsync(HttpClient gv) =>
        (await gv.GetFromJsonAsync<JsonElement>($"{Goc}/lop/cua-toi")).EnumerateArray()
        .Single(l => l.GetProperty("maLop").GetString() == "LTDD01").GetProperty("id").GetInt32();

    private async Task<(HttpClient gv, int buoiId)> MoBuoiAsync()
    {
        var gv = await _f.DangNhapAsync("gv01");
        var buoi = await Json(await gv.PostAsJsonAsync($"{Goc}/buoi", new { lopId = await LopLtddAsync(gv), lat = LatLop, lng = LngLop, banKinh = 100 }));
        Assert.True(buoi.GetProperty("dangMo").GetBoolean());
        return (gv, buoi.GetProperty("id").GetInt32());
    }

    private static async Task<string> PayloadAsync(HttpClient gv, int buoiId) =>
        (await gv.GetFromJsonAsync<JsonElement>($"{Goc}/buoi/{buoiId}/qr")).GetProperty("payload").GetString()!;

    private static Task<HttpResponseMessage> QuetAsync(HttpClient sv, string payload, double metVeBac, string maThietBi = "may-1")
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(payload), "qrPayload" },
            { new StringContent((LatLop + metVeBac * MotMetVi).ToString(System.Globalization.CultureInfo.InvariantCulture)), "lat" },
            { new StringContent(LngLop.ToString(System.Globalization.CultureInfo.InvariantCulture)), "lng" },
            { new StringContent(maThietBi), "maThietBi" },
        };
        var anh = new ByteArrayContent(AnhPng);
        anh.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(anh, "selfie", "selfie.png");
        return sv.PostAsync($"{Goc}/diem-danh", form);
    }

    private static async Task<JsonElement> DanhSachAsync(HttpClient gv, int buoiId) =>
        await gv.GetFromJsonAsync<JsonElement>($"{Goc}/buoi/{buoiId}/diem-danh");

    private static JsonElement CuaSv(JsonElement ds, string hoTen) =>
        ds.EnumerateArray().Single(d => d.GetProperty("hoTen").GetString() == hoTen);

    [Fact]
    public async Task QuyTrinhDiemDanhDayDu()
    {
        var (gv, buoiId) = await MoBuoiAsync();
        var payload = await PayloadAsync(gv, buoiId);
        var sv01 = await _f.DangNhapAsync("sv01");
        var sv02 = await _f.DangNhapAsync("sv02");
        var sv03 = await _f.DangNhapAsync("sv03");

        var gan = await Json(await QuetAsync(sv01, payload, 50));
        Assert.Equal("CoMat", gan.GetProperty("trangThai").GetString());
        Assert.False(gan.GetProperty("nghiVan").GetBoolean());
        Assert.False(string.IsNullOrEmpty(gan.GetProperty("anhSelfieUrl").GetString()));

        var xa = await Json(await QuetAsync(sv02, payload, 300, "may-2"));
        Assert.Equal("CoMatNghiVan", xa.GetProperty("trangThai").GetString());
        Assert.True(xa.GetProperty("nghiVan").GetBoolean());
        Assert.InRange(xa.GetProperty("khoangCachMet").GetDouble(), 295, 305);

        var lan2 = await QuetAsync(sv01, payload, 50);
        Assert.Equal(HttpStatusCode.Conflict, lan2.StatusCode);
        Assert.Equal("Bạn đã điểm danh buổi này", await Message(lan2));

        var svs = await gv.GetFromJsonAsync<JsonElement>($"{Goc}/lop/{await LopLtddAsync(gv)}/sinh-vien");
        var idSv03 = svs.EnumerateArray().Single(u => u.GetProperty("userName").GetString() == "sv03").GetProperty("id").GetString();
        var tay = await Json(await gv.PostAsJsonAsync($"{Goc}/buoi/{buoiId}/diem-danh-tay", new { sinhVienId = idSv03, trangThai = "CoPhep" }));
        Assert.Equal("Tay", tay.GetProperty("cachThuc").GetString());

        var ds = await DanhSachAsync(gv, buoiId);
        Assert.Equal(3, ds.GetArrayLength());
        Assert.All(ds.EnumerateArray(), d => Assert.Equal(JsonValueKind.String, d.GetProperty("trangThai").ValueKind));

        var buoi = await gv.GetFromJsonAsync<JsonElement>($"{Goc}/buoi/lop/{await LopLtddAsync(gv)}");
        // soDaDiemDanh chỉ đếm người có mặt (CoMat + CoMatNghiVan), không đếm Có phép
        Assert.Equal(2, buoi.EnumerateArray().Single(b => b.GetProperty("id").GetInt32() == buoiId).GetProperty("soDaDiemDanh").GetInt32());

        await Json(await gv.PostAsync($"{Goc}/buoi/{buoiId}/dong", null));
        Assert.Equal(HttpStatusCode.BadRequest, (await gv.GetAsync($"{Goc}/buoi/{buoiId}/qr")).StatusCode);
        var sauDong = await QuetAsync(sv03, payload, 10);
        Assert.Equal(HttpStatusCode.BadRequest, sauDong.StatusCode);
        Assert.Equal("Buổi điểm danh đã đóng", await Message(sauDong));
    }

    [Fact]
    public async Task SinhVienKhongThuocLopThi403()
    {
        var admin = await _f.DangNhapAsync("admin");
        await Json(await admin.PostAsJsonAsync($"{Goc}/nguoi-dung", new
        {
            hoTen = "SV Ngoài", mssv = "2380688888", email = "ngoai@test.vn", userName = "svngoai", password = "Sv@12345", vaiTro = "SinhVien", biKhoa = false,
        }));
        var (gv, buoiId) = await MoBuoiAsync();
        var ngoai = await _f.DangNhapAsync("svngoai");

        var r = await QuetAsync(ngoai, await PayloadAsync(gv, buoiId), 10);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("Bạn không thuộc lớp này", await Message(r));
        Assert.Equal(0, (await DanhSachAsync(gv, buoiId)).EnumerateArray().Count(d => d.GetProperty("trangThai").ValueKind == JsonValueKind.String));
    }

    [Fact]
    public async Task MaQrCuThiHetHan()
    {
        var (_, buoiId) = await MoBuoiAsync();
        BuoiHoc buoi;
        using (var scope = _f.Services.CreateScope())
            buoi = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().BuoiHocs.SingleAsync(b => b.Id == buoiId);
        var cu = new QrTokenService(new FakeTimeProvider(DateTimeOffset.UtcNow.AddSeconds(-65))).TaoPayload(buoi);

        var r = await QuetAsync(await _f.DangNhapAsync("sv01"), cu, 10);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("Mã QR đã hết hạn", await Message(r));
    }

    [Fact]
    public async Task MaQrSaiDinhDangThi400()
    {
        var r = await QuetAsync(await _f.DangNhapAsync("sv01"), "https://khong-phai-ma-diem-danh", 10);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("Mã QR không hợp lệ", await Message(r));
    }

    [Fact]
    public async Task DongBuoiThiNguoiChuaDiemDanhBiVang()
    {
        var (gv, buoiId) = await MoBuoiAsync();
        var dong = await Json(await gv.PostAsync($"{Goc}/buoi/{buoiId}/dong", null));
        Assert.False(dong.GetProperty("dangMo").GetBoolean());

        var ds = await DanhSachAsync(gv, buoiId);
        Assert.Equal(3, ds.GetArrayLength());
        Assert.All(ds.EnumerateArray(), d => Assert.Equal("Vang", d.GetProperty("trangThai").GetString()));
    }

    [Fact]
    public async Task SinhVienXemLichSuCoTenMon()
    {
        var (gv, buoiId) = await MoBuoiAsync();
        var sv = await _f.DangNhapAsync("sv03");
        await Json(await QuetAsync(sv, await PayloadAsync(gv, buoiId), 20, "may-3"));

        var ls = await sv.GetFromJsonAsync<JsonElement>($"{Goc}/sinh-vien/lich-su");
        var banGhi = ls.EnumerateArray().Single(d => d.GetProperty("buoiId").GetInt32() == buoiId);
        Assert.Equal("Lập trình di động", banGhi.GetProperty("tenMon").GetString());

        var lop = await sv.GetFromJsonAsync<JsonElement>($"{Goc}/sinh-vien/lop");
        Assert.Contains(lop.EnumerateArray(), l => l.GetProperty("maLop").GetString() == "LTDD01");
    }

    [Fact]
    public async Task GiangVienKhongDayLopThiKhongLayDuocQr()
    {
        var admin = await _f.DangNhapAsync("admin");
        await Json(await admin.PostAsJsonAsync($"{Goc}/nguoi-dung", new
        {
            hoTen = "GV Bốn", email = "gv04@test.vn", userName = "gv04", password = "Gv@12345", vaiTro = "GiangVien", biKhoa = false,
        }));
        var (_, buoiId) = await MoBuoiAsync();
        var gv04 = await _f.DangNhapAsync("gv04", "Gv@12345");
        Assert.Equal(HttpStatusCode.Forbidden, (await gv04.GetAsync($"{Goc}/buoi/{buoiId}/qr")).StatusCode);
    }

    [Fact]
    public async Task DoiChieuSiSoPhatHienDiemDanhHo()
    {
        var (gv, buoiId) = await MoBuoiAsync();
        var payload = await PayloadAsync(gv, buoiId);
        await Json(await QuetAsync(await _f.DangNhapAsync("sv01"), payload, 10, "may-a"));
        await Json(await QuetAsync(await _f.DangNhapAsync("sv02"), payload, 10, "may-chung"));
        await Json(await QuetAsync(await _f.DangNhapAsync("sv03"), payload, 10, "may-chung"));

        var du = await Json(await gv.PostAsJsonAsync($"{Goc}/buoi/{buoiId}/doi-chieu", new { siSoThucTe = 2 }));
        Assert.Equal("DuNguoi", du.GetProperty("ketLuan").GetString());
        Assert.Equal(1, du.GetProperty("chenhLech").GetInt32());
        Assert.Equal("Có thể có điểm danh hộ: dư 1 người", du.GetProperty("thongBao").GetString());
        var dauTien = du.GetProperty("danhSachNghiVan")[0];
        Assert.StartsWith("Cùng thiết bị", dauTien.GetProperty("lyDo")[0].GetString());
        Assert.Equal(2, du.GetProperty("danhSachNghiVan").GetArrayLength());

        var khop = await Json(await gv.PostAsJsonAsync($"{Goc}/buoi/{buoiId}/doi-chieu", new { siSoThucTe = 3 }));
        Assert.Equal("Khop", khop.GetProperty("ketLuan").GetString());
        Assert.Equal("Sĩ số chuẩn: 3/3", khop.GetProperty("thongBao").GetString());

        var thieu = await Json(await gv.PostAsJsonAsync($"{Goc}/buoi/{buoiId}/doi-chieu", new { siSoThucTe = 5 }));
        Assert.Equal("ThieuNguoi", thieu.GetProperty("ketLuan").GetString());
        Assert.Equal("Còn 2 người có mặt chưa điểm danh", thieu.GetProperty("thongBao").GetString());

        var buoi = (await gv.GetFromJsonAsync<JsonElement>($"{Goc}/buoi/lop/{await LopLtddAsync(gv)}")).EnumerateArray()
            .Single(b => b.GetProperty("id").GetInt32() == buoiId);
        Assert.Equal(5, buoi.GetProperty("siSoThucTe").GetInt32());

        Assert.Equal(HttpStatusCode.BadRequest, (await gv.PostAsJsonAsync($"{Goc}/buoi/{buoiId}/doi-chieu", new { siSoThucTe = -1 })).StatusCode);
    }

    [Fact]
    public async Task GiangVienSuaXoaBanGhiVaBuoi()
    {
        var (gv, buoiId) = await MoBuoiAsync();
        var sv = await _f.DangNhapAsync("sv01");
        var bg = await Json(await QuetAsync(sv, await PayloadAsync(gv, buoiId), 10));
        var id = bg.GetProperty("id").GetInt32();

        var sua = await Json(await gv.PutAsJsonAsync($"{Goc}/diem-danh/{id}", new { trangThai = "Vang" }));
        Assert.Equal("Vang", sua.GetProperty("trangThai").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await gv.PutAsJsonAsync($"{Goc}/diem-danh/{id}", new { trangThai = "Linh tinh" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await gv.DeleteAsync($"{Goc}/diem-danh/{id}")).StatusCode);
        var ds = await DanhSachAsync(gv, buoiId);
        Assert.Equal(JsonValueKind.Null, CuaSv(ds, "Trần Văn An").GetProperty("trangThai").ValueKind);

        var suaBuoi = await Json(await gv.PutAsJsonAsync($"{Goc}/buoi/{buoiId}", new { banKinh = 150, ghiChu = "Phòng A1" }));
        Assert.Equal(150, suaBuoi.GetProperty("banKinh").GetDouble());
        Assert.Equal(HttpStatusCode.NoContent, (await gv.DeleteAsync($"{Goc}/buoi/{buoiId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await gv.GetAsync($"{Goc}/buoi/{buoiId}/qr")).StatusCode);
    }
}
