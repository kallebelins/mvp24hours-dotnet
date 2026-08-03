using App.Core.Contract.Logic;
using Microsoft.Extensions.DependencyInjection;

namespace App.Application;

/// <summary>
/// Resolves application services for controllers.
/// </summary>
public class FacadeService(IServiceProvider provider)
{
    public IItemService ItemService => provider.GetRequiredService<IItemService>();
}
