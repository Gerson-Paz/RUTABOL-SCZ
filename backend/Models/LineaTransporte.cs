namespace RutaBol.Api.Models;

public class LineaTransporte
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public bool Activa { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
