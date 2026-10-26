using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using ThuVien.Models;

namespace ThuVien.Data;

// Bổ sung bảng / cột mới vào CSDL đã tồn tại (EnsureCreated không tự thêm vào CSDL cũ)
public static class DbBootstrap
{
    public static void EnsureExtras(AppDbContext db, IWebHostEnvironment environment)
    {
        db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[AuditLogs]', N'U') IS NULL
CREATE TABLE [AuditLogs] (
    [AuditLogId] int IDENTITY(1,1) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UserId] int NULL,
    [Username] nvarchar(50) NULL,
    [Role] nvarchar(20) NULL,
    [Action] nvarchar(100) NOT NULL,
    [Detail] nvarchar(500) NULL,
    [Success] bit NOT NULL,
    [IpAddress] nvarchar(45) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
);");

        db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[Settings]', N'U') IS NULL
CREATE TABLE [Settings] (
    [SettingKey] nvarchar(50) NOT NULL,
    [SettingValue] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Settings] PRIMARY KEY ([SettingKey])
);");

        // Nhật ký gia hạn: lưu ngày gia hạn, số cuốn còn mượn và phí phát sinh.
        // Bảng được thêm theo cách tương thích, không cần xóa hoặc tạo lại database.
        db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[LoanRenewals]', N'U') IS NULL
BEGIN
    CREATE TABLE [LoanRenewals] (
        [LoanRenewalId] int IDENTITY(1,1) NOT NULL,
        [LoanId] int NOT NULL,
        [RenewedAt] datetime2 NOT NULL,
        [RenewDays] int NOT NULL,
        [RenewCount] int NOT NULL,
        [OutstandingCopies] int NOT NULL,
        [FeeAmount] decimal(18,2) NOT NULL,
        [RenewedByUserId] int NOT NULL,
        CONSTRAINT [PK_LoanRenewals] PRIMARY KEY ([LoanRenewalId]),
        CONSTRAINT [FK_LoanRenewals_Loans_LoanId] FOREIGN KEY ([LoanId]) REFERENCES [Loans] ([LoanId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LoanRenewals_Users_RenewedByUserId] FOREIGN KEY ([RenewedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LoanRenewals_RenewedAt' AND object_id = OBJECT_ID(N'[LoanRenewals]'))
    CREATE INDEX [IX_LoanRenewals_RenewedAt] ON [LoanRenewals] ([RenewedAt]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LoanRenewals_LoanId_RenewCount' AND object_id = OBJECT_ID(N'[LoanRenewals]'))
    CREATE UNIQUE INDEX [IX_LoanRenewals_LoanId_RenewCount] ON [LoanRenewals] ([LoanId], [RenewCount]);");

        // Gia hạn: số lần đã gia hạn của phiếu mượn
        db.Database.ExecuteSqlRaw(@"
IF COL_LENGTH(N'Loans', N'RenewCount') IS NULL
ALTER TABLE [Loans] ADD [RenewCount] int NOT NULL CONSTRAINT [DF_Loans_RenewCount] DEFAULT 0;");

        // Phí phạt quá hạn của từng cuốn
        db.Database.ExecuteSqlRaw(@"
IF COL_LENGTH(N'LoanDetails', N'FineAmount') IS NULL
ALTER TABLE [LoanDetails] ADD [FineAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_LoanDetails_FineAmount] DEFAULT 0;");

        // Ảnh bìa là trường bổ sung tùy chọn, tương thích database đã tồn tại.
        db.Database.ExecuteSqlRaw(@"
IF COL_LENGTH(N'Books', N'CoverImageUrl') IS NULL
    ALTER TABLE [Books] ADD [CoverImageUrl] nvarchar(500) NULL;");

        // Hàng chờ đặt trước sách; giữ lịch sử, không xóa bản ghi khi sách được mượn.
        db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[BookReservations]', N'U') IS NULL
BEGIN
    CREATE TABLE [BookReservations] (
        [BookReservationId] int IDENTITY(1,1) NOT NULL,
        [BookId] int NOT NULL,
        [ReaderId] int NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ReadyAt] datetime2 NULL,
        [ExpiresAt] datetime2 NULL,
        [FulfilledAt] datetime2 NULL,
        [CancelledAt] datetime2 NULL,
        [FulfilledLoanId] int NULL,
        CONSTRAINT [PK_BookReservations] PRIMARY KEY ([BookReservationId]),
        CONSTRAINT [FK_BookReservations_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([BookId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BookReservations_Readers_ReaderId] FOREIGN KEY ([ReaderId]) REFERENCES [Readers] ([ReaderId]) ON DELETE NO ACTION
    );
END;
IF OBJECT_ID(N'[BookReservations]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BookReservations_BookId_Status_RequestedAt' AND object_id = OBJECT_ID(N'[BookReservations]'))
        CREATE INDEX [IX_BookReservations_BookId_Status_RequestedAt] ON [BookReservations] ([BookId], [Status], [RequestedAt]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BookReservations_ReaderId_Status_RequestedAt' AND object_id = OBJECT_ID(N'[BookReservations]'))
        CREATE INDEX [IX_BookReservations_ReaderId_Status_RequestedAt] ON [BookReservations] ([ReaderId], [Status], [RequestedAt]);
END;");

        // Chỉ bổ sung ảnh bìa demo trong Development; không cập nhật dữ liệu khi chạy Production.
        if (environment.IsDevelopment())
        {
            db.Database.ExecuteSqlRaw(@"
UPDATE [Books] SET [CoverImageUrl] = N'https://covers.openlibrary.org/b/isbn/9780132350884-L.jpg', [ISBN] = COALESCE(NULLIF([ISBN], N''), N'9780132350884') WHERE [BookCode] = N'S001' AND ([CoverImageUrl] IS NULL OR [CoverImageUrl] = N'');
UPDATE [Books] SET [CoverImageUrl] = N'https://covers.openlibrary.org/b/isbn/9780262033848-L.jpg', [ISBN] = COALESCE(NULLIF([ISBN], N''), N'9780262033848') WHERE [BookCode] = N'S002' AND ([CoverImageUrl] IS NULL OR [CoverImageUrl] = N'');
UPDATE [Books] SET [CoverImageUrl] = N'https://covers.openlibrary.org/b/isbn/9780062316097-L.jpg' WHERE [BookCode] = N'S005' AND ([CoverImageUrl] IS NULL OR [CoverImageUrl] = N'');");
        }

        // Tạo unique filtered index nếu database cũ không có liên kết bị trùng.
        // Nếu dữ liệu lịch sử đang trùng, không làm ứng dụng khởi động thất bại; quản trị viên có thể xử lý riêng.
        db.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Readers_UserId' AND object_id = OBJECT_ID(N'[Readers]'))
AND NOT EXISTS (SELECT [UserId] FROM [Readers] WHERE [UserId] IS NOT NULL GROUP BY [UserId] HAVING COUNT(*) > 1)
    CREATE UNIQUE INDEX [IX_Readers_UserId] ON [Readers] ([UserId]) WHERE [UserId] IS NOT NULL;");

        var existing = db.Settings.Select(s => s.SettingKey).ToHashSet();
        void Ensure(string key, string value)
        {
            if (!existing.Contains(key))
                db.Settings.Add(new Setting { SettingKey = key, SettingValue = value });
        }

        Ensure("LoanDays", "14");
        Ensure("MaxBooksPerLoan", "5");
        Ensure("MaxBooksAtOnce", "5");
        Ensure("FinePerDay", "1000");
        Ensure("RenewalFeePerBook", "2000");
        Ensure("MaxRenewals", "2");
        Ensure("RenewDays", "7");
        db.SaveChanges();
    }
}
