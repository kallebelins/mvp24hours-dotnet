using CustomerAPI.Application.DTOs.Contacts;
using System.Collections.Generic;

namespace CustomerAPI.Application.DTOs.Customers
{
    public class CustomerIdResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; }
        public bool Active { get; set; }
        public IList<ContactResult> Contacts { get; set; } = [];
    }
}
