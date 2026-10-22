> **Repo frontend (Flutter):** https://github.com/thienquan-beep/tranthienquan-diemdanh-mobile  
> **Repo backend (ASP.NET Core API):** https://github.com/thienquan-beep/tranthienquan-diemdanh-api

# App điểm danh sinh viên — tranthienquan

**Sinh viên:** Trần Thiên Quân — **MSSV:** 2380601849

App di động (Flutter) điểm danh sinh viên bằng **mã QR đổi mỗi 90 giây + vị trí + đo tiếng ồn**, backend ASP.NET Core 8 (Identity + JWT + SQL Server).

## Đầu API

- Swagger (API đang chạy): `https://<ten-site>.runasp.net/swagger` ← *điền sau khi deploy*
- Mọi API nằm dưới tiền tố riêng: `/api/tranthienquan/...`

## Tài khoản demo

| Vai trò | Tên đăng nhập | Mật khẩu | OTP |
|---|---|---|---|
| Quản trị viên | `admin` | `Admin@123` | `123456` |
| Giảng viên | `gv01` | `Gv@12345` | `123456` |
| Sinh viên | `sv01`, `sv02`, `sv03` | `Sv@12345` | `123456` |

Tài khoản demo dùng OTP cố định `123456` để thầy tự đăng nhập thử. Tài khoản đăng ký mới nhận OTP qua email.

## Danh sách chức năng

| # | Yêu cầu của đề | Đã làm |
|---|---|---|
| 1 | Đăng ký tài khoản qua email (xác nhận bằng OTP email) | ✅ |
| 1 | Đăng ký / đăng nhập bằng bên thứ 3 — Google, backend xác thực OAuth2.0 idToken | ✅ (cần cấu hình Google Client ID) |
| 2 | Đăng nhập tên đăng nhập + mật khẩu + **Email OTP** (6 số, hết hạn 5 phút, sai 5 lần phải gửi lại) | ✅ |
| 3 | Phân quyền: Admin → trang quản trị; Giảng viên → dashboard giảng viên; Sinh viên → trang sinh viên (đọc role từ JWT) | ✅ |
| 3 | Tự đăng nhập lại khi còn token (shared_preferences + jwt_decode) | ✅ |
| 4 | CRUD người dùng (kèm vai trò, khóa tài khoản), môn học, lớp học phần, sinh viên của lớp, buổi học, bản ghi điểm danh, hồ sơ cá nhân | ✅ |
| 5b | Chia sẻ vị trí: gửi vị trí gần đúng (Wi-Fi / mạng, không cần GPS chính xác) khi điểm danh, giảng viên xem bản đồ vòng bán kính lớp + vị trí từng sinh viên (OpenStreetMap) | ✅ |
| 5 | **Đo tiếng ồn**: quét QR xong app đo độ ồn xung quanh 3 giây qua micro (chỉ đo mức dB, không ghi âm), giảng viên xem dB từng lượt | ✅ |
| 6 | **Tính năng mới — Đối chiếu sĩ số chống điểm danh hộ** (xem bên dưới) | ✅ |
| 7 | Giao diện Material 3, tiếng Việt, có trạng thái tải / lỗi / rỗng | ✅ |
| 5 | **Dịch realtime Anh ⇄ Việt** (nút 🌐 trên thanh tiêu đề): gõ tới đâu dịch tới đó; **chụp / chọn ảnh** để nhận chữ trong ảnh và dịch như Google Dịch. Dùng Google ML Kit chạy trên máy, lần đầu tải gói ngôn ngữ ~30 MB rồi dịch được khi không có mạng | ✅ |
| – | SMS OTP | ❌ không làm (đề cho phép Email OTP) |

### Tính năng mới: Đối chiếu sĩ số chống điểm danh hộ

Các app điểm danh hiện nay chỉ ghi nhận lượt quét, nên một sinh viên có thể quét hộ bạn. Ở app này, giảng viên đếm số người thật trong phòng và nhập vào app:

- **Khớp** → "Sĩ số chuẩn".
- **Số điểm danh nhiều hơn số người thật** → cảnh báo "Có thể có điểm danh hộ: dư N người" kèm danh sách lượt đáng ngờ, xếp theo điểm nghi vấn:
  - ngoài bán kính lớp (+3), sát mép bán kính (+1),
  - **nhiều tài khoản điểm danh từ cùng một điện thoại** (+3),
  - **độ ồn lệch ≥ 15 dB so với các bạn cùng buổi** (+2) — ví dụ lớp ồn 60 dB mà một người quét ở phòng yên tĩnh 35 dB,
  - quét sát lúc đóng buổi (+1).
- **Ít hơn** → nhắc còn N người có mặt chưa điểm danh, giảng viên điểm danh tay.

Giảng viên bấm vào từng lượt để xem khoảng cách, độ ồn và đổi trạng thái.

## Luồng điểm danh

1. Giảng viên mở buổi → app lấy vị trí giảng viên làm tâm lớp (vị trí gần đúng, bán kính mặc định 300 m) và hiện **mã QR đổi mỗi 90 giây**.
2. Sinh viên quét QR → app đo tiếng ồn 3 giây + lấy vị trí gần đúng → gửi điểm danh.
3. Trong bán kính → **Có mặt**; ngoài bán kính → **Có mặt (nghi vấn)**.
4. Giảng viên xem danh sách, điểm danh tay, đối chiếu sĩ số, xem bản đồ, đóng buổi (người chưa điểm danh → **Vắng**).

## Cấu trúc

```
backend/   ASP.NET Core 8 Web API — TranThienQuan.Api.sln (Visual Studio)
  TranThienQuan.Api/        Models, Repositories, Controllers, Services, Dtos
  TranThienQuan.Api.Tests/  63 test (xUnit v3)
mobile/    Flutter — project "tranthienquan" (Android Studio)
docs/      thiết kế và kế hoạch
```

## Cách chạy

### Backend (Visual Studio)

1. Mở `TranThienQuan.Api.sln` bằng Visual Studio.
2. Sửa `ConnectionStrings:DefaultConnection` trong `appsettings.json` nếu không dùng LocalDB. App tự chạy migration và tạo tài khoản demo khi khởi động.
3. Gửi OTP qua Gmail: điền `Email:SmtpHost=smtp.gmail.com`, `Email:User`, `Email:Password` (mật khẩu ứng dụng). Bỏ trống thì OTP được ghi ra log.
4. F5 → Swagger.
5. Khi deploy (môi trường Production), **bắt buộc** đặt khóa JWT riêng qua biến môi trường `Jwt__Key` (≥ 32 ký tự ngẫu nhiên). Connection string và mật khẩu Gmail cũng đặt bằng biến môi trường (`ConnectionStrings__DefaultConnection`, `Email__Password`) hoặc `appsettings.Production.json` (không commit). App từ chối khởi động nếu vẫn dùng khóa dev.

> Nếu Windows Smart App Control chặn chạy DLL tự build, chạy bằng Docker: `bash run-docker.sh` (API ở `http://localhost:5080`, dữ liệu tạm trong bộ nhớ). Chạy test: `bash test-docker.sh`.

### Mobile (Android Studio)

1. Copy `mobile/.env.example` thành `mobile/.env`, sửa `API_URL`:
   - máy ảo + backend trên laptop: `http://10.0.2.2:5080/api/tranthienquan`
   - điện thoại thật: `https://<ten-site>.runasp.net/api/tranthienquan`
2. `flutter pub get`, chọn thiết bị và Run.
3. Đăng nhập Google: tạo OAuth Client ID loại **Android** (package `vn.hutech.tranthienquan.tranthienquan` + SHA-1) và loại **Web**, điền Web Client ID vào `GOOGLE_SERVER_CLIENT_ID` (app) và `Google:WebClientId` (backend).

## Kịch bản quay demo (nói trước, làm sau)

1. "Đăng ký tài khoản bằng email" → đăng ký → nhận OTP trong Gmail → xác nhận.
2. "Đăng nhập bằng mật khẩu + OTP email" → đăng nhập `admin` → nhập OTP → vào trang quản trị.
3. "CRUD": thêm / sửa / xóa môn học, người dùng (đổi vai trò, khóa), lớp, thêm sinh viên vào lớp.
4. "Phân quyền": đăng nhập `gv01` → dashboard giảng viên; `sv01` → trang sinh viên.
5. "Mở buổi điểm danh": giảng viên mở buổi → QR đổi mỗi 90 giây.
6. "Điểm danh QR + vị trí + đo tiếng ồn": sinh viên quét bằng điện thoại thật → thành công; quét lại → "Bạn đã điểm danh buổi này".
7. "Tính năng mới": đăng nhập `sv02` trên cùng điện thoại, quét → giảng viên nhập sĩ số thực tế → cảnh báo điểm danh hộ, danh sách nghi vấn "Cùng thiết bị".
8. "Chia sẻ vị trí": bản đồ buổi học.
9. "Dịch realtime": bấm 🌐 → gõ câu tiếng Anh thấy bản dịch ngay → bấm ⇄ đổi chiều → tab Ảnh, chụp một trang tiếng Anh → hiện chữ trong ảnh và bản dịch.
10. "Đăng nhập Google".
