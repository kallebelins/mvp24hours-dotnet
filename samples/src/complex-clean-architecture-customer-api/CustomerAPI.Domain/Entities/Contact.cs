using System.Text.Json.Serialization;
using CustomerAPI.Domain.Enums;
using Mvp24Hours.Core.Entities;

namespace CustomerAPI.Domain.Entities;

public class Contact : EntityBase<int>
{
    public DateTime Created { get; set; }
    [JsonIgnore]
    public int CustomerId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContactType Type { get; set; }
    public string Description { get; set; }
    public bool Active { get; set; }

    public Customer? Customer { get; set; }
}
