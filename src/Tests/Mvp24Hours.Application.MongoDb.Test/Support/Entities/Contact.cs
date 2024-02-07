using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Mvp24Hours.Application.MongoDb.Test.Support.Enums;
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Application.MongoDb.Test.Support.Entities
{
    public class Contact : IEntityBase
    {
        [BsonIgnore()]
        public object EntityKey => Oid;

        [BsonId()]
        public ObjectId Oid { get; set; } = ObjectId.GenerateNewId();

        [BsonElement("created")]
        [BsonRequired()]
        public DateTime Created { get; set; }

        [BsonElement("type")]
        [BsonRequired()]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public ContactType Type { get; set; }

        [BsonElement("description")]
        [BsonRequired()]
        public string Description { get; set; }

        [BsonElement("active")]
        [BsonRequired()]
        public bool Active { get; set; }
    }
}
