namespace RutaBol.Api.Models;

public class SolicitudCategoria
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Categoria { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? CorreoInstitucional { get; set; }
    public string NombreArchivo { get; set; } = "";
    public string TipoArchivo { get; set; } = "";
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public DateTime? FechaRevision { get; set; }
    public int? AdministradorId { get; set; }
}
