# Tổng quan dự án — Hệ thống quản lý thư viện

## 1. Mục tiêu và phạm vi

Ứng dụng web hỗ trợ tra cứu danh mục sách và các nghiệp vụ quản lý thư viện cơ bản: quản lý sách/thể loại/độc giả/tài khoản, lập phiếu mượn, trả sách, gia hạn, giữ chỗ theo hàng chờ, xem thống kê và xuất một số báo cáo. Đây là bản mô tả theo mã nguồn trong dự án, không phải xác nhận rằng mọi kịch bản đã được kiểm thử trên máy triển khai.

## 2. Công nghệ được nhận diện từ mã nguồn

- ASP.NET Core MVC và Razor Views, target framework `net10.0`.
- C#, Nullable Reference Types và Implicit Usings được bật.
- Entity Framework Core với provider SQL Server.
- SQL Server hoặc SQL Server LocalDB.
- Cookie Authentication cho giao diện MVC; JWT Bearer cho các endpoint API bảo vệ bằng token.
- `PasswordHasher<User>` của ASP.NET Core Identity để băm và xác minh mật khẩu.
- Bootstrap/jQuery và CSS/JavaScript giao diện trong Views và `wwwroot`.
- Tài nguyên `.resx` cho một phần nội dung tiếng Việt/tiếng Anh.
- xUnit cho các unit test của quy tắc nghiệp vụ thuần.

## 3. Kiến trúc và cấu trúc chính

| Thư mục / tệp | Trách nhiệm |
|---|---|
| `Controllers/` | Điều phối request MVC và trả View/redirect; kiểm tra vai trò ở các khu vực được bảo vệ. |
| `Controllers/Api/` | Endpoint JSON cho xác thực token, tra cứu/tạo sách và danh sách quá hạn. |
| `Models/` | Entity EF Core và các hằng/trạng thái nghiệp vụ. |
| `ViewModels/` | Dữ liệu riêng cho biểu mẫu, danh sách và dashboard. |
| `Views/` | Giao diện Razor theo controller, cùng layout/partial dùng chung. |
| `Data/AppDbContext.cs` | DbSet, chỉ mục và cấu hình quan hệ/ràng buộc cơ sở dữ liệu. |
| `Data/DbBootstrap.cs` | Bổ sung một số bảng/cột cho database cũ bằng SQL có kiểm tra tồn tại. |
| `Data/DbSeeder.cs` | Tạo tài khoản và dữ liệu mẫu chỉ trong môi trường Development, nếu chưa có người dùng. |
| `Services/LoanService.cs` | Nghiệp vụ mượn, trả và gia hạn. |
| `Services/ReservationService.cs` | Nghiệp vụ hàng chờ/giữ chỗ. |
| `Services/SettingsService.cs` | Đọc các quy tắc có thể cấu hình. |
| `Services/AuditFilter.cs` | Ghi nhật ký thao tác. |
| `Services/LibraryRules.cs` | Quy tắc tính phí và suy ra trạng thái phiếu theo ngày lịch. |
| `Resources/` | Resource tiếng Việt và tiếng Anh; chưa bao phủ hoàn toàn mọi chuỗi trong giao diện. |
| `Database/` | Script schema được sinh/để tham khảo. Không coi script này thay thế migration cho phát triển dài hạn. |
| `tests/ThuVien.Tests/` | Unit test xUnit cho quy tắc thuần; không phải integration test kết nối database. |
| `docs/` | Tài liệu mô tả dự án, schema, test, checklist ảnh, API và các điểm rà soát. |

## 4. Vai trò và quyền hạn theo mã nguồn

- **Admin (`AppRoles.Admin`)**: quản lý tài khoản, cài đặt và nhật ký; ngoài ra thuộc nhóm nhân viên được phép thao tác nghiệp vụ.
- **Thủ thư (`AppRoles.Librarian`)**: nhóm nhân viên được phân quyền quản lý sách, thể loại, độc giả, phiếu mượn/trả, báo cáo và các màn hình nghiệp vụ liên quan.
- **Độc giả (`AppRoles.Reader`)**: truy cập khu vực dành cho độc giả, xem lịch sử mượn của mình, gia hạn khi đáp ứng quy tắc và thao tác đặt trước sách qua tài khoản đã liên kết hồ sơ độc giả.

Việc một vai trò truy cập được màn hình cụ thể phải được xác nhận lại trên ứng dụng đang chạy; bảng trên tóm tắt từ các `[Authorize]` và luồng hiện có.

## 5. Quy tắc nghiệp vụ nhận diện

- Mã sách, mã độc giả, tên đăng nhập và tên thể loại có ràng buộc duy nhất qua cấu hình chỉ mục.
- Tổng số bản/số bản còn được ràng buộc bằng check constraint: `0 <= AvailableCopies <= TotalCopies`.
- Hạn trả có hiệu lực đến hết ngày ghi trên phiếu; được xem là quá hạn từ ngày lịch tiếp theo.
- Mượn sách kiểm tra thẻ độc giả, số lượng sách đang mượn, tồn kho và các lượt giữ chỗ.
- Trả sách chỉ cập nhật chi tiết chưa trả; tồn kho được tăng trong cùng transaction.
- Gia hạn kiểm tra trạng thái phiếu/thẻ, số lần gia hạn và ghi lịch sử trong `LoanRenewals`.
- Phí phạt quá hạn và phí gia hạn là hai khoản riêng biệt. Hệ thống ghi nhận phí phát sinh nhưng chưa triển khai trạng thái thu tiền/cổng thanh toán.
- Yêu cầu đặt trước được lưu thành lịch sử với các trạng thái `Waiting`, `Ready`, `Fulfilled`, `Cancelled`, `Expired`; lượt `Ready` có thời hạn nhận.

## 6. Cấu hình và cách chạy

1. Cài .NET 10 SDK và SQL Server LocalDB/SQL Server.
2. Sao lưu thư mục dự án và database hiện có trước khi thử bản nâng cấp.
3. Mở PowerShell tại thư mục có `ThuVien.csproj`.
4. Chạy:

```powershell
dotnet restore .\ThuVien.csproj
dotnet build .\ThuVien.csproj
dotnet run --project .\ThuVien.csproj
```

5. Mở URL in ở dòng `Now listening on`. Profile `https` trong `Properties/launchSettings.json` khai báo `https://localhost:7080` và `http://localhost:5080`; môi trường thực tế có thể dùng cổng khác.
6. Chuỗi kết nối mặc định trong `appsettings.json` trỏ tới `(localdb)\MSSQLLocalDB`, database `ThuVienDb`. Giữ chuỗi kết nối đang dùng nếu muốn sử dụng dữ liệu cũ.
7. Ứng dụng gọi `EnsureCreated()` và `DbBootstrap.EnsureExtras()` khi khởi động. Cơ chế này không phải EF Core Migrations; tuyệt đối không xóa database để “sửa” lỗi khởi động.
8. Chỉ trong môi trường `Development`, nếu database chưa có tài khoản nào, seeder tạo dữ liệu demo. Tài khoản mẫu chỉ phục vụ demo cục bộ; không dùng trong triển khai thật.

## 7. Giới hạn hiện tại cần nêu trung thực

- Chưa tích hợp gửi email thực tế để khôi phục mật khẩu.
- Chưa có trạng thái đã thu/chưa thu, biên lai hoặc cổng thanh toán.
- Một phần giao diện/validation vẫn là chuỗi tiếng Việt trực tiếp dù đã có resource tiếng Anh.
- Kiểm thử hiện tại chủ yếu là unit test quy tắc thuần; cần kiểm thử thủ công/integration trên môi trường có SQL Server LocalDB.
- Cần có bước riêng để chuyển từ `EnsureCreated` và SQL bootstrap sang EF Core Migrations nếu tiếp tục phát triển lâu dài.
- HTTPS, secret và cấu hình triển khai production cần cấu hình phù hợp môi trường; mã nguồn trong gói không cung cấp chứng chỉ production.
