# Como utilizar um repositório?
Utilizamos o padrão de repositório para interação com banco de dados. Segundo Martin Fowler:
> Faz a mediação entre o domínio e as camadas de mapeamento de dados usando uma interface semelhante a uma coleção para acessar objetos de domínio. [Repository](http://martinfowler.com/eaaCatalog/repository.html)

## Pré-Requisitos
Realizar instalação e configuração para usar um banco de dados [relacional](database/relational.md) ou [NoSQL](database/nosql.md).

# Repositório
Utilize a unidade de trabalho para carregar o repositório, assim:
```csharp
IRepository<Entity> rpEntity = UnitOfWork.GetRepository<Entity>(); // async => IRepositoryAsync = UnitOfWorkAsync.GetRepository
```

## Métodos Pré-Definidos
```csharp
// IQuery / IQueryAsync
bool ListAny(); // async => ListAnyAsync
int ListCount(); // async => ListCountAsync
IList<TEntity> List(); // async => ListAsync
IList<TEntity> List(IPagingCriteria criteria); // async => ListAsync
bool GetByAny(Expression<Func<TEntity, bool>> clause); // async => GetByAnyAsync
int GetByCount(Expression<Func<TEntity, bool>> clause); // async => GetByCountAsync
IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause); // async => GetByAsync
IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria); // async => GetByAsync
TEntity GetById(object id); // async => GetByIdAsync
TEntity GetById(object id, IPagingCriteria criteria); // async => GetByIdAsync

// ICommand /  ICommandAsync
void Add(TEntity entity); // async => AddAsync
void Add(IList<TEntity> entities); // async => AddAsync
void Modify(TEntity entity); // async => ModifyAsync
void Modify(IList<TEntity> entities); // async => ModifyAsync
void Remove(TEntity entity); // async => RemoveAsync
void Remove(IList<TEntity> entities); // async => RemoveAsync
void RemoveById(object id); // async => RemoveByIdAsync
void RemoveById(IList<object> ids); // async => RemoveByIdAsync

// IRelation / IRelationAsync
void LoadRelation<TProperty>(TEntity entity,
	Expression<Func<TEntity, TProperty>> propertyExpression)
	where TProperty : class; // async => LoadRelationAsync
void LoadRelation<TProperty>(TEntity entity,
	Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
	Expression<Func<TProperty, bool>> clause = null,
	int limit = 0)
	where TProperty : class; // async => LoadRelationAsync
void LoadRelationSortByAscending<TProperty, TKey>(TEntity entity,
	Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
	Expression<Func<TProperty, TKey>> orderKey,
	Expression<Func<TProperty, bool>> clause = null,
	int limit = 0)
	where TProperty : class; // async => LoadRelationSortByAscendingAsync
void LoadRelationSortByDescending<TProperty, TKey>(TEntity entity,
	Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
	Expression<Func<TProperty, TKey>> orderKey,
	Expression<Func<TProperty, bool>> clause = null,
	int limit = 0)
	where TProperty : class; // async => LoadRelationSortByDescendingAsync
```

## Exemplo de Uso
```csharp
IRepository<Entity> rpEntity = UnitOfWork.GetRepository<Entity>(); // async => IRepositoryAsync = UnitOfWorkAsync.GetRepository

// listar tudo
var entities = rpEntity.List();

// listar tudo com paginação
var paging = new PagingCriteria(3, 0); //  limit, offset
var entities = rpEntity.List(paging);

// aplicar filtro
var entities = rpEntity.GetBy(x => x.PropertyName == null);

// aplicar filtro com paginação
var paging = new PagingCriteria(3, 0); //  limit, offset
var entities = rpEntity.GetBy(x => x.PropertyName == null, paging);

// carregar navegação/relacionamentos
var paging = new PagingCriteria(1, 0, navigation: new List<string> { "PropertyName" });
var entities = rpEntity.List(paging);

// carregar navegação/relacionamentos com expressão
var paging = new PagingCriteriaExpression<Entity>(3, 0); //  limit, offset
paging.NavigationExpr.Add(x => x.PropertyName);
var entities = rpEntity.List(paging);

// aplicar ordenação
var paging = new PagingCriteria(3, 0, orderBy: new List<string> { "PropertyName desc" });
var entities = rpEntity.List(paging);

// aplicar ordenação com expressão
var paging = new PagingCriteriaExpression<Entity>(3, 0); //  limit, offset
paging.OrderByDescendingExpr.Add(x => x.PropertyName);
var entities = rpEntity.List(paging);

// carregar navegação/relacionamentos de lista com filtro e/ou paginação
var paging = new PagingCriteria(1, 0, navigation: new List<string> { "PropertyList" });
var entities = rpEntity.List(paging);
foreach (var entity in entities)
{
	rpEntity.LoadRelation(entity, x => x.PropertyList, clause: c => c.Active, limit: 1);
}

// carregar navegação/relacionamentos de lista com expressão com filtro e/ou paginação
var paging = new PagingCriteriaExpression<Entity>(3, 0); //  limit, offset
paging.NavigationExpr.Add(x => x.PropertyList);
var entities = rpEntity.List(paging);
foreach (var entity in entities)
{
	rpEntity.LoadRelation(entity, x => x.PropertyList, clause: c => c.Active, limit: 1);
}

// criar novo critério de um existente
var pagingNew = paging.NewCriteria(navigation: new List<string> { "PropertyList" });

// criar novo critério de expressão de um existente
var pagingExpr = paging.NewCriteriaExpression<Entity>();
pagingExpr.NavigationExpr.Add(x => x.PropertyList);
```