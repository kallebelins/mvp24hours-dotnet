using System.Dynamic;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Infrastructure.Pipe;

public class DynamicContents(IPipelineMessage pipelineMessage) : DynamicObject
{
    private readonly IPipelineMessage _pipelineMessage = pipelineMessage;

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (_pipelineMessage.HasContent(binder.Name))
        {
            result = _pipelineMessage.GetContent<object>(binder.Name) ?? throw new ArgumentNullException($"{binder.Name} property is null in pipeline message");
            return true;
        }

        throw new ArgumentOutOfRangeException($"{binder.Name} property does not exist in pipeline message");
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException($"{binder.Name} property cannot be null in pipeline message");
        }

        _pipelineMessage.AddContent(binder.Name, value);
        return true;
    }

    public override string ToString()
    {
        return base.ToString() ?? nameof(DynamicContents);
    }
}
