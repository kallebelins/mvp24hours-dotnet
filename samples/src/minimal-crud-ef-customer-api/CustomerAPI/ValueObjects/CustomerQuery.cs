using Mvp24Hours.WebAPI.Binders;

namespace CustomerAPI.ValueObjects
{
    /// <summary>
    /// Template for customer filter
    /// </summary>
    public class CustomerQuery : ExtensionBinder<CustomerQuery>
    {
        /// <summary>
        /// Client name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Indicates whether the client is active/inactive
        /// </summary>
        public bool? Active { get; set; }
    }
}
