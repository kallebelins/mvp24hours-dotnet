using CustomerAPI.Core.Contract.Logic;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CustomerAPI.Application
{
    /// <summary>
    /// Provides all services available for use in this project
    /// </summary>
    public class FacadeService(IServiceProvider provider)
    {
        #region [ Services ]
        /// <summary>
        /// <see cref="CustomerAPI.Core.Contract.Logic.ICustomerService"/>
        /// </summary>
        public ICustomerService CustomerService => provider.GetService<ICustomerService>();
        #endregion
    }
}
