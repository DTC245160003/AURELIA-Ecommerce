using System.ComponentModel.DataAnnotations;

namespace HongAnhShop.Models;

public class CheckoutInput
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(20, MinimumLength = 9)]
    [RegularExpression(@"^\+?[0-9 .-]{9,20}$",
        ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string CustomerPhone { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(500)]
    public string CustomerAddress { get; set; } = "";
}