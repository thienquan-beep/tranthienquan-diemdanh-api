using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Repositories;

namespace TranThienQuan.Api.Tests;

[CollectionDefinition("KhongSongSong", DisableParallelization = true)]
public class KhongSongSong;

// Các tình huống chỉ xảy ra khi chạy thật (SQL Server, IIS) mà EF InMemory không tái hiện được
[Collection("KhongSongSong")]
public class TrienKhaiTests
{
    private const string Goc = "/api/tranthienquan";

    private static JsonContent Form(string payload, double lat, double lng) =>
        JsonContent.Create(new { qrPayload = payload, lat, lng, maThietBi = "m1", doOn = 55 });

    private static async Task<(HttpClient gv, string payload)> MoBuoiAsync(ApiFactory f)
    {
        var gv = await f.DangNhapAsync("gv01");
        var lopId = (await gv.GetFromJsonAsync<JsonElement>($"{Goc}/lop/cua-toi"))[0].GetProperty("id").GetInt32();
        var buoi = await (await gv.PostAsJsonAsync($"{Goc}/buoi", new { lopId, lat = 10.8231, lng = 106.6297, banKinh = 100 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var qr = await gv.GetFromJsonAsync<JsonElement>($"{Goc}/buoi/{buoi.GetProperty("id").GetInt32()}/qr");
        return (gv, qr.GetProperty("payload").GetString()!);
    }

    [Fact]
    public async Task ToaDoDocDungKhiMayChuDungVanHoaTiengViet()
    {
        var cu = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("vi-VN");
        try
        {
            using var f = new ApiFactory();
            var (_, payload) = await MoBuoiAsync(f);
            var sv = await f.DangNhapAsync("sv01");
            var r = await sv.PostAsync($"{Goc}/diem-danh", Form(payload, 10.8233, 106.6297));
            var body = await r.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("CoMat", body.GetProperty("trangThai").GetString());
            Assert.InRange(body.GetProperty("khoangCachMet").GetDouble(), 15, 30);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = cu;
        }
    }

    // Giả lập SQL Server: hai lượt quét đồng thời cùng qua bước kiểm tra trùng, lượt sau vấp unique index
    private class RepoNemTrung(ApplicationDbContext db) : IDiemDanhRepository
    {
        private readonly DiemDanhRepository _that = new(db);
        public Task<List<DiemDanh>> GetCuaBuoiAsync(int buoiId) => _that.GetCuaBuoiAsync(buoiId);
        public Task<List<DiemDanh>> GetCuaSinhVienAsync(string sinhVienId) => _that.GetCuaSinhVienAsync(sinhVienId);
        public Task<DiemDanh?> GetByIdAsync(int id) => _that.GetByIdAsync(id);
        public Task<DiemDanh?> TimAsync(int buoiId, string sinhVienId) => Task.FromResult<DiemDanh?>(null);
        public Task<int> DemCoMatAsync(int buoiId) => _that.DemCoMatAsync(buoiId);
        public Task AddAsync(DiemDanh diemDanh) => throw new DbUpdateException("trùng unique index");
        public Task UpdateAsync(DiemDanh diemDanh) => _that.UpdateAsync(diemDanh);
        public Task DeleteAsync(int id) => _that.DeleteAsync(id);
        public Task BoTheoDoiAsync(DiemDanh diemDanh) => _that.BoTheoDoiAsync(diemDanh);
    }

    [Fact]
    public async Task QuetDongThoiTrungThi409KhongPhai500()
    {
        using var f = new ApiFactory();
        var (gv, payload) = await MoBuoiAsync(f);
        var sv = await f.DangNhapAsync("sv01");
        var bienThe = f.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddScoped<IDiemDanhRepository, RepoNemTrung>()));
        var svDuaTranh = bienThe.CreateClient();
        svDuaTranh.DefaultRequestHeaders.Authorization = sv.DefaultRequestHeaders.Authorization;

        var r = await svDuaTranh.PostAsync($"{Goc}/diem-danh", Form(payload, 10.8231, 106.6297));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("Bạn đã điểm danh buổi này",
            (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }

    [Fact]
    public void ProductionVoiKhoaJwtMacDinhThiKhongKhoiDong()
    {
        using var f = new ApiFactory().WithWebHostBuilder(b => b.UseEnvironment("Production"));
        var loi = Assert.ThrowsAny<Exception>(() => f.CreateClient());
        Assert.Contains("Jwt:Key", loi.ToString());
    }
}
