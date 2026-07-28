using CustomerAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerAPI.Infrastructure.Configurations
{
    public class ContactConfiguration : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.ToTable("Contact", "dbo");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Created).IsRequired();
            builder.Property("CustomerId").IsRequired();
            builder
                .Property(p => p.Type)
                .HasConversion<string>()
                .IsRequired();
            builder.Property(p => p.Description).HasMaxLength(255).IsRequired();
            builder.Property(p => p.Active).IsRequired();
        }
    }
}
