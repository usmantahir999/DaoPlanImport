namespace DaoPlanImport.Models;

/// <summary>
/// Represents a geographical location with latitude and longitude
/// </summary>
public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is GeoLocation other)
        {
            return Math.Abs(Latitude - other.Latitude) < 0.0000001 &&
                   Math.Abs(Longitude - other.Longitude) < 0.0000001;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Latitude.GetHashCode() ^ Longitude.GetHashCode();
    }
}

/// <summary>
/// Represents a geographical location vertex for triangulation
/// </summary>
public class GeoLocationVertex
{
    public double[] Position { get; set; }

    public GeoLocationVertex(double latitude, double longitude)
    {
        Position = new[] { latitude, longitude };
    }
}

/// <summary>
/// Represents a line segment between two locations
/// </summary>
public class LineSegment
{
    public GeoLocation LocationA { get; set; }
    public GeoLocation LocationB { get; set; }

    public LineSegment(GeoLocation locationA, GeoLocation locationB)
    {
        LocationA = locationA;
        LocationB = locationB;
    }

    public override bool Equals(object? obj)
    {
        if (obj is LineSegment other)
        {
            return (LocationA.Equals(other.LocationA) && LocationB.Equals(other.LocationB)) ||
                   (LocationA.Equals(other.LocationB) && LocationB.Equals(other.LocationA));
        }
        return false;
    }

    public override int GetHashCode()
    {
        return LocationA.GetHashCode() ^ LocationB.GetHashCode();
    }
}

/// <summary>
/// Represents a shape/polygon with multiple locations
/// </summary>
public class RouteShape
{
    public List<GeoLocation> Locations { get; set; } = new();
}
