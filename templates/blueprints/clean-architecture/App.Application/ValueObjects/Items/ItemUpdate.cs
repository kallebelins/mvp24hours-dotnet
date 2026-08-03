using AutoMapper;
using App.Domain.Entities;
using Mvp24Hours.Core.Contract.Mappings;

namespace App.Application.ValueObjects.Items;

public class ItemUpdate : IMapFrom
{
    public required string Name { get; set; }
    public string? Note { get; set; }

    public virtual void Mapping(Profile profile)
    {
        profile.CreateMap<ItemUpdate, Item>();
    }
}
