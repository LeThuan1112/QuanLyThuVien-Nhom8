using System.ComponentModel.DataAnnotations.Schema;

namespace ThuVien.Models;

/// <summary>Ghi nhận từng lần gia hạn và phí phát sinh. Đây là phí được ghi nhận, không đồng nghĩa đã thu tiền.</summary>
public class LoanRenewal
{
    public int LoanRenewalId { get; set; }

    public int LoanId { get; set; }
    public Loan? Loan { get; set; }

    public DateTime RenewedAt { get; set; } = DateTime.Now;
    public int RenewDays { get; set; }
    public int RenewCount { get; set; }
    public int OutstandingCopies { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FeeAmount { get; set; }

    public int RenewedByUserId { get; set; }
    public User? RenewedByUser { get; set; }
}
