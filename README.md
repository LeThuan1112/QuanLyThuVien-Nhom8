# Hệ thống quản lý thư viện — ASP.NET Core MVC / .NET 10

Bản nâng cấp cho bài tập lớn, xây dựng trên cấu trúc dự án ban đầu. Dùng ASP.NET Core MVC, Entity Framework Core và SQL Server/LocalDB. Giao diện MVC dùng cookie; các endpoint API dành cho nhân viên có JWT Bearer.

## Trước khi dùng bản nâng cấp
1. Sao lưu thư mục dự án đang chạy và database SQL Server/LocalDB.
2. Giải nén bản nâng cấp vào **thư mục mới** để thử trước, không ghi đè ngay dự án đang chạy.
3. So sánh `appsettings.json` và `appsettings.Development.json` của dự án đang chạy với bản mới. Giữ lại chuỗi kết nối đang sử dụng nếu muốn tiếp tục dùng dữ liệu cũ; không đưa secret vào git hoặc chia sẻ công khai.
4. Nếu dùng database hiện có, sao lưu trước lần chạy đầu vì `DbBootstrap` bổ sung bảng/cột cho phí gia hạn, ảnh bìa và đặt trước. Không xóa database để sửa lỗi build.

## Yêu cầu
- .NET 10 SDK hoặc Visual Studio 2026 với workload ASP.NET and web development.
- SQL Server hoặc SQL Server LocalDB.

## Chạy ứng dụng
Tại thư mục chứa `ThuVien.csproj`, mở PowerShell:

```powershell
dotnet restore .\ThuVien.csproj
dotnet build .\ThuVien.csproj
dotnet run --project .\ThuVien.csproj
```

Mở đúng URL in ở dòng `Now listening on`. Profile mặc định trong `Properties/launchSettings.json` dùng `https://localhost:7080` và `http://localhost:5080`, nhưng cổng có thể khác theo môi trường.

## Chạy kiểm thử tự động

```powershell
dotnet test .\tests\ThuVien.Tests\ThuVien.Tests.csproj
```

Bộ test hiện có kiểm tra các quy tắc thuần (trạng thái quá hạn, nhãn vai trò, trạng thái đặt trước). Đây chưa phải integration test bao phủ database; cần chạy trên máy có .NET SDK và bổ sung kiểm thử thực tế trước khi nộp.

## Chức năng trong bản nâng cấp
- Trang chủ công khai, danh mục sách có ảnh bìa, tìm kiếm tên/mã/tác giả/ISBN, lọc thể loại/trạng thái, sắp xếp và phân trang AJAX.
- Trang chi tiết sách; nhân viên quản lý sách; ảnh bìa chấp nhận URL HTTPS hoặc đường dẫn nội bộ bắt đầu bằng `/`.
- CRUD sách/thể loại/độc giả/tài khoản; liên kết tài khoản Độc giả với hồ sơ; phân quyền Admin, Thủ thư, Độc giả.
- Mượn/trả/gia hạn; tồn kho cập nhật trong transaction; trạng thái quá hạn theo ngày lịch; phí phạt quá hạn và phí gia hạn là hai khoản khác nhau.
- Lịch sử gia hạn lưu số cuốn đang mượn, số ngày cộng thêm, người thực hiện và phí phát sinh.
- Đặt trước sách và hàng chờ theo thời điểm yêu cầu; lượt giữ chỗ có thời hạn nhận mặc định 3 ngày; hàng chờ được làm mới định kỳ.
- Dashboard, báo cáo theo kỳ, CSV có BOM UTF-8, nhật ký thao tác, chống CSRF, giới hạn tần suất đăng nhập/API token.
- API REST: danh mục/chi tiết sách công khai, lấy JWT tại `/api/auth/token`, endpoint tạo sách và danh sách quá hạn cần JWT Bearer cùng vai trò Admin/Thủ thư. Xem `docs/API.md`.
- Tài nguyên tiếng Việt/Anh cho các khu vực chính; một số câu validation và màn hình quản trị vẫn có chuỗi tiếng Việt trực tiếp, vì vậy chưa nên khẳng định toàn bộ ứng dụng được bản địa hóa 100%.

## Database / schema
- Với database mới, ứng dụng khởi tạo schema từ mô hình EF Core.
- Với database đã có, `Data/DbBootstrap.cs` bổ sung một số bảng/cột cần thiết (phí gia hạn, hàng chờ, ảnh bìa). Sao lưu trước khi mở phiên bản nâng cấp và xem log khởi động.
- `EnsureCreated()` không thay thế EF Core Migrations cho phát triển dài hạn. Chuyển sang migrations cần một bước riêng có kế hoạch và kiểm tra schema, không chạy lệnh xóa/tạo lại database.
- Seeder chỉ tạo tài khoản và dữ liệu demo khi `ASPNETCORE_ENVIRONMENT=Development`, và chỉ khi database chưa có người dùng. Không sử dụng tài khoản demo cho production.

## Cấu hình JWT
- Ở Development, ứng dụng tạo khóa ký tạm nếu chưa có cấu hình; token sẽ mất hiệu lực sau khi ứng dụng khởi động lại.
- Môi trường triển khai yêu cầu cấu hình biến môi trường `JWT_SIGNING_KEY` hoặc `Authentication:Jwt:SigningKey`, tối thiểu 32 byte. Dùng secret store/biến môi trường, không commit khóa thật vào source control.

## Những gì chưa phải tích hợp thực tế
- **Quên mật khẩu qua email:** chưa gửi mail thật vì cần SMTP/email provider và secret. Admin có thể đặt lại mật khẩu cho người dùng.
- **Thanh toán:** phí phạt và phí gia hạn được ghi nhận là phí phát sinh; chưa có trạng thái “đã thu/chưa thu”, biên lai hoặc cổng thanh toán.
- **HTTPS production:** code đã có HTTPS redirection/HSTS theo môi trường nhưng chứng chỉ TLS, reverse proxy, domain và cấu hình máy chủ phải làm ở nơi triển khai.
- **Đa ngôn ngữ:** các khu vực chính được hỗ trợ Việt/Anh, còn một số nội dung quản trị/validation cần rà soát nếu rubric yêu cầu tất cả chuỗi phải dịch.
- **Kiểm thử:** cần chạy `dotnet build` và `dotnet test` thực tế. Không ghi vào báo cáo những kiểm thử chưa thực hiện.

## Kiểm tra package có lỗ hổng

```powershell
dotnet list .\ThuVien.csproj package --vulnerable --include-transitive
```

Dự án ghim `System.Security.Cryptography.Pkcs` ở phiên bản `10.0.13` và các gói Microsoft chính ở nhánh `10.0.13` nhằm tránh phiên bản `9.0.11` đã bị cảnh báo. Vẫn cần chạy lệnh trên sau `dotnet restore` để kiểm tra cảnh báo phát sinh từ các dependency khác.

## Tài liệu rà soát kỹ thuật và chuẩn bị nghiệm thu

Thư mục `docs/` hiện có thêm các tài liệu được tổng hợp theo mã nguồn:
- `PROJECT_OVERVIEW.md`: mục tiêu, kiến trúc, vai trò và cách chạy.
- `DATABASE_SCHEMA.md`: entity, quan hệ và ràng buộc được khai báo trong mã nguồn.
- `TEST_CASES.md`: kịch bản kiểm thử; những trường hợp chưa chạy được ghi rõ là chưa chạy.
- `SCREENSHOT_CHECKLIST.md`: danh sách ảnh chụp trực tiếp từ website để chuẩn bị báo cáo.
- `ENGINEERING_REVIEW.md`: nhận xét tĩnh, thay đổi thực hiện và các rủi ro cần xác minh.

Trong lần rà soát tiếp theo, `Services/LoanService.cs` có bổ sung kiểm tra lại giới hạn số sách và trạng thái thẻ bên trong transaction khi lập phiếu. **Thay đổi này chưa được build/test trong môi trường tạo gói**, do không có .NET SDK tại đây. Trước khi thay thế bản đang chạy, hãy backup project/database và chạy các lệnh ở `docs/TEST_CASES.md` trên máy Windows.
