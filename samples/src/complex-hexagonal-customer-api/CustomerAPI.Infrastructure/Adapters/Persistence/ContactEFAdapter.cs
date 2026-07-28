using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Ports;
using CustomerAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Infrastructure.Adapters.Persistence
{
    /// <summary>
    /// Outbound EF Core adapter implementing <see cref="IContactReadPort"/> and <see cref="IContactWritePort"/>.
    /// </summary>
    public class ContactEFAdapter(EFDBContext db) : IContactReadPort, IContactWritePort
    {
        public async Task<IList<Contact>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return await db.Contact
                .Where(c => c.CustomerId == customerId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Contact?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await db.Contact.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Contact> AddAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            db.Contact.Add(contact);
            await db.SaveChangesAsync(cancellationToken);
            return contact;
        }

        public async Task UpdateAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            db.Contact.Update(contact);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            db.Contact.Remove(contact);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
