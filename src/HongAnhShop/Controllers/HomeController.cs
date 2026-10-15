
using System.Diagnostics;
using HongAnhShop.Data;
using HongAnhShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace HongAnhShop.Controllers;

public class HomeController : Controller
{
    private readonly ProductRepository _products;

    public HomeController(ProductRepository products)
    {
        _products = products;
    }

    // Giữ nguyên trang chủ AURELIA
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // API lấy danh sách siêu xe từ MySQL
    [HttpGet("/api/vehicles")]
    public async Task<IActionResult> Vehicles()
    {
        var products = await _products.GetAllAsync();

        var vehicles = products.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            make = p.Category,
            description = p.Description,
            price = p.Price,
            imageUrl = p.ImageUrl,
            stock = p.Stock
        });

        return Json(vehicles);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id
                ?? HttpContext.TraceIdentifier
        });
    }
}
