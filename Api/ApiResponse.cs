using System.ComponentModel.DataAnnotations;

namespace ThuVien.Api;

// Cấu trúc JSON thống nhất cho mọi phản hồi của API
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public class BookDto
{
    public int Id { get; set; }
    public string BookCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Publisher { get; set; }
    public int? PublishYear { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string? ShelfLocation { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class BookCreateRequest
{
    [Required(ErrorMessage = "Mã sách là bắt buộc.")]
    [StringLength(20, ErrorMessage = "Mã sách tối đa 20 ký tự.")]
    public string BookCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên sách là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên sách tối đa 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tác giả là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Tác giả tối đa 150 ký tự.")]
    public string Author { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Thể loại không hợp lệ.")]
    public int CategoryId { get; set; }

    [StringLength(150)]
    public string? Publisher { get; set; }

    [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ.")]
    public int? PublishYear { get; set; }

    [Range(0, 10000, ErrorMessage = "Tổng số bản phải từ 0 đến 10000.")]
    public int TotalCopies { get; set; }

    [StringLength(50)]
    public string? ShelfLocation { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? CoverImageUrl { get; set; }
}

public class OverdueDto
{
    public int LoanId { get; set; }
    public string ReaderCode { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public string BookCode { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
}
