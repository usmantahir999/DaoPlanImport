using DaoPlanImport.Data;
using DaoPlanImport.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Algorithm.Hull;
using NetTopologySuite.IO;
using System.Globalization;

public interface IJobPolygonService
{
    Task GenerateJobPolygonsAsync();
}
public class JobPolygonService : IJobPolygonService
{
    private static readonly CultureInfo DanishCulture =
    CultureInfo.GetCultureInfo("da-DK");
    private readonly DaoPlanDbContext _context;
    private readonly ILogger<JobPolygonService> _logger;

    public JobPolygonService(
        DaoPlanDbContext context,
        ILogger<JobPolygonService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task GenerateJobPolygonsAsync()
    {
        try
        {
            _logger.LogInformation("Starting job polygon generation");

            // STEP 1: Get distinct (DiomNr, JobNr)
            var jobKeys = await _context.Ligas
                .AsNoTracking()
                .Where(l => !string.IsNullOrWhiteSpace(l.DIOMNR) && !string.IsNullOrWhiteSpace(l.JOBNR)
                &&l.LAT != null 
                && l.LAT != "" 
                && l.LAT != "0" 
                && l.LONG != null 
                && l.LONG != "" 
                && l.LONG != "0"
                )
                .Select(l => new { l.DIOMNR, l.JOBNR })
                .Distinct()
                .ToListAsync();

            _logger.LogInformation("Found {Count} job groups", jobKeys.Count);

            const int batchSize = 50;

            for (int i = 0; i < jobKeys.Count; i += batchSize)
            {
                var batch = jobKeys.Skip(i).Take(batchSize).ToList();
                var polygons = new List<JobPolygon>();

                foreach (var key in batch)
                {
                    try
                    {
                        var records = await _context.Ligas
                            .AsNoTracking()
                            .Where(l =>
                !string.IsNullOrWhiteSpace(l.DIOMNR) &&
                !string.IsNullOrWhiteSpace(l.JOBNR) && l.DIOMNR == key.DIOMNR &&
                                l.JOBNR == key.JOBNR &&
                !string.IsNullOrWhiteSpace(l.LAT) && l.LAT != "0"&&
                !string.IsNullOrWhiteSpace(l.LONG) && l.LONG != "0")
                            .Select(l=>l)
                            .ToListAsync();

                        var polygon = GeneratePolygonForJob(
                            key.DIOMNR,
                            key.JOBNR,
                            records);

                        if (polygon != null)
                            polygons.Add(polygon);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error processing job {JobNr}/{DiomNr}",
                            key.JOBNR,
                            key.DIOMNR);
                    }
                }

                if (polygons.Count > 0)
                {
                    await UpsertPolygonsAsync(polygons);
                }

                _logger.LogInformation(
                    "Processed batch {Start}-{End}",
                    i,
                    i + batch.Count);
            }

            _logger.LogInformation("Polygon generation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during polygon generation");
            throw;
        }
    }

    private bool TryParseCoordinates(
    string? latStr,
    string? lonStr,
    out double lat,
    out double lon)
    {
        lat = 0;
        lon = 0;

        if (string.IsNullOrWhiteSpace(latStr) ||
            string.IsNullOrWhiteSpace(lonStr))
            return false;

        if (!double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
            return false;

        if (!double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
            return false;

        if (lat < -90 || lat > 90 ||
            lon < -180 || lon > 180)
            return false;

        return true;
    }

    private JobPolygon? GeneratePolygonForJob(
        string? diomNr,
        string? jobNr,
        List<Liga> records)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(diomNr) ||
                string.IsNullOrWhiteSpace(jobNr))
                return null;

            if (records.Count < 3)
            {
                _logger.LogWarning(
                    "Skipping polygon for JobNr={JobNr}, DiomNr={DiomNr} because location count is {Count}",
                    jobNr,
                    diomNr,
                    records.Count);

                return null;
            }

            var locations = new List<(double Lat, double Lon)>();

            foreach (var r in records)
            {
                if (!TryParseCoordinates(r.LAT, r.LONG, out var lat, out var lon))
                    continue;

                locations.Add((lat, lon));
            }

            // remove duplicates
            locations = locations
                .Distinct()
                .ToList();

            if (locations.Count < 3)
                return null;

            var geometryFactory =
                NetTopologySuite.NtsGeometryServices.Instance
                    .CreateGeometryFactory(srid: 4326);

            var coordinates = locations
                .Select(x =>
                    new NetTopologySuite.Geometries.Coordinate(
                        x.Lon, // X
                        x.Lat)) // Y
                .ToArray();

            var multiPoint =
                geometryFactory.CreateMultiPointFromCoords(coordinates);

            var concaveHull = new ConcaveHull(multiPoint)
            {
                MaximumEdgeLength = 0.0012
            };

            var hullGeometry = concaveHull.GetHull();

            if (hullGeometry == null || hullGeometry.IsEmpty)
                return null;

            var writer = new GeoJsonWriter();
            var geoJsonGeometry = writer.Write(hullGeometry);

            var polygonGeoJson =
            $@"{{
              ""type"": ""FeatureCollection"",
              ""features"": [
                {{
                  ""type"": ""Feature"",
                  ""properties"": {{
                    ""jobNr"": ""{jobNr}""
                  }},
                  ""geometry"": {geoJsonGeometry}
                }}
              ]
            }}";

            return new JobPolygon
            {
                DiomNr = diomNr,
                JobNr = jobNr,
                Polygon = polygonGeoJson,
                CreatedDate = DateTime.UtcNow,
                LocationCount = locations.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating polygon for {JobNr}/{DiomNr}",
                jobNr,
                diomNr);

            return null;
        }
    }

    private async Task UpsertPolygonsAsync(List<JobPolygon> polygons)
    {
        try
        {
            foreach (var polygon in polygons)
            {
                var existing = await _context.JobPolygons
                    .FirstOrDefaultAsync(x =>
                        x.DiomNr == polygon.DiomNr &&
                        x.JobNr == polygon.JobNr);

                if (existing == null)
                {
                    _context.JobPolygons.Add(polygon);
                }
                else
                {
                    existing.Polygon = polygon.Polygon;
                    existing.LocationCount = polygon.LocationCount;
                    existing.CreatedDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving polygons");
            throw;
        }
    }
}