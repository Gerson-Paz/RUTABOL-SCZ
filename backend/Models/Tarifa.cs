
using System.ComponentModel.DataAnnotations;


namespace RutaBol.Api.Models;

public class Tarifa
{
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public decimal Monto { get; set; }

    public string Moneda { get; set; } = "BOB";

    public bool Vigente { get; set; } = true;

    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;

    public DateTime? FechaFin { get; set; }

    public int? UsuarioAdministradorId { get; set; }
}
