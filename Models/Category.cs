using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ThuVien.Models;

public class Category
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thể loại")]
    [StringLength(100, ErrorMessage = "Tên thể loại tối đa 100 ký tự")]
    [Display(Name = "Tên thể loại")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [ValidateNever]
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
