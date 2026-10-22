using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranThienQuan.Api.Dtos;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TranThienQuan.Api.Models;
using TranThienQuan.Api.Repositories;
using TranThienQuan.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var laTest = builder.Environment.IsEnvironment("Testing");

// CSDL: SQL Server; khi chạy test dùng InMemory
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (laTest) options.UseInMemoryDatabase(builder.Configuration["TestDbName"] ?? "test");
    else options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Identity: đăng ký, đăng nhập, phân quyền, mã hóa mật khẩu
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<LoiTiengViet>()
    .AddDefaultTokenProviders();

// JWT: token chứa thông tin tài khoản và quyền
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        RoleClaimType = "role",
        NameClaimType = "unique_name",
    };
});
builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<IMonHocRepository, MonHocRepository>();
builder.Services.AddScoped<ILopRepository, LopRepository>();
builder.Services.AddScoped<IBuoiRepository, BuoiRepository>();
builder.Services.AddScoped<IDiemDanhRepository, DiemDanhRepository>();
builder.Services.AddSingleton<QrTokenService>();

// Thư mục lưu ảnh selfie: wwwroot/uploads
var webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "uploads"));
builder.Environment.WebRootPath = webRoot;
builder.Environment.WebRootFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);
builder.Services.AddSingleton(TimeProvider.System);
// Chưa cấu hình SMTP thì ghi OTP ra log
if (string.IsNullOrEmpty(builder.Configuration["Email:SmtpHost"]))
    builder.Services.AddScoped<IEmailSender, LogEmailSender>();
else
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Lỗi validate trả về { message } tiếng Việt
builder.Services.AddControllers().ConfigureApiBehaviorOptions(o =>
    o.InvalidModelStateResponseFactory = ctx => new BadRequestObjectResult(new ThongBao(
        ctx.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrEmpty(m))
        ?? "Dữ liệu không hợp lệ")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API điểm danh - Trần Thiên Quân (2380601849)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Dán JWT vào đây",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() },
    });
});

// CORS: cho phép app di động và Swagger gọi API
builder.Services.AddCors(options =>
    options.AddPolicy("ChoPhepTatCa", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    if (!laTest) scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
    await SeedData.ChayAsync(scope.ServiceProvider);
}

// Bật Swagger cả khi deploy để thầy xem danh sách API
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
app.UseCors("ChoPhepTatCa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
