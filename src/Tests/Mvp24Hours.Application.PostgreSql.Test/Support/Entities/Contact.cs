//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.PostgreSql.Test.Support.Enums;
using Mvp24Hours.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Application.PostgreSql.Test.Support.Entities
{
    public class Contact : EntityBase<int>
    {
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContactType Type { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public bool Active { get; set; }
    }
}
