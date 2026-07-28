using CustomerAPI.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.Application.DTOs.Contacts
{
    public class ContactUpdate
    {
        [Required]
        public ContactType Type { get; set; }

        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        public bool Active { get; set; }
    }
}
