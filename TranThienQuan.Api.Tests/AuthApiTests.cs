using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TranThienQuan.Api.Tests;

public class AuthApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    private readonly HttpClient _c;

    public AuthApiTests(ApiFactory f)
    {
        _f = f;
        _c = f.CreateClient();
    }

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    [Fact]
    public async Task DangKyXacNhanRoiDangNhapBangOtp()
    {
        var dk = await _c.PostAsJsonAsync("/api/tranthienquan/auth/dang-ky", new
        {
            hoTen = "Sinh Viên 99", mssv = "2380699999", email = "sv99@test.vn", userName = "sv99", password = "Sv@12345",
        });
        Assert.Equal(HttpStatusCode.OK, dk.StatusCode);

        var chuaXacNhan = await _c.PostAsJsonAsync("/api/tranthienquan/auth/dang-nhap", new { userName = "sv99", password = "Sv@12345" });
        Assert.Equal(HttpStatusCode.Forbidden, chuaXacNhan.StatusCode);
        Assert.Equal("Tài khoản chưa xác nhận email", await Message(chuaXacNhan));

        var xn = await _c.PostAsJsonAsync("/api/tranthienquan/auth/xac-nhan-dang-ky",
            new { userName = "sv99", otp = _f.Email.LayOtp("sv99@test.vn") });
        Assert.Equal(HttpStatusCode.OK, xn.StatusCode);

        var dn = await _c.PostAsJsonAsync("/api/tranthienquan/auth/dang-nhap", new { userName = "sv99", password = "Sv@12345" });
        Assert.Equal(HttpStatusCode.OK, dn.StatusCode);

        var xndn = await _c.PostAsJsonAsync("/api/tranthienquan/auth/xac-nhan-dang-nhap",
            new { userName = "sv99", otp = _f.Email.LayOtp("sv99@test.vn") });
        Assert.Equal(HttpStatusCode.OK, xndn.StatusCode);
        var body = await xndn.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SinhVien", body.GetProperty("vaiTro").GetString());
        Assert.Equal("sv99", body.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task SaiMatKhauThi401()
    {
        var r = await _c.PostAsJsonAsync("/api/tranthienquan/auth/dang-nhap", new { userName = "admin", password = "sai" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("Sai tên đăng nhập hoặc mật khẩu", await Message(r));
    }

    [Fact]
    public async Task AdminDangNhapBangOtpDemoCoRoleAdmin()
    {
        await _c.PostAsJsonAsync("/api/tranthienquan/auth/dang-nhap", new { userName = "admin", password = "Admin@123" });
        var r = await _c.PostAsJsonAsync("/api/tranthienquan/auth/xac-nhan-dang-nhap", new { userName = "admin", otp = "123456" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var token = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == "role").Value);
    }

    [Fact]
    public async Task OtpSaiThi400()
    {
        await _c.PostAsJsonAsync("/api/tranthienquan/auth/dang-nhap", new { userName = "gv01", password = "Gv@12345" });
        var r = await _c.PostAsJsonAsync("/api/tranthienquan/auth/xac-nhan-dang-nhap", new { userName = "gv01", otp = "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("Mã OTP không đúng", await Message(r));
    }

    [Fact]
    public async Task DangKyTrungTenThi400TiengViet()
    {
        var r = await _c.PostAsJsonAsync("/api/tranthienquan/auth/dang-ky", new
        {
            hoTen = "Trùng", mssv = "1", email = "trung@test.vn", userName = "sv01", password = "Sv@12345",
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("Tên đăng nhập 'sv01' đã tồn tại", await Message(r));
    }

    [Fact]
    public async Task ThieuTruongBatBuocThi400TiengViet()
    {
        var r = await _c.PostAsJsonAsync("/api/tranthienquan/auth/dang-ky", new { userName = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("Vui lòng nhập", await Message(r));
    }

    [Fact]
    public async Task TaiKhoanBiKhoaKhongVuotQuaDuocBangGuiLaiOtp()
    {
        var admin = await _f.DangNhapAsync("admin");
        var r = await admin.PostAsJsonAsync("/api/tranthienquan/nguoi-dung", new
        {
            hoTen = "Sẽ Bị Khóa", email = "sekhoa@test.vn", userName = "sekhoa", password = "Sv@12345", vaiTro = "SinhVien", biKhoa = false,
        });
        var id = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        // Đăng nhập bước 1 khi chưa khóa → có OTP trong mail
        await _c.PostAsJsonAsync("/api/tranthienquan/auth/dang-nhap", new { userName = "sekhoa", password = "Sv@12345" });
        var otp = _f.Email.LayOtp("sekhoa@test.vn");

        await admin.PutAsJsonAsync($"/api/tranthienquan/nguoi-dung/{id}", new
        {
            hoTen = "Sẽ Bị Khóa", email = "sekhoa@test.vn", userName = "sekhoa", password = (string?)null, vaiTro = "SinhVien", biKhoa = true,
        });

        var xn = await _c.PostAsJsonAsync("/api/tranthienquan/auth/xac-nhan-dang-nhap", new { userName = "sekhoa", otp });
        Assert.Equal(HttpStatusCode.Forbidden, xn.StatusCode);
        var guiLai = await _c.PostAsJsonAsync("/api/tranthienquan/auth/gui-lai-otp", new { userName = "sekhoa", mucDich = "DangNhap" });
        Assert.Equal(HttpStatusCode.Forbidden, guiLai.StatusCode);
    }

    [Fact]
    public async Task GuiLaiOtpDangNhapPhaiQuaBuocMatKhauTruoc()
    {
        // sv03 là tài khoản demo: không được lấy OTP đăng nhập khi chưa nhập đúng mật khẩu
        var r = await _c.PostAsJsonAsync("/api/tranthienquan/auth/gui-lai-otp", new { userName = "sv03", mucDich = "DangNhap" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("Vui lòng đăng nhập bằng mật khẩu trước", await Message(r));
        var xn = await _c.PostAsJsonAsync("/api/tranthienquan/auth/xac-nhan-dang-nhap", new { userName = "sv03", otp = "123456" });
        Assert.Equal(HttpStatusCode.BadRequest, xn.StatusCode);
    }

    [Fact]
    public async Task ToiKhongTokenThi401()
    {
        var r = await _c.GetAsync("/api/tranthienquan/auth/toi");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task ToiCoTokenThiTraHoSoVaSuaDuoc()
    {
        var c = await _f.DangNhapAsync("sv02");
        var sua = await c.PutAsJsonAsync("/api/tranthienquan/auth/toi", new { hoTen = "Lê Thị Bình Mới", email = "sv02@tranthienquan.demo", mssv = "2380600002" });
        Assert.Equal(HttpStatusCode.OK, sua.StatusCode);
        var toi = await c.GetFromJsonAsync<JsonElement>("/api/tranthienquan/auth/toi");
        Assert.Equal("Lê Thị Bình Mới", toi.GetProperty("hoTen").GetString());
        Assert.Equal("SinhVien", toi.GetProperty("vaiTro").GetString());
    }
}
