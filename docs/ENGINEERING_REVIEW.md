# Báo cáo rà soát kỹ thuật — bản nguồn được cung cấp

## Phạm vi và phương pháp

- Bản rà soát: mã nguồn trong `ThuVien_RealLibrary_Upgrade.zip`, đã giải nén vào thư mục làm việc.
- Rà soát tĩnh các Controller, Model, ViewModel, Services, `Program.cs`, `AppDbContext`, `DbBootstrap`, seeder, csproj, unit test và tài liệu hiện có.
- Không có thư mục Git trong gói được rà soát, nên không thể xem lịch sử commit hoặc xác nhận trạng thái thay đổi của repository gốc.
- Môi trường xử lý không có lệnh `dotnet`; do đó chưa thực hiện được `dotnet restore`, `dotnet build`, `dotnet test`, khởi chạy web, thử API hay xác minh kết nối SQL Server LocalDB.

## Điểm tốt nhìn thấy trong mã nguồn

1. MVC dùng Cookie Authentication; khu vực quản trị được chặn bằng vai trò trên server.
2. API dùng JWT Bearer riêng cho các endpoint dành cho nhân viên; endpoint tạo sách không dựa vào cookie trình duyệt.
3. `Program.cs` đăng ký `AutoValidateAntiforgeryTokenAttribute` toàn cục cho MVC, dùng password hasher và cấu hình cookie `HttpOnly`.
4. Có rate limiting ở endpoint đăng nhập và cấp token; thông tin JWT production được yêu cầu cấu hình thay vì tự sinh khóa cố định ngoài Development.
5. `AppDbContext` có unique index cho các mã quan trọng, check constraint tồn kho và hạn chế xóa dây chuyền để bảo vệ lịch sử nghiệp vụ.
6. Nghiệp vụ trả sách dùng cập nhật có điều kiện để ngăn trả trùng; các thay đổi tồn kho và gia hạn được tổ chức trong transaction.
7. Dữ liệu demo chỉ được seed trong Development và chỉ khi database chưa có user.
8. Có unit test cho quy tắc tính phí, trạng thái quá hạn, nhãn vai trò và trạng thái đặt trước.

## Thay đổi đã thực hiện trong lần rà soát này

- Trong `Services/LoanService.cs`, bổ sung kiểm tra lại trạng thái thẻ độc giả và số cuốn đang mượn bên trong transaction `Serializable` của `CreateLoanAsync`. Mục đích là giảm nguy cơ hai request đồng thời cùng vượt giới hạn `MaxBooksAtOnce`; lần kiểm tra trước transaction vẫn được giữ để trả lỗi sớm.
- Tạo tài liệu dự án, schema, kế hoạch kiểm thử, checklist ảnh thật và báo cáo rà soát kỹ thuật trong `docs/`.

**Lưu ý quan trọng:** thay đổi mã nguồn chưa được biên dịch hoặc chạy test trong môi trường này. Cần chạy lệnh kiểm tra trên máy có .NET 10 SDK/SQL Server LocalDB trước khi thay thế bản đang dùng.

## Rủi ro/giới hạn cần xử lý hoặc kiểm tra tiếp

### P1 — Cần kiểm thử trước khi nộp

- **Schema database:** ứng dụng hiện dùng `EnsureCreated()` kết hợp `DbBootstrap` tự bổ sung bảng/cột, không có chuỗi EF Core Migrations trong gói. Cần backup và thử trên bản sao database; nếu phát triển lâu dài, thiết kế quy trình migrations riêng.
- **Build/runtime chưa xác minh:** không có .NET SDK trong môi trường rà soát. Kết quả build/test chỉ được cập nhật sau khi chạy thật trên máy Windows.
- **Tính nhất quán khi có concurrency:** đã bổ sung kiểm tra lại giới hạn sách trong transaction, nhưng cần kiểm thử hai yêu cầu mượn đồng thời, giao dịch bị deadlock và tải thực tế trên SQL Server.
- **Kiểm thử tích hợp:** bộ test đang có là unit test cho quy tắc thuần, chưa kiểm tra database, phân quyền HTTP, CSRF, luồng mượn/trả bằng trình duyệt.

### P2 — Cải thiện bảo mật/độ tin cậy

- **Phiên đăng nhập sau khi đổi mật khẩu:** kiểm tra cookie hiện xác minh tài khoản còn hoạt động và vai trò không thay đổi, nhưng chưa thấy cơ chế security stamp/version trong `User` để chủ động thu hồi các cookie tồn tại ngay sau khi đổi hoặc reset mật khẩu. Cần kiểm tra yêu cầu của đồ án và cân nhắc triển khai cùng thay đổi schema có backup.
- **Bản địa hóa:** có resource Việt/Anh nhưng nhiều view/validation vẫn có chuỗi tiếng Việt viết trực tiếp; không nên mô tả rằng ứng dụng được dịch hoàn toàn.
- **URL ảnh bìa:** kiểm tra cả đường dẫn nội bộ và HTTPS; cần thử giá trị biên như `//host`, `javascript:`, URL quá dài và ảnh không truy cập được. Không cho người dùng cung cấp HTML/script như nội dung ảnh.
- **API:** kiểm thử trạng thái 400/401/403/404/409, token hết hạn và lỗi dữ liệu thực tế; không chỉ dựa trên đọc mã.

### P3 — Hoàn thiện trước khi đưa vào báo cáo

- Chụp giao diện thật sau khi chạy đúng database và tài khoản/role tương ứng.
- Có dữ liệu demo đủ để thể hiện dashboard, phiếu đang mượn, phiếu đã trả, quá hạn, gia hạn và đặt trước. Seeder hiện không tạo phiếu mượn mẫu; nếu muốn thêm seed giao dịch, cần tính toán để `AvailableCopies` khớp hoàn toàn và chỉ dùng Development.
- Kiểm tra mọi nút trong các màn hình thường trình diễn; cập nhật tài liệu các phần chưa có thay vì mô tả chúng như đã triển khai.

## Hành động xác minh trên máy Windows

Tại thư mục chứa `ThuVien.csproj`, dừng instance app đang chạy rồi thực hiện:

```powershell
dotnet restore .\ThuVien.csproj
dotnet build .\ThuVien.csproj
dotnet test .\tests\ThuVien.Tests\ThuVien.Tests.csproj
dotnet list .\ThuVien.csproj package --vulnerable --include-transitive
dotnet run --project .\ThuVien.csproj
```

Sau đó thực hiện các trường hợp trong `docs/TEST_CASES.md`, cập nhật cột kết quả theo kết quả quan sát được và chụp ảnh theo `docs/SCREENSHOT_CHECKLIST.md`.
