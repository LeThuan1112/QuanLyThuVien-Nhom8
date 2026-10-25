using System.ComponentModel.DataAnnotations;

namespace ThuVien.Models;

// Cài đặt hệ thống dạng khóa - giá trị
public class Setting
{
    [Key, StringLength(50)]
    public string SettingKey { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string SettingValue { get; set; } = string.Empty;
}
