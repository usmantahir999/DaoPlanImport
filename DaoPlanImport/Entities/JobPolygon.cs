namespace DaoPlanImport.Entities;


/// <summary>
/// JobPolygon entity - Stores computed alpha shapes for job areas
/// Represents the geographical polygon for each job/district combination
/// </summary>
public class JobPolygon
{
    public int Id { get; set; }
    
    // Identifiers
    public string? DiomNr { get; set; }
    public string? JobNr { get; set; }
    
    // Polygon data - stored as WKT (Well-Known Text) format or JSON
    // Example WKT: "POLYGON((lat1 lon1, lat2 lon2, lat3 lon3, lat1 lon1))"
    public string? Polygon { get; set; }
    
    // Metadata
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int LocationCount { get; set; } // Number of unique locations used to create polygon
}
