namespace DaoPlanImport.Entities;

/// <summary>
/// Liga entity - Single table to store all CSV data
/// All columns from Liga CSV files are stored here
/// </summary>
public class Liga
{
    public int Id { get; set; }
    
    // Metadata
    public string? FileName { get; set; } // For segregation (e.g., "E_MATR_12032026_Liga.csv")
    public DateTime ImportDate { get; set; }

    // Liga CSV Columns
    public string? DATO { get; set; }
    public string? DARTID { get; set; }
    public string? DISTNR { get; set; }
    public string? DIOMNR { get; set; }
    public string? JOBSID { get; set; }
    public string? JOBNR { get; set; }
    public string? RAEKKEFOELGE { get; set; }
    public string? LIGASORTNR { get; set; }
    public string? VEJBEMAERK { get; set; }
    public string? ADRBEMAERK { get; set; }
    public string? GADENAVN { get; set; }
    public string? HUSNR { get; set; }
    public string? LITRA { get; set; }
    public string? ETAGE { get; set; }
    public string? SIDELEJLIGHED { get; set; }
    public string? ABONNR { get; set; }
    public string? ABONNAVN { get; set; }
    public string? CONAVN { get; set; }
    public string? AFLOTEKST { get; set; }
    public string? ETAGELEVERING { get; set; }
    public string? SUPPADRESSE { get; set; }
    public string? PRODUKTNR { get; set; }
    public string? PRODUKTKORT { get; set; }
    public string? PRODUKTANTAL { get; set; }
    public string? ADRESSERET { get; set; }
    public string? NOEGLEBUNDTHUL { get; set; }
    public string? REKLAMATION { get; set; }
    public string? TILGANG { get; set; }
    public string? FORDNR { get; set; }
    public string? POSTNR { get; set; }
    public string? POSTDISTRIKT { get; set; }
    public string? STEDBETEGNELSE { get; set; }
    public string? GADESORT { get; set; }
    public string? HUSNRSORT { get; set; }
    public string? LABELSLEVERING { get; set; }
    public string? STANGNR { get; set; }
    public string? STANGSUFFIX { get; set; }
    public string? JOBADRNR { get; set; }
    public string? HUSN_ID { get; set; }
    public string? SOURCE { get; set; }
    public string? LONG { get; set; }
    public string? LAT { get; set; }
    public string? RECEIPT { get; set; }
    public string? LIGA_ID { get; set; }
    public string? BARCODE { get; set; }
    public string? PHOTO_URL { get; set; }
    public string? SORT_NO { get; set; }
    public string? JOSTNR { get; set; }
    public string? PRIORITET { get; set; }
    public string? DOERKODE { get; set; }
    public string? PAKKE_TYPE { get; set; }
    public string? LABELLESS { get; set; }
    public string? FULD_ID { get; set; }
    public string? HOMEBOX_ID { get; set; }
    public string? FOTO { get; set; }
    public string? LOCATION_TYPE { get; set; }
    public string? KONTO_NO { get; set; }
    public string? HOEJDE { get; set; }
    public string? BREDDE { get; set; }
    public string? LAENGDE { get; set; }
    public string? VAEGT { get; set; }
}
