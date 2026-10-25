using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Api;
using ThuVien.Data;
using ThuVien.Models;

namespace ThuVien.Controllers.Api;

[ApiController]
[Route("api/books")]
public class BooksApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public BooksApiController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/books?keyword=&categoryId=&page=1&pageSize=10
    [HttpGet]
    public async Task<IActionResult> GetBooks(
        [FromQuery] string? keyword,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Books.AsNoTracking().Where(b => b.IsActive);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(b => b.Title.Contains(k) || b.BookCode.Contains(k) || b.Author.Contains(k) || (b.ISBN != null && b.ISBN.Contains(k)));
        }
        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query
            .OrderBy(b => b.Title).ThenBy(b => b.BookId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookDto
            {
                Id = b.BookId,
                BookCode = b.BookCode,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category != null ? b.Category.Name : null,
                Publisher = b.Publisher,
                PublishYear = b.PublishYear,
                TotalCopies = b.TotalCopies,
                AvailableCopies = b.AvailableCopies,
                ShelfLocation = b.ShelfLocation,
                CoverImageUrl = b.CoverImageUrl
            })
            .ToListAsync();

        var result = new PagedResult<BookDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = totalPages
        };
        return Ok(ApiResponse<PagedResult<BookDto>>.Ok(result));
    }

    // GET /api/books/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBook(int id)
    {
        var isStaff = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Librarian);

        var book = await _db.Books.AsNoTracking()
            .Where(b => b.BookId == id && (b.IsActive || isStaff))
            .Select(b => new BookDto
            {
                Id = b.BookId,
                BookCode = b.BookCode,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category != null ? b.Category.Name : null,
                Publisher = b.Publisher,
                PublishYear = b.PublishYear,
                TotalCopies = b.TotalCopies,
                AvailableCopies = b.AvailableCopies,
                ShelfLocation = b.ShelfLocation,
                CoverImageUrl = b.CoverImageUrl
            })
            .FirstOrDefaultAsync();

        if (book == null)
        {
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy sách."));
        }
        return Ok(ApiResponse<BookDto>.Ok(book));
    }

    // POST /api/books  (chỉ Admin / Thủ thư; xác thực bằng JWT Bearer token)
    // Endpoint này yêu cầu JWT Bearer, không dựa vào cookie phiên của trình duyệt.
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Staff)]
    public async Task<IActionResult> Create([FromBody] BookCreateRequest request)
    {
        var code = request.BookCode.Trim();

        if (!await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId))
        {
            return BadRequest(ApiResponse<object>.Fail("Thể loại không tồn tại."));
        }
        if (await _db.Books.AnyAsync(b => b.BookCode == code))
        {
            return Conflict(ApiResponse<object>.Fail("Mã sách đã tồn tại."));
        }
        if (!string.IsNullOrWhiteSpace(request.CoverImageUrl)
            && !(request.CoverImageUrl.StartsWith("/", StringComparison.Ordinal) && !request.CoverImageUrl.StartsWith("//", StringComparison.Ordinal))
            && !(Uri.TryCreate(request.CoverImageUrl, UriKind.Absolute, out var coverUri) && coverUri.Scheme == Uri.UriSchemeHttps))
        {
            return BadRequest(ApiResponse<object>.Fail("Ảnh bìa phải là URL HTTPS hoặc đường dẫn nội bộ."));
        }

        var book = new Book
        {
            BookCode = code,
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            CategoryId = request.CategoryId,
            Publisher = request.Publisher,
            PublishYear = request.PublishYear,
            TotalCopies = request.TotalCopies,
            AvailableCopies = request.TotalCopies,
            ShelfLocation = request.ShelfLocation,
            Description = request.Description,
            CoverImageUrl = request.CoverImageUrl,
            IsActive = true
        };
        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        var dto = new BookDto
        {
            Id = book.BookId,
            BookCode = book.BookCode,
            Title = book.Title,
            Author = book.Author,
            Publisher = book.Publisher,
            PublishYear = book.PublishYear,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            ShelfLocation = book.ShelfLocation,
            CoverImageUrl = book.CoverImageUrl
        };
        return CreatedAtAction(nameof(GetBook), new { id = book.BookId }, ApiResponse<BookDto>.Ok(dto));
    }
}
