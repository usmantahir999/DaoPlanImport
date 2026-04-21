using DaoPlanImport.Data;
using DaoPlanImport.Entities;
using DaoPlanImport.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DaoPlanImport.Services;

public interface IJobPolygonService
{
    Task GenerateJobPolygonsAsync();
}

/// <summary>
/// Service for generating job polygons from Liga data
/// Filters valid records, groups by job/district, and computes alpha shapes
/// </summary>
public class JobPolygonService : IJobPolygonService
{
    private readonly DaoPlanDbContext _context;
    private readonly ShapeManagementService _shapeManagement;
    private readonly ILogger<JobPolygonService> _logger;

    public JobPolygonService(
        DaoPlanDbContext context,
        ShapeManagementService shapeManagement,
        ILogger<JobPolygonService> logger)
    {
        _context = context;
        _shapeManagement = shapeManagement;
        _logger = logger;
    }

    public async Task GenerateJobPolygonsAsync()
    {
        try
        {
            _logger.LogInformation("Starting job polygon generation");

            // Get all valid Liga records
            var validRecords = await GetValidRecordsAsync();
            _logger.LogInformation("Found {Count} valid Liga records", validRecords.Count);

            // Group by DiomNr and JobNr
            var groupedByJob = validRecords
                .GroupBy(r => new { r.DIOMNR, r.JOBNR })
                .ToList();

            _logger.LogInformation("Found {Count} unique job/district combinations", groupedByJob.Count);

            var polygons = new List<JobPolygon>();
            var processedCount = 0;

            foreach (var jobGroup in groupedByJob)
            {
                try
                {
                    var polygon = await GeneratePolygonForJobAsync(
                        jobGroup.Key.DIOMNR,
                        jobGroup.Key.JOBNR,
                        jobGroup.ToList());

                    if (polygon != null)
                    {
                        polygons.Add(polygon);
                        processedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating polygon for job {JobNr}/{DiomNr}",
                        jobGroup.Key.JOBNR, jobGroup.Key.DIOMNR);
                }
            }

            // Save all polygons to database
            if (polygons.Count > 0)
            {
                await SavePolygonsAsync(polygons);
            }

            _logger.LogInformation("Job polygon generation completed. Generated {Count} polygons",
                processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job polygon generation");
            throw;
        }
    }

    /// <summary>
    /// Get all valid Liga records (filtering out records with invalid LAT/LONG, JobNr, or DiomNr)
    /// </summary>
    private async Task<List<Liga>> GetValidRecordsAsync()
    {
        return await _context.Ligas
            .Where(l =>
                // Must have valid DiomNr
                !string.IsNullOrWhiteSpace(l.DIOMNR) &&
                // Must have valid JobNr
                !string.IsNullOrWhiteSpace(l.JOBNR) &&
                // Must have valid LAT (not null, not "0", not empty)
                l.LAT != null && l.LAT != "" && l.LAT != "0" &&
                // Must have valid LONG (not null, not "0", not empty)
                l.LONG != null && l.LONG != "" && l.LONG != "0")
            .ToListAsync();
    }

    /// <summary>
    /// Generate a single polygon for a job/district combination
    /// </summary>
    private async Task<JobPolygon?> GeneratePolygonForJobAsync(
        string? diomNr,
        string? jobNr,
        List<Liga> records)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(diomNr) || string.IsNullOrWhiteSpace(jobNr))
                return null;

            if (records.Count < 3)
            {
                _logger.LogWarning("Insufficient records for polygon (need at least 3): {JobNr}/{DiomNr}",
                    jobNr, diomNr);
                return null;
            }

            // Extract unique locations
            var locations = records
                .Where(r => TryParseCoordinates(r.LAT, r.LONG, out var lat, out var lon))
                .Select(r =>
                {
                    TryParseCoordinates(r.LAT, r.LONG, out var lat, out var lon);
                    return new GeoLocation { Latitude = lat, Longitude = lon };
                })
                .Distinct()
                .ToList();

            if (locations.Count < 3)
            {
                _logger.LogWarning("Insufficient unique locations for polygon: {JobNr}/{DiomNr}",
                    jobNr, diomNr);
                return null;
            }

            // Compute alpha shape
            var shapes = _shapeManagement.GetAlphaShape(locations);

            if (shapes == null || shapes.Count == 0)
            {
                _logger.LogWarning("Failed to compute alpha shape for job: {JobNr}/{DiomNr}",
                    jobNr, diomNr);
                return null;
            }

            // Use the first (largest) polygon
            var mainShape = shapes.First();
            var wktPolygon = GeometryCalculator.LocationsToWktPolygon(mainShape.Locations);

            if (string.IsNullOrWhiteSpace(wktPolygon))
            {
                _logger.LogWarning("Failed to convert polygon to WKT format: {JobNr}/{DiomNr}",
                    jobNr, diomNr);
                return null;
            }

            var jobPolygon = new JobPolygon
            {
                DiomNr = diomNr,
                JobNr = jobNr,
                Polygon = wktPolygon,
                CreatedDate = DateTime.UtcNow,
                LocationCount = locations.Count
            };

            _logger.LogInformation("Generated polygon for job {JobNr}/{DiomNr} with {LocationCount} locations",
                jobNr, diomNr, locations.Count);

            return jobPolygon;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating polygon for job {JobNr}/{DiomNr}",
                jobNr, diomNr);
            return null;
        }
    }

    /// <summary>
    /// Try to parse latitude and longitude from string values
    /// </summary>
    private bool TryParseCoordinates(string? latStr, string? lonStr, out double lat, out double lon)
    {
        lat = 0;
        lon = 0;

        if (string.IsNullOrWhiteSpace(latStr) || string.IsNullOrWhiteSpace(lonStr))
            return false;

        if (!double.TryParse(latStr, out lat) || !double.TryParse(lonStr, out lon))
            return false;

        // Validate coordinate ranges
        // Latitude: -90 to 90
        // Longitude: -180 to 180
        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
            return false;

        // Reject zero coordinates
        if (Math.Abs(lat) < 0.0001 && Math.Abs(lon) < 0.0001)
            return false;

        return true;
    }

    /// <summary>
    /// Save all generated polygons to database
    /// </summary>
    private async Task SavePolygonsAsync(List<JobPolygon> polygons)
    {
        try
        {
            // Clear existing polygons for fresh generation
            var existingPolygons = await _context.JobPolygons.ToListAsync();
            _context.JobPolygons.RemoveRange(existingPolygons);

            // Add new polygons
            _context.JobPolygons.AddRange(polygons);
            var savedCount = await _context.SaveChangesAsync();

            _logger.LogInformation("Saved {Count} polygons to database", savedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving polygons to database");
            throw;
        }
    }
}
