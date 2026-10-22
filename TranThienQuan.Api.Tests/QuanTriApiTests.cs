using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TranThienQuan.Api.Tests;

public class QuanTriApiTests : IClassFixture<ApiFactory>
{
    private const string Goc = "/api/tranthienquan";
    private readonly ApiFactory _f;

    public QuanTriApiTests(ApiFactory f) => _f = f;

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static async Task<JsonElement> Json(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task AdminCrudMonHoc()
    {
        var c = await _f.DangNhapAsync("admin");
        var tao = await Json(await c.PostAsJsonAsync($"{Goc}/mon-hoc", new { maMon = "CTDL", tenMon = "Cấu trúc dữ liệu", soTinChi = 3 }));
        var id = tao.GetProperty("id").GetInt32();

        await Json(await c.PutAsJsonAsync($"{Goc}/mon-hoc/{id}", new { maMon = "CTDL", tenMon = "CTDL & Giải thuật", soTinChi = 4 }));
        var mot = await c.GetFromJsonAsync<JsonElement>($"{Goc}/mon-hoc/{id}");
        Assert.Equal("CTDL & Giải thuật", mot.GetProperty("tenMon").GetString());
        Assert.Equal(4, mot.GetProperty("soTinChi").GetInt32());

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"{Goc}/mon-hoc/{id}")).StatusCode);
        var ds = await c.GetFromJsonAsync<JsonElement>($"{Goc}/mon-hoc");
        Assert.DoesNotContain(ds.EnumerateArray(), m => m.GetProperty("maMon").GetString() == "CTDL");
    }

    [Fact]
    public async Task MaMonTrungKhacHoaThuongThi409()
    {
        // SQL Server so sánh không phân biệt hoa thường → phải báo 409, không để rơi vào lỗi unique index (500)
        var c = await _f.DangNhapAsync("admin");
        await Json(await c.PostAsJsonAsync($"{Goc}/mon-hoc", new { maMon = "KTMT", tenMon = "Kiến trúc máy tính", soTinChi = 3 }));
        var r = await c.PostAsJsonAsync($"{Goc}/mon-hoc", new { maMon = "ktmt", tenMon = "Trùng", soTinChi = 3 });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);

        var gv = (await c.GetFromJsonAsync<JsonElement>($"{Goc}/nguoi-dung")).EnumerateArray()
            .Single(u => u.GetProperty("userName").GetString() == "gv01").GetProperty("id").GetString();
        var mon = (await c.GetFromJsonAsync<JsonElement>($"{Goc}/mon-hoc")).EnumerateArray()
            .Single(m => m.GetProperty("maMon").GetString() == "KTMT").GetProperty("id").GetInt32();
        var lop = await c.PostAsJsonAsync($"{Goc}/lop", new { maLop = "ltdd01", monHocId = mon, giangVienId = gv });
        Assert.Equal(HttpStatusCode.Conflict, lop.StatusCode);
    }

    [Fact]
    public async Task SinhVienKhongTaoDuocMonHoc()
    {
        var c = await _f.DangNhapAsync("sv01");
        var r = await c.PostAsJsonAsync($"{Goc}/mon-hoc", new { maMon = "X", tenMon = "X", soTinChi = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"{Goc}/mon-hoc")).StatusCode);
    }

    [Fact]
    public async Task XoaMonDangCoLopThi409()
    {
        var c = await _f.DangNhapAsync("admin");
        var ds = await c.GetFromJsonAsync<JsonElement>($"{Goc}/mon-hoc");
        var ltdd = ds.EnumerateArray().Single(m => m.GetProperty("maMon").GetString() == "LTDD").GetProperty("id").GetInt32();
        var r = await c.DeleteAsync($"{Goc}/mon-hoc/{ltdd}");
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("Môn học đang có lớp, không thể xóa", await Message(r));
    }

    [Fact]
    public async Task AdminTaoUserDoiVaiTroVaKhoa()
    {
        var c = await _f.DangNhapAsync("admin");
        var u = await Json(await c.PostAsJsonAsync($"{Goc}/nguoi-dung", new
        {
            hoTen = "Giảng Viên 2", mssv = (string?)null, email = "gv02@test.vn", userName = "gv02", password = "Gv@12345", vaiTro = "GiangVien", biKhoa = false,
        }));
        var id = u.GetProperty("id").GetString();
        Assert.Equal("GiangVien", u.GetProperty("vaiTro").GetString());

        var sua = await Json(await c.PutAsJsonAsync($"{Goc}/nguoi-dung/{id}", new
        {
            hoTen = "Giảng Viên 2", mssv = "2380611111", email = "gv02@test.vn", userName = "gv02", password = (string?)null, vaiTro = "SinhVien", biKhoa = false,
        }));
        Assert.Equal("SinhVien", sua.GetProperty("vaiTro").GetString());

        // Người dùng do admin tạo được xác nhận email sẵn → đăng nhập được
        var khach = _f.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await khach.PostAsJsonAsync($"{Goc}/auth/dang-nhap", new { userName = "gv02", password = "Gv@12345" })).StatusCode);

        await Json(await c.PutAsJsonAsync($"{Goc}/nguoi-dung/{id}", new
        {
            hoTen = "Giảng Viên 2", mssv = "2380611111", email = "gv02@test.vn", userName = "gv02", password = (string?)null, vaiTro = "SinhVien", biKhoa = true,
        }));
        var r = await khach.PostAsJsonAsync($"{Goc}/auth/dang-nhap", new { userName = "gv02", password = "Gv@12345" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("Tài khoản đã bị khóa", await Message(r));

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"{Goc}/nguoi-dung/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"{Goc}/nguoi-dung/{id}")).StatusCode);
    }

    [Fact]
    public async Task VaiTroKhongHopLeThi400()
    {
        var c = await _f.DangNhapAsync("admin");
        var r = await c.PostAsJsonAsync($"{Goc}/nguoi-dung", new
        {
            hoTen = "X", email = "x@test.vn", userName = "xx01", password = "Sv@12345", vaiTro = "Vua", biKhoa = false,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("Vai trò không hợp lệ", await Message(r));
    }

    [Fact]
    public async Task AdminTaoLopThemSinhVien()
    {
        var c = await _f.DangNhapAsync("admin");
        var mon = await Json(await c.PostAsJsonAsync($"{Goc}/mon-hoc", new { maMon = "CSDL", tenMon = "Cơ sở dữ liệu", soTinChi = 3 }));
        var gv = (await c.GetFromJsonAsync<JsonElement>($"{Goc}/nguoi-dung")).EnumerateArray()
            .Single(u => u.GetProperty("userName").GetString() == "gv01");
        var lop = await Json(await c.PostAsJsonAsync($"{Goc}/lop", new
        {
            maLop = "CSDL01", monHocId = mon.GetProperty("id").GetInt32(), giangVienId = gv.GetProperty("id").GetString(),
        }));
        var lopId = lop.GetProperty("id").GetInt32();
        Assert.Equal("Cơ sở dữ liệu", lop.GetProperty("tenMon").GetString());
        Assert.Equal("Nguyễn Văn Giảng", lop.GetProperty("tenGiangVien").GetString());

        var them = await Json(await c.PostAsJsonAsync($"{Goc}/lop/{lopId}/sinh-vien", new { mssvHoacUserName = "2380600001" }));
        Assert.Contains(them.EnumerateArray(), u => u.GetProperty("userName").GetString() == "sv01");
        var trung = await c.PostAsJsonAsync($"{Goc}/lop/{lopId}/sinh-vien", new { mssvHoacUserName = "sv01" });
        Assert.Equal(HttpStatusCode.Conflict, trung.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync($"{Goc}/lop/{lopId}/sinh-vien", new { mssvHoacUserName = "khongco" })).StatusCode);

        var lopSau = await c.GetFromJsonAsync<JsonElement>($"{Goc}/lop/{lopId}");
        Assert.Equal(1, lopSau.GetProperty("siSo").GetInt32());

        await Json(await c.PutAsJsonAsync($"{Goc}/lop/{lopId}", new
        {
            maLop = "CSDL02", monHocId = mon.GetProperty("id").GetInt32(), giangVienId = gv.GetProperty("id").GetString(),
        }));
        Assert.Equal("CSDL02", (await c.GetFromJsonAsync<JsonElement>($"{Goc}/lop/{lopId}")).GetProperty("maLop").GetString());

        var svId = them.EnumerateArray().Single().GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"{Goc}/lop/{lopId}/sinh-vien/{svId}")).StatusCode);
        Assert.Empty((await c.GetFromJsonAsync<JsonElement>($"{Goc}/lop/{lopId}/sinh-vien")).EnumerateArray());

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"{Goc}/lop/{lopId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"{Goc}/lop/{lopId}")).StatusCode);
    }

    [Fact]
    public async Task GiangVienXemLopCuaMinhVaKhongSuaLopNguoiKhac()
    {
        var gv = await _f.DangNhapAsync("gv01");
        var ds = await gv.GetFromJsonAsync<JsonElement>($"{Goc}/lop/cua-toi");
        var ltdd = ds.EnumerateArray().Single(l => l.GetProperty("maLop").GetString() == "LTDD01");
        Assert.Equal(3, ltdd.GetProperty("siSo").GetInt32());

        var admin = await _f.DangNhapAsync("admin");
        var mon = await Json(await admin.PostAsJsonAsync($"{Goc}/mon-hoc", new { maMon = "MMT", tenMon = "Mạng máy tính", soTinChi = 3 }));
        var gv03 = await Json(await admin.PostAsJsonAsync($"{Goc}/nguoi-dung", new
        {
            hoTen = "GV 3", email = "gv03@test.vn", userName = "gv03", password = "Gv@12345", vaiTro = "GiangVien", biKhoa = false,
        }));
        var lopKhac = await Json(await admin.PostAsJsonAsync($"{Goc}/lop", new
        {
            maLop = "MMT01", monHocId = mon.GetProperty("id").GetInt32(), giangVienId = gv03.GetProperty("id").GetString(),
        }));

        var r = await gv.PostAsJsonAsync($"{Goc}/lop/{lopKhac.GetProperty("id").GetInt32()}/sinh-vien", new { mssvHoacUserName = "sv01" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);

        var lopMinh = ltdd.GetProperty("id").GetInt32();
        var svs = await gv.GetFromJsonAsync<JsonElement>($"{Goc}/lop/{lopMinh}/sinh-vien");
        Assert.Equal(3, svs.GetArrayLength());
    }
}
