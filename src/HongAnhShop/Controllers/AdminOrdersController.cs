using HongAnhShop.Data;
using HongAnhShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace HongAnhShop.Controllers;

[Authorize(Roles = "Admin,Staff")]
[Route("Admin/Orders")]
public sealed class AdminOrdersController : Controller
{
    private readonly AdminOrderRepository _orders;

    public AdminOrdersController(AdminOrderRepository orders) => _orders = orders;

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] string? status)
    {
        var model = await _orders.GetPageAsync(q, status);
        return View("~/Views/Admin/Orders.cshtml", model);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _orders.GetDetailsAsync(id);
        return model is null ? NotFound() : View("~/Views/Admin/OrderDetails.cshtml", model);
    }

    [HttpPost("{id:int}/Status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int id, [FromForm] string nextStatus)
    {
        if (id <= 0 || nextStatus is not ("confirmed" or "completed" or "cancelled"))
            return BadRequest("Trạng thái không hợp lệ.");

        try
        {
            var result = await _orders.ChangeStatusAsync(id, nextStatus);
            switch (result)
            {
                case AdminOrderStatusResult.NotFound:
                    return NotFound();
                case AdminOrderStatusResult.InvalidTransition:
                    TempData["OrderError"] = "Không được đổi trạng thái theo hướng này. Hãy tải lại trang.";
                    break;
                case AdminOrderStatusResult.AlreadySet:
                    TempData["OrderMessage"] = "Đơn hàng đã ở trạng thái này. Không thao tác trùng.";
                    break;
                case AdminOrderStatusResult.Updated:
                    TempData["OrderMessage"] = nextStatus switch
                    {
                        "confirmed" => "Đã xác nhận đơn hàng.",
                        "completed" => "Đơn hàng đã hoàn tất.",
                        _ => "Đã hủy đơn và hoàn tồn kho một lần."
                    };
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["OrderError"] = ex.Message;
        }
        catch (MySqlException)
        {
            TempData["OrderError"] = "Có lỗi cơ sở dữ liệu. Chưa thể đổi trạng thái đơn hàng.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
