using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Enums;
using Mvp24Hours.Core.Contract.Data;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Infrastructure.Data
{
    /// <summary>
    /// Seeds development Customer documents when the collection is empty.
    /// </summary>
    public static class MongoDBContextSeed
    {
        public static async Task SeedAsync(
            IUnitOfWorkAsync unitOfWork,
            TimeProvider timeProvider,
            CancellationToken cancellationToken = default)
        {
            IRepositoryAsync<Customer> repository = unitOfWork.GetRepository<Customer>();
            if (await repository.ListAnyAsync(cancellationToken))
            {
                return;
            }

            await repository.AddAsync(GetCustomers(timeProvider), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static List<Customer> GetCustomers(TimeProvider timeProvider)
        {
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            var customerWithContactsId = ObjectId.GenerateNewId().ToString();
            var prospectId = ObjectId.GenerateNewId().ToString();
            var noContactId = ObjectId.GenerateNewId().ToString();

            return
            [
                new Customer
                {
                    Id = customerWithContactsId,
                    Created = utcNow,
                    Name = "Cherokee Macdonald",
                    Active = true,
                    Note = "Customer charged via standard charge.",
                    Contacts =
                    [
                        new Contact
                        {
                            Id = ObjectId.GenerateNewId().ToString(),
                            Created = utcNow,
                            CustomerId = customerWithContactsId,
                            Type = ContactType.CellPhone,
                            Description = "(800) 997-348",
                            Active = true
                        },
                        new Contact
                        {
                            Id = ObjectId.GenerateNewId().ToString(),
                            Created = utcNow,
                            CustomerId = customerWithContactsId,
                            Type = ContactType.Email,
                            Description = "cherokee@example.com",
                            Active = true
                        }
                    ]
                },
                new Customer
                {
                    Id = prospectId,
                    Created = utcNow,
                    Name = "Jonah Harvey",
                    Active = true,
                    Note = "prospect lead from marketing campaign.",
                    Contacts =
                    [
                        new Contact
                        {
                            Id = ObjectId.GenerateNewId().ToString(),
                            Created = utcNow,
                            CustomerId = prospectId,
                            Type = ContactType.Email,
                            Description = "jonah@example.com",
                            Active = true
                        }
                    ]
                },
                new Customer
                {
                    Id = noContactId,
                    Created = utcNow,
                    Name = "Avery Quinn",
                    Active = true,
                    Note = "Customer without contacts for specification demos.",
                    Contacts = []
                }
            ];
        }
    }
}
