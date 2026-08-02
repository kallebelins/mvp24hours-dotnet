//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Geospatial;
using Xunit;

namespace Mvp24Hours.Application.MongoDb.Test.Geospatial;

[Trait("Category", "Unit")]
public class GeospatialTest
{
    #region [ GeoPoint - Constructors ]

    [Fact]
    public void GeoPoint_DefaultConstructor_InitializesCoordinates()
    {
        var point = new GeoPoint();

        Assert.NotNull(point.Coordinates);
        Assert.Equal(2, point.Coordinates.Length);
        Assert.Equal("Point", point.Type);
        Assert.Equal(0, point.Longitude);
        Assert.Equal(0, point.Latitude);
    }

    [Fact]
    public void GeoPoint_ParameterizedConstructor_SetsCoordinates()
    {
        var point = new GeoPoint(-43.1729, -22.9068);

        Assert.Equal(-43.1729, point.Longitude);
        Assert.Equal(-22.9068, point.Latitude);
        Assert.Equal(-43.1729, point.Coordinates[0]);
        Assert.Equal(-22.9068, point.Coordinates[1]);
    }

    [Fact]
    public void GeoPoint_Constructor_InvalidLongitude_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(-181, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(181, 0));
    }

    [Fact]
    public void GeoPoint_Constructor_InvalidLatitude_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(0, -91));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(0, 91));
    }

    [Fact]
    public void GeoPoint_Constructor_BoundaryValues_DoNotThrow()
    {
        Exception ex1 = Record.Exception(() => new GeoPoint(-180, -90));
        Exception ex2 = Record.Exception(() => new GeoPoint(180, 90));

        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    #endregion

    #region [ GeoPoint - Properties ]

    [Fact]
    public void GeoPoint_SetLongitude_UpdatesCoordinates()
    {
        var point = new GeoPoint
        {
            Longitude = 10.5
        };

        Assert.Equal(10.5, point.Coordinates[0]);
        Assert.Equal(10.5, point.Longitude);
    }

    [Fact]
    public void GeoPoint_SetLatitude_UpdatesCoordinates()
    {
        var point = new GeoPoint
        {
            Latitude = 45.3
        };

        Assert.Equal(45.3, point.Coordinates[1]);
        Assert.Equal(45.3, point.Latitude);
    }

    [Fact]
    public void GeoPoint_TypeIsAlwaysPoint()
    {
        var point = new GeoPoint(10, 20);
        Assert.Equal("Point", point.Type);
    }

    #endregion

    #region [ GeoPoint - Static Methods ]

    [Fact]
    public void GeoPoint_FromLatLng_CreatesPointWithSwappedOrder()
    {
        var point = GeoPoint.FromLatLng(-22.9068, -43.1729);

        Assert.Equal(-43.1729, point.Longitude);
        Assert.Equal(-22.9068, point.Latitude);
    }

    #endregion

    #region [ GeoPoint - ToBsonDocument ]

    [Fact]
    public void GeoPoint_ToBsonDocument_ContainsTypeAndCoordinates()
    {
        var point = new GeoPoint(-43.1729, -22.9068);
        var doc = point.ToBsonDocument();

        Assert.Equal("Point", doc["type"].AsString);
        BsonArray coords = doc["coordinates"].AsBsonArray;
        Assert.Equal(-43.1729, coords[0].ToDouble());
        Assert.Equal(-22.9068, coords[1].ToDouble());
    }

    #endregion

    #region [ GeoPoint - DistanceTo ]

    [Fact]
    public void GeoPoint_DistanceTo_SamePoint_ReturnsZero()
    {
        var point = new GeoPoint(-43.1729, -22.9068);
        double distance = point.DistanceTo(point);

        Assert.Equal(0, distance, precision: 5);
    }

    [Fact]
    public void GeoPoint_DistanceTo_KnownPoints_ReturnsApproximateDistance()
    {
        // Rio de Janeiro (approx) to São Paulo (approx) ~360km
        var rio = new GeoPoint(-43.1729, -22.9068);
        var sao = new GeoPoint(-46.6333, -23.5505);

        double distance = rio.DistanceTo(sao);

        Assert.True(distance > 300_000 && distance < 400_000,
            $"Expected ~360km but got {distance / 1000:F1}km");
    }

    [Fact]
    public void GeoPoint_DistanceTo_IsSymmetric()
    {
        var a = new GeoPoint(-43.1729, -22.9068);
        var b = new GeoPoint(-46.6333, -23.5505);

        double dAB = a.DistanceTo(b);
        double dBA = b.DistanceTo(a);

        Assert.Equal(dAB, dBA, precision: 5);
    }

    #endregion

    #region [ GeoPolygon - FromPoints ]

    [Fact]
    public void GeoPolygon_FromPoints_LessThan4Points_Throws()
    {
        var p1 = new GeoPoint(0, 0);
        var p2 = new GeoPoint(1, 0);
        var p3 = new GeoPoint(0, 1);

        Assert.Throws<ArgumentException>(() => GeoPolygon.FromPoints(p1, p2, p3));
    }

    [Fact]
    public void GeoPolygon_FromPoints_Null_Throws()
    {
        Assert.Throws<ArgumentException>(() => GeoPolygon.FromPoints(null!));
    }

    [Fact]
    public void GeoPolygon_FromPoints_ValidPoints_CreatesPolygon()
    {
        var p1 = new GeoPoint(0, 0);
        var p2 = new GeoPoint(1, 0);
        var p3 = new GeoPoint(1, 1);
        var p4 = new GeoPoint(0, 0);

        var polygon = GeoPolygon.FromPoints(p1, p2, p3, p4);

        Assert.NotNull(polygon);
        Assert.Equal("Polygon", polygon.Type);
        Assert.Single(polygon.Coordinates);
        Assert.Equal(4, polygon.Coordinates[0].Length);
    }

    [Fact]
    public void GeoPolygon_ToBsonDocument_ContainsTypeAndCoordinates()
    {
        var p1 = new GeoPoint(0, 0);
        var p2 = new GeoPoint(1, 0);
        var p3 = new GeoPoint(1, 1);
        var p4 = new GeoPoint(0, 0);
        var polygon = GeoPolygon.FromPoints(p1, p2, p3, p4);

        var doc = polygon.ToBsonDocument();

        Assert.Equal("Polygon", doc["type"].AsString);
        Assert.True(doc.Contains("coordinates"));
    }

    #endregion

    #region [ GeoPolygon - CreateCircle ]

    [Fact]
    public void GeoPolygon_CreateCircle_DefaultSegments_Creates33Points()
    {
        var center = new GeoPoint(0, 0);
        var circle = GeoPolygon.CreateCircle(center, 1000);

        // 32 segments + 1 closing point = 33 points in the ring
        Assert.Single(circle.Coordinates);
        Assert.Equal(33, circle.Coordinates[0].Length);
    }

    [Fact]
    public void GeoPolygon_CreateCircle_CustomSegments()
    {
        var center = new GeoPoint(0, 0);
        var circle = GeoPolygon.CreateCircle(center, 1000, segments: 16);

        Assert.Equal(17, circle.Coordinates[0].Length);
    }

    [Fact]
    public void GeoPolygon_CreateCircle_FirstAndLastPointAreEqual()
    {
        var center = new GeoPoint(-43.1729, -22.9068);
        var circle = GeoPolygon.CreateCircle(center, 500);

        double[] first = circle.Coordinates[0][0];
        double[] last = circle.Coordinates[0][^1];

        Assert.Equal(first[0], last[0], precision: 10);
        Assert.Equal(first[1], last[1], precision: 10);
    }

    #endregion

    #region [ GeoPolygon - Defaults ]

    [Fact]
    public void GeoPolygon_DefaultType_IsPolygon()
    {
        var polygon = new GeoPolygon();
        Assert.Equal("Polygon", polygon.Type);
    }

    [Fact]
    public void GeoPolygon_DefaultCoordinates_IsEmpty()
    {
        var polygon = new GeoPolygon();
        Assert.Empty(polygon.Coordinates);
    }

    #endregion
}
