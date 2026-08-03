using App.Core.Models;
using AutoMapper;
using Mvp24Hours.Core.Contract.Mappings;

namespace App.Core.ValueObjects.Items;

public class ItemResult : IMapFrom
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public required string Name { get; set; }
    public string? Note { get; set; }
    public bool Active { get; set; }

    public virtual void Mapping(Profile profile)
    {
        profile.CreateMap<Item, ItemResult>();
    }
}
