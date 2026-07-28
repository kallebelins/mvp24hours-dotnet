using CustomerAPI.Application.DTOs.Contacts;
using CustomerAPI.Application.DTOs.Customers;
using CustomerAPI.Application.Ports;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Ports;
using CustomerAPI.Core.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.UseCases
{
    /// <summary>
    /// Application use case that orchestrates customer CRUD through outbound ports only.
    /// No infrastructure concern leaks into this class.
    /// </summary>
    public class CustomerUseCase(
        ICustomerReadPort customerReadPort,
        ICustomerWritePort customerWritePort,
        IContactReadPort contactReadPort,
        IContactWritePort contactWritePort,
        TimeProvider timeProvider,
        ILogger<CustomerUseCase> logger) : ICustomerUseCase
    {
        public async Task<IBusinessResult<IList<CustomerResult>>> GetCustomersAsync(CustomerQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("GetCustomersAsync called with Name={Name} Active={Active}", query.Name, query.Active);

            var customers = await customerReadPort.GetAllAsync(query.Name, query.Active, cancellationToken);

            if (!customers.AnySafe())
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                    .ToBusiness<IList<CustomerResult>>();

            IList<CustomerResult> result = customers.Select(c => new CustomerResult
            {
                Id = c.Id,
                Name = c.Name,
                Note = c.Note,
                Active = c.Active
            }).ToList();

            return result.ToBusiness();
        }

        public async Task<IBusinessResult<CustomerIdResult>> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("GetCustomerByIdAsync called with id={Id}", id);

            var customer = await customerReadPort.GetByIdAsync(id, cancellationToken);

            if (customer == null)
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                    .ToBusiness<CustomerIdResult>();

            var result = new CustomerIdResult
            {
                Id = customer.Id,
                Name = customer.Name,
                Note = customer.Note,
                Active = customer.Active,
                Contacts = customer.Contacts?.Select(c => new ContactResult
                {
                    Id = c.Id,
                    Type = c.Type,
                    Description = c.Description,
                    Active = c.Active
                }).ToList() ?? []
            };

            return result.ToBusiness();
        }

        public async Task<IBusinessResult<int>> CreateCustomerAsync(CustomerCreate dto, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("CreateCustomerAsync called for Name={Name}", dto.Name);

            var entity = new Customer
            {
                Name = dto.Name,
                Note = dto.Note,
                Active = true,
                Created = timeProvider.GetUtcNow().UtcDateTime
            };

            var created = await customerWritePort.AddAsync(entity, cancellationToken);

            return created.Id.ToBusiness();
        }

        public async Task<IBusinessResult<bool>> UpdateCustomerAsync(int id, CustomerUpdate dto, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("UpdateCustomerAsync called for id={Id}", id);

            var customer = await customerReadPort.GetByIdAsync(id, cancellationToken);

            if (customer == null)
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                    .ToBusiness<bool>();

            customer.Name = dto.Name;
            customer.Note = dto.Note;
            customer.Active = dto.Active;

            await customerWritePort.UpdateAsync(customer, cancellationToken);

            return true.ToBusiness();
        }

        public async Task<IBusinessResult<bool>> DeleteCustomerAsync(int id, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("DeleteCustomerAsync called for id={Id}", id);

            var customer = await customerReadPort.GetByIdAsync(id, cancellationToken);

            if (customer == null)
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                    .ToBusiness<bool>();

            await customerWritePort.DeleteAsync(customer, cancellationToken);

            return true.ToBusiness();
        }

        public async Task<IBusinessResult<IList<ContactResult>>> GetContactsAsync(int customerId, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("GetContactsAsync called for customerId={CustomerId}", customerId);

            if (!await customerReadPort.ExistsAsync(customerId, cancellationToken))
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                    .ToBusiness<IList<ContactResult>>();

            var contacts = await contactReadPort.GetByCustomerIdAsync(customerId, cancellationToken);

            if (!contacts.AnySafe())
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                    .ToBusiness<IList<ContactResult>>();

            IList<ContactResult> result = contacts.Select(c => new ContactResult
            {
                Id = c.Id,
                Type = c.Type,
                Description = c.Description,
                Active = c.Active
            }).ToList();

            return result.ToBusiness();
        }

        public async Task<IBusinessResult<int>> CreateContactAsync(int customerId, ContactCreate dto, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("CreateContactAsync called for customerId={CustomerId}", customerId);

            if (!await customerReadPort.ExistsAsync(customerId, cancellationToken))
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                    .ToBusiness<int>();

            var entity = new Contact
            {
                CustomerId = customerId,
                Type = dto.Type,
                Description = dto.Description,
                Active = true,
                Created = timeProvider.GetUtcNow().UtcDateTime
            };

            var created = await contactWritePort.AddAsync(entity, cancellationToken);

            return created.Id.ToBusiness();
        }

        public async Task<IBusinessResult<bool>> UpdateContactAsync(int customerId, int contactId, ContactUpdate dto, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("UpdateContactAsync called for customerId={CustomerId} contactId={ContactId}", customerId, contactId);

            var contact = await contactReadPort.GetByIdAsync(contactId, cancellationToken);

            if (contact == null || contact.CustomerId != customerId)
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                    .ToBusiness<bool>();

            contact.Type = dto.Type;
            contact.Description = dto.Description;
            contact.Active = dto.Active;

            await contactWritePort.UpdateAsync(contact, cancellationToken);

            return true.ToBusiness();
        }

        public async Task<IBusinessResult<bool>> DeleteContactAsync(int customerId, int contactId, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("DeleteContactAsync called for customerId={CustomerId} contactId={ContactId}", customerId, contactId);

            var contact = await contactReadPort.GetByIdAsync(contactId, cancellationToken);

            if (contact == null || contact.CustomerId != customerId)
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                    .ToBusiness<bool>();

            await contactWritePort.DeleteAsync(contact, cancellationToken);

            return true.ToBusiness();
        }
    }
}
