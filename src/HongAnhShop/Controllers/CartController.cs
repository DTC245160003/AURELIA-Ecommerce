
using System.Security.Claims;
using System.Text.Json;
using HongAnhShop.Data;
using HongAnhShop.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace HongAnhShop.Controllers;

public class CartController : Controller
{
    private readonly ProductRepository _repository;
    private readonly OrderRepository _orders;
    private const string CartKey = "SHOP_CART";

    public CartController(
        ProductRepository repository,
        OrderRepository orders)
    {
        _repository = repository;
        _orders = orders;
    }

    private List<CartItem> GetCart()
    {
        var data = HttpContext.Session.GetString(CartKey);

        return string.IsNullOrEmpty(data)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(data)
              ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString(
            CartKey, JsonSerializer.Serialize(cart));
    }

    // Hien thi gio hang
    public IActionResult Index()
    {
        return View(GetCart());
    }

    // Them san pham vao gio hang
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId)
    {
        var product = await _repository.GetByIdAsync(productId);

        if (product == null)
            return NotFound();

        if (product.Stock <= 0)
            return BadRequest("San pham da het hang.");

        var cart = GetCart();

        var item = cart.FirstOrDefault(
            x => x.ProductId == productId);

        if (item == null)
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                Quantity = 1
            });
        }
        else
        {
            if (item.Quantity >= product.Stock ||
                item.Quantity >= 100)
            {
                TempData["CartError"] =
                    "So luong da dat gioi han.";
                return RedirectToAction(nameof(Index));
            }

            item.Quantity++;
            item.Price = product.Price;
        }

        SaveCart(cart);

        TempData["CartSuccess"] =
            "Da them san pham vao gio hang.";

        return RedirectToAction(nameof(Index));
    }

    // Cap nhat so luong ngay trong gio hang
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int productId, int quantity)
    {
        var cart = GetCart();

        var item = cart.FirstOrDefault(
            x => x.ProductId == productId);

        if (item == null)
        {
            TempData["CartError"] =
                "San pham khong co trong gio hang.";
            return RedirectToAction(nameof(Index));
        }

        if (quantity < 1 || quantity > 100)
        {
            TempData["CartError"] =
                "So luong phai tu 1 den 100.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _repository.GetByIdAsync(productId);

        if (product == null)
        {
            TempData["CartError"] =
                "San pham khong con ton tai.";
            return RedirectToAction(nameof(Index));
        }

        if (quantity > product.Stock)
        {
            TempData["CartError"] =
                $"Chi con {product.Stock} san pham trong kho.";
            return RedirectToAction(nameof(Index));
        }

        item.Quantity = quantity;
        item.Price = product.Price;
        item.Name = product.Name;

        SaveCart(cart);

        TempData["CartSuccess"] =
            "Da cap nhat so luong thanh cong.";

        return RedirectToAction(nameof(Index));
    }

    // Xoa san pham khoi gio hang
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        var cart = GetCart();

        cart.RemoveAll(x => x.ProductId == productId);

        SaveCart(cart);

        TempData["CartSuccess"] =
            "Da xoa san pham khoi gio hang.";

        return RedirectToAction(nameof(Index));
    }

    // Trang nhap thong tin dat hang
    [HttpGet]
    public IActionResult Checkout()
    {
        var cart = GetCart();

        if (cart.Count == 0)
            return RedirectToAction(nameof(Index));

        ViewBag.Cart = cart;

        return View(new CheckoutInput());
    }

    // Xu ly dat hang
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        CheckoutInput input)
    {
        var cart = GetCart();

        if (cart.Count == 0)
            return RedirectToAction(nameof(Index));

        if (ModelState.IsValid)
        {
            try
            {
                // Chỉ gắn đơn với User hiện đang xác thực; không nhận userId từ form.
                int? userId = null;
                if (User.Identity?.IsAuthenticated == true && User.IsInRole("User"))
                {
                    if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId)
                        || parsedId <= 0)
                        throw new InvalidOperationException("Phiên đăng nhập không hợp lệ. Hãy đăng nhập lại.");
                    userId = parsedId;
                }

                var orderCode =
                    await _orders.CreateAsync(input, cart, userId);

                HttpContext.Session.Remove(CartKey);

                TempData["OrderCode"] = orderCode;

                return RedirectToAction(nameof(Success));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (MySqlException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong the luu don hang. Vui long thu lai.");
            }
        }

        ViewBag.Cart = cart;

        return View(input);
    }

    // Trang thong bao dat hang thanh cong
    public IActionResult Success()
    {
        var code = TempData["OrderCode"] as string;

        if (string.IsNullOrWhiteSpace(code))
            return RedirectToAction(nameof(Index));

        ViewBag.OrderCode = code;

        return View();
    }
}
