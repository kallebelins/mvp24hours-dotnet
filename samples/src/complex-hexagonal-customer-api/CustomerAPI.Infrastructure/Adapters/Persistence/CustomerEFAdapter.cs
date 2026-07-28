using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Ports;
using CustomerAPI.Core.Specifications.Customers;
using CustomerAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Infrastructure.Adapters.Persistence
{
    /// <summary>
    /// Outbound EF Core adapter implementing <see cref="ICustomerReadPort"/> and <see cref="ICustomerWritePort"/>.
    /// Application code never references EF; it only calls port interfaces defined in CustomerAPI.Core.
    /// </summary>
    public class CustomerEFAdapter(EFDBContext db) : ICustomerReadPort, ICustomerWritePort
    {
        public async Task<IList<Customer>> GetAllAsync(string? name, bool? active, bool hasEmailContact = false, CancellationToken cancellationToken = default)
        {
            Expression<Func<Customer, bool>> clause =
                x => (string.IsNullOrWhiteSpace(name) || x.Name.Contains(name))
                    && (!active.HasValue || x.Active == active.Value);

            if (hasEmailContact)
            {
                clause = clause.And<Customer, CustomerHasEmailContactSpec>();
            }

            return await db.Customer
                .Include(c => c.Contacts)
                .Where(clause)
                .ToListAsync(cancellationToken);
        }

        public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await db.Customer
                .Include(c => c.Contacts)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await db.Customer.AnyAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            db.Customer.Add(customer);
            await db.SaveChangesAsync(cancellationToken);
            return customer;
        }

        public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            db.Customer.Update(customer);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            db.Customer.Remove(customer);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
