//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Core.ValueObjects.Logic;

/// <summary>
/// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria"/>
/// </summary>
[DataContract, Serializable]
public class PagingCriteriaExpression<T>(
    int limit,
    int offset,
    IReadOnlyCollection<string>? orderBy = null,
    IReadOnlyCollection<string>? navigation = null) : PagingCriteria(limit, offset, orderBy, navigation), IPagingCriteriaExpression<T>
{
    #region [ Fields ]
    private IList<Expression<Func<T, dynamic>>>? orderByAscendingExpr;
    private IList<Expression<Func<T, dynamic>>>? orderByDescendingExpr;
    private IList<Expression<Func<T, dynamic>>>? navigationExpr;
    #endregion

    #region [ Properties ]
    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria.OrderByAscendingExpr"/>
    /// </summary>
    [IgnoreDataMember]
    [JsonIgnore]
    public IList<Expression<Func<T, dynamic>>> OrderByAscendingExpr => orderByAscendingExpr ??= [];
    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria.OrderByDescendingExpr"/>
    /// </summary>
    [IgnoreDataMember]
    [JsonIgnore]
    public IList<Expression<Func<T, dynamic>>> OrderByDescendingExpr => orderByDescendingExpr ??= [];
    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria.NavigationExpr"/>
    /// </summary>
    [IgnoreDataMember]
    [JsonIgnore]
    public IList<Expression<Func<T, dynamic>>> NavigationExpr => navigationExpr ??= [];
    #endregion

    #region [ Methods ]
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return base.GetEqualityComponents();
        yield return OrderByAscendingExpr;
        yield return OrderByDescendingExpr;
        yield return NavigationExpr;
    }
    #endregion
}
