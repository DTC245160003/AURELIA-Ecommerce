using System.Security.Claims;
using HongAnhShop.Data;
using HongAnhShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace HongAnhShop.Controllers;

// Security boundary: Staff and User are forbidden at the controller level.
[Authorize(Roles = "Admin")]
[Route("Admin/Staff")]
public sealed class AdminStaffController : Controller
{
    private readonly AdminStaffRepository _staff;
    public AdminStaffController(AdminStaffRepository staff) => _staff = staff;

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] string? role)
    {
        var model = await _staff.GetPageAsync(q, role);
        return View("~/Views/Admin/Staff.cshtml", model);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0) return NotFound();
        var row = await _staff.GetAsync(id);
        return row is null ? NotFound() : View("~/Views/Admin/StaffDetails.cshtml", row);
    }

    [HttpPost("{id:int}/Role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Role(int id, [FromForm] string? expectedRole,
        [FromForm] string? nextRole)
    {
        if (id <= 0 || expectedRole is null || nextRole is null ||
            !((expectedRole == "User" && nextRole == "Staff") ||
              (expectedRole == "Staff" && nextRole == "User")))
            return BadRequest("Yêu cầu thay đổi quyền không hợp lệ.");

        // Extra defence: even if Admin role is altered, never change own account.
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var selfId) && selfId == id)
            return Forbid();

        try
        {
            var result = await _staff.ChangeRoleAsync(id, expectedRole, nextRole);
            switch (result)
            {
                case AdminStaffChangeResult.Updated:
                    TempData["StaffMessage"] = nextRole == "Staff"
                        ? "Đã cấp quyền Staff. Phiên đăng nhập cũ phải đăng nhập lại."
                        : "Đã thu hồi quyền Staff. Cookie mang quyền cũ sẽ bị từ chối.";
                    break;
                case AdminStaffChangeResult.InactiveAccount:
                    TempData["StaffError"] = "Tài khoản đang bị khóa. Mở khóa trước khi cấp quyền Staff.";
                    break;
                case AdminStaffChangeResult.NotFound:
                    return NotFound();
                default:
                    TempData["StaffError"] = "Vai trò tài khoản đã thay đổi. Vui lòng tải lại trang.";
                    break;
            }
        }
        catch (MySqlException)
        {
            TempData["StaffError"] = "Không thể cập nhật quyền trong MySQL. Vui lòng thử lại.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
