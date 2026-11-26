namespace RutaBol.Api.Models;

public class Pasaje
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int MicroId { get; set; }
    public int TarifaId { get; set; }
    public string CategoriaAplicada { get; set; } = "";
    public decimal Monto { get; set; }
    public string CodigoComprobante { get; set; } = Guid.NewGuid().ToString("N");
    public string Estado { get; set; } = "Simulado";
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
