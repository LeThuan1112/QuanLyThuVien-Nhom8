# Hệ thống quản lý thư viện — ASP.NET Core MVC / .NET 10

## Tài khoản demo chạy thử

Các tài khoản dưới đây được cung cấp cho mục đích trình diễn bài tập lớn:

| Vai trò | Tên đăng nhập | Mật khẩu demo |
|---|---|---|
| Quản trị viên (Admin) | `Admin2` | `admin123` |
| Thủ thư | `thuthu2` | `thuthu123` |
| Độc giả | `docgia2` | `docgia123` |

**Lưu ý:** Các tài khoản trên cần tồn tại trong database của phiên bản bàn giao để đăng nhập được. Nếu tài khoản không đăng nhập được trên máy mới, hãy kiểm tra dữ liệu khởi tạo người dùng và database đang được ứng dụng sử dụng. Đây là tài khoản demo cho bài tập, không dùng lại các mật khẩu này cho email hoặc dịch vụ cá nhân. Do repository đang công khai, bất kỳ ai xem README cũng có thể đọc được các thông tin demo ở đây.

Bài tập lớn học phần **Lập trình Web**, **Nhóm 8**, lớp **N07.BT1**, Khoa Công nghệ thông tin, Trường Đại học Giao thông vận tải.

**Giảng viên hướng dẫn:** Lại Mạnh Dũng.

## 1. Giới thiệu đề tài

Hệ thống quản lý thư viện là ứng dụng web hỗ trợ quản lý sách, thể loại, tác giả, nhà xuất bản, độc giả và các nghiệp vụ mượn — trả sách. Ứng dụng được xây dựng bằng C# và ASP.NET Core MVC, sử dụng Entity Framework Core để làm việc với cơ sở dữ liệu SQL Server/LocalDB. Hệ thống có các vai trò Admin, Thủ thư và Độc giả; mỗi vai trò được phép sử dụng các chức năng phù hợp với quyền được cấp.

## 2. Thành viên và phân công

| Thành viên | Mã sinh viên | Phần việc phụ trách |
|---|---|---|
| **Lê Văn Thuận (Trưởng nhóm)** | **V624022** | Phụ trách chính: phân tích yêu cầu, thiết kế cơ sở dữ liệu, phát triển Backend và Frontend, tích hợp chức năng, kiểm thử, chụp ảnh giao diện, tổng hợp và hoàn thiện báo cáo. |
| Trịnh Ngọc Huy | V624012 | Đọc, xem xét mã nguồn và báo cáo sau khi trưởng nhóm hoàn thành; tìm hiểu nội dung và kết quả của hệ thống. |
| Nguyễn Trần Ngọc Minh | V624018 | Đọc, xem xét nội dung báo cáo sau khi trưởng nhóm hoàn thành; tìm hiểu kết quả thực hiện và cách trình bày. |
| Bùi Thành Long | V624016 | Trải nghiệm và kiểm thử giao diện ứng dụng web; rà soát lỗi hiển thị, phông chữ, chính tả và bố cục; ghi nhận lỗi và đóng góp ý kiến để hoàn thiện báo cáo. |

## 3. Chức năng

### 3.1. Tra cứu và quản lý dữ liệu

- Tra cứu, tìm kiếm và xem thông tin sách.
- Quản lý sách: xem, thêm, sửa và cập nhật thông tin theo chức năng của ứng dụng.
- Quản lý thể loại sách.
- Quản lý thông tin độc giả.
- Hỗ trợ cấp mã sách và mã độc giả tự động theo quy tắc trong ứng dụng.

### 3.2. Đăng nhập và phân quyền

- Đăng nhập và đăng xuất.
- Phân quyền theo các vai trò **Admin**, **Thủ thư** và **Độc giả**.
- Điều hướng giao diện và giới hạn chức năng theo vai trò người dùng.
- Kiểm soát quyền truy cập vào các khu vực chức năng.

### 3.3. Nghiệp vụ mượn — trả

- Lập phiếu mượn và theo dõi tình trạng mượn sách.
- Xử lý trả sách và cập nhật số lượng sách còn có thể cho mượn.
- Theo dõi sách quá hạn và phí phạt.
- Gia hạn mượn sách và lưu lịch sử gia hạn.
- Đặt trước sách và xử lý hàng chờ.
- Xem lịch sử mượn — trả theo phạm vi quyền được cấp.

### 3.4. Thống kê và quản trị

- Dashboard tổng hợp một số số liệu hoạt động của thư viện.
- Xuất báo cáo CSV.
- Quản lý thiết lập động của ứng dụng.
- Ghi nhật ký hoạt động (Audit Logs) phục vụ theo dõi và rà soát.
- Có các API phục vụ những thao tác đã được triển khai trong dự án.

## 4. Công nghệ sử dụng

- **Ngôn ngữ:** C#.
- **Nền tảng:** ASP.NET Core MVC, .NET 10.
- **Truy cập dữ liệu:** Entity Framework Core.
- **Cơ sở dữ liệu:** SQL Server LocalDB.
- **Giao diện:** Razor, HTML5, CSS3, Bootstrap, JavaScript và jQuery.
- **Kiểm thử:** .NET test project.

## 5. Cấu trúc mã nguồn

```text
ThuVien/
├── Api/                 # Các kiểu dữ liệu phản hồi API
├── Controllers/         # Controller cho các trang và nghiệp vụ
│   └── Api/             # API controller
├── Data/                # DbContext, khởi tạo và bootstrap dữ liệu
├── Database/            # Script cơ sở dữ liệu
├── Models/              # Entity và các kiểu dữ liệu miền
├── Properties/          # Cấu hình chạy local
├── Resources/           # Tài nguyên ngôn ngữ
├── Services/            # Dịch vụ nghiệp vụ
├── ViewModels/          # ViewModel cho giao diện
├── Views/               # Các trang Razor
├── docs/                # Tài liệu kỹ thuật và kiểm thử
├── sql/                 # Script SQL tùy chọn
├── tests/               # Unit test
├── wwwroot/             # CSS, JavaScript và tài nguyên tĩnh
├── appsettings.json     # Cấu hình ứng dụng
├── Program.cs           # Điểm khởi chạy ứng dụng
├── ThuVien.csproj       # File dự án
└── README.md            # Tài liệu này
```

## 6. Yêu cầu môi trường

- Windows 10/11.
- **.NET 10 SDK**.
- **Visual Studio 2022 hoặc phiên bản hỗ trợ .NET 10**, có workload **ASP.NET and web development**.
- **SQL Server LocalDB**. Có thể sử dụng SSMS để xem và quản lý database; SSMS là công cụ hỗ trợ, không phải điều kiện bắt buộc để mở mã nguồn.
- Kết nối mạng khi cần khôi phục các gói NuGet hoặc tải tài nguyên bên ngoài.

## 7. Cách tải, mở và chạy ứng dụng bằng Visual Studio

### Bước 1. Tải và giải nén mã nguồn

Tải mã nguồn ZIP từ GitHub hoặc tải gói mã nguồn do nhóm cung cấp. Giải nén vào một thư mục dễ tìm, ví dụ `Downloads` hoặc `Documents`. Không chạy dự án trực tiếp bên trong file ZIP.

### Bước 2. Mở dự án trong Visual Studio

1. Mở **Visual Studio**.
2. Ở màn hình bắt đầu, nhấn **Open a project or solution**.
3. Duyệt đến thư mục vừa giải nén.
4. Mở thư mục con có tên **`ThuVien`** nếu mã nguồn được đóng gói theo cấu trúc có thư mục con này.
5. Chọn file **`ThuVien.csproj`**, sau đó nhấn **Open**.
6. Chờ Visual Studio tải dự án và khôi phục các gói NuGet. Nếu được yêu cầu, xác nhận khôi phục gói.

### Bước 3. Kiểm tra cấu hình database

Mở file `appsettings.json` trong dự án và kiểm tra phần `ConnectionStrings:DefaultConnection`. Cấu hình LocalDB đang được sử dụng trong môi trường phát triển là:

```text
Server=(localdb)\MSSQLLocalDB;Database=ThuVienDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Chuỗi kết nối này sử dụng SQL Server LocalDB trên chính máy đang chạy ứng dụng, với database tên `ThuVienDb`. Database trên máy phát triển **không tự động được đóng gói cùng mã nguồn GitHub**. Nếu đây là máy mới hoặc database chưa tồn tại, làm theo script/hướng dẫn khởi tạo database đi kèm mã nguồn. Tránh chạy các lệnh xóa hoặc ghi đè dữ liệu khi chưa sao lưu.

### Bước 4. Khôi phục gói và build dự án

Visual Studio thường tự khôi phục gói NuGet khi mở dự án. Nếu cần kiểm tra thủ công, mở Terminal/PowerShell tại thư mục chứa `ThuVien.csproj` và chạy:

```powershell
dotnet restore
dotnet build .\ThuVien.csproj
```

Chỉ tiếp tục khi quá trình build kết thúc thành công.

### Bước 5. Chạy ứng dụng

Trong Visual Studio, đặt `ThuVien` làm Startup Project nếu được yêu cầu. Sau đó:

- Nhấn **F5** để chạy kèm trình gỡ lỗi; hoặc
- Nhấn **Ctrl + F5** để chạy không kèm trình gỡ lỗi.

Trình duyệt sẽ mở ứng dụng. Trong cấu hình đã dùng khi phát triển, các địa chỉ thường xuất hiện là:

- `https://localhost:7080`
- `http://localhost:5080`

Nếu máy của bạn hiển thị cổng khác, hãy sử dụng URL được in trong cửa sổ chạy ứng dụng hoặc tại dòng `Now listening on`.

### Bước 6. Đăng nhập

Mở trang đăng nhập của website và sử dụng một trong các tài khoản chạy thử ở mục tiếp theo. Sau khi đăng nhập, kiểm tra các trang và chức năng theo vai trò tài khoản.


## 8. Kiểm thử

Trong lần kiểm tra đã ghi nhận, dự án build thành công và bộ unit test có **22/22 trường hợp thành công**, không có trường hợp thất bại hoặc bị bỏ qua.

Có thể chạy bộ unit test bằng lệnh sau tại thư mục dự án:

```powershell
dotnet test .\tests\ThuVien.Tests\ThuVien.Tests.csproj
```

Unit test không thay thế việc kiểm thử thủ công toàn bộ ứng dụng. Trước khi bàn giao, nên kiểm tra các luồng chính như đăng nhập, phân quyền, tra cứu sách, quản lý độc giả, mượn — trả, gia hạn, đặt trước, phí phạt và xuất CSV. Chỉ xác nhận một ca kiểm thử đạt khi đã thực sự thực hiện và ghi nhận kết quả.

## 9. Phạm vi và giới hạn

Phiên bản tập trung vào quản lý sách, thể loại, độc giả và các nghiệp vụ mượn — trả, gia hạn, đặt trước, phân quyền, thống kê và nhật ký hoạt động. Khả năng vận hành trên máy khác phụ thuộc vào việc cài đúng .NET 10, cấu hình chuỗi kết nối và có database phù hợp. Một số cảnh báo phục vụ môi trường Development có thể xuất hiện khi chạy local; cần rà soát cấu hình bảo mật trước khi triển khai thực tế.

## 10. Quản lý công việc

Nhóm sử dụng Trello để tổng hợp và theo dõi các đầu việc của bài tập lớn. Bảng gồm các cột **Cần Làm**, **Đang Thực Hiện**, **Kiểm Thử** và **Hoàn Thành**; trạng thái thẻ cần phản ánh tiến độ thực tế.

**Link Trello:** https://trello.com/b/BoHNkTaI/btl-lap-trinh-web-quan-ly-thu-vien-nhom-8

## 11. Ghi chú bảo mật và bàn giao

- Không đưa mật khẩu cá nhân, secret, token hoặc thông tin kết nối riêng tư lên repository công khai.
- Các tài khoản demo trong README chỉ phục vụ trình diễn bài tập lớn; không dùng mật khẩu này trên dịch vụ khác.
- Kiểm tra `appsettings.json`, file hướng dẫn và script SQL trước khi công khai repository.
- Không đưa các thư mục build (`bin/`, `obj/`, `.vs/`, `TestResults/`) hoặc file database backup `.bak` lên GitHub nếu không có yêu cầu rõ ràng.
- Cơ sở dữ liệu LocalDB lưu trên máy chạy ứng dụng và không tự động đi kèm repository; nếu cần chuyển dữ liệu mẫu, cung cấp backup hoặc script khởi tạo phù hợp qua kênh được giảng viên chấp nhận.
