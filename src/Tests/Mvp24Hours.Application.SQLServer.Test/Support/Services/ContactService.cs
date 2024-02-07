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
    public class ContactService(IUnitOfWork unitOfWork) : RepositoryService<Customer, IUnitOfWork>(unitOfWork)
    {

        // custom methods here
    }
}
