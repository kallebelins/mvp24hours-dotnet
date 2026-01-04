# How to use a service repository?
You will be able to reduce the complexity of building services at the application layer using Mvp24Hours architecture patterns.

## Prerequisites
Perform installation and configuration to use a [relational](relational.md) or [NoSQL](nosql.md) database.

## Core Repository Service
### Inheritance
```csharp
public class EntityService : RepositoryService<Entity, IUnitOfWork> { ... } // async => RepositoryServiceAsync<Entity, IUnitOfWorkAsync>
```

### Predefined Methods
We use the BusinessService / BusinessObject pattern to encapsulate the response data in the application layer. The methods are:
```csharp
IBusinessResult<bool> ListAny(); // async => Task<IBusinessResult<bool>> ListAnyAsync
IBusinessResult<int> ListCount(); // async => Task<IBusinessResult<int>> ListCountAsync
IBusinessResult<IList<TEntity>> List(); // async => Task<IBusinessResult<IList<TEntity>>> ListAsync
IBusinessResult<IList<TEntity>> List(IPagingCriteria criteria); // async => Task<IBusinessResult<IList<TEntity>>> ListAsync
IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause); // async => Task<IBusinessResult<bool>> GetByAnyAsync
IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause); // async => Task<IBusinessResult<int>> GetByCountAsync
IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause); // async => Task<IBusinessResult<IList<TEntity>>> GetByAsync
IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria); // async => Task<IBusinessResult<IList<TEntity>>> GetByAsync
IBusinessResult<TEntity> GetById(object id); // async => Task<IBusinessResult<TEntity>> GetByIdAsync
IBusinessResult<TEntity> GetById(object id, IPagingCriteria criteria); // async => Task<IBusinessResult<TEntity>> GetByIdAsync

// For paginated results use:
IPagingResult<IList<TEntity>> ListWithPagination(IPagingCriteria criteria = null); // async => Task<IPagingResult<IList<TEntity>>> ListWithPaginationAsync
IPagingResult<IList<TEntity>> GetByWithPagination(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria = null); // async => Task<IPagingResult<IList<TEntity>>> GetByWithPaginationAsync
```

### User Defined Methods
In this example we will use the reference to obtain customer contacts. Up to two contacts for each customer will be uploaded, see:
```csharp
public IList<Customer> GetWithContacts()
{
    // create pagination for client
    var paging = new PagingCriteria(3, 0);

    // get client repository instance
    var rpCustomer = UnitOfWork.GetRepository<Customer>();

    // applies filter to customers who have contacts with pagination
    var customers = rpCustomer.GetBy(x => x.Contacts.Any(), paging);

    //scroll through customer results to load contacts (late load with filter and/or pagination)
    foreach (var customer in customers)
    {
        rpCustomer.LoadRelation(customer, x => x.Contacts, clause: c => c.Active, limit: 2);
    }
    return customers;
}
```

## Base Services
You will be able to reference the following services as a basis for expertise (Mvp24Hours.Application.Logic):
* RepositoryService: query and commands;
* RepositoryPagingService: query, paged query and commands.