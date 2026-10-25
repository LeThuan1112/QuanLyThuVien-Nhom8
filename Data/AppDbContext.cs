using Microsoft.EntityFrameworkCore;
using ThuVien.Models;

namespace ThuVien.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Reader> Readers => Set<Reader>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanDetail> LoanDetails => Set<LoanDetail>();
    public DbSet<LoanRenewal> LoanRenewals => Set<LoanRenewal>();
    public DbSet<BookReservation> BookReservations => Set<BookReservation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mặc định không cho xóa dây chuyền (tránh xóa nhầm dữ liệu lịch sử)
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Book>(e =>
        {
            e.HasIndex(x => x.BookCode).IsUnique();
            e.HasIndex(x => x.Title);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Books_Copies",
                "[AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]"));
        });

        modelBuilder.Entity<Reader>(e =>
        {
            e.HasIndex(x => x.ReaderCode).IsUnique();
            // Một tài khoản chỉ được liên kết với tối đa một hồ sơ độc giả.
            e.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        });

        modelBuilder.Entity<Loan>(e =>
        {
            e.HasIndex(x => new { x.ReaderId, x.LoanDate });
            e.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);
        });

        modelBuilder.Entity<LoanRenewal>(e =>
        {
            e.HasIndex(x => x.RenewedAt);
            e.HasIndex(x => new { x.LoanId, x.RenewCount }).IsUnique();
            e.HasOne(x => x.Loan).WithMany(l => l.Renewals).HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RenewedByUser).WithMany().HasForeignKey(x => x.RenewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LoanDetail>(e =>
        {
            e.HasIndex(x => new { x.LoanId, x.BookId }).IsUnique();
            e.HasOne(x => x.Loan).WithMany(l => l.Details).HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReceivedByUser).WithMany().HasForeignKey(x => x.ReceivedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookReservation>(e =>
        {
            e.HasIndex(x => new { x.BookId, x.Status, x.RequestedAt });
            e.HasIndex(x => new { x.ReaderId, x.Status, x.RequestedAt });
            e.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
