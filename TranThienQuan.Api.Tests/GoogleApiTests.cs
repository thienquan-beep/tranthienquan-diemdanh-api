using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TranThienQuan.Api.Tests;

public class GoogleApiTests : IClassFixture<ApiFactory>
{
    private const string Goc = "/api/tranthienquan";
    private readonly ApiFactory _f;

    public GoogleApiTests(ApiFactory f) => _f = f;

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static async Task<JsonElement> Json(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task GoogleTaoSinhVienMoiVaKhongTaoTrung()
    {
        _f.Google.KetQua = ("nguoimoi@gmail.com", "Người Mới");
        var c = _f.CreateClient();
        var lan1 = await Json(await c.PostAsJsonAsync($"{Goc}/auth/google", new { idToken = "x" }));
        Assert.Equal("SinhVien", lan1.GetProperty("vaiTro").GetString());
        Assert.Equal("Người Mới", lan1.GetProperty("hoTen").GetString());
        Assert.Equal("nguoimoi", lan1.GetProperty("userName").GetString());

        var lan2 = await Json(await c.PostAsJsonAsync($"{Goc}/auth/google", new { idToken = "x" }));
        Assert.Equal("nguoimoi", lan2.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task GoogleUserNameTrungThiThemSo()
    {
        _f.Google.KetQua = ("sv03@gmail.com", "Trùng Tên");
        var r = await Json(await _f.CreateClient().PostAsJsonAsync($"{Goc}/auth/google", new { idToken = "x" }));
        Assert.Equal("sv031", r.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task GoogleTokenSaiThi401()
    {
        _f.Google.KetQua = null;
        var r = await _f.CreateClient().PostAsJsonAsync($"{Goc}/auth/google", new { idToken = "sai" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("Đăng nhập Google thất bại", await Message(r));
    }

    [Fact]
    public async Task GoogleTaiKhoanBiKhoaThi403()
    {
        var admin = await _f.DangNhapAsync("admin");
        await Json(await admin.PostAsJsonAsync($"{Goc}/nguoi-dung", new
        {
            hoTen = "Bị Khóa", email = "bikhoa@gmail.com", userName = "bikhoa", password = "Sv@12345", vaiTro = "SinhVien", biKhoa = true,
        }));
        _f.Google.KetQua = ("bikhoa@gmail.com", "Bị Khóa");
        var r = await _f.CreateClient().PostAsJsonAsync($"{Goc}/auth/google", new { idToken = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("Tài khoản đã bị khóa", await Message(r));
    }
}
