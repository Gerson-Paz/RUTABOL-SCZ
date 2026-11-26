namespace RutaBol.Api.Models;

public class Micro
{
    public int Id { get; set; }
    public string Linea { get; set; } = "";
    public int? LineaId { get; set; }
    public string NumeroInterno { get; set; } = "";
    public int CapacidadMaxima { get; set; }
    public int PasajerosActuales { get; set; } = 0;
    public string Estado { get; set; } = "Disponible";
    public bool Activo { get; set; } = true;
    public DateTime? FechaRetiro { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
