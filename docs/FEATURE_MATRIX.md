# Ma trận chức năng và trạng thái

| Nhóm | Trạng thái mã nguồn | Cần kiểm tra khi demo |
|---|---|---|
| Trang chủ / danh mục sách | Có trang chủ công khai, ảnh bìa, lọc/tìm kiếm/sắp xếp/phân trang | Hình bìa bên ngoài có tải được trên máy demo không |
| Quản lý sách / thể loại | CRUD, validation và quy tắc mã sách | Không cho tổng bản nhỏ hơn số bản đang mượn; thử ISBN/ảnh bìa |
| Hồ sơ độc giả / tài khoản | CRUD, khóa tài khoản và liên kết ReaderId | Đăng nhập với một độc giả đã liên kết đúng |
| Phân quyền | Admin, Thủ thư, Độc giả; hạn chế server-side | Thử URL quản trị khi đăng nhập Độc giả |
| Mượn / trả | Cập nhật tồn kho theo giao dịch; chống trả trùng | Mượn 1 bản, trả, đối chiếu số bản |
| Quá hạn / gia hạn | Quá hạn từ ngày sau hạn; lưu lần gia hạn và phí riêng | Hạn hôm nay vs 00:00 ngày mai; gia hạn 1/many copies |
| Đặt trước | Hàng chờ, giữ chỗ và hạn nhận 3 ngày | Tạo hai lượt chờ cùng sách, trả sách và xem thứ tự |
| Báo cáo / CSV | Báo cáo ngày, phí và các file CSV | Đối chiếu với một số bản ghi thật trước khi nộp |
| API / JWT | API tra cứu công khai; một số endpoint staff dùng JWT | Token sai/hết hạn/không đúng vai trò phải bị từ chối |
| xUnit | Có một số unit test quy tắc thuần | Chạy `dotnet test`; không tuyên bố chưa chạy là thành công |
| Việt/Anh | Các khu vực chính dùng resource keys | Một số câu validation/quản trị còn cần dịch |
| Email reset / thanh toán | Chưa tích hợp email thật hoặc cổng thanh toán | Ghi rõ trong báo cáo, không giả lập là đã làm thật |
| HTTPS production | Có middleware redirect/HSTS; chứng chỉ máy chủ chưa cấu hình tại đây | Không triển khai public nếu chưa cấu hình TLS/secret |
