//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mvp24Hours.Application.SQLServer.Test.Support.Enums;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Entities;

namespace Mvp24Hours.Application.SQLServer.Test.Support.Entities.BasicLogs;

public class ContactBasicLog : EntityBase<int>, IEntityDateLog
{

    [Required]
    public DateTime Created { get; set; }

    public DateTime? Modified { get; set; }

    public DateTime? Removed { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContactType Type { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public bool Active { get; set; }
}
