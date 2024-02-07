//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Application.Redis.Test.Support.Entities
{
    public class Customer
    {
        public Guid Oid { get; set; }

        public DateTime Created { get; set; }

        public string Name { get; set; }

        public bool Active { get; set; }
    }
}
