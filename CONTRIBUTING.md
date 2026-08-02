# Contributing to Mvp24Hours

First of all, thank you for considering contributing to Mvp24Hours! 🎉

It is thanks to people like you that Mvp24Hours continues to be a useful, high-quality library for the .NET community.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Getting Started](#getting-started)
- [Development Process](#development-process)
- [Code Standards](#code-standards)
- [Commits and Pull Requests](#commits-and-pull-requests)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Improvements](#suggesting-improvements)
- [Documentation](#documentation)
- [Tests](#tests)
- [Community](#community)

## 📜 Code of Conduct

This project and all participants are governed by a code of conduct. By participating, you agree to uphold this code. Please report unacceptable behavior to [kallebe.santos@outlook.com].

### Our Standards

**Behaviors that contribute to a positive environment include:**

- ✅ Using welcoming and inclusive language
- ✅ Being respectful of differing viewpoints and experiences
- ✅ Gracefully accepting constructive criticism
- ✅ Focusing on what is best for the community
- ✅ Showing empathy toward other community members

**Unacceptable behaviors include:**

- ❌ Use of sexualized language or imagery
- ❌ Trolling, insulting or derogatory comments, and personal or political attacks
- ❌ Public or private harassment
- ❌ Publishing others' private information without explicit permission
- ❌ Other conduct that could reasonably be considered inappropriate

## 🤝 How Can I Contribute?

There are several ways to contribute to Mvp24Hours:

### 1. Report Bugs 🐛
Found a bug? [Open an issue](https://github.com/kallebelins/mvp24hours-dotnet/issues/new?template=bug_report.md)

### 2. Suggest Improvements 💡
Have an idea? [Suggest an improvement](https://github.com/kallebelins/mvp24hours-dotnet/issues/new?template=feature_request.md)

### 3. Improve Documentation 📖
Documentation is never perfect. Corrections and improvements are always welcome!

### 4. Write Code 💻
- Implement new features
- Fix existing bugs
- Improve performance
- Add tests

### 5. Review Pull Requests 👀
Help by reviewing PRs from other contributors

### 6. Share 📢
Share the project on social media, blogs, events, etc.

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Git](https://git-scm.com/)
- Recommended IDE: [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Docker](https://www.docker.com/) (optional, for integration tests)

### Setting Up the Environment

1. **Fork the repository**
   
   Click the "Fork" button in the upper-right corner of the GitHub page.

2. **Clone your fork**
   ```bash
   git clone https://github.com/your-username/mvp24hours-dotnet.git
   cd mvp24hours-dotnet
   ```

3. **Add the upstream repository**
   ```bash
   git remote add upstream https://github.com/kallebelins/mvp24hours-dotnet.git
   ```

4. **Restore dependencies**
   ```bash
   dotnet restore
   ```

5. **Build the project**
   ```bash
   dotnet build
   ```

6. **Run the tests**
   ```bash
   dotnet test
   ```

### Project Structure

```
mvp24hours-dotnet/
├── src/
│   ├── Mvp24Hours.Core/                    # Base contracts and interfaces
│   ├── Mvp24Hours.Application/             # Services and application logic
│   ├── Mvp24Hours.Infrastructure/          # Utilities and helpers
│   ├── Mvp24Hours.Infrastructure.Data.EFCore/     # Entity Framework Core
│   ├── Mvp24Hours.Infrastructure.Data.MongoDb/    # MongoDB
│   ├── Mvp24Hours.Infrastructure.Caching/         # Base cache
│   ├── Mvp24Hours.Infrastructure.Caching.Redis/   # Redis
│   ├── Mvp24Hours.Infrastructure.RabbitMQ/        # RabbitMQ
│   ├── Mvp24Hours.Infrastructure.Pipe/            # Pipeline
│   ├── Mvp24Hours.Infrastructure.CronJob/         # CronJob
│   ├── Mvp24Hours.WebAPI/                         # Web API extensions
│   └── Tests/                              # Unit and integration tests
├── docs/                                   # Documentation
├── CHANGELOG.md                            # Change history
├── CONTRIBUTING.md                         # This file
└── README.md                               # Main readme
```

## 🔨 Development Process

### 1. Choose an Issue

Browse [open issues](https://github.com/kallebelins/mvp24hours-dotnet/issues) and pick one that interests you.

Issues labeled `good first issue` are ideal for beginners.

### 2. Create a Branch

```bash
# Update your fork
git checkout main
git pull upstream main

# Create a new branch
git checkout -b feature/my-new-feature
# or
git checkout -b fix/bug-fix
```

**Branch naming convention:**
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation changes
- `refactor/` - Refactoring
- `test/` - Test additions or fixes
- `perf/` - Performance improvements

### 3. Make Your Changes

- Write clean, well-documented code
- Follow [code standards](#code-standards)
- Add tests for new features
- Update documentation if necessary

### 4. Test Your Changes

```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test src/Tests/Mvp24Hours.Core.Test/

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

### 5. Commit Your Changes

See [Commits and Pull Requests](#commits-and-pull-requests) for conventions.

### 6. Push to Your Fork

```bash
git push origin feature/my-new-feature
```

### 7. Open a Pull Request

- Go to the upstream repository on GitHub
- Click "Pull Request"
- Select your branch
- Fill in the PR template with details
- Wait for review

## 📝 Code Standards

### C# Style Guide

We follow Microsoft's [C# coding conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions).

#### Main Rules:

1. **Naming**
   ```csharp
   // Classes, methods, and properties: PascalCase
   public class CustomerService { }
   public string FirstName { get; set; }
   public void GetCustomer() { }
   
   // Local variables and parameters: camelCase
   var customerId = 1;
   public void Add(Customer customer) { }
   
   // Interfaces: start with 'I'
   public interface IRepository<T> { }
   
   // Constants: PascalCase
   public const int MaxRetries = 3;
   
   // Private fields: start with '_'
   private readonly ILogger _logger;
   ```

2. **Indentation and Formatting**
   ```csharp
   // Use 4 spaces for indentation (not tabs)
   // Braces on a new line (Allman style)
   public void MyMethod()
   {
       if (condition)
       {
           // code
       }
   }
   ```

3. **XML Documentation**
   ```csharp
   /// <summary>
   /// Retrieves a customer by ID.
   /// </summary>
   /// <param name="id">The customer identifier.</param>
   /// <returns>The customer if found; otherwise, null.</returns>
   /// <exception cref="ArgumentException">Thrown when id is invalid.</exception>
   public Customer GetById(int id)
   {
       // implementation
   }
   ```

4. **Async/Await**
   ```csharp
   // Always use 'Async' in async method names
   public async Task<Customer> GetCustomerAsync(int id)
   {
       return await repository.GetByIdAsync(id);
   }
   
   // Use ConfigureAwait(false) in libraries
   var result = await operation.ExecuteAsync().ConfigureAwait(false);
   ```

5. **Error Handling**
   ```csharp
   // Use custom exceptions from the Mvp24Hours hierarchy
   if (customer == null)
   {
       throw new DataException(
           "Customer not found",
           "CUSTOMER_NOT_FOUND",
           new Dictionary<string, object> { ["CustomerId"] = id }
       );
   }
   ```

6. **SOLID Principles**
   - **S**ingle Responsibility Principle
   - **O**pen/Closed Principle
   - **L**iskov Substitution Principle
   - **I**nterface Segregation Principle
   - **D**ependency Inversion Principle

### Static Analysis

Run static analysis before committing:

```bash
# Check for code issues
dotnet format --verify-no-changes

# Format automatically
dotnet format
```

## 💬 Commits and Pull Requests

### Commit Messages

We follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).

**Format:**
```
<type>[optional scope]: <description>

[optional body]

[optional footer]
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Formatting, semicolons, etc. (no code change)
- `refactor`: Code refactoring
- `perf`: Performance improvement
- `test`: Test additions or fixes
- `chore`: Build process or tooling changes

**Examples:**
```bash
# Simple feature
git commit -m "feat: add pagination support to repository"

# Fix with scope
git commit -m "fix(efcore): resolve null reference in GetById method"

# With detailed body
git commit -m "feat(rabbitmq): implement dead letter queue

- Add configuration for DLQ
- Implement retry mechanism
- Add tests for DLQ flow

Closes #123"

# Breaking change
git commit -m "feat!: change IRepository signature

BREAKING CHANGE: GetById now returns Task<T> instead of T"
```

### Pull Request Template

When opening a PR, fill in the template with:

```markdown
## Description
[Describe your changes here]

## Type of Change
- [ ] 🐛 Bug fix (non-breaking change)
- [ ] ✨ New feature (non-breaking change)
- [ ] 💥 Breaking change (fix or feature that causes incompatible changes)
- [ ] 📝 Documentation
- [ ] 🎨 Refactoring

## Checklist
- [ ] My code follows project standards
- [ ] I performed a self-review of the code
- [ ] I commented complex areas of the code
- [ ] I updated the documentation
- [ ] I did not add warnings
- [ ] I added tests that prove my fix/feature works
- [ ] New and existing unit tests pass locally
- [ ] I updated CHANGELOG.md

## How to Test?
[Describe the steps to test your changes]

## Screenshots (if applicable)
[Add screenshots if there are visual changes]

## Related Issues
Closes #[issue number]
```

## 🐛 Reporting Bugs

Before reporting a bug, check whether an issue about the problem is already open.

### How to Report a Bug

1. Go to [Issues](https://github.com/kallebelins/mvp24hours-dotnet/issues/new?template=bug_report.md)
2. Use the bug report template
3. Fill in all sections

**Essential Information:**

- **Clear, descriptive title**
- **Detailed description** of the problem
- **Steps to reproduce** the behavior
- **Expected behavior** vs **actual behavior**
- **Screenshots** (if applicable)
- **Environment:**
  - Mvp24Hours version
  - .NET version
  - OS (Windows, Linux, macOS)
  - IDE and version
- **Relevant logs and stack traces**
- **Sample code** that reproduces the problem

### Bug Report Example

```markdown
**Bug Description**
GetById returns null even when the record exists in the database.

**To Reproduce**
1. Configure DbContext with SQL Server
2. Add a Customer
3. Call `repository.GetById(1)`
4. Returns null

**Expected Behavior**
Should return the customer with ID 1.

**Environment**
- Mvp24Hours: 9.1.x
- .NET: 9
- OS: Windows 11
- SQL Server: 2022

**Code to Reproduce**
\```csharp
var customer = new Customer { Name = "Test" };
repository.Add(customer);
unitOfWork.SaveChanges();

var retrieved = repository.GetById(1); // Returns null
\```
```

## 💡 Suggesting Improvements

Improvement suggestions are always welcome!

### How to Suggest an Improvement

1. Go to [Issues](https://github.com/kallebelins/mvp24hours-dotnet/issues/new?template=feature_request.md)
2. Use the feature request template
3. Describe your suggestion in detail

**Important Information:**

- **Problem the feature solves**
- **Proposed solution** in detail
- **Alternatives considered**
- **Usage examples**
- **Breaking change impact**
- **Benefits for the community**

### Feature Request Example

```markdown
**Is your feature related to a problem?**
Yes, there is currently no support for efficient bulk operations.

**Describe the solution you'd like**
Add BulkInsert, BulkUpdate, and BulkDelete methods to IRepository.

**Usage Example**
\```csharp
var customers = GetLargeCustomerList();
repository.BulkInsert(customers); // Inserts thousands of records efficiently
unitOfWork.SaveChanges();
\```

**Alternatives Considered**
- Use a loop with Add() - too slow for large volumes
- Use raw SQL - loses Repository abstraction

**Benefits**
- Significant performance improvement for high-volume scenarios
- Maintains Repository Pattern abstraction
- Simplifies batch operations
```

## 📚 Documentation

Documentation is as important as code!

### Documentation Types

1. **XML Comments** - For IntelliSense
   ```csharp
   /// <summary>
   /// Retrieves entities with pagination.
   /// </summary>
   ```

2. **README** - Module overview

3. **Docs** - Detailed guides in `docs/en-us/`
   - Tutorials
   - Practical examples
   - Architecture
   - Best practices

4. **CHANGELOG** - Change history

### How to Contribute to Documentation

1. Improve existing documentation
2. Add practical examples
3. Fix spelling or grammar errors
4. Create video or blog tutorials

Follow the [Documentation Authoring Guide](docs/en-us/documentation-authoring-guide.md) for English documentation under `docs/en-us/**`.

**Tip:** Documentation can be a great first PR!

## 🧪 Tests

Tests are required for new features and bug fixes.

### Test Structure

```
Tests/
├── Mvp24Hours.Core.Test/              # Core tests
├── Mvp24Hours.Application.Test/       # Application tests
├── Mvp24Hours.Application.SQLServer.Test/  # SQL integration tests
├── Mvp24Hours.Application.MongoDb.Test/    # Mongo integration tests
└── ...
```

### Writing Tests

```csharp
using Xunit;

namespace Mvp24Hours.Core.Test
{
    public class RepositoryTests
    {
        [Fact]
        public void Add_ValidEntity_ShouldAddToContext()
        {
            // Arrange
            var repository = CreateRepository();
            var customer = new Customer { Name = "Test" };
            
            // Act
            repository.Add(customer);
            var result = repository.GetById(customer.Id);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GetById_InvalidId_ShouldReturnNull(int invalidId)
        {
            // Arrange
            var repository = CreateRepository();
            
            // Act
            var result = repository.GetById(invalidId);
            
            // Assert
            Assert.Null(result);
        }
    }
}
```

### Running Tests

```bash
# All tests
dotnet test

# Single project only
dotnet test src/Tests/Mvp24Hours.Core.Test/

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Single category only
dotnet test --filter Category=Integration
```

### Code Coverage

We aim to maintain **minimum 80% coverage** on new code.

```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/
```

## 🌍 Community

### Where to Get Help

- 📖 [Documentation](https://kallebelins.github.io/mvp24hours-dotnet)
- 💬 [GitHub Discussions](https://github.com/kallebelins/mvp24hours-dotnet/discussions)
- 🐛 [GitHub Issues](https://github.com/kallebelins/mvp24hours-dotnet/issues)
- 💼 [LinkedIn - Kallebe Lins](https://www.linkedin.com/in/kallebelins/)

### Communication Channels

- **GitHub Issues** - Bugs and feature requests
- **GitHub Discussions** - Questions and general discussion
- **Pull Requests** - Code contributions

### Recognition

All contributors are recognized:

- Contributor list in the README
- Mention in release notes
- GitHub contributor badge

## 📜 License

By contributing to Mvp24Hours, you agree that your contributions will be licensed under the [MIT License](LICENSE).

## 🎯 Roadmap

See the [task roadmap](docs/tasks.md) to learn what is planned:

- 156 organized tasks
- Categorized by priority
- Covers code, tests, and documentation

### Priority Tasks

See [docs/tasks.md](docs/tasks.md) for the full list. Some current priorities:

1. ⚡ Implement consistent guard clauses
2. ⚡ Add unit tests for Extension Methods
3. ⚡ Configure code coverage reporting
4. ⚡ Review and optimize async implementations

## 🙏 Acknowledgments

Thank you for making Mvp24Hours better! Every contribution, no matter how small, makes a difference.

Ways to help beyond code:

- ⭐ Star the repository
- 📢 Share the project
- 📝 Write about the project
- 🐛 Report bugs
- 💡 Suggest improvements
- 👥 Help other users
- 📖 Improve documentation

---

**Questions?** Open a [Discussion](https://github.com/kallebelins/mvp24hours-dotnet/discussions) or contact via [LinkedIn](https://www.linkedin.com/in/kallebelins/).

**Ready to contribute?** Start by choosing an [issue labeled "good first issue"](https://github.com/kallebelins/mvp24hours-dotnet/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22)!

Built with ❤️ by [Kallebe Lins](https://github.com/kallebelins).

**Be the first contributor!** 🎉
