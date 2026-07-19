using System.Linq.Expressions;
using Mvp24Hours.Core.Contract.Domain.Specifications;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

public sealed class ActiveTestEntitySpecification : ISpecificationQuery<TestEntity>
{
    public Expression<Func<TestEntity, bool>> IsSatisfiedByExpression => e => e.Active;
}
