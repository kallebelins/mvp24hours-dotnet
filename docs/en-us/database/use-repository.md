# How to use a repository?
We use the repository pattern to interact with the database. According to Martin Fowler:
> Mediates between the domain and data mapping layers using a collection-like interface to access domain objects. [Repository](http://martinfowler.com/eaaCatalog/repository.html)

## Prerequisites
Perform installation and configuration to use a [relational](relational.md) or [NoSQL](nosql.md) database.

# Repository
Use the unit of work to load the repository, like this:
```csharp
IRepository<Entity> rpEntity = UnitOfWork.GetRepository<Entity>(); // async => IRepositoryAsync = UnitOfWorkAsync.GetRepository
```

## Predefined Methods
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

## Example of use
```csharp
IRepository<Entity> rpEntity = UnitOfWork.GetRepository<Entity>(); // async => IRepositoryAsync = UnitOfWorkAsync.GetRepository

// list all
var entities = rpEntity.List();

// list everything with pagination
var paging = new PagingCriteria(3, 0); //  limit, offset
var entities = rpEntity.List(paging);

// apply filter
var entities = rpEntity.GetBy(x => x.PropertyName == null);

// apply filter with pagination
var paging = new PagingCriteria(3, 0); //  limit, offset
var entities = rpEntity.GetBy(x => x.PropertyName == null, paging);

// load navigation/relationships
var paging = new PagingCriteria(1, 0, navigation: new List<string> { "PropertyName" });
var entities = rpEntity.List(paging);

// load navigation/relationships with expression
var paging = new PagingCriteriaExpression<Entity>(3, 0); //  limit, offset
paging.NavigationExpr.Add(x => x.PropertyName);
var entities = rpEntity.List(paging);

// apply ordering
var paging = new PagingCriteria(3, 0, orderBy: new List<string> { "PropertyName desc" });
var entities = rpEntity.List(paging);

// apply ordering with expression
var paging = new PagingCriteriaExpression<Entity>(3, 0); //  limit, offset
paging.OrderByDescendingExpr.Add(x => x.PropertyName);
var entities = rpEntity.List(paging);

// load list navigation/relationships with filter and/or pagination
var paging = new PagingCriteria(1, 0, navigation: new List<string> { "PropertyList" });
var entities = rpEntity.List(paging);
foreach (var entity in entities)
{
	rpEntity.LoadRelation(entity, x => x.PropertyList, clause: c => c.Active, limit: 1);
}

// load navigation/list relationships with expression with filter and/or pagination
var paging = new PagingCriteriaExpression<Entity>(3, 0); //  limit, offset
paging.NavigationExpr.Add(x => x.PropertyList);
var entities = rpEntity.List(paging);
foreach (var entity in entities)
{
	rpEntity.LoadRelation(entity, x => x.PropertyList, clause: c => c.Active, limit: 1);
}

// create new criteria from an existing one
var pagingNew = paging.NewCriteria(navigation: new List<string> { "PropertyList" });

// create new expression criteria from an existing one
var pagingExpr = paging.NewCriteriaExpression<Entity>();
pagingExpr.NavigationExpr.Add(x => x.PropertyList);
```