using System.Security.Claims;
using HongAnhShop.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace HongAnhShop.Controllers;

[Authorize(Roles = "Admin,Staff")]
[Route("Admin/Customers")]
public sealed class AdminCustomersController : Controller
{
    private readonly AdminCustomerRepository _customers;
    private readonly VehicleOrderHistoryRepository _history;
    public AdminCustomersController(AdminCustomerRepository customers, VehicleOrderHistoryRepository history)
    {
        _customers = customers;
        _history = history;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] string? status)
    {
        var page = await _customers.GetPageAsync(q, status);
        return View("~/Views/Admin/Customers.cshtml", page);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0) return NotFound();
        var customer = await _customers.GetAsync(id);
        if (customer is null) return NotFound();
        ViewBag.LinkedOrders = await _history.GetByUserIdAsync(id, limit: 30);
        return View("~/Views/Admin/CustomerDetails.cshtml", customer);
    }

    [HttpPost("{id:int}/Status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int id, [FromForm] string? nextStatus)
    {
        if (id <= 0 || nextStatus is not ("lock" or "unlock")) return BadRequest();
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var selfId) && selfId == id)
            return Forbid();

        try
        {
            var customer = await _customers.GetAsync(id);
            if (customer is null) return NotFound(); // Không chạm vào Admin/Staff.

            var enable = nextStatus == "unlock";
            if (customer.IsActive == enable)
            {
                TempData["CustomerMessage"] = "Tài khoản đã ở trạng thái này.";
            }
            else if (await _customers.SetActiveAsync(id, enable))
            {
                TempData["CustomerMessage"] = enable ?
                    "Đã mở khóa khách hàng." : "Đã khóa khách hàng. Cookie cũ sẽ bị từ chối khi yêu cầu tiếp theo.";
            }
            else
            {
                TempData["CustomerError"] = "Tài khoản đã thay đổi. Vui lòng tải lại.";
            }
        }
        catch (MySqlException)
        {
            TempData["CustomerError"] = "Không thể cập nhật MySQL. Vui lòng thử lại.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
