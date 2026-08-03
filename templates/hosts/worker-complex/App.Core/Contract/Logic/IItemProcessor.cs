namespace App.Core.Contract.Logic;

public interface IItemProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken = default);
}
