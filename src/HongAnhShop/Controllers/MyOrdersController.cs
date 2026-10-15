using System.Security.Claims;
using HongAnhShop.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HongAnhShop.Controllers;

[Authorize(Roles = "User")]
[Route("Account/MyOrders")]
public sealed class MyOrdersController : Controller
{
    private readonly VehicleOrderHistoryRepository _history;
    public MyOrdersController(VehicleOrderHistoryRepository history) => _history = history;

    private int CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
            ? id : 0;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var id = CurrentUserId();
        if (id == 0) return Forbid();
        var orders = await _history.GetByUserIdAsync(id);
        return View("~/Views/Orders/MyOrders.cshtml", orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var userId = CurrentUserId();
        if (userId == 0) return Forbid();
        var detail = await _history.GetForOwnerAsync(id, userId);
        return detail is null ? NotFound() : View("~/Views/Orders/MyOrderDetails.cshtml", detail);
    }
}
