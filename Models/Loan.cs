using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ThuVien.Models;

public class Loan
{
    public int LoanId { get; set; }

    public int ReaderId { get; set; }
    public Reader? Reader { get; set; }

    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    [Display(Name = "Ngày mượn")]
    public DateTime LoanDate { get; set; } = DateTime.Now;

    [Display(Name = "Hạn trả")]
    public DateTime DueDate { get; set; }

    [StringLength(255)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    // Số lần đã gia hạn
    public int RenewCount { get; set; }

    [ValidateNever]
    public ICollection<LoanDetail> Details { get; set; } = new List<LoanDetail>();

    [ValidateNever]
    public ICollection<LoanRenewal> Renewals { get; set; } = new List<LoanRenewal>();
}
