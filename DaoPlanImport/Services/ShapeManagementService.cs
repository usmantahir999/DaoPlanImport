using DaoPlanImport.Models;
using Microsoft.Extensions.Logging;

namespace DaoPlanImport.Services;

/// <summary>
/// Service for computing alpha shapes (concave hulls) from geographic locations
/// </summary>
public class ShapeManagementService
{
    private readonly ILogger<ShapeManagementService> _logger;

    public ShapeManagementService(ILogger<ShapeManagementService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Compute alpha shape from a list of locations
    /// Falls back to convex hull if alpha shape computation fails or has insufficient points
    /// </summary>
    public List<RouteShape> GetAlphaShape(List<GeoLocation> locations, double? alpha = null)
    {
        if (locations.Count < 4 || (alpha.HasValue && alpha <= 0))
        {
            return ComputeConvexHull(locations);
        }

        var trianglesMesh = GetTriangulationMesh(locations);
        if (trianglesMesh is null)
        {
            return ComputeConvexHull(locations);
        }

        double optimizedAlpha = alpha.HasValue ? alpha.Value : OptimizeAlpha(trianglesMesh, locations);

        if (optimizedAlpha <= 0)
        {
            return ComputeConvexHull(locations);
        }

        return GetShape(trianglesMesh, optimizedAlpha);
    }

    private List<RouteShape>? GetTriangulationMesh(List<GeoLocation> locations)
    {
        try
        {
            // Simplified triangulation - using a basic algorithm
            // In production, you might want to use a proper Delaunay triangulation library
            var triangles = new List<RouteShape>();

            if (locations.Count < 3)
                return null;

            // For now, create triangles from consecutive points
            for (int i = 0; i < locations.Count - 2; i++)
            {
                triangles.Add(new RouteShape
                {
                    Locations = new List<GeoLocation>
                    {
                        locations[i],
                        locations[i + 1],
                        locations[i + 2]
                    }
                });
            }

            return triangles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing triangulation mesh");
            return null;
        }
    }

    private List<LineSegment> CalculateSimplexBoarderEdges(List<RouteShape> triangulationVertices, double alpha)
    {
        List<LineSegment> borderLineSegements = new();
        foreach (var triangleVertices in triangulationVertices)
        {
            var vertices = triangleVertices.Locations.ToArray();
            var circumRadius = CalculateCircumRadius(vertices);

            if (circumRadius < 1.0 / alpha)
            {
                foreach (var edge in GetSimplexPairs(vertices))
                {
                    if (!borderLineSegements.Contains(edge))
                    {
                        borderLineSegements.Add(edge);
                    }
                    else
                    {
                        borderLineSegements.Remove(edge);
                    }
                }
            }
        }

        return borderLineSegements;
    }

    private IEnumerable<LineSegment> GetSimplexPairs(GeoLocation[] simplex)
    {
        for (int i = 0; i < simplex.Length; i++)
        {
            for (int j = i + 1; j < simplex.Length; j++)
            {
                yield return new LineSegment(simplex[i], simplex[j]);
            }
        }
    }

    public double CalculateCircumRadius(GeoLocation[] locations)
    {
        if (locations.Length < 3)
            return 0;

        var circumCenter = CalculateCircumCenter(locations);
        var circumRadius = GeometryCalculator.CalculateDistance(circumCenter, locations[0]);
        return circumRadius;
    }

    public GeoLocation CalculateCircumCenter(GeoLocation[] locations)
    {
        if (locations.Length < 3)
            return locations.FirstOrDefault() ?? new GeoLocation();

        var midPoint1 = GeometryCalculator.CalculateMidPoint(locations[0], locations[1]);
        var midPoint2 = GeometryCalculator.CalculateMidPoint(locations[1], locations[2]);

        var lineSlope1 = GeometryCalculator.CalculateLineSlope(locations[0], locations[1]);
        var lineSlope2 = GeometryCalculator.CalculateLineSlope(locations[1], locations[2]);

        return CalculateTriangleCenterPoint(midPoint1, midPoint2, lineSlope1, lineSlope2);
    }

    private GeoLocation CalculateTriangleCenterPoint(GeoLocation location1, GeoLocation location2, double slope1, double slope2)
    {
        double perpendicularSlope1 = Math.Abs(slope1) < 0.0000001 ? double.PositiveInfinity : -1 / slope1;
        double perpendicularSlope2 = Math.Abs(slope2) < 0.0000001 ? double.PositiveInfinity : -1 / slope2;

        double latitude, longitude;

        if (double.IsInfinity(perpendicularSlope1))
        {
            latitude = location1.Latitude;
            longitude = double.IsInfinity(perpendicularSlope2) ? 
                location2.Longitude : 
                perpendicularSlope2 * (latitude - location2.Latitude) + location2.Longitude;
        }
        else if (double.IsInfinity(perpendicularSlope2))
        {
            latitude = location2.Latitude;
            longitude = perpendicularSlope1 * (latitude - location1.Latitude) + location1.Longitude;
        }
        else
        {
            latitude = (perpendicularSlope1 * location1.Latitude - perpendicularSlope2 * location2.Latitude + 
                       location2.Longitude - location1.Longitude) / (perpendicularSlope1 - perpendicularSlope2);
            longitude = perpendicularSlope1 * (latitude - location1.Latitude) + location1.Longitude;
        }

        return new GeoLocation { Latitude = latitude, Longitude = longitude };
    }

    private List<RouteShape> ComputeConvexHull(List<GeoLocation> locations)
    {
        if (locations.Count < 3)
            return new List<RouteShape>();

        // Simple convex hull using gift wrapping algorithm
        var hull = GiftWrapConvexHull(locations);
        var lineSegments = new List<LineSegment>();

        for (int i = 0; i < hull.Count; i++)
        {
            var point1 = hull[i];
            var point2 = i == hull.Count - 1 ? hull[0] : hull[i + 1];
            lineSegments.Add(new LineSegment(point1, point2));
        }

        return ExtractPolygons(lineSegments);
    }

    private List<GeoLocation> GiftWrapConvexHull(List<GeoLocation> points)
    {
        if (points.Count < 3)
            return points;

        var hull = new List<GeoLocation>();
        var leftmost = points.MinBy(p => p.Longitude) ?? points[0];

        var current = leftmost;
        do
        {
            hull.Add(current);
            var next = points[0] == current ? points[1] : points[0];

            foreach (var point in points)
            {
                if (point.Equals(current))
                    continue;

                var cross = (next.Longitude - current.Longitude) * (point.Latitude - current.Latitude) -
                           (next.Latitude - current.Latitude) * (point.Longitude - current.Longitude);

                if (cross < 0)
                {
                    next = point;
                }
            }

            current = next;
        } while (!current.Equals(leftmost) && hull.Count < points.Count + 1);

        return hull;
    }

    private List<RouteShape> ExtractPolygons(List<LineSegment> lineSegments)
    {
        List<RouteShape> polygonShapes = new();
        List<LineSegment> visitedSegments = new();

        foreach (var segment in lineSegments)
        {
            if (visitedSegments.Contains(segment)) continue;

            List<GeoLocation> polygon = new() { segment.LocationA };
            var nextLocation = segment.LocationB;
            visitedSegments.Add(segment);

            while (true)
            {
                polygon.Add(nextLocation);
                var nextSegment = lineSegments.Find(seg => !visitedSegments.Contains(seg) &&
                                                           (seg.LocationA.Equals(nextLocation) || seg.LocationB.Equals(nextLocation)));
                if (nextSegment is null) break;

                visitedSegments.Add(nextSegment);
                nextLocation = nextSegment.LocationA.Equals(nextLocation) ? nextSegment.LocationB : nextSegment.LocationA;

                if (nextLocation.Equals(polygon[0]))
                {
                    polygon.Add(nextLocation);
                    break;
                }
            }

            if (polygon.Count > 2 && polygon[0].Equals(polygon[polygon.Count - 1]))
            {
                RouteShape mesh = new() { Locations = polygon };
                polygonShapes.Add(mesh);
            }
        }

        return polygonShapes;
    }

    private List<RouteShape> GetShape(List<RouteShape> triangulationMeshes, double alpha)
    {
        var simplexBoarderEdges = CalculateSimplexBoarderEdges(triangulationMeshes, alpha);
        var polygons = ExtractPolygons(simplexBoarderEdges);
        return polygons;
    }

    private double OptimizeAlpha(List<RouteShape> traingelMesh, List<GeoLocation> locations, int maxIterations = 100)
    {
        int counter = 0;
        double lowerBound = 0, upperBound = 500;

        while ((upperBound - lowerBound) > 0.0005 && counter < maxIterations)
        {
            double testAlpha = (upperBound + lowerBound) * 0.5;

            if (TestAlpha(traingelMesh, locations, testAlpha))
            {
                lowerBound = testAlpha;
            }
            else
            {
                upperBound = testAlpha;
            }

            counter++;
        }

        return lowerBound;
    }

    private bool TestAlpha(List<RouteShape> triangleMesh, List<GeoLocation> locations, double alpha)
    {
        var alphaShape = GetShape(triangleMesh, alpha);
        if (alphaShape == null || alphaShape.Count == 0)
        {
            return false;
        }

        var polygon = alphaShape[0].Locations;
        var polygonExcludedPoints = locations
            .Where(x => !polygon.Any(p => p.Equals(x)))
            .ToList();

        var allPointsInsideOfPolygon = polygonExcludedPoints.TrueForAll(point => 
            GeometryCalculator.IsLocationInPolygon(polygon, point));

        return allPointsInsideOfPolygon;
    }
}
