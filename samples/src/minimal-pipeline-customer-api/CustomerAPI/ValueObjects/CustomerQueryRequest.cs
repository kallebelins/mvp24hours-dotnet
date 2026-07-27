using Mvp24Hours.WebAPI.Binders;

namespace CustomerAPI.ValueObjects
{
    public class CustomerQueryRequest : ExtensionBinder<CustomerQueryRequest>
    {
        public string Name { get; set; }

        public bool HasCellContact { get; set; }
        public bool HasEmailContact { get; set; }
    }
}
