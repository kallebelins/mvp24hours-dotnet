using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class EntityIdModelBuilderExtensionsTest
{
    [Fact]
    public void HasGuidEntityIdConversion_RoundTripsWithInMemory()
    {
        string databaseName = $"EntityId_Guid_{Guid.NewGuid():N}";
        var id = CustomerId.New();

        using (GuidConversionDbContext context = CreateGuidConversionContext(databaseName))
        {
            context.Customers.Add(new CustomerWithGuidId { Id = id, Name = "Acme" });
            context.SaveChanges();
        }

        using (GuidConversionDbContext context = CreateGuidConversionContext(databaseName))
        {
            CustomerWithGuidId entity = context.Customers.Single();

            entity.Id.Should().Be(id);
            entity.Name.Should().Be("Acme");

            ValueConverter? converter = context.Model
                .FindEntityType(typeof(CustomerWithGuidId))!
                .FindProperty(nameof(CustomerWithGuidId.Id))!
                .GetValueConverter();

            converter.Should().NotBeNull();
            converter!.ProviderClrType.Should().Be(typeof(Guid));
            converter.ModelClrType.Should().Be(typeof(CustomerId));
        }
    }

    [Fact]
    public void ApplyStronglyTypedIdConversions_ConfiguresConvertersWithoutThrowing()
    {
        string databaseName = $"EntityId_Apply_{Guid.NewGuid():N}";
        var customerId = CustomerId.New();
        var orderId = OrderId.New();

        using (AutoConversionDbContext context = CreateAutoConversionContext(databaseName))
        {
            context.Customers.Add(new CustomerWithGuidId { Id = customerId, Name = "Auto" });
            context.Orders.Add(new OrderWithGuidId { Id = orderId, CustomerName = "Buyer" });
            context.SaveChanges();
        }

        using (AutoConversionDbContext context = CreateAutoConversionContext(databaseName))
        {
            context.Customers.Single().Id.Should().Be(customerId);
            context.Orders.Single().Id.Should().Be(orderId);

            ValueConverter? customerConverter = context.Model
                .FindEntityType(typeof(CustomerWithGuidId))!
                .FindProperty(nameof(CustomerWithGuidId.Id))!
                .GetValueConverter();

            ValueConverter? orderConverter = context.Model
                .FindEntityType(typeof(OrderWithGuidId))!
                .FindProperty(nameof(OrderWithGuidId.Id))!
                .GetValueConverter();

            customerConverter.Should().NotBeNull();
            orderConverter.Should().NotBeNull();
            customerConverter!.ProviderClrType.Should().Be(typeof(Guid));
            orderConverter!.ProviderClrType.Should().Be(typeof(Guid));
        }
    }

    [Fact]
    public void ApplyStronglyTypedIdConversions_OnEntityBuilder_ConfiguresConverter()
    {
        DbContextOptions<EntityBuilderConversionDbContext> options = new DbContextOptionsBuilder<EntityBuilderConversionDbContext>()
            .UseInMemoryDatabase($"EntityId_EntityBuilder_{Guid.NewGuid():N}")
            .Options;

        using var context = new EntityBuilderConversionDbContext(options);
        context.Database.EnsureCreated();

        ValueConverter? converter = context.Model
            .FindEntityType(typeof(CustomerWithGuidId))!
            .FindProperty(nameof(CustomerWithGuidId.Id))!
            .GetValueConverter();

        converter.Should().NotBeNull();
        converter!.ModelClrType.Should().Be(typeof(CustomerId));
        converter.ProviderClrType.Should().Be(typeof(Guid));
    }

    private static GuidConversionDbContext CreateGuidConversionContext(string databaseName)
    {
        DbContextOptions<GuidConversionDbContext> options = new DbContextOptionsBuilder<GuidConversionDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new GuidConversionDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static AutoConversionDbContext CreateAutoConversionContext(string databaseName)
    {
        DbContextOptions<AutoConversionDbContext> options = new DbContextOptionsBuilder<AutoConversionDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AutoConversionDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public sealed class CustomerWithGuidId
    {
        public CustomerId Id { get; set; } = CustomerId.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public sealed class OrderWithGuidId
    {
        public OrderId Id { get; set; } = OrderId.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    private sealed class GuidConversionDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<CustomerWithGuidId> Customers => Set<CustomerWithGuidId>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerWithGuidId>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasGuidEntityIdConversion<CustomerWithGuidId, CustomerId>();
            });
        }
    }

    private sealed class AutoConversionDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<CustomerWithGuidId> Customers => Set<CustomerWithGuidId>();
        public DbSet<OrderWithGuidId> Orders => Set<OrderWithGuidId>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerWithGuidId>().HasKey(e => e.Id);
            modelBuilder.Entity<OrderWithGuidId>().HasKey(e => e.Id);
            modelBuilder.ApplyStronglyTypedIdConversions();
        }
    }

    private sealed class EntityBuilderConversionDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<CustomerWithGuidId> Customers => Set<CustomerWithGuidId>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerWithGuidId>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<CustomerWithGuidId>()
                .ApplyStronglyTypedIdConversions();
        }
    }
}
