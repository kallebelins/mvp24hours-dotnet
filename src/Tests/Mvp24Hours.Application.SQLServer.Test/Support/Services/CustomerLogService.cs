//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities.Logs;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects.Logic;
using System.Collections.Generic;

namespace Mvp24Hours.Application.SQLServer.Test.Support.Services
{
    public class CustomerLogService(IUnitOfWork unitOfWork) : RepositoryService<CustomerLog, IUnitOfWork>(unitOfWork)
    {

        // custom methods here

        public IList<CustomerLog> GetWithContacts()
        {
            var paging = new PagingCriteria(3, 0);

            var customers = Repository.GetBy(x => x.Contacts.Count != 0, paging);

            foreach (var customer in customers)
            {
                Repository.LoadRelation(customer, x => x.Contacts);
            }
            return customers;
        }

        public IList<CustomerLog> GetWithPagedContacts()
        {
            var paging = new PagingCriteria(3, 0);

            var customers = Repository.GetBy(x => x.Contacts.Count != 0, paging);

            foreach (var customer in customers)
            {
                Repository.LoadRelation(customer, x => x.Contacts, clause: c => c.Active, limit: 1);
            }
            return customers;
        }
    }
}
