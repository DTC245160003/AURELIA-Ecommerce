using HongAnhShop.Data;
using Microsoft.AspNetCore.Mvc;

namespace HongAnhShop.Controllers;

public class ProductsController : Controller
{
    private readonly ProductRepository _repository;

    public ProductsController(ProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _repository.GetAllAsync();
        return View(products);
    }
}