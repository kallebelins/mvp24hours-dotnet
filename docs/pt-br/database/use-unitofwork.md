# Como utilizar uma unidade de trabalho?
Utilizamos o padrão de unidade de trabalho para controlar as transações. Segundo Martin Fowler:
> Mantém uma lista de objetos afetados por uma transação de negócios e coordena a gravação das alterações e a resolução de problemas de simultaneidade. [Unit Of Work](http://martinfowler.com/eaaCatalog/unitOfWork.html)

## Pré-Requisitos
Realizar instalação e configuração para usar um banco de dados [relacional](pt-br/database/relational.md) ou [NoSQL](pt-br/database/nosql.md).

# Unidade de Trabalho
Para obter basta aplicar o conceito de injeção através do construtor ou utilizar o provedor de ajuda da arquitetura Mvp24Hours, assim:
```csharp
IUnitOfWork unitOfWork = serviceProvider.GetService<IUnitOfWork>(); // async => IUnitOfWorkAsync
```

## Métodos Pré-Definidos
```csharp
// IUnitOfWork / IUnitOfWorkAsync
int SaveChanges(CancellationToken cancellationToken = default); // async => SaveChangesAsync
void Rollback(); // async => RollbackAsync
IRepository<T> GetRepository<T>() where T : class, IEntityBase;
IDbConnection GetConnection();
```

## Usando Dapper
Abaixo segue pacote para instalação e execução de comandos/consultas SQL com Dapper.

```csharp
/// Package Manager Console >
Install-Package Dapper -Version 2.0.123

/// Exemplo
var result = await UnitOfWork
    .GetConnection()
    .QueryAsync<Contact>("select * from Contact where CustomerId = @customerId;", new { customerId });
```