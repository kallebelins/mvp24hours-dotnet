using CustomerAPI.Core.Contract.Logic;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerAPI.Application;

/// <summary>
/// Provides all services available for use in this project.
/// </summary>
public class FacadeService(IServiceProvider provider)
{
    #region [ Services ]

    /// <summary>
    /// <see cref="ICustomerService"/>
    /// </summary>
    public ICustomerService CustomerService => provider.GetRequiredService<ICustomerService>();

    /// <summary>
    /// <see cref="IContactService"/>
    /// </summary>
    public IContactService ContactService => provider.GetRequiredService<IContactService>();

    #endregion
}
