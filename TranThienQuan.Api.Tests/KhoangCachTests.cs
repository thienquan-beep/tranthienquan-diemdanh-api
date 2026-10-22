using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Tests;

public class KhoangCachTests
{
    [Fact]
    public void CungDiemBangKhong() => Assert.Equal(0, KhoangCach.Met(10.8231, 106.6297, 10.8231, 106.6297), 3);

    [Fact]
    public void LechVi0Do0009XapXi100Met() =>
        Assert.InRange(KhoangCach.Met(10.8231, 106.6297, 10.8240, 106.6297), 98, 102);
}
