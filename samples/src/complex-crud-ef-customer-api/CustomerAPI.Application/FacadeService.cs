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
        public ICustomerService CustomerService
        {
            get { return provider.GetService<ICustomerService>(); }
        }
        /// <summary>
        /// <see cref="CustomerAPI.Core.Contract.Logic.IContactService"/>
        /// </summary>
        public IContactService ContactService
        {
            get { return provider.GetService<IContactService>(); }
        }
        #endregion
    }
}
