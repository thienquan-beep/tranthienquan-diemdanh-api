namespace TranThienQuan.Api.Services;

public static class KhoangCach
{
    private const double BanKinhTraiDat = 6_371_000;

    // Công thức Haversine, kết quả tính bằng mét
    public static double Met(double lat1, double lng1, double lat2, double lng2)
    {
        static double Rad(double d) => d * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLng = Rad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * BanKinhTraiDat * Math.Asin(Math.Sqrt(a));
    }
}
