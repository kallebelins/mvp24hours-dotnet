//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.WebAPI.Configuration
{
    public class CorsOptions
    {
        public bool AllowAll { get; set; }

        public bool AllowRequestOptions { get; set; } = true;

        public string Origin { get; set; }
        public string Headers { get; set; }
        public string Methods { get; set; }

        public string Credentials { get; set; }
    }
}
