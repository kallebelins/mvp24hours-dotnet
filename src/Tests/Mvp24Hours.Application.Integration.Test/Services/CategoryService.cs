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
/// Category service for integration tests.
/// </summary>
public class CategoryService : RepositoryServiceAsync<Category, IUnitOfWorkAsync>
{
    public CategoryService(IUnitOfWorkAsync unitOfWork) : base(unitOfWork)
    {
    }
}
