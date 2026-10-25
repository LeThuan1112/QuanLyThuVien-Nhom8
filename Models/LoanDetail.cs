using System.ComponentModel.DataAnnotations.Schema;

namespace ThuVien.Models;

public class LoanDetail
{
    public int LoanDetailId { get; set; }

    public int LoanId { get; set; }
    public Loan? Loan { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    // null = chưa trả
    public DateTime? ReturnDate { get; set; }

    // Phí phạt quá hạn (tính khi trả sách)
    [Column(TypeName = "decimal(18,2)")]
    public decimal FineAmount { get; set; }

    public int? ReceivedByUserId { get; set; }
    public User? ReceivedByUser { get; set; }
}
