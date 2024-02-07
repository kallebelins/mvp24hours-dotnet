# Como utilizar um repositório de serviço?
Você poderá diminuir a complexidade de construção de serviços na camada de aplicação usando padrões da arquitetura Mvp24Hours.

## Pré-Requisitos
Realizar instalação e configuração para usar um banco de dados [relacional](pt-br/database/relational.md) ou [NoSQL](pt-br/database/nosql.md).

## Serviço de Repositório Básico
### Herança
```csharp
public class EntityService : RepositoryService<Entity, IUnitOfWork> { ... } // async => RepositoryServiceAsync<Entity, IUnitOfWorkAsync>
```

### Métodos Pré-Definidos
Utilizamos o padrão de BusinessService / BusinessObject para encapsular os dados de resposta na camada de aplicação. Os métodos são:
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

// Para resultados paginados use:
IPagingResult<IList<TEntity>> ListWithPagination(IPagingCriteria criteria = null); // async => Task<IPagingResult<IList<TEntity>>> ListWithPaginationAsync
IPagingResult<IList<TEntity>> GetByWithPagination(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria = null); // async => Task<IPagingResult<IList<TEntity>>> GetByWithPaginationAsync
```

### Métodos Definidos pelo Usuário
Neste exemplo usaremos a referência para obter contatos de cliente. Serão carregados até dois contatos de cada cliente, veja:
```csharp
public IList<Customer> GetWithContacts()
{
    // cria paginação para cliente
    var paging = new PagingCriteria(3, 0);

    // obtém instância de repositório de cliente
    var rpCustomer = UnitOfWork.GetRepository<Customer>();

    // aplica filtro para clientes que possuam contatos com paginação
    var customers = rpCustomer.GetBy(x => x.Contacts.Any(), paging);

    // percorre resultado de clientes para carregar contatos (carga tardia com filtro e/ou paginação)
    foreach (var customer in customers)
    {
        rpCustomer.LoadRelation(customer, x => x.Contacts, clause: c => c.Active, limit: 2);
    }
    return customers;
}
```

## Serviços Base
Você poderá os seguintes serviços de referência como base para especialização (Mvp24Hours.Application.Logic):
* RepositoryService: consulta e comandos;
* RepositoryPagingService: consulta, consulta paginada e comandos.