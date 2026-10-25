using Microsoft.AspNetCore.Identity;
using ThuVien.Models;

namespace ThuVien.Data;

// DỮ LIỆU MẪU - chỉ dùng cho môi trường demo
public static class DbSeeder
{
    public static void Seed(AppDbContext db, IPasswordHasher<User> hasher)
    {
        if (db.Users.Any()) return;

        User MakeUser(string username, string password, string fullName, string role)
        {
            var user = new User
            {
                Username = username,
                FullName = fullName,
                Role = role,
                Email = username + "@demo.local"
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            return user;
        }

        var admin = MakeUser("admin", "Admin@123", "Quản trị viên (demo)", AppRoles.Admin);
        var librarian = MakeUser("thuthu", "Thuthu@123", "Thủ thư (demo)", AppRoles.Librarian);
        var readerUser = MakeUser("docgia", "Docgia@123", "Độc giả (demo)", AppRoles.Reader);
        db.Users.AddRange(admin, librarian, readerUser);

        var categories = new[]
        {
            new Category { Name = "Công nghệ thông tin", Description = "Lập trình, thuật toán, cơ sở dữ liệu" },
            new Category { Name = "Văn học Việt Nam", Description = "Tiểu thuyết, truyện ngắn trong nước" },
            new Category { Name = "Khoa học - Lịch sử", Description = "Sách tham khảo, phổ biến kiến thức" },
            new Category { Name = "Thiếu nhi", Description = "Sách dành cho thiếu nhi" }
        };
        db.Categories.AddRange(categories);
        db.SaveChanges();

        db.Books.AddRange(
            new Book { BookCode = "S001", Title = "Clean Code", Author = "Robert C. Martin", Publisher = "Prentice Hall", PublishYear = 2008, ISBN = "9780132350884", CoverImageUrl = "https://covers.openlibrary.org/b/isbn/9780132350884-L.jpg", CategoryId = categories[0].CategoryId, TotalCopies = 5, AvailableCopies = 5, ShelfLocation = "A1-01", Description = "Dữ liệu mẫu" },
            new Book { BookCode = "S002", Title = "Introduction to Algorithms", Author = "Cormen, Leiserson, Rivest, Stein", Publisher = "MIT Press", PublishYear = 2009, ISBN = "9780262033848", CoverImageUrl = "https://covers.openlibrary.org/b/isbn/9780262033848-L.jpg", CategoryId = categories[0].CategoryId, TotalCopies = 3, AvailableCopies = 3, ShelfLocation = "A1-02", Description = "Dữ liệu mẫu" },
            new Book { BookCode = "S003", Title = "Số đỏ", Author = "Vũ Trọng Phụng", Publisher = "NXB Văn học", PublishYear = 2015, CategoryId = categories[1].CategoryId, TotalCopies = 4, AvailableCopies = 4, ShelfLocation = "B2-01", Description = "Dữ liệu mẫu" },
            new Book { BookCode = "S004", Title = "Dế Mèn phiêu lưu ký", Author = "Tô Hoài", Publisher = "NXB Kim Đồng", PublishYear = 2018, CategoryId = categories[3].CategoryId, TotalCopies = 6, AvailableCopies = 6, ShelfLocation = "C1-01", Description = "Dữ liệu mẫu" },
            new Book { BookCode = "S005", Title = "Sapiens: Lược sử loài người", Author = "Yuval Noah Harari", Publisher = "NXB Tổng hợp", PublishYear = 2017, CoverImageUrl = "https://covers.openlibrary.org/b/isbn/9780062316097-L.jpg", CategoryId = categories[2].CategoryId, TotalCopies = 2, AvailableCopies = 2, ShelfLocation = "D1-01", Description = "Dữ liệu mẫu" },
            new Book { BookCode = "S006", Title = "Lập trình web với ASP.NET Core", Author = "Tác giả mẫu", Publisher = "NXB mẫu", PublishYear = 2024, CategoryId = categories[0].CategoryId, TotalCopies = 1, AvailableCopies = 1, ShelfLocation = "A2-01", Description = "Dữ liệu mẫu" }
        );

        db.Readers.AddRange(
            new Reader { ReaderCode = "DG001", FullName = "Độc giả mẫu 1", Phone = "0900000001", Email = "dg1@demo.local", Address = "Hà Nội (dữ liệu mẫu)", CardIssuedDate = DateTime.Today, CardExpiryDate = DateTime.Today.AddYears(1), UserId = readerUser.UserId },
            new Reader { ReaderCode = "DG002", FullName = "Độc giả mẫu 2", Phone = "0900000002", Email = "dg2@demo.local", Address = "Hà Nội (dữ liệu mẫu)", CardIssuedDate = DateTime.Today, CardExpiryDate = DateTime.Today.AddYears(1) }
        );

        db.SaveChanges();
    }
}
