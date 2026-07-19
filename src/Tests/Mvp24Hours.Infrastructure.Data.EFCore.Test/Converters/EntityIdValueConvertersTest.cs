using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Infrastructure.Data.EFCore.Converters;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Converters;

[Trait("Category", "Unit")]
public class EntityIdValueConvertersTest
{
    public sealed class SampleIntId(int value) : IntEntityId<SampleIntId>(value);

    public sealed class SampleLongId(long value) : LongEntityId<SampleLongId>(value);

    public sealed class SampleStringId(string value) : StringEntityId<SampleStringId>(value);

    [Fact]
    public void GuidEntityIdValueConverter_ShouldRoundTrip()
    {
        var converter = new GuidEntityIdValueConverter<CustomerId>();
        var id = CustomerId.New();

        Guid providerValue = (Guid)converter.ConvertToProvider(id)!;
        CustomerId roundTrip = (CustomerId)converter.ConvertFromProvider(providerValue)!;

        providerValue.Should().Be(id.Value);
        roundTrip.Should().Be(id);
    }

    [Fact]
    public void IntEntityIdValueConverter_ShouldRoundTrip()
    {
        var converter = new IntEntityIdValueConverter<SampleIntId>();
        var id = new SampleIntId(42);

        int providerValue = (int)converter.ConvertToProvider(id)!;
        SampleIntId roundTrip = (SampleIntId)converter.ConvertFromProvider(providerValue)!;

        providerValue.Should().Be(42);
        roundTrip.Should().Be(id);
    }

    [Fact]
    public void LongEntityIdValueConverter_ShouldRoundTrip()
    {
        var converter = new LongEntityIdValueConverter<SampleLongId>();
        var id = new SampleLongId(9_000_000_000L);

        long providerValue = (long)converter.ConvertToProvider(id)!;
        SampleLongId roundTrip = (SampleLongId)converter.ConvertFromProvider(providerValue)!;

        providerValue.Should().Be(9_000_000_000L);
        roundTrip.Should().Be(id);
    }

    [Fact]
    public void StringEntityIdValueConverter_ShouldRoundTrip()
    {
        var converter = new StringEntityIdValueConverter<SampleStringId>();
        var id = new SampleStringId("doc-123");

        string providerValue = (string)converter.ConvertToProvider(id)!;
        SampleStringId roundTrip = (SampleStringId)converter.ConvertFromProvider(providerValue)!;

        providerValue.Should().Be("doc-123");
        roundTrip.Should().Be(id);
    }

    [Fact]
    public void EntityIdValueConverter_Generic_ShouldRoundTrip()
    {
        var converter = new EntityIdValueConverter<OrderId, Guid>();
        var id = OrderId.New();

        Guid providerValue = (Guid)converter.ConvertToProvider(id)!;
        OrderId roundTrip = (OrderId)converter.ConvertFromProvider(providerValue)!;

        providerValue.Should().Be(id.Value);
        roundTrip.Should().Be(id);
    }
}
