//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Application.MySql.Test.Support.Entities;
using Mvp24Hours.Core.Contract.Data;

namespace Mvp24Hours.Application.MySql.Test.Support.Services.Async
{
    public class CustomerPagingServiceAsync(IUnitOfWorkAsync unitOfWork) : RepositoryPagingServiceAsync<Customer, IUnitOfWorkAsync>(unitOfWork)
    {

        // custom methods here
    }
}
