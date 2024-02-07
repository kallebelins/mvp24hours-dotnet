using System;
using System.Collections.Generic;

namespace Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authorization
{
    public class UserToken
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string PreferredUserName { get; set; }
        public string Email { get; set; }
        public bool? EmailVerified { get; set; }
        public string Scope { get; set; }
        public Guid? SessionId { get; set; }
        public Guid? SessionState { get; set; }
        public string AuthorizedParty { get; set; }
        public List<string> AllowedOrigins { get; set; }
        public List<string> RealmRoles { get; set; }
        public List<string> ResourceRoles { get; set; }
    }
}
