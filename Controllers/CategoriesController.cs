using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;

namespace ThuVien.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Categories.AsNoTracking()
            .Include(c => c.Books)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Category());
    }

    [HttpPost]
    public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
    {
        category.Name = (category.Name ?? string.Empty).Trim();

        if (await _db.Categories.AnyAsync(c => c.Name == category.Name))
        {
            ModelState.AddModelError(nameof(Category.Name), "Tên thể loại đã tồn tại.");
        }
        if (!ModelState.IsValid) return View(category);

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm thể loại \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,Description")] Category posted)
    {
        if (id != posted.CategoryId) return NotFound();

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound();

        posted.Name = (posted.Name ?? string.Empty).Trim();

        if (await _db.Categories.AnyAsync(c => c.Name == posted.Name && c.CategoryId != id))
        {
            ModelState.AddModelError(nameof(Category.Name), "Tên thể loại đã tồn tại.");
        }
        if (!ModelState.IsValid) return View(posted);

        category.Name = posted.Name;
        category.Description = posted.Description;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật thể loại \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound();

        // BR07: không xóa thể loại đang được sách sử dụng
        if (await _db.Books.AnyAsync(b => b.CategoryId == id))
        {
            TempData["Error"] = $"Không thể xóa thể loại \"{category.Name}\" vì đang có sách thuộc thể loại này.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa thể loại \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }
}
