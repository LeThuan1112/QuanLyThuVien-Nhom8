# Checklist ảnh chụp màn hình thật để đưa vào báo cáo

**Nguyên tắc:** chỉ chụp trực tiếp từ ứng dụng đang chạy. Không dựng hình bằng công cụ vẽ, không thay dữ liệu trên ảnh, không đưa mockup vào báo cáo với chú thích như thể là giao diện thật. Ẩn thông tin cá nhân/secret trước khi chia sẻ.

| # | Tên file gợi ý | Màn hình / trạng thái cần chụp | Nội dung phải nhìn thấy | Mục báo cáo phù hợp |
|---|---|---|---|---|
| 01 | `01_dang_nhap.png` | Trang đăng nhập | Tên ứng dụng, ô tên đăng nhập/mật khẩu, nút đăng nhập | Giới thiệu giao diện, xác thực |
| 02 | `02_trang_chu_cong_khai.png` | Trang chủ công khai | Thanh điều hướng và khối giới thiệu/tra cứu sách | Giao diện người dùng |
| 03 | `03_danh_muc_sach.png` | Danh mục sách có kết quả | Ảnh bìa thật nếu tải được, mã/tên sách, tác giả, thể loại và tìm kiếm/lọc | Chức năng tra cứu sách |
| 04 | `04_chi_tiet_sach.png` | Chi tiết một sách | Thông tin sách, tồn kho/trạng thái và ảnh bìa nếu có | Thiết kế và tra cứu dữ liệu |
| 05 | `05_dashboard_nhan_vien.png` | Dashboard Admin/Thủ thư | KPI/biểu đồ được tính từ database đang dùng | Dashboard, thống kê |
| 06 | `06_quan_ly_sach.png` | Danh sách sách ở khu vực quản trị | Bảng, tìm kiếm/lọc, thao tác quản lý và phân trang nếu có | Quản lý sách |
| 07 | `07_them_sach.png` | Form thêm hoặc sửa sách | Nhãn trường, trường bắt buộc, thể loại, số bản, validation nếu có | Biểu mẫu sách |
| 08 | `08_quan_ly_the_loai.png` | Danh sách thể loại | Tên thể loại và các thao tác CRUD | Quản lý thể loại |
| 09 | `09_danh_sach_doc_gia.png` | Danh sách độc giả | Mã, tên, trạng thái thẻ, tìm kiếm/lọc | Quản lý độc giả |
| 10 | `10_them_doc_gia.png` | Form thêm/sửa độc giả | Họ tên, mã, điện thoại/email, ngày cấp/hết hạn, validation | Biểu mẫu độc giả |
| 11 | `11_quan_ly_tai_khoan.png` | Quản lý tài khoản, chụp bằng Admin | Username, vai trò, trạng thái; tránh để lộ dữ liệu riêng tư không cần thiết | Tài khoản và phân quyền |
| 12 | `12_lap_phieu_muon.png` | Form lập phiếu mượn, trước khi gửi | Độc giả được chọn, danh sách sách, ghi chú nếu có | Quy trình mượn sách |
| 13 | `13_danh_sach_phieu_muon.png` | Danh sách phiếu mượn | Mã phiếu, độc giả, ngày mượn/hạn trả, trạng thái | Quản lý mượn trả |
| 14 | `14_chi_tiet_phieu_muon.png` | Chi tiết phiếu có tối thiểu một cuốn chưa trả | Danh sách sách, hạn trả, thao tác trả/gia hạn phù hợp | Chi tiết nghiệp vụ |
| 15 | `15_lich_su_gia_han.png` | Lịch sử gia hạn, nếu được hiển thị | Lần gia hạn, số ngày, số cuốn còn mượn và phí phát sinh | Gia hạn và lịch sử |
| 16 | `16_danh_sach_qua_han.png` | Trang quá hạn với bản ghi hợp lệ | Độc giả, sách, hạn trả và số ngày quá hạn | Quản lý quá hạn |
| 17 | `17_dat_truoc_cua_doc_gia.png` | Khu vực đặt trước của Reader đã đăng nhập | Trạng thái hàng chờ/thời hạn nhận nếu đang Ready | Đặt trước sách |
| 18 | `18_bao_cao_thong_ke.png` | Báo cáo với khoảng ngày có dữ liệu | Bộ lọc ngày, KPI/bảng tổng hợp | Báo cáo thống kê |
| 19 | `19_xuat_csv.png` | Chụp thao tác export hoặc mở CSV trong Excel | Nút xuất và/hoặc dữ liệu CSV mẫu, cần che dữ liệu cá nhân | Xuất dữ liệu |
| 20 | `20_cai_dat_he_thong.png` | Cài đặt với Admin | Số ngày mượn, giới hạn sách, phí phạt/gia hạn | Cấu hình quy tắc |
| 21 | `21_nhat_ky_hoat_dong.png` | Audit log với Admin | Thời điểm, hành động, kết quả; che IP nếu cần | Nhật ký hoạt động |
| 22 | `22_api_books.png` | Kết quả GET `/api/books` trên browser/Postman | HTTP status và JSON thực tế; không chụp token/secret | API REST |
| 23 | `23_api_token_protected.png` | Kết quả API token hợp lệ/sai role (nên che token) | Status code và phần phản hồi đại diện | Kiểm thử bảo mật API |

## Cách đặt và chọn ảnh

1. Tạo thư mục `docs/screenshots/` hoặc thư mục `screenshots/` ở gốc dự án.
2. Chỉ giữ các ảnh phù hợp với chức năng thực sự tồn tại trong bản cuối; những màn hình không có thì bỏ dòng tương ứng.
3. Chụp toàn bộ cửa sổ trình duyệt ở kích thước dễ đọc; không cắt mất tiêu đề, thanh điều hướng hoặc thông tin chứng minh đang ở trang nào.
4. Không cần đưa cả 23 ảnh vào báo cáo. Chọn khoảng 10–16 ảnh rõ nét, mỗi ảnh hỗ trợ trực tiếp cho phần mô tả chức năng.
5. Trước khi chụp báo cáo, kiểm tra dữ liệu là dữ liệu demo rõ ràng, nhất quán và không có thông tin cá nhân thật.
