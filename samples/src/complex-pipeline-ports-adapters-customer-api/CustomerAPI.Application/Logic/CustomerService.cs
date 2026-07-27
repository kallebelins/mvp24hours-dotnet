using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Contract.Pipe.Builders;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Logic
{
    /// <summary>
    /// 
    /// </summary>
    public class CustomerService(IPipelineAsync _pipeline, IServiceProvider provider) : ICustomerService
    {
        #region [ Actions ]

        public async Task<IBusinessResult<IList<CustomerResult>>> GetBy(CustomerQuery filter)
        {
            // note: IGetByCustomerBuilder can be injected through class constructor as class member

            // add operations/steps with IGetByCustomerBuilder builder
            provider.GetService<IGetByCustomerBuilder>()
                ?.Builder(_pipeline);

            // run pipeline with package with content (int -> id)
            await _pipeline.ExecuteAsync(filter.ToMessage());

            // try to get response content
            IList<CustomerResult> result = _pipeline.GetMessage()
                .GetContent<List<CustomerResult>>();

            // checks if there are any records
            if (!result.AnySafe())
            {
                // reply with standard message for record not found
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                        .ToBusiness<IList<CustomerResult>>();
            }

            return result.ToBusiness();
        }

        public async Task<IBusinessResult<CustomerIdResult>> GetById(int id)
        {
            // note: IGetByIdCustomerBuilder can be injected through class constructor as class member

            // add operations/steps with IGetByCustomerBuilder builder
            provider.GetService<IGetByIdCustomerBuilder>()
                ?.Builder(_pipeline);

            // run pipeline with package with content (int -> id)
            await _pipeline.ExecuteAsync(id.ToMessage("id"));

            // try to get response content
            var result = _pipeline.GetMessage()
                .GetContent<CustomerIdResult>();

            // checks if there are any records
            if (result == null)
            {
                // reply with standard message for record not found
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                        .ToBusiness<CustomerIdResult>();
            }
            return result.ToBusiness();
        }

        #endregion
    }
}
