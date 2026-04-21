using DaoPlanImport.Models;

namespace DaoPlanImport.Services;

/// <summary>
/// Utility service for geometric calculations
/// </summary>
public static class GeometryCalculator
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Calculate the distance between two geographic locations using Haversine formula
    /// </summary>
    public static double CalculateDistance(GeoLocation location1, GeoLocation location2)
    {
        var lat1Rad = location1.Latitude * Math.PI / 180.0;
        var lat2Rad = location2.Latitude * Math.PI / 180.0;
        var deltaLatRad = (location2.Latitude - location1.Latitude) * Math.PI / 180.0;
        var deltaLonRad = (location2.Longitude - location1.Longitude) * Math.PI / 180.0;

        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Calculate the midpoint between two locations
    /// </summary>
    public static GeoLocation CalculateMidPoint(GeoLocation location1, GeoLocation location2)
    {
        var lat1Rad = location1.Latitude * Math.PI / 180.0;
        var lat2Rad = location2.Latitude * Math.PI / 180.0;
        var lon1Rad = location1.Longitude * Math.PI / 180.0;
        var lon2Rad = location2.Longitude * Math.PI / 180.0;

        var bx = Math.Cos(lat2Rad) * Math.Cos(lon2Rad - lon1Rad);
        var by = Math.Cos(lat2Rad) * Math.Sin(lon2Rad - lon1Rad);

        var lat3Rad = Math.Atan2(Math.Sin(lat1Rad) + Math.Sin(lat2Rad),
                                 Math.Sqrt((Math.Cos(lat1Rad) + bx) * (Math.Cos(lat1Rad) + bx) + by * by));
        var lon3Rad = lon1Rad + Math.Atan2(by, Math.Cos(lat1Rad) + bx);

        return new GeoLocation
        {
            Latitude = lat3Rad * 180.0 / Math.PI,
            Longitude = lon3Rad * 180.0 / Math.PI
        };
    }

    /// <summary>
    /// Calculate the slope of a line between two locations
    /// </summary>
    public static double CalculateLineSlope(GeoLocation location1, GeoLocation location2)
    {
        var deltaLat = location2.Latitude - location1.Latitude;
        var deltaLon = location2.Longitude - location1.Longitude;

        if (Math.Abs(deltaLon) < 0.0000001)
        {
            return double.PositiveInfinity;
        }

        return deltaLat / deltaLon;
    }

    /// <summary>
    /// Check if a location is inside a polygon using ray casting algorithm
    /// </summary>
    public static bool IsLocationInPolygon(List<GeoLocation> polygon, GeoLocation location)
    {
        if (polygon.Count < 3)
            return false;

        int intersectCount = 0;
        for (int i = 0; i < polygon.Count - 1; i++)
        {
            var p1 = polygon[i];
            var p2 = polygon[i + 1];

            if (IsRayIntersectingSegment(location, p1, p2))
            {
                intersectCount++;
            }
        }

        // Check last segment to first segment
        if (IsRayIntersectingSegment(location, polygon[polygon.Count - 1], polygon[0]))
        {
            intersectCount++;
        }

        return intersectCount % 2 == 1;
    }

    private static bool IsRayIntersectingSegment(GeoLocation point, GeoLocation p1, GeoLocation p2)
    {
        if ((p2.Latitude <= point.Latitude && point.Latitude < p1.Latitude) ||
            (p1.Latitude <= point.Latitude && point.Latitude < p2.Latitude))
        {
            var xinters = (point.Latitude - p1.Latitude) * (p2.Longitude - p1.Longitude) /
                         (p2.Latitude - p1.Latitude) + p1.Longitude;
            if (point.Longitude < xinters)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Convert a list of locations to WKT Polygon format
    /// </summary>
    public static string LocationsToWktPolygon(List<GeoLocation> locations)
    {
        if (locations.Count < 3)
            return string.Empty;

        var wktCoordinates = string.Join(", ", 
            locations.Select(l => $"{l.Latitude:F8} {l.Longitude:F8}"));

        // Ensure polygon is closed
        if (!locations.First().Equals(locations.Last()))
        {
            wktCoordinates += $", {locations.First().Latitude:F8} {locations.First().Longitude:F8}";
        }

        return $"POLYGON(({wktCoordinates}))";
    }
}
