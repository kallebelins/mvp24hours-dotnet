# Padrões de Testes para Agentes de IA

> **Instrução para Agente de IA**: Use estes padrões ao gerar testes para aplicações baseadas em Mvp24Hours. Siga as convenções e use os frameworks de teste descritos abaixo.

---

## Frameworks de Teste

| Framework | Propósito | Pacote |
|-----------|-----------|--------|
| xUnit | Testes Unitários e de Integração | `xunit` |
| NSubstitute | Mocking | `NSubstitute` |
| FluentAssertions | Asserções | `FluentAssertions` |
| Bogus | Geração de Dados de Teste | `Bogus` |
| Microsoft.EntityFrameworkCore.InMemory | Banco de Dados em Memória | `Microsoft.EntityFrameworkCore.InMemory` |
| Testcontainers | Testes de Integração com Docker | `Testcontainers` |

---

## Estrutura de Projetos

```
ProjectName/
├── src/
│   ├── ProjectName.Core/
│   ├── ProjectName.Infrastructure/
│   ├── ProjectName.Application/
│   └── ProjectName.WebAPI/
└── tests/
    ├── ProjectName.UnitTests/
    │   ├── ProjectName.UnitTests.csproj
    │   ├── Domain/
    │   │   └── CustomerTests.cs
    │   ├── Application/
    │   │   └── CustomerServiceTests.cs
    │   └── Fixtures/
    │       └── CustomerFixture.cs
    ├── ProjectName.IntegrationTests/
    │   ├── ProjectName.IntegrationTests.csproj
    │   ├── Controllers/
    │   │   └── CustomersControllerTests.cs
    │   ├── Repositories/
    │   │   └── CustomerRepositoryTests.cs
    │   └── Setup/
    │       ├── WebApplicationFactory.cs
    │       └── DatabaseFixture.cs
    └── ProjectName.ArchitectureTests/
        ├── ProjectName.ArchitectureTests.csproj
        └── LayerDependencyTests.cs
```

---

## Arquivo de Projeto de Teste (.csproj)

### Testes Unitários

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\ProjectName.Application\ProjectName.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="NSubstitute" Version="5.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="Bogus" Version="35.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.*" />
  </ItemGroup>
</Project>
```

### Testes de Integração

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\ProjectName.WebAPI\ProjectName.WebAPI.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.*" />
    <PackageReference Include="Testcontainers.MsSql" Version="3.*" />
  </ItemGroup>
</Project>
```

---

## Padrões de Testes Unitários

### Testes de Entidade

```csharp
// UnitTests/Domain/CustomerTests.cs
using FluentAssertions;
using ProjectName.Core.Entities;
using ProjectName.Core.Exceptions;
using Xunit;

namespace ProjectName.UnitTests.Domain;

public class CustomerTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCustomer()
    {
        // Arrange
        var name = "João Silva";
        var email = "joao@exemplo.com";

        // Act
        var customer = Customer.Create(name, email);

        // Assert
        customer.Should().NotBeNull();
        customer.Name.Should().Be(name);
        customer.Email.Value.Should().Be(email.ToLower());
        customer.Active.Should().BeTrue();
        customer.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        // Arrange & Act
        var act = () => Customer.Create(invalidName!, "joao@exemplo.com");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivateCustomer()
    {
        // Arrange
        var customer = Customer.Create("João Silva", "joao@exemplo.com");

        // Act
        customer.Deactivate();

        // Assert
        customer.Active.Should().BeFalse();
        customer.DomainEvents.Should().ContainSingle(e => e is CustomerDeactivatedEvent);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrowException()
    {
        // Arrange
        var customer = Customer.Create("João Silva", "joao@exemplo.com");
        customer.Deactivate();

        // Act
        var act = () => customer.Deactivate();

        // Assert
        act.Should().Throw<InvalidCustomerStateException>();
    }
}
```

### Testes de Value Object

```csharp
// UnitTests/Domain/EmailTests.cs
using FluentAssertions;
using ProjectName.Core.ValueObjects;
using Xunit;

namespace ProjectName.UnitTests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("joao@exemplo.com", "joao@exemplo.com")]
    [InlineData("JOAO@EXEMPLO.COM", "joao@exemplo.com")]
    [InlineData("Joao.Silva@Exemplo.Com", "joao.silva@exemplo.com")]
    public void Create_WithValidEmail_ShouldNormalizeToLowercase(string input, string expected)
    {
        // Act
        var email = new Email(input);

        // Assert
        email.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalido")]
    [InlineData("invalido@")]
    [InlineData("@invalido.com")]
    public void Create_WithInvalidEmail_ShouldThrowException(string? invalidEmail)
    {
        // Act
        var act = () => new Email(invalidEmail!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var email1 = new Email("joao@exemplo.com");
        var email2 = new Email("JOAO@EXEMPLO.COM");

        // Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }
}
```

### Testes de Serviço com Mocking

```csharp
// UnitTests/Application/CustomerServiceTests.cs
using FluentAssertions;
using Mvp24Hours.Core.Contract.Data;
using NSubstitute;
using ProjectName.Application.Services;
using ProjectName.Core.Entities;
using ProjectName.Core.ValueObjects;
using Xunit;

namespace ProjectName.UnitTests.Application;

public class CustomerServiceTests
{
    private readonly IUnitOfWorkAsync _uow;
    private readonly IRepositoryAsync<Customer> _repository;
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _uow = Substitute.For<IUnitOfWorkAsync>();
        _repository = Substitute.For<IRepositoryAsync<Customer>>();
        _uow.GetRepository<Customer>().Returns(_repository);
        
        _sut = new CustomerService(_uow);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingCustomer_ShouldReturnCustomer()
    {
        // Arrange
        var customerId = 1;
        var customer = Customer.Create("João Silva", "joao@exemplo.com");
        _repository.GetByIdAsync(customerId).Returns(customer);

        // Act
        var result = await _sut.GetByIdAsync(customerId);

        // Assert
        result.HasData.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("João Silva");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingCustomer_ShouldReturnEmptyResult()
    {
        // Arrange
        var customerId = 999;
        _repository.GetByIdAsync(customerId).Returns((Customer?)null);

        // Act
        var result = await _sut.GetByIdAsync(customerId);

        // Assert
        result.HasData.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateAndSaveCustomer()
    {
        // Arrange
        var dto = new CustomerCreateDto("João Silva", "joao@exemplo.com");

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.HasData.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Is<Customer>(c => c.Name == dto.Name));
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingCustomer_ShouldDelete()
    {
        // Arrange
        var customerId = 1;
        var customer = Customer.Create("João Silva", "joao@exemplo.com");
        _repository.GetByIdAsync(customerId).Returns(customer);

        // Act
        var result = await _sut.DeleteAsync(customerId);

        // Assert
        result.Data.Should().BeTrue();
        await _repository.Received(1).RemoveAsync(customer);
        await _uow.Received(1).SaveChangesAsync();
    }
}
```

---

## Construtores de Dados de Teste (Bogus)

```csharp
// UnitTests/Fixtures/CustomerFixture.cs
using Bogus;
using ProjectName.Core.Entities;
using ProjectName.Core.ValueObjects;

namespace ProjectName.UnitTests.Fixtures;

public static class CustomerFixture
{
    private static readonly Faker<Customer> _customerFaker = new Faker<Customer>("pt_BR")
        .CustomInstantiator(f => Customer.Create(
            f.Person.FullName,
            f.Person.Email
        ));

    public static Customer CreateValid() => _customerFaker.Generate();

    public static IList<Customer> CreateMany(int count = 5) => _customerFaker.Generate(count);

    public static Customer CreateWithName(string name)
    {
        return Customer.Create(name, new Faker("pt_BR").Person.Email);
    }

    public static Customer CreateInactive()
    {
        var customer = CreateValid();
        customer.Deactivate();
        return customer;
    }
}

// UnitTests/Fixtures/CustomerDtoFixture.cs
using Bogus;
using ProjectName.Core.ValueObjects;

namespace ProjectName.UnitTests.Fixtures;

public static class CustomerDtoFixture
{
    private static readonly Faker<CustomerCreateDto> _createDtoFaker = new Faker<CustomerCreateDto>("pt_BR")
        .CustomInstantiator(f => new CustomerCreateDto(
            f.Person.FullName,
            f.Person.Email
        ));

    public static CustomerCreateDto CreateValidDto() => _createDtoFaker.Generate();

    public static CustomerCreateDto CreateWithInvalidEmail()
    {
        return new CustomerCreateDto(new Faker("pt_BR").Person.FullName, "email-invalido");
    }
}
```

---

## Padrões de Testes de Integração

### Configuração do WebApplicationFactory

```csharp
// IntegrationTests/Setup/CustomWebApplicationFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectName.Infrastructure.Data;

namespace ProjectName.IntegrationTests.Setup;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove o registro existente do DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DataContext>));
            
            if (descriptor != null)
                services.Remove(descriptor);

            // Adiciona banco em memória para testes
            services.AddDbContext<DataContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Constrói service provider e garante que o banco foi criado
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            db.Database.EnsureCreated();
        });
    }
}
```

### Testes de Integração do Controller

```csharp
// IntegrationTests/Controllers/CustomersControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ProjectName.Core.Entities;
using ProjectName.Core.ValueObjects;
using ProjectName.Infrastructure.Data;
using ProjectName.IntegrationTests.Setup;
using Xunit;

namespace ProjectName.IntegrationTests.Controllers;

public class CustomersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CustomersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_WithExistingCustomer_ShouldReturnOk()
    {
        // Arrange
        var customer = await SeedCustomerAsync();

        // Act
        var response = await _client.GetAsync($"/api/customers/{customer.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CustomerDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(customer.Name);
    }

    [Fact]
    public async Task GetById_WithNonExistingCustomer_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/customers/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var createDto = new CustomerCreateDto("João Silva", "joao@exemplo.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CustomerCreateDto("", "email-invalido");

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_WithExistingCustomer_ShouldReturnNoContent()
    {
        // Arrange
        var customer = await SeedCustomerAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/customers/{customer.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<Customer> SeedCustomerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var customer = Customer.Create("Cliente Teste", "teste@exemplo.com");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        
        return customer;
    }
}
```

### Testes de Repositório com Testcontainers

```csharp
// IntegrationTests/Repositories/CustomerRepositoryTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectName.Core.Entities;
using ProjectName.Infrastructure.Data;
using Testcontainers.MsSql;
using Xunit;

namespace ProjectName.IntegrationTests.Repositories;

public class CustomerRepositoryTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private DataContext _context = null!;

    public CustomerRepositoryTests()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;
        
        _context = new DataContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistCustomer()
    {
        // Arrange
        var customer = Customer.Create("João Silva", "joao@exemplo.com");

        // Act
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Assert
        var savedCustomer = await _context.Customers.FindAsync(customer.Id);
        savedCustomer.Should().NotBeNull();
        savedCustomer!.Name.Should().Be("João Silva");
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnCorrectCustomer()
    {
        // Arrange
        var customer = Customer.Create("João Silva", "joao@exemplo.com");
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email.Value == "joao@exemplo.com");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
    }
}
```

---

## Testando Pipelines do Mvp24Hours

```csharp
// UnitTests/Pipelines/CustomerPipelineTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using NSubstitute;
using ProjectName.Application.Pipelines;
using ProjectName.Application.Pipelines.Operations;
using ProjectName.Core.ValueObjects;
using Xunit;

namespace ProjectName.UnitTests.Pipelines;

public class CustomerPipelineTests
{
    [Fact]
    public async Task Execute_WithValidData_ShouldCompleteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ValidateCustomerOperation>();
        services.AddScoped<CreateCustomerOperation>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var pipeline = new Pipeline();
        pipeline.Add<ValidateCustomerOperation>(serviceProvider);
        pipeline.Add<CreateCustomerOperation>(serviceProvider);

        var dto = new CustomerCreateDto("João Silva", "joao@exemplo.com");

        // Act
        var result = await pipeline.ExecuteAsync(dto);

        // Assert
        result.IsLocked.Should().BeFalse();
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task Execute_WithInvalidData_ShouldLockPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ValidateCustomerOperation>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var pipeline = new Pipeline();
        pipeline.Add<ValidateCustomerOperation>(serviceProvider);

        var dto = new CustomerCreateDto("", "email-invalido");

        // Act
        var result = await pipeline.ExecuteAsync(dto);

        // Assert
        result.IsLocked.Should().BeTrue();
        result.HasErrors.Should().BeTrue();
    }
}
```

---

## Convenções de Nomenclatura de Testes

| Padrão | Exemplo |
|--------|---------|
| MethodName_StateUnderTest_ExpectedBehavior | `Create_WithValidData_ShouldCreateCustomer` |
| Should_ExpectedBehavior_When_StateUnderTest | `Should_CreateCustomer_When_DataIsValid` |
| Given_Precondition_When_Action_Then_Result | `Given_ValidEmail_When_Creating_Then_NormalizesToLowercase` |

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Padrões de Tratamento de Erros](error-handling-patterns.md)
- [Padrões de Segurança](security-patterns.md)

