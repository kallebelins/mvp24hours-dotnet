using CustomerAPI.Application.Pipe.Operations.Customers;
using CustomerAPI.Core.Contract.Pipe.Builders;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace CustomerAPI.Application.Pipe.Builders;

/// <summary>
/// Composes the get-by-filter use case. Steps are constructor-injected so the builder is unit-testable without a service locator.
/// </summary>
public class GetByCustomerBuilder(
    GetCustomerClientStep clientStep,
    GetByCustomerMapperResponseStep mapperStep) : IGetByCustomerBuilder
{
    public IPipelineAsync Builder(IPipelineAsync pipeline) => pipeline
        .Add(clientStep)
        .Add(mapperStep);
}
