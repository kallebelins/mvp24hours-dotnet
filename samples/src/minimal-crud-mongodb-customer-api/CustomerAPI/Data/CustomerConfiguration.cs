using CustomerAPI.Entities;
using MongoDB.Bson.Serialization;
using Mvp24Hours.Core.Contract.Data;

namespace CustomerAPI.Data
{
    public class CustomerConfiguration : IBsonClassMap
    {
        public void Configure()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Customer)))
            {
                BsonClassMap.RegisterClassMap<Customer>(cm =>
                {
                    cm.AutoMap();
                });
            }
        }
    }
}
