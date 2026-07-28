using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.Application.DTOs.Customers
{
    public class CustomerCreate
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Note { get; set; }
    }
}
