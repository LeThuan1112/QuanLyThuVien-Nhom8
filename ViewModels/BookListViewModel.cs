using ThuVien.Models;

namespace ThuVien.ViewModels;

public class BookListViewModel
{
    public List<Book> Books { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    // Số bản đang được giữ cho độc giả chờ nhận; không tính là sẵn sàng cho người khác mượn.
    public Dictionary<int, int> HeldCopies { get; set; } = new();
    public int AvailableToBorrow(Book book) => Math.Max(0, book.AvailableCopies - (HeldCopies.TryGetValue(book.BookId, out var held) ? held : 0));

    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public string? Availability { get; set; }
    public string Sort { get; set; } = "title";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public int TotalItems { get; set; }

    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
}
