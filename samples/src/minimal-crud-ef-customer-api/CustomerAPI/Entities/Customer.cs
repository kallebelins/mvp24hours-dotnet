using Mvp24Hours.Core.Entities;

namespace CustomerAPI.Entities
{
    public class Customer : EntityBase<int>
    {
        public DateTime Created { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
        public bool Active { get; set; }
    }
}
