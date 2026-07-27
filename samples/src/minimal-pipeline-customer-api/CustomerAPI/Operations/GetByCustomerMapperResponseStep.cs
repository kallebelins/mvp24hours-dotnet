using CustomerAPI.Core.Resources;
using CustomerAPI.ValueObjects;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Operations.Custom;

namespace CustomerAPI.Operations
{
    /// <summary>
    /// 
    /// </summary>
    public class GetByCustomerMapperResponseStep : OperationMapperAsync<IList<CustomerResponse>>
    {
        /// <summary>
        /// 
        /// </summary>
        public override async Task<IList<CustomerResponse>> MapperAsync(IPipelineMessage input)
        {
            if (!input.HasContent("customers"))
            {
                input.Messages.AddMessage("GetByCustomerMapperResponseStep", Messages.RECORD_NOT_FOUND, Mvp24Hours.Core.Enums.MessageType.Error);
                return await Task.FromResult<IList<CustomerResponse>>(default);
            }

            var customers = input.GetContent<dynamic>("customers");
            var filter = input.GetContent<CustomerQueryRequest>();

            List<CustomerResponse> result = [];

            foreach (var customer in customers)
            {
                if (filter != null)
                {
                    if (filter.HasCellContact && customer.phone == null)
                    {
                        continue;
                    }
                    if (filter.HasEmailContact && customer.email == null)
                    {
                        continue;
                    }
                    if (filter.Name.HasValue()
                        && customer.name != null
                        && !((string)customer.name).Contains(filter.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }
                }

                result.Add(new CustomerResponse
                {
                    Id = customer.id,
                    Name = customer.name
                });
            }

            return await result.TaskResult();
        }
    }
}
