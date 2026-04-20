using DaoPlanImport.Entities;
using Microsoft.Extensions.Logging;

namespace DaoPlanImport.Services;

public interface IDataMapperService
{
    Liga? MapToLiga(Dictionary<string, object?> record, string? fileName);
}

public class DataMapperService : IDataMapperService
{
    private readonly ILogger<DataMapperService> _logger;

    public DataMapperService(ILogger<DataMapperService> logger)
    {
        _logger = logger;
    }

    public Liga? MapToLiga(Dictionary<string, object?> record, string? fileName)
    {
        try
        {
            // Log available keys for first record of each file to diagnose header issues
            if (record.Count > 0)
            {
                _logger.LogDebug("Available CSV keys: {Keys}", string.Join(", ", record.Keys));
            }

            return new Liga
            {
                FileName = fileName,
                ImportDate = DateTime.UtcNow,
                
                // Map all Liga CSV columns
                DATO = GetStringValue(record, "DATO"),
                DARTID = GetStringValue(record, "DARTID"),
                DISTNR = GetStringValue(record, "DISTNR"),
                DIOMNR = GetStringValue(record, "DIOMNR"),
                JOBSID = GetStringValue(record, "JOBSID"),
                JOBNR = GetStringValue(record, "JOBNR"),
                RAEKKEFOELGE = GetStringValue(record, "RAEKKEFOELGE"),
                LIGASORTNR = GetStringValue(record, "LIGASORTNR"),
                VEJBEMAERK = GetStringValue(record, "VEJBEMAERK"),
                ADRBEMAERK = GetStringValue(record, "ADRBEMAERK"),
                GADENAVN = GetStringValue(record, "GADENAVN"),
                HUSNR = GetStringValue(record, "HUSNR"),
                LITRA = GetStringValue(record, "LITRA"),
                ETAGE = GetStringValue(record, "ETAGE"),
                SIDELEJLIGHED = GetStringValue(record, "SIDELEJLIGHED"),
                ABONNR = GetStringValue(record, "ABONNR"),
                ABONNAVN = GetStringValue(record, "ABONNAVN"),
                CONAVN = GetStringValue(record, "CONAVN"),
                AFLOTEKST = GetStringValue(record, "AFLOTEKST"),
                ETAGELEVERING = GetStringValue(record, "ETAGELEVERING"),
                SUPPADRESSE = GetStringValue(record, "SUPPADRESSE"),
                PRODUKTNR = GetStringValue(record, "PRODUKTNR"),
                PRODUKTKORT = GetStringValue(record, "PRODUKTKORT"),
                PRODUKTANTAL = GetStringValue(record, "PRODUKTANTAL"),
                ADRESSERET = GetStringValue(record, "ADRESSERET"),
                NOEGLEBUNDTHUL = GetStringValue(record, "NOEGLEBUNDTHUL"),
                REKLAMATION = GetStringValue(record, "REKLAMATION"),
                TILGANG = GetStringValue(record, "TILGANG"),
                FORDNR = GetStringValue(record, "FORDNR"),
                POSTNR = GetStringValue(record, "POSTNR"),
                POSTDISTRIKT = GetStringValue(record, "POSTDISTRIKT"),
                STEDBETEGNELSE = GetStringValue(record, "STEDBETEGNELSE"),
                GADESORT = GetStringValue(record, "GADESORT"),
                HUSNRSORT = GetStringValue(record, "HUSNRSORT"),
                LABELSLEVERING = GetStringValue(record, "LABELSLEVERING"),
                STANGNR = GetStringValue(record, "STANGNR"),
                STANGSUFFIX = GetStringValue(record, "STANGSUFFIX"),
                JOBADRNR = GetStringValue(record, "JOBADRNR"),
                HUSN_ID = GetStringValue(record, "HUSN_ID"),
                SOURCE = GetStringValue(record, "SOURCE"),
                LONG = GetStringValue(record, "LONG"),
                LAT = GetStringValue(record, "LAT"),
                RECEIPT = GetStringValue(record, "RECEIPT"),
                LIGA_ID = GetStringValue(record, "LIGA_ID"),
                BARCODE = GetStringValue(record, "BARCODE"),
                PHOTO_URL = GetStringValue(record, "PHOTO_URL"),
                SORT_NO = GetStringValue(record, "SORT_NO"),
                JOSTNR = GetStringValue(record, "JOSTNR"),
                PRIORITET = GetStringValue(record, "PRIORITET"),
                DOERKODE = GetStringValue(record, "DOERKODE"),
                PAKKE_TYPE = GetStringValue(record, "PAKKE_TYPE"),
                LABELLESS = GetStringValue(record, "LABELLESS"),
                FULD_ID = GetStringValue(record, "FULD_ID"),
                HOMEBOX_ID = GetStringValue(record, "HOMEBOX_ID"),
                FOTO = GetStringValue(record, "FOTO"),
                LOCATION_TYPE = GetStringValue(record, "LOCATION_TYPE"),
                KONTO_NO = GetStringValue(record, "KONTO_NO"),
                HOEJDE = GetStringValue(record, "HOEJDE"),
                BREDDE = GetStringValue(record, "BREDDE"),
                LAENGDE = GetStringValue(record, "LAENGDE"),
                VAEGT = GetStringValue(record, "VAEGT")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping Liga record");
            return null;
        }
    }

    private string? GetStringValue(Dictionary<string, object?> record, string key)
    {
        // Try exact match first
        if (record.TryGetValue(key, out var value))
        {
            var result = value?.ToString();
            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogDebug("Found value for key '{Key}': '{Value}'", key, result);
            }
            return result;
        }

        // Try case-insensitive match
        var caseInsensitiveKey = record.Keys.FirstOrDefault(k => 
            k.Equals(key, StringComparison.OrdinalIgnoreCase));
        
        if (caseInsensitiveKey != null && record.TryGetValue(caseInsensitiveKey, out var caseInsensitiveValue))
        {
            var result = caseInsensitiveValue?.ToString();
            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogDebug("Found case-insensitive match for key '{Key}' (actual: '{ActualKey}'): '{Value}'", 
                    key, caseInsensitiveKey, result);
            }
            return result;
        }

        _logger.LogDebug("Key '{Key}' not found in record", key);
        return null;
    }
}
