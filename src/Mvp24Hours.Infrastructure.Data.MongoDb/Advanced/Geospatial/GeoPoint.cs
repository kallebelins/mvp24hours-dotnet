//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Geospatial
{
    /// <summary>
    /// Represents a GeoJSON Point for geospatial operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// GeoJSON Point format: { type: "Point", coordinates: [longitude, latitude] }
    /// Note: Coordinates are stored as [longitude, latitude] (not [latitude, longitude]).
    /// </para>
    /// </remarks>
    public class GeoPoint
    {
        /// <summary>
        /// Gets or sets the GeoJSON type. Always "Point" for this class.
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = "Point";

        /// <summary>
        /// Gets or sets the coordinates as [longitude, latitude].
        /// </summary>
        [BsonElement("coordinates")]
        public double[] Coordinates { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoPoint"/> class.
        /// </summary>
        public GeoPoint()
        {
            Coordinates = new double[2];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoPoint"/> class with coordinates.
        /// </summary>
        /// <param name="longitude">The longitude (-180 to 180).</param>
        /// <param name="latitude">The latitude (-90 to 90).</param>
        public GeoPoint(double longitude, double latitude)
        {
            ValidateCoordinates(longitude, latitude);
            Coordinates = new[] { longitude, latitude };
        }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        [BsonIgnore]
        public double Longitude
        {
            get => Coordinates?[0] ?? 0;
            set
            {
                if (Coordinates == null) Coordinates = new double[2];
                Coordinates[0] = value;
            }
        }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        [BsonIgnore]
        public double Latitude
        {
            get => Coordinates?[1] ?? 0;
            set
            {
                if (Coordinates == null) Coordinates = new double[2];
                Coordinates[1] = value;
            }
        }

        /// <summary>
        /// Converts to a BsonDocument.
        /// </summary>
        public BsonDocument ToBsonDocument()
        {
            return new BsonDocument
            {
                { "type", Type },
                { "coordinates", new BsonArray(Coordinates) }
            };
        }

        /// <summary>
        /// Creates a GeoPoint from latitude and longitude.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        public static GeoPoint FromLatLng(double latitude, double longitude)
        {
            return new GeoPoint(longitude, latitude);
        }

        /// <summary>
        /// Calculates the distance in meters between two points using the Haversine formula.
        /// </summary>
        /// <param name="other">The other point.</param>
        /// <returns>Distance in meters.</returns>
        public double DistanceTo(GeoPoint other)
        {
            const double EarthRadiusMeters = 6371000;

            var lat1 = ToRadians(Latitude);
            var lat2 = ToRadians(other.Latitude);
            var deltaLat = ToRadians(other.Latitude - Latitude);
            var deltaLng = ToRadians(other.Longitude - Longitude);

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusMeters * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        private static void ValidateCoordinates(double longitude, double latitude)
        {
            if (longitude < -180 || longitude > 180)
            {
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
            }

            if (latitude < -90 || latitude > 90)
            {
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
            }
        }
    }

    /// <summary>
    /// Represents a GeoJSON Polygon for geospatial operations.
    /// </summary>
    public class GeoPolygon
    {
        /// <summary>
        /// Gets or sets the GeoJSON type. Always "Polygon" for this class.
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = "Polygon";

        /// <summary>
        /// Gets or sets the coordinates as an array of linear rings.
        /// The first ring is the outer boundary, subsequent rings are holes.
        /// Each ring is an array of [longitude, latitude] points.
        /// </summary>
        [BsonElement("coordinates")]
        public double[][][] Coordinates { get; set; }

        /// <summary>
        /// Creates a simple polygon from an array of points.
        /// </summary>
        /// <param name="points">The points defining the polygon boundary.</param>
        /// <returns>A GeoPolygon.</returns>
        /// <remarks>
        /// The first and last points must be the same to close the polygon.
        /// </remarks>
        public static GeoPolygon FromPoints(params GeoPoint[] points)
        {
            if (points == null || points.Length < 4)
            {
                throw new ArgumentException("A polygon requires at least 4 points (3 unique + closing point).", nameof(points));
            }

            var ring = new double[points.Length][];
            for (int i = 0; i < points.Length; i++)
            {
                ring[i] = points[i].Coordinates;
            }

            return new GeoPolygon
            {
                Coordinates = new[] { ring }
            };
        }

        /// <summary>
        /// Creates a circle approximation polygon.
        /// </summary>
        /// <param name="center">The center point.</param>
        /// <param name="radiusMeters">The radius in meters.</param>
        /// <param name="segments">Number of segments (default 32).</param>
        public static GeoPolygon CreateCircle(GeoPoint center, double radiusMeters, int segments = 32)
        {
            const double EarthRadiusMeters = 6371000;
            var points = new GeoPoint[segments + 1];
            var angularDistance = radiusMeters / EarthRadiusMeters;

            for (int i = 0; i < segments; i++)
            {
                var angle = (2 * Math.PI * i) / segments;
                var lat = Math.Asin(
                    Math.Sin(center.Latitude * Math.PI / 180) * Math.Cos(angularDistance) +
                    Math.Cos(center.Latitude * Math.PI / 180) * Math.Sin(angularDistance) * Math.Cos(angle));

                var lng = center.Longitude * Math.PI / 180 +
                    Math.Atan2(
                        Math.Sin(angle) * Math.Sin(angularDistance) * Math.Cos(center.Latitude * Math.PI / 180),
                        Math.Cos(angularDistance) - Math.Sin(center.Latitude * Math.PI / 180) * Math.Sin(lat));

                points[i] = new GeoPoint(lng * 180 / Math.PI, lat * 180 / Math.PI);
            }

            // Close the polygon
            points[segments] = points[0];

            return FromPoints(points);
        }

        /// <summary>
        /// Converts to a BsonDocument.
        /// </summary>
        public BsonDocument ToBsonDocument()
        {
            var rings = new BsonArray();
            foreach (var ring in Coordinates)
            {
                var points = new BsonArray();
                foreach (var point in ring)
                {
                    points.Add(new BsonArray(point));
                }
                rings.Add(points);
            }

            return new BsonDocument
            {
                { "type", Type },
                { "coordinates", rings }
            };
        }
    }
}

