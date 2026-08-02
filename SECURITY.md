# Security Policy

## 🛡️ Supported Versions

We provide security updates for the following Mvp24Hours versions:

| Version | Supported          | Support until     |
| ------- | ------------------ | ----------------- |
| 9.1.x   | ✅ Yes             | Current           |
| 9.0.x   | ✅ Yes             | Jun 2027          |
| 8.3.x   | ⚠️ Limited         | Dec 2026          |
| 8.2.x   | ❌ No              | EOL               |
| < 8.2   | ❌ No              | EOL               |

**Recommendation:** Always use the latest version to ensure you have the most up-to-date security fixes.

## 🔒 Reporting a Vulnerability

Mvp24Hours security is a priority. If you discover a security vulnerability, please **DO NOT** open a public issue.

### Reporting Process

1. **📧 Send an email to:** [kallebe.santos@outlook.com]
   
   Include the following information:
   - Detailed description of the vulnerability
   - Steps to reproduce the issue
   - Affected versions
   - Potential impact
   - Suggested fix (if any)

2. **⏱️ Response Time:**
   - We will confirm receipt within 48 hours
   - We will assess the vulnerability within 7 days
   - We will keep you informed about progress

3. **🔍 Assessment:**
   - We will verify and validate the report
   - We will determine severity (Critical, High, Medium, Low)
   - We will develop a fix

4. **🚀 Disclosure:**
   - We will release a fix
   - We will publish a security advisory
   - We will credit you (if desired) for the discovery

### Vulnerability Severity

We classify vulnerabilities using [CVSS v3.1](https://www.first.org/cvss/):

- **🔴 Critical (9.0-10.0):** Remote exploitation without authentication
- **🟠 High (7.0-8.9):** Significant data or system compromise
- **🟡 Medium (4.0-6.9):** Limited access to sensitive information
- **🟢 Low (0.1-3.9):** Minimal security impact

## 🎯 Security Scope

### In Scope

Vulnerabilities related to:

- ✅ Injection (SQL, NoSQL, Command, etc.)
- ✅ Broken authentication and authorization
- ✅ Sensitive data exposure
- ✅ XXE (XML External Entities)
- ✅ Broken access control
- ✅ Security misconfiguration
- ✅ XSS (Cross-Site Scripting)
- ✅ Insecure deserialization
- ✅ Components with known vulnerabilities
- ✅ Insufficient logging and monitoring
- ✅ CSRF (Cross-Site Request Forgery)
- ✅ Path traversal
- ✅ Denial of Service (DoS)

### Out of Scope

- ❌ Usability issues
- ❌ Bugs with no security impact
- ❌ Vulnerabilities in third-party dependencies (report directly to maintainers)
- ❌ Social engineering attacks
- ❌ Physical attacks

## 🔐 Security Best Practices

### For Library Users

#### 1. Input Validation
```csharp
// ✅ GOOD: Use proper validation
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(150);
    }
}
```

#### 2. Query Parameterization
```csharp
// ✅ GOOD: Use Repository pattern (automatic in Mvp24Hours)
var customers = repository.GetBy(c => c.Name == userName);

// ❌ BAD: String concatenation (avoid!)
// var sql = $"SELECT * FROM Customers WHERE Name = '{userName}'";
```

#### 3. Exception Handling
```csharp
// ✅ GOOD: Do not expose internal details
try
{
    // operation
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    return new MessageResult("An error occurred")
        .ToBusiness<Customer>();
}

// ❌ BAD: Expose stack trace to the client
// throw new Exception(ex.ToString());
```

#### 4. Secure Configuration
```csharp
// ✅ GOOD: Use User Secrets in development
// dotnet user-secrets set "ConnectionStrings:Default" "..."

// ✅ GOOD: Use environment variables in production
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

// ❌ BAD: Hard-coded secrets
// var connectionString = "Server=...;Password=secret123";
```

#### 5. HTTPS and Encryption
```csharp
// ✅ GOOD: Force HTTPS in production
app.UseHttpsRedirection();
app.UseHsts();

// ✅ GOOD: Encrypt sensitive data
// Use ASP.NET Core Data Protection API
```

#### 6. Auditing and Logging
```csharp
// ✅ GOOD: Use Mvp24Hours auditing features
public class Customer : EntityBaseLog<int, string>
{
    // Automatic Created, Modified, Removed tracking
}

// ✅ GOOD: Log sensitive operations
_logger.LogInformation(
    "User {UserId} accessed customer {CustomerId}",
    userId, customerId
);
```

### For Contributors

1. **Never commit secrets:** Use .gitignore to exclude sensitive files
2. **Review dependencies:** Check for known vulnerabilities
3. **Validate input:** Always validate and sanitize user input
4. **Use async safely:** Avoid race conditions
5. **Test security:** Include security tests in PRs

## 📋 Security Checklist

Before deploying to production:

- [ ] All dependencies are up to date
- [ ] Secrets are not in code or configuration
- [ ] HTTPS is enabled
- [ ] Input validation is implemented
- [ ] Logs do not contain sensitive information
- [ ] Error handling does not expose internal details
- [ ] Authentication and authorization are configured
- [ ] Auditing is enabled
- [ ] Backups are configured
- [ ] Monitoring is active

## 🔄 Security Updates

### How We Stay Secure

1. **Monitoring:** We continuously monitor for vulnerabilities
2. **Automated Scans:** GitHub Dependabot and CodeQL
3. **Code Review:** All PRs go through review
4. **Testing:** Automated test suite
5. **Updates:** Regular patch releases

### How to Stay Up to Date

- ⭐ **Watch** the repository on GitHub
- 📧 Enable release notifications
- 📖 Read [CHANGELOG.md](CHANGELOG.md)
- 🔔 Follow [@kallebelins](https://linkedin.com/in/kallebelins) on LinkedIn

## 🏆 Security Hall of Fame

We thank researchers who responsibly report vulnerabilities.

<!-- 
Security contributors will be listed here after disclosure:

### 2025
- [Name] - [Vulnerability description]
-->

**No vulnerabilities reported yet.**

The project has followed security practices from the start. If you find a vulnerability, you will be the first to be recognized! 🎯

## 📚 Additional Resources

### .NET Security
- [ASP.NET Core Security Guide](https://docs.microsoft.com/aspnet/core/security/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Guidelines](https://docs.microsoft.com/dotnet/standard/security/)

### Database Security
- [SQL Injection Prevention](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)
- [MongoDB Security Checklist](https://docs.mongodb.com/manual/administration/security-checklist/)

### Tools
- [Snyk](https://snyk.io/) - Vulnerability scanning
- [OWASP Dependency-Check](https://owasp.org/www-project-dependency-check/)
- [WhiteSource](https://www.whitesourcesoftware.com/)

## 📞 Contact

For security-related questions:

- **Email:** [kallebe.santos@outlook.com]
- **LinkedIn:** [Kallebe Lins](https://linkedin.com/in/kallebelins)
- **PGP Key:** [Add your PGP key here if available]

---

**Thank you for helping keep Mvp24Hours secure! 🛡️**

*Last updated: January 2026*
