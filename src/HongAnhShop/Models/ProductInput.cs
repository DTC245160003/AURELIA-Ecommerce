using System.ComponentModel.DataAnnotations;

namespace HongAnhShop.Models;

public class ProductInput
{
    [Required(ErrorMessage = "Vui lòng nhập tên siêu xe.")]
    [StringLength(150, ErrorMessage = "Tên xe không được quá 150 ký tự.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập hãng xe.")]
    [StringLength(100, ErrorMessage = "Tên hãng không được quá 100 ký tự.")]
    public string Category { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mô tả.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập đường dẫn ảnh.")]
    [StringLength(255, ErrorMessage = "Đường dẫn ảnh không được quá 255 ký tự.")]
    public string ImageUrl { get; set; } = "";

    public decimal Price { get; set; }

    [Range(0, 1000000, ErrorMessage = "Tồn kho phải từ 0 đến 1.000.000.")]
    public int Stock { get; set; }
}
