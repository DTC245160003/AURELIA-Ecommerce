
using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using HongAnhShop.Data;
using HongAnhShop.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace HongAnhShop.Controllers;

public class AccountController : Controller
{
    private readonly AccountRepository _repository;
    private readonly IPasswordHasher<ShopUser> _hasher;

    public AccountController(
        AccountRepository repository,
        IPasswordHasher<ShopUser> hasher)
    {
        _repository = repository;
        _hasher = hasher;
    }

    // ======================================
    // 1. HIỂN THỊ TRANG ĐĂNG KÝ
    // ======================================

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // ======================================
    // 2. XỬ LÝ ĐĂNG KÝ
    // ======================================

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        [FromForm] RegisterInput input)
    {
        var fullName = Regex.Replace(
            input.FullName?.Trim() ?? "",
            @"\s+",
            " "
        );

        if (fullName.Length < 2 || fullName.Length > 100)
        {
            ModelState.AddModelError(
                nameof(input.FullName),
                "Họ và tên cần từ 2 đến 100 ký tự."
            );
        }

        var identity = NormalizeIdentifier(
            input.EmailOrPhone ?? ""
        );

        if (identity is null)
        {
            ModelState.AddModelError(
                nameof(input.EmailOrPhone),
                "Email hoặc số điện thoại Việt Nam không hợp lệ."
            );
        }

        if (string.IsNullOrEmpty(input.Password) ||
            input.Password.Length < 8 ||
            input.Password.Length > 128)
        {
            ModelState.AddModelError(
                nameof(input.Password),
                "Mật khẩu phải có từ 8 đến 128 ký tự."
            );
        }

        if (input.Password != input.ConfirmPassword)
        {
            ModelState.AddModelError(
                nameof(input.ConfirmPassword),
                "Mật khẩu xác nhận không khớp."
            );
        }

        if (!input.AgreeTerms)
        {
            ModelState.AddModelError(
                nameof(input.AgreeTerms),
                "Bạn cần chấp nhận điều khoản sử dụng."
            );
        }

        if (!ModelState.IsValid || identity is null)
        {
            return View(input);
        }

        // Kiểm tra email hoặc số điện thoại đã tồn tại
        if (await _repository.FindAsync(identity) is not null)
        {
            ModelState.AddModelError(
                nameof(input.EmailOrPhone),
                "Email hoặc số điện thoại đã được đăng ký."
            );

            return View(input);
        }

        // Người tự đăng ký luôn có quyền User
        var user = new ShopUser
        {
            FullName = fullName,
            Email = identity.Contains('@') ? identity : null,
            Phone = identity.Contains('@') ? null : identity,
            Role = "User"
        };

        // Băm mật khẩu trước khi lưu MySQL
        user.PasswordHash = _hasher.HashPassword(
            user,
            input.Password
        );

        try
        {
            await _repository.CreateAsync(user);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            ModelState.AddModelError(
                nameof(input.EmailOrPhone),
                "Email hoặc số điện thoại đã được đăng ký."
            );

            return View(input);
        }

        TempData["AuthMessage"] =
            "Tạo tài khoản thành công. Hãy đăng nhập để tiếp tục.";

        return RedirectToAction(nameof(Login));
    }

    // ======================================
    // 3. HIỂN THỊ TRANG ĐĂNG NHẬP
    // ======================================

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // ======================================
    // 4. XỬ LÝ ĐĂNG NHẬP
    // ======================================

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] LoginInput input)
    {
        var identity = NormalizeIdentifier(
            input.EmailOrPhone ?? ""
        );

        if (identity is null ||
            string.IsNullOrWhiteSpace(input.Password))
        {
            ModelState.AddModelError(
                "",
                "Vui lòng kiểm tra email/số điện thoại và mật khẩu."
            );

            return View(input);
        }

        // Tìm tài khoản thật trong MySQL
        var user = await _repository.FindAsync(identity);

        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(
                "",
                "Thông tin đăng nhập không đúng hoặc tài khoản không khả dụng."
            );

            return View(input);
        }

        // Kiểm tra tài khoản đang bị khóa tạm thời
        if (user.LockedUntilUtc is DateTime until &&
            until > DateTime.UtcNow)
        {
            ModelState.AddModelError(
                "",
                "Tài khoản đang tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử sau."
            );

            return View(input);
        }

        // Kiểm tra mật khẩu đã được băm
        var verification = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            input.Password
        );

        if (verification == PasswordVerificationResult.Failed)
        {
            await _repository.MarkFailedLoginAsync(user.Id);

            ModelState.AddModelError(
                "",
                "Email/số điện thoại hoặc mật khẩu không chính xác."
            );

            return View(input);
        }

        // Đăng nhập đúng thì xóa số lần đăng nhập sai
        await _repository.ResetFailedLoginAsync(user.Id);

        if (verification ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Có thể cập nhật thuật toán băm mật khẩu
            // trong một phiên bản nâng cấp sau.
        }

        // ==================================
        // 5. TẠO COOKIE ĐĂNG NHẬP
        // ==================================

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),
            new(
                ClaimTypes.Name,
                user.FullName
            ),
            new(
                ClaimTypes.Role,
                user.Role
            )
        };

        if (user.Email is not null)
        {
            claims.Add(
                new Claim(ClaimTypes.Email, user.Email)
            );
        }

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            )
        );

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = input.RememberMe,

                ExpiresUtc = DateTimeOffset.UtcNow.Add(
                    input.RememberMe
                        ? TimeSpan.FromDays(14)
                        : TimeSpan.FromHours(8)
                ),

                AllowRefresh = true
            }
        );

        // ==================================
        // 6. CHUYỂN HƯỚNG THEO PHÂN QUYỀN
        // ==================================

        // Admin và Staff vào trang quản trị
        if (user.Role is "Admin" or "Staff")
        {
            return RedirectToAction(
                "Index",
                "Admin"
            );
        }

        // Khách hàng vào trang chủ AURELIA
        return RedirectToAction(
            "Index",
            "Home"
        );
    }

    // ======================================
    // 7. ĐĂNG XUẤT
    // ======================================

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        // Sau đăng xuất luôn trở về Home
        return RedirectToAction(
            "Index",
            "Home"
        );
    }

    // ======================================
    // 8. TRANG KHÔNG CÓ QUYỀN TRUY CẬP
    // ======================================

    [Authorize]
    [HttpGet]
    public IActionResult Denied()
    {
        return StatusCode(
            403,
            "Bạn không có quyền truy cập khu vực này."
        );
    }

    // ======================================
    // 9. CHUẨN HÓA EMAIL / SỐ ĐIỆN THOẠI
    // ======================================

    private static string? NormalizeIdentifier(string text)
    {
        var trimmed = text.Trim();

        if (trimmed.Length == 0 || trimmed.Length > 255)
        {
            return null;
        }

        // Trường hợp đăng nhập bằng Email
        if (trimmed.Contains('@'))
        {
            if (trimmed.Any(char.IsWhiteSpace))
            {
                return null;
            }

            if (!MailAddress.TryCreate(trimmed, out var mail) ||
                !string.Equals(
                    mail.Address,
                    trimmed,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return trimmed.ToLowerInvariant();
        }

        // Trường hợp đăng nhập bằng số điện thoại
        var phone = Regex.Replace(
            trimmed,
            @"[\s.\-]",
            ""
        );

        if (phone.StartsWith(
            "+84",
            StringComparison.Ordinal))
        {
            phone = "0" + phone[3..];
        }

        return Regex.IsMatch(
            phone,
            @"^0[35789][0-9]{8}$"
        )
            ? phone
            : null;
    }
}
