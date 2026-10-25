# Kế hoạch kiểm thử — Hệ thống quản lý thư viện

**Trạng thái tại thời điểm rà soát:** các trường hợp bên dưới là kế hoạch và kết quả mong đợi. Chưa thể đánh dấu Pass từ môi trường xử lý gói vì không có .NET SDK/SQL Server LocalDB; người thực hiện cần cập nhật cột kết quả sau khi chạy trên máy phát triển.

| ID | Nhóm | Tiền điều kiện / thao tác | Kết quả mong đợi | Kết quả hiện tại |
|---|---|---|---|---|
| TC-01 | Khởi chạy | Chạy `dotnet restore` tại thư mục dự án | Restore kết thúc không có lỗi | Chưa chạy tại môi trường rà soát |
| TC-02 | Build | Chạy `dotnet build .\ThuVien.csproj` | Build không có lỗi | Chưa chạy tại môi trường rà soát |
| TC-03 | Unit test | Chạy `dotnet test .\tests\ThuVien.Tests\ThuVien.Tests.csproj` | Các test thuần hiện có chạy và đạt | Chưa chạy tại môi trường rà soát |
| TC-04 | Khởi tạo DB mới | Chạy Development với LocalDB database chưa tồn tại | Schema được tạo, bootstrap bổ sung các bảng/cột còn thiếu, seeder chỉ chạy khi chưa có user | Cần kiểm thử trên LocalDB |
| TC-05 | Dữ liệu cũ | Backup DB hiện hữu, chạy phiên bản mới | Dữ liệu cũ còn nguyên; bootstrap hoàn tất; không có lỗi schema | Cần kiểm thử trên bản sao backup |
| TC-06 | Đăng nhập hợp lệ | Đăng nhập với tài khoản Development đã seed hoặc tài khoản có sẵn | Được chuyển đến khu vực theo vai trò | Cần kiểm thử thủ công |
| TC-07 | Đăng nhập sai | Nhập sai mật khẩu nhiều lần | Thông báo không tiết lộ trạng thái tài khoản; áp dụng giới hạn theo cấu hình | Cần kiểm thử thủ công |
| TC-08 | Phân quyền Reader | Đăng nhập Reader, truy cập URL quản trị trực tiếp | Bị từ chối/được chuyển trang phù hợp; không nhận dữ liệu quản trị | Cần kiểm thử thủ công |
| TC-09 | Phân quyền Thủ thư | Đăng nhập Thủ thư, mở cài đặt và nhật ký Admin | Không được phép truy cập màn hình chỉ dành cho Admin | Cần kiểm thử thủ công |
| TC-10 | Tạo sách | Tạo sách với mã mới, trường bắt buộc hợp lệ | Sách được lưu và xuất hiện trong danh sách | Cần kiểm thử thủ công |
| TC-11 | Mã sách trùng | Tạo sách với `BookCode` đã tồn tại | Bị từ chối; không tạo bản ghi trùng | Cần kiểm thử thủ công |
| TC-12 | Tồn kho | Thử nhập số bản âm, tồn kho lớn hơn tổng bản, hoặc sửa tổng bản thấp hơn số bản đang mượn | Validation/ràng buộc từ chối; dữ liệu không vượt giới hạn | Cần kiểm thử thủ công |
| TC-13 | Tạo độc giả | Tạo độc giả hợp lệ và mã trùng | Bản hợp lệ được lưu; mã trùng bị từ chối | Cần kiểm thử thủ công |
| TC-14 | Thẻ hết hạn/khóa | Lập phiếu cho độc giả hết hạn hoặc bị khóa | Hệ thống từ chối mượn | Cần kiểm thử thủ công |
| TC-15 | Mượn sách bình thường | Chọn độc giả và sách còn bản; lập phiếu | Tạo phiếu, tạo chi tiết và giảm `AvailableCopies` đúng số bản | Cần kiểm thử thủ công |
| TC-16 | Mượn hết tồn | Chọn sách hết bản | Thao tác bị từ chối; tồn kho không âm | Cần kiểm thử thủ công |
| TC-17 | Mượn vượt giới hạn | Mượn thêm khi độc giả đã đạt giới hạn `MaxBooksAtOnce` | Bị từ chối; không tạo phiếu và không thay đổi tồn kho | Cần kiểm thử thủ công |
| TC-18 | Đồng thời mượn | Hai request gần đồng thời cho cùng độc giả khi chỉ còn một suất theo giới hạn | Không vượt giới hạn sách đang mượn; nếu xảy ra xung đột, một thao tác bị từ chối hoặc lỗi được xử lý rõ | Cần kiểm thử concurrency trên SQL Server |
| TC-19 | Trả sách | Trả một chi tiết chưa trả | Ghi `ReturnDate`, người nhận, phí; tăng tồn kho một lần | Cần kiểm thử thủ công |
| TC-20 | Trả trùng | Gửi thao tác trả lần thứ hai cho cùng chi tiết | Bị từ chối; không tăng tồn kho thêm | Cần kiểm thử thủ công |
| TC-21 | Tính phí quá hạn | Trả trong ngày hạn và trả từ ngày tiếp theo | Trong ngày hạn phí bằng 0; từ ngày kế tiếp tính theo số ngày lịch và cấu hình | Có unit test thuần, cần chạy lại `dotnet test` |
| TC-22 | Gia hạn | Gia hạn phiếu còn đủ điều kiện | Cộng số ngày cấu hình; tăng bộ đếm; lưu lịch sử và phí riêng | Cần kiểm thử thủ công |
| TC-23 | Gia hạn không hợp lệ | Gia hạn phiếu đã trả hết, quá hạn, thẻ không hợp lệ hoặc vượt số lần | Thao tác bị từ chối; không cập nhật hạn trả/lịch sử | Cần kiểm thử thủ công |
| TC-24 | Đặt trước | Tạo lượt chờ cho sách, thử hủy và nhận thông báo có sách | Lượt chờ/status/hạn nhận nhất quán theo nghiệp vụ | Cần kiểm thử thủ công |
| TC-25 | Báo cáo CSV | Xuất dữ liệu có dấu tiếng Việt, ký tự CSV đặc biệt và các ngày biên | File mở được, UTF-8 đúng, các trường CSV được escape an toàn | Cần kiểm thử thủ công |
| TC-26 | API công khai | Gọi `GET /api/books` và `GET /api/books/{id}` | JSON đúng cấu trúc, phân trang/lọc chính xác, 404 khi không tồn tại | Cần kiểm thử HTTP thực tế |
| TC-27 | API bảo vệ | Gọi POST sách/API quá hạn với token thiếu/sai/hết hạn/đúng vai trò | Trả 401 khi xác thực không đạt, 403 khi thiếu quyền, thành công khi đúng quyền | Cần kiểm thử HTTP thực tế |
| TC-28 | API đầu vào | Gửi request tạo sách thiếu trường, mã trùng, thể loại không tồn tại, URL ảnh không hợp lệ | Trả lỗi 400/409 phù hợp; không lưu dữ liệu không hợp lệ | Cần kiểm thử HTTP thực tế |
| TC-29 | Ngôn ngữ | Chuyển Việt/Anh tại trang chủ, sách, độc giả, mượn, báo cáo và tài khoản | Resource được áp dụng; ghi lại những label chưa dịch | Cần kiểm thử thủ công |
| TC-30 | Responsive | Kiểm tra màn hình desktop và mobile | Không tràn bố cục, bảng có vùng cuộn, form dùng được | Cần kiểm thử trình duyệt |

## Lệnh kiểm thử trên Windows PowerShell

```powershell
dotnet restore .\ThuVien.csproj
dotnet build .\ThuVien.csproj
dotnet test .\tests\ThuVien.Tests\ThuVien.Tests.csproj
dotnet list .\ThuVien.csproj package --vulnerable --include-transitive
```

Nếu `ThuVien.exe` vẫn đang chạy, dừng bằng `Ctrl+C` trong cửa sổ terminal hoặc Stop trong Visual Studio trước khi build lại. Không dùng database thật để kiểm tra trường hợp gây lỗi dữ liệu; dùng bản sao database đã backup.
