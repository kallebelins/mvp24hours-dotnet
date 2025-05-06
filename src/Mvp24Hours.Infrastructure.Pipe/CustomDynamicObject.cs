using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Dynamic;

namespace Mvp24Hours.Infrastructure.Pipe
{
    public class CustomDynamicObject : DynamicObject
    {
        private readonly IPipelineMessage _pipelineMessage;

        public CustomDynamicObject(IPipelineMessage pipelineMessage)
        {
            _pipelineMessage = pipelineMessage;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_pipelineMessage.HasContent(binder.Name))
            {
                result = _pipelineMessage.GetContent<object>(binder.Name);

                if (result == null)
                    throw new ArgumentNullException($"{binder.Name} property is null in pipeline message");

                return true;
            }

            throw new ArgumentOutOfRangeException($"{binder.Name} property does not exist in pipeline message");
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (value == null)
                throw new ArgumentNullException($"{binder.Name} property cannot be null in pipeline message");

            _pipelineMessage.AddContent(binder.Name, value);
            return true;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
