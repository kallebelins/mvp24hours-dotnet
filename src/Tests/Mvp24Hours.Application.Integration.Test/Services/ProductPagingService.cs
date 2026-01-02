//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Data;

namespace Mvp24Hours.Application.Integration.Test.Services;

/// <summary>
/// Product paging service for integration tests - inherits from RepositoryPagingServiceAsync.
/// </summary>
public class ProductPagingService : RepositoryPagingServiceAsync<Product, IUnitOfWorkAsync>
{
    public ProductPagingService(IUnitOfWorkAsync unitOfWork) : base(unitOfWork)
    {
    }
}
