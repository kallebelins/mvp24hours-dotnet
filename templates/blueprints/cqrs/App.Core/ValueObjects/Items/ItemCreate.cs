using AutoMapper;
using App.Core.Entities;
using Mvp24Hours.Core.Contract.Mappings;

namespace App.Core.ValueObjects.Items;

public class ItemCreate : IMapFrom
{
    public required string Name { get; set; }
    public string? Note { get; set; }

    public virtual void Mapping(Profile profile)
    {
        profile.CreateMap<ItemCreate, Item>();
    }
}
