using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TranThienQuan.Api.Tests;

public class KhuonMatGoogleApiTests : IClassFixture<ApiFactory>
{
    private const string Goc = "/api/tranthienquan";
    private readonly ApiFactory _f;

    public KhuonMatGoogleApiTests(ApiFactory f) => _f = f;

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static async Task<JsonElement> Json(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task BatKhuonMatRoiDangNhapBangRefreshToken()
    {
        var sv = await _f.DangNhapAsync("sv01");
        var refresh = (await Json(await sv.PostAsync($"{Goc}/auth/khuon-mat/bat", null))).GetProperty("refreshToken").GetString();
        Assert.True((await sv.GetFromJsonAsync<JsonElement>($"{Goc}/auth/toi")).GetProperty("faceLoginEnabled").GetBoolean());

        var khach = _f.CreateClient();
        var dn = await Json(await khach.PostAsJsonAsync($"{Goc}/auth/khuon-mat/dang-nhap", new { refreshToken = refresh }));
        Assert.Equal("sv01", dn.GetProperty("userName").GetString());
        Assert.Equal("SinhVien", dn.GetProperty("vaiTro").GetString());

        await Json(await sv.PostAsync($"{Goc}/auth/khuon-mat/tat", null));
        var sauTat = await khach.PostAsJsonAsync($"{Goc}/auth/khuon-mat/dang-nhap", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.Unauthorized, sauTat.StatusCode);
        Assert.Equal("Phiên đăng nhập khuôn mặt không hợp lệ", await Message(sauTat));
    }

    [Fact]
    public async Task BatLaiThiTokenCuMatHieuLuc()
    {
        var sv = await _f.DangNhapAsync("sv02");
        var cu = (await Json(await sv.PostAsync($"{Goc}/auth/khuon-mat/bat", null))).GetProperty("refreshToken").GetString();
        await Json(await sv.PostAsync($"{Goc}/auth/khuon-mat/bat", null));
        var r = await _f.CreateClient().PostAsJsonAsync($"{Goc}/auth/khuon-mat/dang-nhap", new { refreshToken = cu });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenRacThi401()
    {
        var r = await _f.CreateClient().PostAsJsonAsync($"{Goc}/auth/khuon-mat/dang-nhap", new { refreshToken = "rac" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
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
