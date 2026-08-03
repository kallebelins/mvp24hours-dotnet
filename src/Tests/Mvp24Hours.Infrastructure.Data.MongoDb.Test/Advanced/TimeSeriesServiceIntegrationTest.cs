using MongoDB.Bson;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TimeSeries;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;
using MongoDbTimeSeries = Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TimeSeries;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Advanced;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class TimeSeriesServiceIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task TimeSeriesService_ShouldCreateCollectionInsertQueryAndAggregate()
    {
        string collectionName = $"sensor_readings_{Guid.NewGuid():N}";
        var service = new MongoDbTimeSeriesService<SensorReading>(
            fixture.Database,
            collectionName,
            timeField: "Timestamp",
            metaField: "SensorId");

        await service.CreateTimeSeriesCollectionAsync(collectionName, new MongoDbTimeSeries.TimeSeriesOptions
        {
            TimeField = "Timestamp",
            MetaField = "SensorId",
            Granularity = MongoDbTimeSeries.TimeSeriesGranularity.Seconds
        });

        DateTime baseTime = new(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        await service.InsertMeasurementsAsync(
        [
            new SensorReading { Timestamp = baseTime, SensorId = "sensor-001", Temperature = 20.0, Humidity = 40.0 },
            new SensorReading { Timestamp = baseTime.AddMinutes(1), SensorId = "sensor-001", Temperature = 22.0, Humidity = 42.0 },
            new SensorReading { Timestamp = baseTime.AddMinutes(2), SensorId = "sensor-001", Temperature = 24.0, Humidity = 44.0 },
            new SensorReading { Timestamp = baseTime.AddMinutes(3), SensorId = "sensor-002", Temperature = 18.0, Humidity = 38.0 }
        ]);

        IList<SensorReading> rangeResults = await service.QueryByTimeRangeAsync(
            baseTime,
            baseTime.AddMinutes(3));

        rangeResults.Should().HaveCount(3);
        rangeResults.Should().OnlyContain(r => r.SensorId == "sensor-001");

        IList<TimeWindowAggregation> aggregations = await service.AggregateByTimeWindowAsync(
            baseTime,
            baseTime.AddMinutes(4),
            TimeSpan.FromMinutes(2),
            "Temperature",
            TimeSeriesAggregationType.Average);

        aggregations.Should().NotBeEmpty();
        aggregations.Should().Contain(a => a.Count >= 1);

        SensorReading latest = await service.GetLatestMeasurementAsync();
        latest.Should().NotBeNull();
        latest.Timestamp.Should().BeOnOrAfter(baseTime);

        BsonDocument stats = await service.GetCollectionStatsAsync();
        stats.Contains("timeseries").Should().BeTrue();
    }
}
