using System.ComponentModel.DataAnnotations;

namespace ThuVien.Models;

public class Book
{
    public int BookId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã sách")]
    [StringLength(20, ErrorMessage = "Mã sách tối đa 20 ký tự")]
    [Display(Name = "Mã sách")]
    public string BookCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên sách")]
    [StringLength(200, ErrorMessage = "Tên sách tối đa 200 ký tự")]
    [Display(Name = "Tên sách")]
    public string Title { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "ISBN")]
    public string? ISBN { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thể loại")]
    [Display(Name = "Thể loại")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tác giả")]
    [StringLength(150)]
    [Display(Name = "Tác giả")]
    public string Author { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Nhà xuất bản")]
    public string? Publisher { get; set; }

    [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    [Display(Name = "Năm xuất bản")]
    public int? PublishYear { get; set; }

    [Range(0, 10000, ErrorMessage = "Tổng số bản phải từ 0 đến 10000")]
    [Display(Name = "Tổng số bản")]
    public int TotalCopies { get; set; }

    [Range(0, 10000)]
    [Display(Name = "Số bản còn lại")]
    public int AvailableCopies { get; set; }

    [StringLength(50)]
    [Display(Name = "Vị trí kệ")]
    public string? ShelfLocation { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn ảnh bìa tối đa 500 ký tự")]
    [Display(Name = "Ảnh bìa (URL hoặc đường dẫn trong wwwroot)")]
    public string? CoverImageUrl { get; set; }

    [StringLength(2000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    // true = đang lưu hành, false = ngừng lưu hành
    [Display(Name = "Đang lưu hành")]
    public bool IsActive { get; set; } = true;

    // Kiểm soát cập nhật đồng thời
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
