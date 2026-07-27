using AutoMapper;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.ValueObjects.Customers;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Pipe.Operations.Customers
{
    /// <summary>
    /// Create client in data store
    /// </summary>
    public class CreateCustomerRepositoryStep(IUnitOfWorkAsync unitOfWorkAsync, IMapper mapper) : OperationBaseAsync
    {
        public override async Task ExecuteAsync(IPipelineMessage input)
        {
            if (!input.HasContent("model-customers"))
            {
                input.SetLock();
                return;
            }

            var customers = input.GetContent<IList<CustomerCreate>>("model-customers");

            var repo = unitOfWorkAsync.GetRepository<Customer>();

            foreach (var c in customers)
            {
                await repo.AddAsync(mapper.Map<Customer>(c));
            }

            int numberOfRecords = await unitOfWorkAsync.SaveChangesAsync();
            input.AddContent("numberOfRecords", numberOfRecords);
        }
    }
}
