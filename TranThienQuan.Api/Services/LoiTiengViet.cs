using Microsoft.AspNetCore.Identity;

namespace TranThienQuan.Api.Services;

// Thông báo lỗi Identity bằng tiếng Việt
public class LoiTiengViet : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"Tên đăng nhập '{userName}' đã tồn tại" };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"Email '{email}' đã được sử dụng" };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "Tên đăng nhập chỉ gồm chữ không dấu, số và . _ -" };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Mật khẩu phải có ít nhất {length} ký tự" };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Mật khẩu phải có ít nhất một chữ số" };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "Mật khẩu phải có ít nhất một chữ in hoa" };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "Mật khẩu phải có ít nhất một chữ thường" };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Mật khẩu phải có ít nhất một ký tự đặc biệt" };
}
