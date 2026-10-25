using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;

namespace ThuVien.Services;

public class LibrarySettings
{
    [Range(1, 90, ErrorMessage = "Số ngày mượn phải từ 1 đến 90")]
    [Display(Name = "Số ngày được mượn")]
    public int LoanDays { get; set; } = 14;

    [Range(1, 20, ErrorMessage = "Số đầu sách mỗi phiếu phải từ 1 đến 20")]
    [Display(Name = "Số đầu sách tối đa mỗi phiếu")]
    public int MaxBooksPerLoan { get; set; } = 5;

    [Range(1, 50, ErrorMessage = "Số cuốn đang mượn cùng lúc phải từ 1 đến 50")]
    [Display(Name = "Số cuốn tối đa một độc giả được mượn cùng lúc")]
    public int MaxBooksAtOnce { get; set; } = 5;

    [Range(0, 1000000, ErrorMessage = "Mức phạt phải từ 0 đến 1.000.000 đồng")]
    [Display(Name = "Phí phạt quá hạn (đồng / cuốn / ngày)")]
    public int FinePerDay { get; set; } = 1000;

    [Range(0, 1000000, ErrorMessage = "Phí gia hạn phải từ 0 đến 1.000.000 đồng/cuốn")]
    [Display(Name = "Phí gia hạn (đồng / cuốn / lần)")]
    public int RenewalFeePerBook { get; set; } = 2000;

    [Range(0, 5, ErrorMessage = "Số lần gia hạn tối đa từ 0 đến 5 (0 = không cho gia hạn)")]
    [Display(Name = "Số lần gia hạn tối đa mỗi phiếu")]
    public int MaxRenewals { get; set; } = 2;

    [Range(1, 30, ErrorMessage = "Số ngày mỗi lần gia hạn từ 1 đến 30")]
    [Display(Name = "Số ngày cộng thêm mỗi lần gia hạn")]
    public int RenewDays { get; set; } = 7;
}

// Đọc/ghi các quy tắc nghiệp vụ có thể cấu hình từ bảng Settings
public class SettingsService
{
    private readonly AppDbContext _db;

    public SettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LibrarySettings> GetAsync()
    {
        var dict = await _db.Settings.AsNoTracking().ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

        int Get(string key, int fallback, int min) =>
            dict.TryGetValue(key, out var v) && int.TryParse(v, out var n) && n >= min ? n : fallback;

        return new LibrarySettings
        {
            LoanDays = Get("LoanDays", 14, 1),
            MaxBooksPerLoan = Get("MaxBooksPerLoan", 5, 1),
            MaxBooksAtOnce = Get("MaxBooksAtOnce", 5, 1),
            FinePerDay = Get("FinePerDay", 1000, 0),
            RenewalFeePerBook = Get("RenewalFeePerBook", 2000, 0),
            MaxRenewals = Get("MaxRenewals", 2, 0),
            RenewDays = Get("RenewDays", 7, 1)
        };
    }

    public async Task SaveAsync(LibrarySettings settings)
    {
        var existing = await _db.Settings.ToListAsync();

        void Set(string key, int value)
        {
            var row = existing.FirstOrDefault(x => x.SettingKey == key);
            if (row == null)
                _db.Settings.Add(new Setting { SettingKey = key, SettingValue = value.ToString() });
            else
                row.SettingValue = value.ToString();
        }

        Set("LoanDays", settings.LoanDays);
        Set("MaxBooksPerLoan", settings.MaxBooksPerLoan);
        Set("MaxBooksAtOnce", settings.MaxBooksAtOnce);
        Set("FinePerDay", settings.FinePerDay);
        Set("RenewalFeePerBook", settings.RenewalFeePerBook);
        Set("MaxRenewals", settings.MaxRenewals);
        Set("RenewDays", settings.RenewDays);
        await _db.SaveChangesAsync();
    }
}
