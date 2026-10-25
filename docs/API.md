# Tài liệu Web API (RESTful)

Base URL khi chạy Development: `https://localhost:7080` (xem `Properties/launchSettings.json` để xác nhận cổng thực tế).
Phản hồi API được bọc theo cấu trúc `{ "success": bool, "data": ..., "error": string|null }`.

| Method | Route | Mô tả | Quyền | Mã trạng thái |
|---|---|---|---|---|
| POST | `/api/auth/token` | Đăng nhập API và nhận JWT Bearer | Công khai, giới hạn tần suất theo IP | 200, 400, 401, 429 |
| GET | `/api/books?keyword=&categoryId=&page=1&pageSize=10` | Tra cứu sách, phân trang | Công khai | 200 |
| GET | `/api/books/{id}` | Chi tiết sách | Công khai với sách đang lưu hành | 200, 404 |
| POST | `/api/books` | Thêm sách | JWT, Admin/Thủ thư | 201, 400, 401, 403, 409 |
| GET | `/api/loans/overdue` | Danh sách sách quá hạn | JWT, Admin/Thủ thư | 200, 401, 403 |

## Lấy token

POST `/api/auth/token` với body JSON:

```json
{ "username": "admin", "password": "<mat-khau-cua-ban>" }
```

Phản hồi thành công chứa `data.accessToken`, `data.tokenType` (`Bearer`) và `data.expiresIn`. Không lưu token vào mã nguồn hay commit lên git. Token có thời hạn; khóa ký JWT phải được cấu hình ổn định bằng biến môi trường `JWT_SIGNING_KEY` (tối thiểu 32 byte) hoặc `Authentication:Jwt:SigningKey` ở môi trường triển khai. Development có thể tự tạo khóa tạm và token sẽ hết hiệu lực khi ứng dụng khởi động lại.

## Gọi endpoint có JWT

Gửi header `Authorization: Bearer <accessToken>`.

Ví dụ GET công khai:

- `/api/books`
- `/api/books?keyword=clean`
- `/api/books/1`

Ví dụ POST thêm sách trong PowerShell (thay token và `categoryId` hợp lệ):

```powershell
$token = "<accessToken>"
$body = @{
  bookCode = "API001"
  title = "Sach them qua API"
  author = "Tac gia mau"
  categoryId = 1
  totalCopies = 3
  shelfLocation = "Z1-01"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7080/api/books" -Method Post `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" -Body $body
```

Lưu ý: chứng chỉ HTTPS local có thể cần tin cậy trong Windows. Không tắt kiểm tra TLS trong môi trường triển khai.

## Bảo mật và giới hạn

- MVC dùng cookie `HttpOnly`, `SameSite=Lax`, cùng kiểm tra tài khoản còn hoạt động ở mỗi request; các form POST dùng Anti-Forgery Token.
- API ghi dữ liệu và API quá hạn yêu cầu JWT Bearer, vai trò Admin/Thủ thư.
- API token và đăng nhập MVC có giới hạn tần suất theo IP; đăng nhập MVC còn có cơ chế đếm sai dựa trên nhật ký.
- API không bật CORS toàn cục; khi tích hợp client ngoài, hãy cấu hình origin cụ thể thay vì `*`.
- Dữ liệu phí gia hạn là phí phát sinh, chưa được đồng nghĩa với đã thanh toán. Chưa có cổng thanh toán thật.
