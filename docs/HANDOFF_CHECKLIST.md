# Checklist bàn giao và demo

## Trước khi mở website
- [ ] Sao lưu thư mục dự án và database.
- [ ] `dotnet restore` thành công.
- [ ] `dotnet build` thành công, xem và xử lý mọi cảnh báo bảo mật.
- [ ] Website mở ở URL đúng, không chạy nhầm công cụ tiện ích.
- [ ] Đã xác nhận môi trường Development khi dùng dữ liệu mẫu.

## Kịch bản kiểm thử thực tế
- [ ] Admin đăng nhập, quản lý tài khoản/cài đặt/nhật ký.
- [ ] Thủ thư tìm sách, lập phiếu, trả một cuốn và trả toàn bộ.
- [ ] Tồn kho không âm, không vượt tổng; không thể trả trùng.
- [ ] Sách có hạn trả hôm nay không quá hạn; từ ngày lịch tiếp theo mới quá hạn.
- [ ] Gia hạn phiếu đủ điều kiện cộng số ngày cấu hình và tạo lịch sử/phí riêng.
- [ ] Gia hạn thất bại không thêm lịch sử phí.
- [ ] Độc giả chỉ xem phiếu và đặt trước của mình.
- [ ] Đặt trước sách hết bản, queue xử lý đúng thứ tự, Ready có hạn nhận 3 ngày.
- [ ] Hết hạn nhận thì chuyển lượt cho người tiếp theo khi hàng chờ được làm mới.
- [ ] API `/api/books` trả dữ liệu; endpoint staff từ chối thiếu/sai JWT; JWT đúng vai trò cho phép.
- [ ] Việt/Anh hoạt động trên trang chủ, danh mục và đặt trước; kiểm tra những màn hình chưa dịch hết.
- [ ] Báo cáo/CSV được đối chiếu với bản ghi thực, không điền số liệu giả.

## Chưa được coi là hoàn chỉnh nếu chưa cấu hình thật
- Email quên mật khẩu (cần SMTP/provider và credentials).
- Thanh toán/thu tiền online (chưa có cổng thanh toán; phí hiện chỉ ghi nhận phát sinh).
- TLS/chứng chỉ trên máy chủ production.
- Integration tests với SQL Server/test database và kiểm tra tải.
