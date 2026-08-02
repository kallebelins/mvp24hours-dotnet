//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Core.Contract.Application.Pipe.Async;

/// <summary>
/// Strategic for aggregating operations in the pipeline
/// </summary>
/// <example><![CDATA[
/// // interface
/// public interface IProductCategoryListBuilder : IPipelineBuilderAsync { }
/// 
/// // implementation
/// public class ProductCategoryListBuilder : IProductCategoryListBuilder
/// {
///     public IPipelineAsync Builder(IPipelineAsync pipeline)
///     {
///         return pipeline
///             .Add<ProductCategoryFileOperation>()
///             .Add<ProductCategoryResponseMapperOperation>();
///     }
/// }
/// 
/// // use
/// var builder = ServiceProviderHelper.GetService{IProductGetByBuilder}();
/// builder.Builder(pipeline);
/// ]]></example>
public interface IPipelineBuilderAsync
{
    /// <summary>
    /// Operations aggregator
    /// </summary>
    IPipelineAsync Builder(IPipelineAsync pipeline);
}
