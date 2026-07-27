using CustomerAPI.Core.Resources;
using CustomerAPI.Enums;
using CustomerAPI.ValueObjects;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Operations.Custom;

namespace CustomerAPI.Operations
{
    /// <summary>
    /// 
    /// </summary>
    public class GetByIdCustomerMapperResponseStep : OperationMapperAsync<CustomerIdResponse>
    {
        /// <summary>
        /// 
        /// </summary>
        public override async Task<CustomerIdResponse> MapperAsync(IPipelineMessage input)
        {
            if (!input.HasContent("customers"))
            {
                input.Messages.AddMessage("GetByCustomerMapperResponseStep", Messages.RECORD_NOT_FOUND, Mvp24Hours.Core.Enums.MessageType.Error);
            }

            if (!input.HasContent("id"))
            {
                input.Messages.AddMessage("GetByCustomerMapperResponseStep", Messages.PARAMETER_ID_REQUIRED, Mvp24Hours.Core.Enums.MessageType.Error);
            }

            if (input.IsFaulty)
            {
                return await Task.FromResult<CustomerIdResponse>(default);
            }

            var id = input.GetContent<int>("id");
            var customers = input.GetContent<dynamic>("customers");

            CustomerIdResponse result = null;

            foreach (var customer in customers)
            {
                if (customer.id == id)
                {
                    result = new CustomerIdResponse
                    {
                        Id = customer.id,
                        Name = customer.name,
                        Contacts = new List<ContactIdResponse>()
                    };

                    if (customer.email != null)
                    {
                        result.Contacts.Add(new ContactIdResponse
                        {
                            Description = customer.email,
                            Type = ContactType.Email
                        });
                    }

                    if (customer.phone != null)
                    {
                        result.Contacts.Add(new ContactIdResponse
                        {
                            Description = customer.phone,
                            Type = ContactType.CellPhone
                        });
                    }

                    if (customer.website != null)
                    {
                        result.Contacts.Add(new ContactIdResponse
                        {
                            Description = customer.website,
                            Type = ContactType.Other
                        });
                    }
                }
            }

            return await result.TaskResult();
        }
    }
}
