using TranThienQuan.Api.Services;

namespace TranThienQuan.Api.Tests;

public class FakeGoogleValidator : IGoogleTokenValidator
{
    public (string email, string hoTen)? KetQua { get; set; }

    public Task<(string email, string hoTen)?> KiemTraAsync(string idToken) => Task.FromResult(KetQua);
}
