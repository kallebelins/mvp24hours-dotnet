//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Core.Contract.Data;

namespace Mvp24Hours.Application.SQLServer.Test.Support.Services
{
    public class CustomerPagingService(IUnitOfWork unitOfWork) : RepositoryPagingService<Customer, IUnitOfWork>(unitOfWork)
    {

        // custom methods
    }
}
