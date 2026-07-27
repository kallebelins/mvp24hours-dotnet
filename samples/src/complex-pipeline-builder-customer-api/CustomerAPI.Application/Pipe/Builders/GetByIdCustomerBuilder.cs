using CustomerAPI.Application.Pipe.Operations.Customers;
using CustomerAPI.Core.Contract.Pipe.Builders;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace CustomerAPI.Application.Pipe.Builders;

/// <summary>
/// Composes the get-by-id use case. Steps are constructor-injected so the builder is unit-testable without a service locator.
/// </summary>
public class GetByIdCustomerBuilder(
    GetCustomerClientStep clientStep,
    GetByIdCustomerMapperResponseStep mapperStep) : IGetByIdCustomerBuilder
{
    public IPipelineAsync Builder(IPipelineAsync pipeline) => pipeline
        .Add(clientStep)
        .Add(mapperStep);
}
