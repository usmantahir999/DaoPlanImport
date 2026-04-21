using DaoPlanImport.Data;
using DaoPlanImport.Entities;
using DaoPlanImport.Models;
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

            var validRecords = await GetValidRecordsAsync();

            var groupedByJob = validRecords
                .GroupBy(r => new { r.DIOMNR, r.JOBNR })
                // remove this filter when done debugging
                .Where(x => x.Key.JOBNR == "B83701")
                .ToList();

            var polygons = new List<JobPolygon>();

            foreach (var jobGroup in groupedByJob)
            {
                try
                {
                    var polygon = await GeneratePolygonForJobAsync(
                        jobGroup.Key.DIOMNR,
                        jobGroup.Key.JOBNR,
                        jobGroup.ToList());

                    if (polygon != null)
                        polygons.Add(polygon);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error generating polygon for {JobNr}/{DiomNr}",
                        jobGroup.Key.JOBNR,
                        jobGroup.Key.DIOMNR);
                }
            }

            if (polygons.Count > 0)
                await SavePolygonsAsync(polygons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error during job polygon generation");
            throw;
        }
    }

    private async Task<List<Liga>> GetValidRecordsAsync()
    {
        return await _context.Ligas.Where(x => x.JOBNR == "B83701")
            .Where(l =>
                !string.IsNullOrWhiteSpace(l.DIOMNR) &&
                !string.IsNullOrWhiteSpace(l.JOBNR) &&
                l.LAT != null &&
                l.LAT != "" &&
                l.LAT != "0" &&
                l.LONG != null &&
                l.LONG != "" &&
                l.LONG != "0")
            .ToListAsync();
    }

    private async Task<JobPolygon?> GeneratePolygonForJobAsync(
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
                return null;

            // Parse + distinct coordinates
            var locations = records
                .Where(r =>
                    TryParseCoordinates(
                        r.LAT,
                        r.LONG,
                        out _,
                        out _))
                .Select(r =>
                {
                    TryParseCoordinates(
                        r.LAT,
                        r.LONG,
                        out var lat,
                        out var lon);

                    return new
                    {
                        Lat = Math.Round(lat, 8),
                        Lon = Math.Round(lon, 8)
                    };
                })
                .Distinct()
                .Select(x => new GeoLocation
                {
                    Latitude = x.Lat,
                    Longitude = x.Lon
                })
                .ToList();

            if (locations.Count < 3)
                return null;

            _logger.LogInformation(
                "Using {Count} unique points for {JobNr}",
                locations.Count,
                jobNr);

            var geometryFactory =
                NetTopologySuite.NtsGeometryServices.Instance
                    .CreateGeometryFactory(
                        srid: 4326);

            var coordinates = locations
                .Select(x =>
                    new NetTopologySuite.Geometries.Coordinate(
                        x.Longitude, // X
                        x.Latitude)) // Y
                .ToArray();

            var multiPoint =
                geometryFactory
                    .CreateMultiPointFromCoords(
                        coordinates);

            // Concave Hull
            var concaveHull =
                new ConcaveHull(
                    multiPoint);

            /*
             Tune this:
             smaller = tighter boundary
             bigger  = looser boundary
            */
            concaveHull.MaximumEdgeLength = 0.0012;

            var hullGeometry =
                concaveHull.GetHull();

            if (hullGeometry == null ||
                hullGeometry.IsEmpty)
            {
                return null;
            }

            //var polygonWkt =
            //    hullGeometry.AsText();
            var writer = new GeoJsonWriter();

            var geoJsonGeometry =
                writer.Write(hullGeometry);

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
            _logger.LogError(
                ex,
                "Error generating polygon for {JobNr}/{DiomNr}",
                jobNr,
                diomNr);

            return null;
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

        latStr = latStr.Trim();
        lonStr = lonStr.Trim();

        if (!double.TryParse(
                latStr,
                NumberStyles.Float,
                DanishCulture,
                out lat))
        {
            if (!double.TryParse(
                    latStr.Replace(',', '.'),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out lat))
                return false;
        }

        if (!double.TryParse(
                lonStr,
                NumberStyles.Float,
                DanishCulture,
                out lon))
        {
            if (!double.TryParse(
                    lonStr.Replace(',', '.'),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out lon))
                return false;
        }

        if (lat < -90 || lat > 90 ||
            lon < -180 || lon > 180)
            return false;

        if (Math.Abs(lat) < 0.0001 &&
            Math.Abs(lon) < 0.0001)
            return false;

        return true;
    }

    private async Task SavePolygonsAsync(
        List<JobPolygon> polygons)
    {
        try
        {
            var existing =
                await _context.JobPolygons.ToListAsync();

            _context.JobPolygons.RemoveRange(existing);

            _context.JobPolygons.AddRange(polygons);

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error saving polygons");
            throw;
        }
    }
}