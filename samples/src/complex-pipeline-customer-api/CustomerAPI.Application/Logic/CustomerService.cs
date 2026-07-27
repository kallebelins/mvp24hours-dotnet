using CustomerAPI.Application.Pipe.Operations.Customers;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Logic
{
    /// <summary>
    /// Composes cancelable pipeline operations for Customer queries.
    /// </summary>
    public class CustomerService(IPipelineAsync pipeline, ILogger<CustomerService> logger) : ICustomerService
    {
        private readonly IPipelineAsync pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        private readonly ILogger<CustomerService> logger = logger ?? throw new ArgumentNullException(nameof(logger));

        #region [ Actions ]

        public async Task<IBusinessResult<IList<CustomerResult>>> GetBy(CustomerQuery filter, CancellationToken cancellationToken = default)
        {
            pipeline.Add<GetCustomerClientStep>();
            pipeline.Add<GetByCustomerMapperResponseStep>();

            var message = filter.ToMessage();
            message.AddContent("cancellationToken", cancellationToken);
            await pipeline.ExecuteAsync(message);

            var pipelineMessage = pipeline.GetMessage();
            if (pipelineMessage.IsFaulty)
            {
                logger.LogWarning("Customer list pipeline failed with {MessageCount} messages", pipelineMessage.Messages?.Count ?? 0);
                return pipelineMessage.Messages.ToBusiness<IList<CustomerResult>>();
            }

            IList<CustomerResult> result = pipelineMessage.GetContent<List<CustomerResult>>();

            if (!result.AnySafe())
            {
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                        .ToBusiness<IList<CustomerResult>>();
            }

            return result.ToBusiness();
        }

        public async Task<IBusinessResult<CustomerIdResult>> GetById(int id, CancellationToken cancellationToken = default)
        {
            pipeline.Add<GetCustomerClientStep>();
            pipeline.Add<GetByIdCustomerMapperResponseStep>();

            var message = id.ToMessage("id");
            message.AddContent("cancellationToken", cancellationToken);
            await pipeline.ExecuteAsync(message);

            var pipelineMessage = pipeline.GetMessage();
            if (pipelineMessage.IsFaulty)
            {
                logger.LogWarning("Customer get-by-id pipeline failed for {CustomerId}", id);
                return pipelineMessage.Messages.ToBusiness<CustomerIdResult>();
            }

            var result = pipelineMessage.GetContent<CustomerIdResult>();

            if (result == null)
            {
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                        .ToBusiness<CustomerIdResult>();
            }
            return result.ToBusiness();
        }

        #endregion
    }
}
