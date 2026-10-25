using System.ComponentModel.DataAnnotations;

namespace ThuVien.Models;

public static class ReservationStatuses
{
    public const string Waiting = "Waiting";
    public const string Ready = "Ready";
    public const string Fulfilled = "Fulfilled";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";

    public static string DisplayName(string status) => status switch
    {
        Waiting => "Đang chờ sách",
        Ready => "Đã có sách – vui lòng nhận",
        Fulfilled => "Đã mượn sách",
        Cancelled => "Đã hủy",
        Expired => "Hết thời hạn nhận",
        _ => "Không xác định"
    };
}

/// <summary>
/// Một yêu cầu đặt trước sách. Trạng thái Ready giữ chỗ trong thời hạn nhận;
/// đây là hàng chờ, không phải giao dịch mượn hoặc khoản thanh toán.
/// </summary>
public class BookReservation
{
    public int BookReservationId { get; set; }

    [Required]
    public int BookId { get; set; }
    public Book? Book { get; set; }

    [Required]
    public int ReaderId { get; set; }
    public Reader? Reader { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.Now;

    [Required, StringLength(20)]
    public string Status { get; set; } = ReservationStatuses.Waiting;

    public DateTime? ReadyAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Mã phiếu mượn phát sinh sau khi độc giả nhận sách; không cascade xóa lịch sử.
    public int? FulfilledLoanId { get; set; }
}
