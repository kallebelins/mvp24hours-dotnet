using CustomerAPI.Core.Contract.Logic;

namespace CustomerAPI.Application;

/// <summary>
/// Provides all services available for use in this project.
/// </summary>
public class FacadeService(ICustomerService customerService)
{
    /// <summary>
    /// <see cref="ICustomerService"/>
    /// </summary>
    public ICustomerService CustomerService { get; } = customerService;
}
