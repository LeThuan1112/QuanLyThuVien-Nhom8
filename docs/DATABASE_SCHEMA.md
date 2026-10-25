# Lược đồ dữ liệu — theo AppDbContext và Models

Tài liệu này được tổng hợp tĩnh từ `Data/AppDbContext.cs`, các entity trong `Models/` và `Data/DbBootstrap.cs`. Schema thực tế trong database có thể khác nếu database đã tồn tại từ phiên bản cũ; cần kiểm tra schema thật trên SSMS trước khi khẳng định.

## 1. Các bảng / entity

| Entity / bảng | Khóa chính | Trường dữ liệu đáng chú ý | Vai trò |
|---|---|---|---|
| `Users` | `UserId` | `Username`, `PasswordHash`, `FullName`, `Email`, `Role`, `IsActive`, `CreatedAt` | Tài khoản và vai trò đăng nhập. `Username` có unique index. Mật khẩu lưu dạng hash. |
| `Categories` | `CategoryId` | `Name`, `Description` | Thể loại sách. `Name` có unique index. |
| `Books` | `BookId` | `BookCode`, `Title`, `ISBN`, `CategoryId`, `Author`, `Publisher`, `PublishYear`, `TotalCopies`, `AvailableCopies`, `ShelfLocation`, `CoverImageUrl`, `Description`, `IsActive`, `RowVersion` | Thông tin đầu sách và số bản. `BookCode` unique; có index `Title`; có check constraint về tồn kho. |
| `Readers` | `ReaderId` | `ReaderCode`, `FullName`, `Phone`, `Email`, `Address`, `CardIssuedDate`, `CardExpiryDate`, `IsActive`, `UserId` | Hồ sơ độc giả và thời hạn thẻ. `ReaderCode` unique; `UserId` có unique filtered index khi khác null. |
| `Loans` | `LoanId` | `ReaderId`, `CreatedByUserId`, `LoanDate`, `DueDate`, `Note`, `RenewCount` | Phiếu mượn thuộc một độc giả và được tạo bởi tài khoản nhân viên. |
| `LoanDetails` | `LoanDetailId` | `LoanId`, `BookId`, `ReturnDate`, `FineAmount`, `ReceivedByUserId` | Chi tiết từng đầu sách trên phiếu; `ReturnDate = NULL` thể hiện chưa trả. Có unique index `(LoanId, BookId)`. |
| `LoanRenewals` | `LoanRenewalId` | `LoanId`, `RenewedAt`, `RenewDays`, `RenewCount`, `OutstandingCopies`, `FeeAmount`, `RenewedByUserId` | Lịch sử gia hạn và phí gia hạn phát sinh. Unique index `(LoanId, RenewCount)`. |
| `BookReservations` | `BookReservationId` | `BookId`, `ReaderId`, `RequestedAt`, `Status`, `ReadyAt`, `ExpiresAt`, `FulfilledAt`, `CancelledAt`, `FulfilledLoanId` | Hàng chờ đặt trước sách và thời hạn giữ chỗ. `FulfilledLoanId` là trường mã phiếu trong entity hiện tại; chưa được khai báo quan hệ FK bằng Fluent API. |
| `AuditLogs` | `AuditLogId` | `CreatedAt`, `UserId`, `Username`, `Role`, `Action`, `Detail`, `Success`, `IpAddress` | Nhật ký hoạt động và kết quả thao tác. |
| `Settings` | `SettingKey` | `SettingValue` | Các quy tắc hệ thống dạng khóa/giá trị. |

## 2. Quan hệ chính

- `Categories (1) -> (n) Books` qua `Books.CategoryId`.
- `Readers (1) -> (n) Loans` qua `Loans.ReaderId`.
- `Users (1) -> (n) Loans` qua `Loans.CreatedByUserId` (nhân viên lập phiếu).
- `Loans (1) -> (n) LoanDetails` qua `LoanDetails.LoanId`.
- `Books (1) -> (n) LoanDetails` qua `LoanDetails.BookId`.
- `Users (1) -> (n) LoanDetails` qua `LoanDetails.ReceivedByUserId` (có thể null).
- `Loans (1) -> (n) LoanRenewals` qua `LoanRenewals.LoanId`.
- `Users (1) -> (n) LoanRenewals` qua `LoanRenewals.RenewedByUserId`.
- `Books (1) -> (n) BookReservations` qua `BookReservations.BookId`.
- `Readers (1) -> (n) BookReservations` qua `BookReservations.ReaderId`.
- `Readers.UserId` liên kết hồ sơ độc giả với tài khoản đăng nhập; unique filtered index bảo đảm một tài khoản chỉ gắn tối đa một hồ sơ trong database được khởi tạo theo model.

## 3. Ràng buộc và hành vi dữ liệu

- Mặc định, quan hệ được cấu hình `DeleteBehavior.Restrict`; các quan hệ chi tiết phiếu có thiết lập cụ thể: `LoanDetails -> Loan` cascade, còn `LoanDetails -> Book` restrict.
- Cấu hình model có chỉ mục duy nhất cho `Users.Username`, `Categories.Name`, `Books.BookCode`, `Readers.ReaderCode`, `Readers.UserId` (filtered), `LoanDetails.(LoanId, BookId)` và `LoanRenewals.(LoanId, RenewCount)`.
- `Books` có check constraint `AvailableCopies >= 0 AND AvailableCopies <= TotalCopies`.
- `DbBootstrap.EnsureExtras` tạo/ bổ sung các bảng `AuditLogs`, `Settings`, `LoanRenewals`, `BookReservations` và các cột `Loans.RenewCount`, `LoanDetails.FineAmount`, `Books.CoverImageUrl` nếu cần.
- Script bootstrap không thay thế migration. Với database đang sử dụng, hãy backup trước khi khởi động phiên bản này; so sánh schema trên SSMS với entity và bootstrap.

## 4. Sơ đồ quan hệ dạng văn bản

```text
Categories ──< Books ──< LoanDetails >── Loans >── Readers ──(0..1)── Users
                         │                 │                       │
                         │                 └──< LoanRenewals >──────┘
                         │
                         └── BookReservations >── Readers
Users ──< LoanDetails (ReceivedByUserId)
Users ──< Loans (CreatedByUserId)
Users ──< LoanRenewals (RenewedByUserId)
Settings (key/value)
AuditLogs (activity records)
```

Sơ đồ khái quát; liên kết `BookReservations.FulfilledLoanId` hiện được lưu bằng trường số nguyên nhưng không có FK/navigation được khai báo trong `AppDbContext`.
