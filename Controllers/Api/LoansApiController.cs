using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Api;
using ThuVien.Data;
using ThuVien.Models;

namespace ThuVien.Controllers.Api;

[ApiController]
[Route("api/loans")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Staff)]
public class LoansApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public LoansApiController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/loans/overdue
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue()
    {
        var today = DateTime.Today;

        var list = await _db.LoanDetails.AsNoTracking()
            .Where(d => d.ReturnDate == null && d.Loan!.DueDate < today)
            .OrderBy(d => d.Loan!.DueDate)
            .Select(d => new OverdueDto
            {
                LoanId = d.LoanId,
                ReaderCode = d.Loan!.Reader!.ReaderCode,
                ReaderName = d.Loan.Reader.FullName,
                BookCode = d.Book!.BookCode,
                BookTitle = d.Book.Title,
                DueDate = d.Loan.DueDate
            })
            .ToListAsync();

        foreach (var item in list)
        {
            item.DaysOverdue = Math.Max(1, (today - item.DueDate.Date).Days);
        }
        return Ok(ApiResponse<List<OverdueDto>>.Ok(list));
    }
}
