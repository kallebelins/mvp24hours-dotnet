using CustomerAPI.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.Application.DTOs.Contacts
{
    public class ContactCreate
    {
        [Required]
        public ContactType Type { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Description { get; set; } = string.Empty;
    }
}
