# Especificação (Specification Pattern)
O padrão de especificação usamos como requisitos para filtros de pesquisa. Cada especificação deve ser criada com o objetivo de definir casos concretos. Cada especificação poderá fazer parte de uma composição ou ser aplicada individualmente.

>Na programação de computadores, o padrão de especificação é um padrão de design de software específico, por meio do qual as regras de negócios podem ser recombinadas encadeando as regras de negócios usando a lógica booleana. O padrão é freqüentemente usado no contexto de design orientado a domínio. [Wikipédia](https://en.wikipedia.org/wiki/Specification_pattern)

### Exemplo
Neste exemplo criamos um especificação que filtra pessoas que tenham número de celular no registro de contato.

```csharp
/// CustomerHasCellContactSpec.cs

public class CustomerHasCellContactSpec : ISpecificationQuery<Customer>
{
    public Expression<Func<Customer, bool>> IsSatisfiedByExpression => x => x.Contacts.Any(y => y.Type == ContactType.CellPhone);
}

/// CustomerService.cs -> Get Method

Expression<Func<Customer, bool>> filter = x => x.Active;
filter = filter.And<Customer, CustomerHasCellContactSpec>();
var paging = new PagingCriteriaExpression<Customer>(3, 0);
paging.NavigationExpr.Add(x => x.Contacts);
var boResult = service.GetBy(filter, paging);

```
