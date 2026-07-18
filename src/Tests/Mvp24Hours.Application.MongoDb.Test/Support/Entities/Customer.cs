//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Application.MongoDb.Test.Support.Entities;

public class Customer : IEntityBase
{
    public Customer()
    {
        Contacts = [];
    }

    [BsonIgnore()]
    public object EntityKey => Oid;

    [BsonId()]
    public ObjectId Oid { get; set; } = ObjectId.GenerateNewId();

    [BsonElement("created")]
    [BsonRequired()]
    public DateTime Created { get; set; }

    [BsonElement("name")]
    [BsonRequired()]
    public string Name { get; set; } = string.Empty;

    [BsonElement("active")]
    [BsonRequired()]
    public bool Active { get; set; }

    // collections

    [BsonElement("contacts")]
    public ICollection<ObjectId> Contacts { get; set; }
}
