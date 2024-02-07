//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Configuration
{
    [Serializable]
    public sealed class MongoDbOptions
    {
        public string DatabaseName { get; set; }
        public string ConnectionString { get; set; }
        public bool EnableTls { get; set; }
        public bool EnableTransaction { get; set; }
    }
}
