namespace RutaBol.Api.Models;

// Una ubicación por micro; el GPS se actualiza sin borrar los micros anteriores.
public class UbicacionMicro
{
    public int MicroId { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double? PrecisionMetros { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public int? ConductorUsuarioId { get; set; }
}
