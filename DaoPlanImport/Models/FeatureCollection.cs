namespace DaoPlanImport.Models
{
    public class FeatureCollection
    {
        public string Type { get; set; }
        public List<Feature> Features { get; set; }
    }

    public class Feature
    {
        public string Type { get; set; }
        public Properties Properties { get; set; }
        public Geometry Geometry { get; set; }
    }

    public class Properties
    {
        public string? DiomNr { get; set; }   // <- add this
        public string JobNr { get; set; }
    }

    public class Geometry
    {
        public string Type { get; set; }
        public object Coordinates { get; set; }
    }
}
