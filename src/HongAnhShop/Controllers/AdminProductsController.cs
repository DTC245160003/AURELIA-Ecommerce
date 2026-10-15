using HongAnhShop.Data;
using HongAnhShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HongAnhShop.Controllers;

[Authorize(Roles = "Admin,Staff")]
[Route("Admin/Products")]
public class AdminProductsController : Controller
{
    private readonly ProductRepository _repository;

    public AdminProductsController(ProductRepository repository)
        => _repository = repository;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var products = await _repository.GetAllForAdminAsync();
        return View("~/Views/Admin/Products.cshtml", products);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["IsEdit"] = false;
        return View("~/Views/Admin/ProductForm.cshtml", new ProductInput());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] ProductInput input)
    {
        ValidateInput(input);
        if (!ModelState.IsValid)
        {
            ViewData["IsEdit"] = false;
            return View("~/Views/Admin/ProductForm.cshtml", input);
        }

        await _repository.CreateAsync(input);
        TempData["ProductMessage"] = "Đã thêm siêu xe mới vào MySQL.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _repository.GetByIdForAdminAsync(id);
        if (product is null) return NotFound();

        ViewData["IsEdit"] = true;
        ViewData["ProductId"] = id;
        return View("~/Views/Admin/ProductForm.cshtml", ToInput(product));
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] ProductInput input)
    {
        var product = await _repository.GetByIdForAdminAsync(id);
        if (product is null) return NotFound();

        ValidateInput(input);
        if (!ModelState.IsValid)
        {
            ViewData["IsEdit"] = true;
            ViewData["ProductId"] = id;
            return View("~/Views/Admin/ProductForm.cshtml", input);
        }

        await _repository.UpdateAsync(id, input);
        TempData["ProductMessage"] = "Đã cập nhật siêu xe trong MySQL.";
        return RedirectToAction(nameof(Index));
    }

    // Khong DELETE vat ly de khong pha lich su don hang.
    [HttpPost("Toggle/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var product = await _repository.GetByIdForAdminAsync(id);
        if (product is null) return NotFound();

        await _repository.SetActiveAsync(id, !product.IsActive);
        TempData["ProductMessage"] = product.IsActive
            ? "Đã ngừng bán xe (dữ liệu vẫn được giữ)."
            : "Đã mở bán lại xe.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateInput(ProductInput input)
    {
        if (input.Price <= 0 || input.Price > 9999999999999999.99m)
            ModelState.AddModelError(nameof(input.Price), "Giá xe phải lớn hơn 0 và phù hợp DECIMAL(18,2).");
        if (decimal.Round(input.Price, 2) != input.Price)
            ModelState.AddModelError(nameof(input.Price), "Giá chỉ được tối đa 2 chữ số thập phân.");

        // Cho phep duong dan noi bo '/images/...' hoac URL HTTPS/HTTP.
        var image = input.ImageUrl?.Trim() ?? "";
        var relative = image.StartsWith("/", StringComparison.Ordinal)
            && !image.StartsWith("//", StringComparison.Ordinal)
            && !image.Contains('\\');
        var absolute = Uri.TryCreate(image, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        if (!relative && !absolute)
            ModelState.AddModelError(nameof(input.ImageUrl),
                "Ảnh phải là đường dẫn bắt đầu bằng / hoặc URL http/https hợp lệ.");
    }

    private static ProductInput ToInput(Product product) => new()
    {
        Name = product.Name,
        Category = product.Category,
        Description = product.Description,
        Price = product.Price,
        ImageUrl = product.ImageUrl,
        Stock = product.Stock
    };
}
