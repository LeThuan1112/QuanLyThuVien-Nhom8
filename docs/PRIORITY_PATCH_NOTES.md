# Ghi chú bàn giao bản nâng cấp

## Đã thêm hoặc hoàn thiện trong mã nguồn này

1. **Danh mục công khai:** trang chủ, ảnh bìa, tìm kiếm sách theo tên/mã/tác giả/ISBN, lọc thể loại/tình trạng, sắp xếp, phân trang và trang chi tiết.
2. **Tài khoản độc giả:** trường `ReaderId` trong `UserEditViewModel`; quản trị viên có thể liên kết tài khoản với hồ sơ độc giả chưa được liên kết. Không xóa hồ sơ/lịch sử mượn khi đổi liên kết.
3. **Mượn/trả/gia hạn:** ngày quá hạn thống nhất theo ngày lịch; kiểm tra số lượng trong transaction; chống trả trùng; phí phạt trả trễ tách khỏi phí gia hạn.
4. **Phí gia hạn:** mặc định 2.000 đồng/cuốn/lần, ngày gia hạn lấy từ cài đặt (mặc định 7), lưu bảng `LoanRenewals` với người thao tác và số cuốn còn mượn. Đây là phí phát sinh, chưa phải ghi nhận đã thu tiền.
5. **Đặt trước:** bảng `BookReservations`, hàng chờ theo thời điểm, trạng thái Waiting/Ready/Fulfilled/Cancelled/Expired, giữ chỗ 3 ngày, hủy yêu cầu và nền tác vụ làm mới hàng chờ.
6. **Bảo mật ứng dụng:** cookie MVC và JWT cho một số endpoint API, giới hạn tần suất đăng nhập/phát hành token, validation, CSRF cho form MVC, kiểm tra tài khoản còn hoạt động trên mỗi request.
7. **API:** `POST /api/auth/token`, `GET /api/books`, `GET /api/books/{id}`, `POST /api/books` (JWT Admin/Thủ thư), `GET /api/loans/overdue` (JWT Admin/Thủ thư). Xem `docs/API.md`.
8. **Dependency:** nhánh .NET 10, các gói EF/Authentication/Pkcs ghim phiên bản 10.0.13; vẫn cần xác minh cây dependency bằng `dotnet list package --vulnerable` trên máy có SDK.
9. **Kiểm thử:** project xUnit kiểm thử một số quy tắc trạng thái/quá hạn/hiển thị. Chưa thay thế integration test với SQL Server.

## Chưa được coi là đã kiểm thử

Môi trường tạo gói không có .NET SDK, nên chưa chạy được `dotnet build` hoặc `dotnet test` tại đây. Cần chạy trên Windows/Visual Studio, xử lý lỗi compiler thật nếu có, rồi test các kịch bản trong `HANDOFF_CHECKLIST.md`.

## Chức năng cần cấu hình ngoài mã nguồn hoặc chưa tích hợp

- Khôi phục mật khẩu email: cần SMTP/email provider, tài khoản và secret.
- Thu tiền/phí: chưa có trạng thái thanh toán/biên lai/cổng thanh toán.
- HTTPS production: cần domain, chứng chỉ và cấu hình máy chủ/reverse proxy.
- Đa ngôn ngữ: các khu vực chính đã có key Việt/Anh, nhưng một số màn hình và validation vẫn còn tiếng Việt viết trực tiếp.
- EF Core migrations: database hiện được tạo/cập nhật tương thích bằng EnsureCreated + DbBootstrap; migrations cần lên kế hoạch riêng.

## Chạy kiểm tra sau khi giải nén

```powershell
dotnet restore .\ThuVien.csproj
dotnet build .\ThuVien.csproj
dotnet test .\tests\ThuVien.Tests\ThuVien.Tests.csproj
dotnet list .\ThuVien.csproj package --vulnerable --include-transitive
```

Trước khi kết nối vào database hiện có, sao lưu database và bảo đảm chuỗi kết nối đúng. Không xóa database để sửa lỗi build.
