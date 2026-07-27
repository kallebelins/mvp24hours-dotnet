using CustomerAPI.Core.Contract.Pipe.Builders;
using CustomerAPI.Typicode.Application.Pipe.Operations.Customers;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace CustomerAPI.Typicode.Application.Pipe.Builders;

/// <summary>
/// Outbound-adapter builder for the get-by-filter use case. Steps are constructor-injected for testability.
/// </summary>
public class GetByCustomerBuilder(
    GetCustomerClientStep clientStep,
    GetByCustomerMapperResponseStep mapperStep) : IGetByCustomerBuilder
{
    public IPipelineAsync Builder(IPipelineAsync pipeline) => pipeline
        .Add(clientStep)
        .Add(mapperStep);
}
